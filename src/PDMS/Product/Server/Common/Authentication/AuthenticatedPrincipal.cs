namespace Microsoft.PrivacyServices.DataManagement.Common.Authentication
{
    using System.Net;
    using System.Security.Claims;

    /// <summary>
    /// Defines all known information about the requestor.
    /// </summary>
    public class AuthenticatedPrincipal
    {
        /// <summary>
        /// Gets or sets the user id in the common schema formats.        
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user alias. Part of the user's microsoft email address without the domain. <c>Like yuhyang from yuhyang@microsoft.com.</c>
        /// </summary>
        public string UserAlias { get; set; }

        /// <summary>
        /// Gets or sets the application id. This should be used by authorization for access control.
        /// </summary>
        public string ApplicationId { get; set; }

        /// <summary>
        /// Gets or sets the claims principal that was used to populate the other properties on this class.
        /// </summary>
        public ClaimsPrincipal ClaimsPrincipal { get; set; }

        /// <summary>
        /// Gets or sets the operation name. This should be used by authorization for access control.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// Gets or sets the IP address of the authenticated user.
        /// </summary>
        public IPAddress IPAddress { get; set; }
    }
}