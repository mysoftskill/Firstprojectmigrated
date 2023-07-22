[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines contact and security information for associated entities.
    /// </summary>
    public class User : Entity
    {
        /// <summary>
        /// Security groups that the user is part of.
        /// </summary>
        [JsonProperty(PropertyName = "securityGroups")]
        public IEnumerable<Guid> SecurityGroups { get; set; }
    }
}