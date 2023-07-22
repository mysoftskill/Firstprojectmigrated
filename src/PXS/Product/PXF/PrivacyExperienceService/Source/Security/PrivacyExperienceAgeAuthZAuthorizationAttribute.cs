// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System;
    using System.Web.Http.Controllers;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;

    internal enum PrivacyAction
    {
        Default,

        View,

        Delete
    }

    /// <summary>
    ///     PrivacyExperience AuthorizationAttribute
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    internal sealed class PrivacyExperienceAgeAuthZAuthorizationAttribute : PrivacyExperienceAuthorizationAttributeBase
    {
        private readonly IAgeAuthZRules ageAuthZRules;

        private readonly PrivacyAction privacyAction;

        /// <summary>
        ///     Calls when a process requests authorization.
        /// </summary>
        /// <param name="actionContext">The action context, which encapsulates information for using <see cref="T:System.Web.Http.Filters.AuthorizationFilterAttribute" />.</param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var msaIdentity = ReadIdentity<MsaSelfIdentity>(actionContext);

            Error error;
            if (string.IsNullOrWhiteSpace(msaIdentity.UserProxyTicket))
            {
                error = new Error(ErrorCode.MissingClientCredentials, "User proxy ticket must be provided for this API through S2S header.");
                actionContext.Response = actionContext.Request.CreateErrorResponse(error);
                return;
            }

            bool shouldEnforce = GetPrivacyExperienceServiceConfiguration(actionContext)?.OnBehalfOfConfiguration?.EnforceAgeAuthZRules ?? false;
            if (!shouldEnforce)
            {
                return;
            }

            string errorMessageUnauthorizedMajorityAge =
                $"User is unauthorized by majority age rules. Auth-type: {msaIdentity.AuthType}. IsChildInFamily: {msaIdentity.IsChildInFamily}";
            string errorMessageUnauthorizedAuthType = $"User is unauthorized with Auth-type: {msaIdentity.AuthType}";
            switch (this.privacyAction)
            {
                case PrivacyAction.Default:
                    return;

                case PrivacyAction.View:
                    switch (msaIdentity.AuthType)
                    {
                        case AuthType.MsaSelf:
                        case AuthType.OnBehalfOf:
                            if (!this.ageAuthZRules.CanView(msaIdentity))
                            {
                                error = new Error(ErrorCode.UnauthorizedMajorityAge, errorMessageUnauthorizedMajorityAge);
                                actionContext.Response = actionContext.Request.CreateErrorResponse(error);
                            }

                            return;

                        case AuthType.None:
                        default:
                            error = new Error(ErrorCode.InvalidClientCredentials, errorMessageUnauthorizedAuthType);
                            actionContext.Response = actionContext.Request.CreateErrorResponse(error);
                            return;
                    }

                case PrivacyAction.Delete:
                    switch (msaIdentity.AuthType)
                    {
                        case AuthType.MsaSelf:
                        case AuthType.OnBehalfOf:
                            if (!this.ageAuthZRules.CanDelete(msaIdentity))
                            {
                                error = new Error(ErrorCode.UnauthorizedMajorityAge, errorMessageUnauthorizedMajorityAge);
                                actionContext.Response = actionContext.Request.CreateErrorResponse(error);
                            }

                            return;

                        case AuthType.None:
                        default:
                            error = new Error(ErrorCode.InvalidClientCredentials, errorMessageUnauthorizedAuthType);
                            actionContext.Response = actionContext.Request.CreateErrorResponse(error);
                            return;
                    }

                default:
                    throw new ArgumentOutOfRangeException($"Unknown action specified: {this.privacyAction}");
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyExperienceAgeAuthZAuthorizationAttribute" /> class.
        /// </summary>
        /// <param name="ageRulesType">The authorization rules type.</param>
        /// <param name="privacyAction">The privacy action.</param>
        internal PrivacyExperienceAgeAuthZAuthorizationAttribute(Type ageRulesType, PrivacyAction privacyAction)
        {
            if (Activator.CreateInstance(ageRulesType) is IAgeAuthZRules ageRules)
            {
                this.ageAuthZRules = ageRules;
            }
            else
            {
                throw new ArgumentException($"Argument has to inherit from {nameof(IAgeAuthZRules)}", nameof(ageRulesType));
            }

            this.privacyAction = privacyAction;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrivacyExperienceAgeAuthZAuthorizationAttribute" /> class.
        /// </summary>
        /// <param name="ageRules">The authorization rules.</param>
        /// <param name="privacyAction">The privacy action.</param>
        internal PrivacyExperienceAgeAuthZAuthorizationAttribute(IAgeAuthZRules ageRules, PrivacyAction privacyAction)
        {
            this.ageAuthZRules = ageRules;
            this.privacyAction = privacyAction;
        }
    }
}
