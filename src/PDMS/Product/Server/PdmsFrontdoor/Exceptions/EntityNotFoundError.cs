namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;

    /// <summary>
    /// EntityNotFound error.
    /// </summary>
    [Serializable]
    public class EntityNotFoundError : NotFoundError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityNotFoundError" /> class.
        /// </summary>
        /// <param name="id">Id of the missing entity.</param>
        /// <param name="entityType">Type of the missing entity.</param>
        public EntityNotFoundError(Guid id, string entityType = null) : base("Entity was not found", new InnerError(id, entityType))
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
            /// <param name="entityType">Type of the missing entity.</param>
            public InnerError(Guid id, string entityType) : base("Entity")
            {
                this.Id = id;
                this.Type = entityType;
            }

            /// <summary>
            /// Gets or sets the id that could not be found.
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the type of the entity.
            /// </summary>
            public string Type { get; set; }
        }
    }
}