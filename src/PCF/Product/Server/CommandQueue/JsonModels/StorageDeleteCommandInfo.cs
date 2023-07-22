namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Format of delete commands in storage.
    /// </summary>
    internal class StorageDeleteCommandInfo
    {
        [JsonProperty("p")]
        public IPrivacyPredicate Predicate { get; set; }

        [JsonProperty("t")]
        public TimeRangePredicate TimeRangePredicate { get; set; }

        [JsonProperty("d")]
        public string PrivacyDataType { get; set; }
    }
}
