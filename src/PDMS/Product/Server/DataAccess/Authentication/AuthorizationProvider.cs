namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.ActiveDirectory;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Encapsulates authorization logic.
    /// </summary>
    public class AuthorizationProvider : IAuthorizationProvider
    {
        private readonly ICachedActiveDirectory activeDirectory;
        private readonly IEnumerable<Guid> serviceAdminSecurityGroups;
        private readonly IEnumerable<Guid> variantEditorSecurityGroups;
        private readonly IEnumerable<Guid> incidentManagerSecurityGroups;
        private readonly string variantEditorApplicationId;
        private readonly AuthenticatedPrincipal authenticatedPrincipal;
        private readonly IEventWriterFactory eventWriterFactory;

        private readonly string componentName = nameof(AuthorizationProvider);

        private IEnumerable<Guid> usersSecurityGroups;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationProvider"/> class.
        /// </summary>
        /// <param name="activeDirectory">The active directory instance.</param>
        /// <param name="coreConfiguration">The configuration instance.</param>
        /// <param name="eventWriterFactory">The event writer factory instance.</param>
        /// <param name="authenticatedPrincipal">The current authenticated principal for this session.</param>
        public AuthorizationProvider(
            ICachedActiveDirectory activeDirectory,
            ICoreConfiguration coreConfiguration,
            IEventWriterFactory eventWriterFactory,
            AuthenticatedPrincipal authenticatedPrincipal)
        {
            this.activeDirectory = activeDirectory;
            this.eventWriterFactory = eventWriterFactory;

            this.serviceAdminSecurityGroups = this.ParseSecurityGroupIds(coreConfiguration.ServiceAdminSecurityGroups);
            this.variantEditorSecurityGroups = this.ParseSecurityGroupIds(coreConfiguration.VariantEditorSecurityGroups);
            this.incidentManagerSecurityGroups = this.ParseSecurityGroupIds(coreConfiguration.IncidentManagerSecurityGroups);
            this.variantEditorApplicationId = coreConfiguration.VariantEditorApplicationId;

            this.authenticatedPrincipal = authenticatedPrincipal;
        }

        /// <summary>
        /// Determines if the current authenticated user contains the set of required roles.
        /// </summary>
        /// <param name="requiredRoles">The set of roles.</param>
        /// <param name="getDataOwnersAsync">A function to load data owners if needed.</param>
        /// <returns>Throws an exception if not authorized.</returns>
        public async Task AuthorizeAsync(
            AuthorizationRole requiredRoles,
            Func<Task<IEnumerable<DataOwner>>> getDataOwnersAsync = null)
        {
            var exn = await this.AuthorizeAsyncInternal(requiredRoles, getDataOwnersAsync).ConfigureAwait(false);

            if (exn != null)
            {
                throw exn;
            }
        }

        /// <summary>
        /// Determines if the current authenticated user contains the set of required roles.
        /// </summary>
        /// <param name="requiredRoles">The set of roles.</param>
        /// <param name="getDataOwnersAsync">A function to load data owners if needed.</param>
        /// <returns>False if not authorized.</returns>
        public async Task<bool> TryAuthorizeAsync(AuthorizationRole requiredRoles, Func<Task<IEnumerable<DataOwner>>> getDataOwnersAsync = null)
        {
            var exn = await this.AuthorizeAsyncInternal(requiredRoles, getDataOwnersAsync).ConfigureAwait(false);

            return exn == null;
        }

        private async Task<Exception> AuthorizeAsyncInternal(
            AuthorizationRole requiredRoles,
            Func<Task<IEnumerable<DataOwner>>> getDataOwnersAsync = null)
        {
            if (requiredRoles == AuthorizationRole.None)
            {
                return new MissingWritePermissionException("Unknown", AuthorizationRole.None.ToString(), "This action has no authorization role.");
            }
            else
            {
                // We must have a user context in order to load security groups.
                // If we don't have a user context, then check to see if the API is allowed to be accessed by services.
                if (string.IsNullOrEmpty(this.authenticatedPrincipal.UserId))
                {
                    if (requiredRoles.HasFlag(AuthorizationRole.ApplicationAccess))
                    {
                        return null;
                    }
                    else if (this.variantEditorApplicationId.Equals(this.authenticatedPrincipal.ApplicationId) && requiredRoles.HasFlag(AuthorizationRole.VariantEditor))
                    {
                        this.eventWriterFactory.Trace(componentName, $"AuthorizeAsyncInternal: ApplicationId: [{this.authenticatedPrincipal.ApplicationId}].");
                        return null;
                    } 
                    else
                    {
                        return new MissingWritePermissionException("Unknown", AuthorizationRole.ApplicationAccess.ToString(), "A user identity is required for this action.");
                    }
                }
                else
                {
                    requiredRoles = this.RemoveFlag(AuthorizationRole.ApplicationAccess, requiredRoles);
                }

                // Honor the NoCachedSecurityGroups flag, and then remove it from the required roles.
                // It isn't a role that will ever be set on the authorization context, it is just a flag to alter behavior here.
                var forceCacheRefresh = requiredRoles.HasFlag(AuthorizationRole.NoCachedSecurityGroups);
                requiredRoles = this.RemoveFlag(AuthorizationRole.NoCachedSecurityGroups, requiredRoles);

                var authorizationContext = await this.GetAuthorizationContextAsync(forceCacheRefresh).ConfigureAwait(false);

                // If the variant editor role is required and the user has the variant editor role,
                // then we do not need to check the service roles
                if (requiredRoles.HasFlag(AuthorizationRole.VariantEditor) && authorizationContext.Roles.HasFlag(AuthorizationRole.VariantEditor))
                {
                    return null;
                }

                IEnumerable<Tuple<AuthorizationContext, DataOwner>> checks = new[] { Tuple.Create(authorizationContext, (DataOwner)null) };

                // The service editor role is based on the security groups on the data owner.
                // As such, we must retrieve the data owners if this role is required and update the user's auth context.
                // This accepts multiple data owners, because an update requires that you be in
                // the write security groups of both the incoming and existing entities.
                if (requiredRoles.HasFlag(AuthorizationRole.ServiceEditor))
                {
                    // If we get here, we've already determined that the caller is not a VariantEditor;
                    // remove it from the requireRoles so that we get an appropriate error message.
                    requiredRoles = this.RemoveFlag(AuthorizationRole.VariantEditor, requiredRoles);

                    // If the user is in the ServiceAdmin role, then they do not need to be in the ServiceEditor role,
                    // and we can skip loading up the data owners.
                    if (authorizationContext.Roles.HasFlag(AuthorizationRole.ServiceAdmin))
                    {
                        requiredRoles = this.RemoveFlag(AuthorizationRole.ServiceEditor, requiredRoles);
                        requiredRoles = this.RemoveFlag(AuthorizationRole.ServiceTreeAdmin, requiredRoles);
                    }
                    else if (getDataOwnersAsync != null)
                    {
                        var dataOwners = await getDataOwnersAsync.Invoke().ConfigureAwait(false);

                        // We by-pass authorization in scenarios where we want property validation failures to trigger.
                        // We signal this in the entity writers by returning null for data owners.
                        if (dataOwners == null)
                        {
                            requiredRoles = this.RemoveFlag(AuthorizationRole.ServiceEditor, requiredRoles);
                            requiredRoles = this.RemoveFlag(AuthorizationRole.ServiceTreeAdmin, requiredRoles);
                        }
                        else if (dataOwners.Any())
                        {
                            checks = dataOwners?.Select(x => Tuple.Create(this.ApplyDataOwnerAuthorization(authorizationContext, x), x));
                        }
                    }
                }

                foreach (var check in checks)
                {
                    foreach (AuthorizationRole role in Enum.GetValues(typeof(AuthorizationRole)))
                    {
                        // If it is a required role, but the user doesn't have that role,
                        // then we should fail, but we need to fail with the correct error.
                        if ((role & requiredRoles) == role && (role & check.Item1.Roles) != role)
                        {
                            switch (role)
                            {
                                case AuthorizationRole.ServiceTreeAdmin:
                                    return new ServiceTreeMissingWritePermissionException(this.authenticatedPrincipal.UserAlias, check.Item2.ServiceTree?.ServiceId, role.ToString());
                                case AuthorizationRole.ServiceEditor:
                                    var securityGroups = string.Join(";", check.Item2?.WriteSecurityGroups?.Select(x => string.Join(";", x)) ?? Enumerable.Empty<string>());
                                    return new SecurityGroupMissingWritePermissionException(this.authenticatedPrincipal.UserAlias, securityGroups, role.ToString());
                                default:
                                    return new MissingWritePermissionException(this.authenticatedPrincipal.UserId, role.ToString());
                            }
                        }
                    }
                }
            }

            return null;
        }

        private AuthorizationRole RemoveFlag(AuthorizationRole flag, AuthorizationRole roles)
        {
            var hasFlag = roles.HasFlag(flag);

            if (hasFlag)
            {
                roles ^= flag;
            }

            return roles;
        }

        private IEnumerable<Guid> ParseSecurityGroupIds(IList<string> configurationData)
        {
            return configurationData.Select(Guid.Parse);
        }

        /// <summary>
        /// Retrieve the authorization context for the current user.
        /// </summary>
        /// <param name="forceCacheRefresh">Whether or not the cache should be forcibly refreshed.</param>
        /// <returns>The authorization context.</returns>
        private async Task<AuthorizationContext> GetAuthorizationContextAsync(bool forceCacheRefresh)
        {
            if (!string.IsNullOrEmpty(this.authenticatedPrincipal.UserId))
            {
                this.eventWriterFactory.Trace(
                    componentName,
                    $"GetAuthorizationContextAsync: UserId: [{this.authenticatedPrincipal.UserId}], Operation: [{this.authenticatedPrincipal.OperationName}].");
            }

            // Cache the values so that repeated calls do not hit storage multiple times.
            if (this.usersSecurityGroups == null ||
                (forceCacheRefresh && !this.activeDirectory.ForceRefreshCache))
            {
                this.activeDirectory.ForceRefreshCache = forceCacheRefresh;

                this.usersSecurityGroups = await this.activeDirectory.GetSecurityGroupIdsAsync(this.authenticatedPrincipal).ConfigureAwait(false);
            }

            var authorizationContext = new AuthorizationContext
            {
                SecurityGroupIds = this.usersSecurityGroups ?? Enumerable.Empty<Guid>(),
                Roles = AuthorizationRole.None
            };

            // Determine a user's static roles.
            foreach (var id in authorizationContext.SecurityGroupIds)
            {
                if (this.serviceAdminSecurityGroups.Any(x => x.Equals(id)))
                {
                    this.eventWriterFactory.Trace(componentName, "GetAuthorizationContextAsync: Adding ServiceAdmin role to authorizationContext.");
                    authorizationContext.Roles |= AuthorizationRole.ServiceAdmin;
                }

                if (this.variantEditorSecurityGroups.Any(x => x.Equals(id)))
                {
                    this.eventWriterFactory.Trace(componentName, "GetAuthorizationContextAsync: Adding VariantEditor role to authorizationContext.");
                    authorizationContext.Roles |= AuthorizationRole.VariantEditor;
                }

                if (this.incidentManagerSecurityGroups.Any(x => x.Equals(id)))
                {
                    this.eventWriterFactory.Trace(componentName, "GetAuthorizationContextAsync: Adding IncidentManager role to authorizationContext.");
                    authorizationContext.Roles |= AuthorizationRole.IncidentManager;
                }
            }

            return authorizationContext;
        }

        /// <summary>
        /// Given an existing authorization context and a data owner,
        /// produces a new authorization context with updated role information.
        /// The original authorization context is not modified.
        /// </summary>
        /// <param name="authorizationContext">The original authorization context.</param>
        /// <param name="dataOwner">The data owner information.</param>
        /// <returns>A new authorization context with updated role information.</returns>
        private AuthorizationContext ApplyDataOwnerAuthorization(AuthorizationContext authorizationContext, DataOwner dataOwner)
        {
            // Determine if the user is in any of the write security groups of the data owner.
            var securityGroupCollection = dataOwner?.WriteSecurityGroups?.Distinct();

            var intersection = securityGroupCollection?.Intersect(authorizationContext.SecurityGroupIds);

            var updatedContext = new AuthorizationContext
            {
                SecurityGroupIds = authorizationContext.SecurityGroupIds,
                Roles = authorizationContext.Roles
            };

            // Determine if the user is an admin in service tree for this data owner.
            var serviceTreeAdmins = dataOwner?.ServiceTree?.ServiceAdmins?.Distinct();

            // If there are no write security groups set, then we bypass the authorization.
            // This way the validation logic in the specific entities can trigger more specific errors.
            if (intersection?.Any() == true || intersection == null
                || (serviceTreeAdmins != null && serviceTreeAdmins.Any(x => string.Equals(x, this.authenticatedPrincipal.UserAlias, StringComparison.OrdinalIgnoreCase))))
            {
                updatedContext.Roles |= AuthorizationRole.ServiceEditor;
            }

            // If there are no service admins set, then we bypass the authorization.
            // This way the validation logic in the specific entities can trigger more specific errors.
            if (serviceTreeAdmins?.Any(x => string.Equals(x, this.authenticatedPrincipal.UserAlias, StringComparison.OrdinalIgnoreCase)) == true ||
                serviceTreeAdmins == null)
            {
                updatedContext.Roles |= AuthorizationRole.ServiceTreeAdmin;
            }

            return updatedContext;
        }
    }
}
