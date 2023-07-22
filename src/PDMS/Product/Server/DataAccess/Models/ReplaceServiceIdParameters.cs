namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;
    
    /// <summary>
    /// This object defines the payload for the V2.DataOwners.ReplaceServiceId API.
    /// </summary>
    public class ReplaceServiceIdParameters
    {
        /// <summary>
        /// The data owner that should be updated.
        /// It must have the serviceTree.serviceId set.        
        /// </summary>
        [JsonProperty(PropertyName = "value")]
        public DataOwner Value { get; set; }
    }
}