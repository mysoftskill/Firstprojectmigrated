namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;

    /// <summary>
    /// WithoutWritePermission error.
    /// </summary>
    [Serializable]
    public class UserNotAuthorizedError : NotAuthorizedError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserNotAuthorizedError" /> class.
        /// </summary>
        /// <param name="userName">Id of the missing entity.</param>
        /// <param name="role">The missing role that triggered this error.</param>
        /// <param name="message">Message for the error.</param>
        public UserNotAuthorizedError(string userName, string role, string message) : base(message, new InnerError(userName, role))
        {
        }

        /// <summary>
        /// Specific inner error information.
        /// </summary>
        private class InnerError : Exceptions.InnerError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InnerError" /> class.
            /// </summary>
            /// <param name="userName">User name of the request.</param>
            /// <param name="role">The missing role that triggered this error.</param>
            public InnerError(string userName, string role) : base("User")
            {
                this.UserName = userName;
                this.Role = role;
            }

            /// <summary>
            /// Gets or sets the user name that does not have write permissions.
            /// </summary>
            public string UserName { get; set; }

            /// <summary>
            /// Gets or sets the missing role that triggered this error.
            /// </summary>
            public string Role { get; set; }
        }
    }
}