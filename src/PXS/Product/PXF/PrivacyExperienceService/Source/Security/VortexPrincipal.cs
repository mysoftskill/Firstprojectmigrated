// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System.Security.Principal;

    /// <summary>
    ///     Vortex Principal for an authorized vortex client
    /// </summary>
    /// <seealso cref="System.Security.Principal.IPrincipal" />
    internal class VortexPrincipal : IPrincipal
    {
        /// <summary>
        ///     Generic identity for showing that this is from an authorized vortex client
        /// </summary>
        private readonly IIdentity identity = new GenericIdentity("vortex");

        /// <summary>
        ///     Gets the caller name formatted
        /// </summary>
        public string CallerNameFormatted { get; }

        /// <inheritdoc />
        /// <summary>
        ///     Gets the identity of the current principal.
        /// </summary>
        public IIdentity Identity => this.identity;

        /// <summary>
        ///     Ctor
        /// </summary>
        /// <param name="callerNameFormatted">The caller name</param>
        public VortexPrincipal(string callerNameFormatted)
        {
            this.CallerNameFormatted = callerNameFormatted;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Determines whether the current principal belongs to the specified role.
        /// </summary>
        /// <param name="role">The name of the role for which to check membership.</param>
        /// <returns>
        ///     <see langword="true" /> if the current principal is a member of the specified role; otherwise, <see langword="false" />.
        /// </returns>
        public bool IsInRole(string role) => false;
    }
}
