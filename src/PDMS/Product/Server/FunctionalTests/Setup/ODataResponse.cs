namespace Microsoft.PrivacyServices.DataManagement.FunctionalTests.Setup
{
    using Newtonsoft.Json;

    public class ODataResponse<T>
    {
        [JsonProperty("odata.metadata")]
        public string Metadata { get; set; }
        public T Value { get; set; }
    }
}
