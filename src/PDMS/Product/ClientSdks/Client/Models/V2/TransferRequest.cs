[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a request to transfer the ownership of a collection of asset groups to different data owner.
    /// </summary>
    public class TransferRequest : Entity
    {
        /// <summary>
        /// The id of the owner that currently owns the collection of asset groups in this request.
        /// </summary>
        [JsonProperty(PropertyName = "sourceOwnerId")]
        public string SourceOwnerId { get; set; }

        /// <summary>
        /// The id of the target owner that is supposed to owner the collection of asset groups in this request.
        /// </summary>
        [JsonProperty(PropertyName = "targetOwnerId")]
        public string TargetOwnerId { get; set; }

        /// <summary>
        /// The transfer request state - used internally by the service.
        /// </summary>
        [JsonProperty(PropertyName = "requestState")]
        public TransferRequestStates RequestState { get; set; }

        /// <summary>
        /// The set of asset groups for which the transfer of ownership is requested.
        /// </summary>
        [JsonProperty(PropertyName = "assetGroups")]
        public IEnumerable<string> AssetGroups { get; set; }
    }
}