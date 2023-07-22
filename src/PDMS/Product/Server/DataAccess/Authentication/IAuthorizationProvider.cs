namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authorization
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Encapsulates authorization logic.
    /// </summary>
    public interface IAuthorizationProvider
    {
        /// <summary>
        /// Determines if the current authenticated user contains the set of required roles.
        /// </summary>
        /// <param name="requiredRoles">The set of roles.</param>
        /// <param name="getDataOwnersAsync">A function to load data owners if needed.</param>
        /// <returns>Throws an exception if not authorized.</returns>
        Task AuthorizeAsync(AuthorizationRole requiredRoles, Func<Task<IEnumerable<DataOwner>>> getDataOwnersAsync = null);

        /// <summary>
        /// Determines if the current authenticated user contains the set of required roles.
        /// </summary>
        /// <param name="requiredRoles">The set of roles.</param>
        /// <param name="getDataOwnersAsync">A function to load data owners if needed.</param>
        /// <returns>False if not authorized.</returns>
        Task<bool> TryAuthorizeAsync(AuthorizationRole requiredRoles, Func<Task<IEnumerable<DataOwner>>> getDataOwnersAsync = null);
    }
}