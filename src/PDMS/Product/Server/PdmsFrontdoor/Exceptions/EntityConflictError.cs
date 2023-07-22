namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;

    using Newtonsoft.Json;

    /// <summary>
    /// DataAgentConflict error.
    /// </summary>
    [Serializable]
    public class EntityConflictError : ConflictError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityConflictError" /> class.
        /// </summary>
        /// <param name="exception">The exception from the business logic.</param>
        public EntityConflictError(ConflictException exception) : base(exception.Message, new EntityConflictInnerError(exception.ConflictType, exception.Value))
        {
            this.ServiceError.Target = exception.Target;
        }

        /// <summary>
        /// Specific inner error information.
        /// </summary>
        private class EntityConflictInnerError : InnerError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InnerError" /> class.
            /// </summary>
            /// <param name="conflictType">The conflict type to map.</param>
            /// <param name="value">The value.</param>
            public EntityConflictInnerError(ConflictType conflictType, string value) : this(conflictType.ToString().Split('_'))
            {
                this.Value = value;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="InnerError" /> class.
            /// </summary>
            /// <param name="parts">The remaining portions of the conflict type name split by _.</param>
            private EntityConflictInnerError(IEnumerable<string> parts) : base(parts.First())
            {
                parts = parts.Skip(1);

                if (parts.Any())
                {
                    this.NestedError = new EntityConflictInnerError(parts);
                }
            }

            /// <summary>
            /// Gets or sets the value of the bad argument.
            /// </summary>
            [JsonProperty(PropertyName = "value")]
            public string Value { get; set; }
        }
    }
}