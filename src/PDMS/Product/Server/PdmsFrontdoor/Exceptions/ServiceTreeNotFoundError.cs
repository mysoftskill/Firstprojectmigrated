namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;

    /// <summary>
    /// ServiceTreeNotFound error.
    /// </summary>
    [Serializable]
    public class ServiceTreeNotFoundError : NotFoundError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTreeNotFoundError" /> class.
        /// </summary>
        /// <param name="id">Id of the missing service tree entity.</param>
        public ServiceTreeNotFoundError(Guid id) : base("Service tree entity was not found", new InnerError(id))
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
            /// <param name="id">Id of the missing service tree entity.</param>
            public InnerError(Guid id) : base("ServiceTree")
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