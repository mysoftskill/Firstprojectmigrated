// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System.Security.Principal;

    /// <summary>
    /// Representation of calling principal
    /// </summary>
    public class CallerPrincipal : IPrincipal
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallerPrincipal"/> class.
        /// </summary>
        /// <param name="identity">The identity.</param>
        public CallerPrincipal(IIdentity identity)
        {
            this.Identity = identity;
        }

        /// <summary>
        /// Gets the identity of the current principal.
        /// </summary>
        public IIdentity Identity { get; private set; }

        /// <summary>
        /// Determines whether the current principal belongs to the specified role.
        /// </summary>
        /// <param name="role">The name of the role for which to check membership.</param>
        /// <returns>
        /// true if the current principal is a member of the specified role; otherwise, false.
        /// </returns>
        public bool IsInRole(string role)
        {
            return false;
        }
    }
}