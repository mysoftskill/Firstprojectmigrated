namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;

    /// <summary>
    /// EntityNotFound error.
    /// </summary>
    [Serializable]
    public class SecurityGroupNotFoundError : NotFoundError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityGroupNotFoundError" /> class.
        /// </summary>
        /// <param name="id">Id of the missing entity.</param>
        public SecurityGroupNotFoundError(Guid id) : base("Security group was not found", new InnerError(id))
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
            /// <param name="id">Id of the missing entity.</param>
            public InnerError(Guid id) : base("SecurityGroup")
            {
                this.Id = id;
            }

            /// <summary>
            /// Gets or sets the id that could not be found.
            /// </summary>
            public Guid Id { get; set; }
        }
    }
}