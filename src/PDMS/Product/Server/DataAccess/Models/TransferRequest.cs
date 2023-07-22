namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Defines a request to transfer a collection of asset groups from one data owner to other.
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