namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Common.Authentication;

    /// <summary>
    /// Exposes methods for interacting with active directory.
    /// </summary>
    public interface IActiveDirectory
    {
        /// <summary>
        /// Given an authenticated principal, retrieves the associated security group ids.
        /// </summary>
        /// <param name="principal">The authenticated principal.</param>
        /// <returns>The security group ids.</returns>
        Task<IEnumerable<Guid>> GetSecurityGroupIdsAsync(AuthenticatedPrincipal principal);

        /// <summary>
        /// Determines if a security group id exists or not.
        /// This is necessary for scenarios where the user does not need to be in the security group.
        /// </summary>
        /// <param name="principal">The current authenticated user.</param>
        /// <param name="id">The id of the security group.</param>
        /// <returns>True if it exists. Otherwise, false.</returns>
        Task<bool> SecurityGroupIdExistsAsync(AuthenticatedPrincipal principal, Guid id);
    }
}