namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;

    /// <summary>
    /// WithoutWritePermission error.
    /// </summary>
    [Serializable]
    public class ApplicationNotAuthorizedError : NotAuthorizedError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationNotAuthorizedError" /> class.
        /// </summary>
        /// <param name="applicationId">Id of the application.</param>
        public ApplicationNotAuthorizedError(string applicationId) : base("Application does not have ACL permissions.", new InnerError(applicationId))
        {
        }

        /// <summary>
        /// Specific inner error information.
        /// </summary>
        public class InnerError : Exceptions.InnerError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InnerError" /> class.
            /// </summary>
            /// <param name="applicationId">Id of the application.</param>
            public InnerError(string applicationId) : base("Application")
            {
                this.ApplicationId = applicationId;
            }

            /// <summary>
            /// Gets or sets the application id that does not have ACL permissions.
            /// </summary>
            public string ApplicationId { get; set; }
        }
    }
}