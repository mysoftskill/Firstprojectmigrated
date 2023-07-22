namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.UnitTests
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Newtonsoft.Json;

    public static class TestHelper
    {
        static TestHelper()
        {
            SerializerSettings.Instance.ContractResolver = new DerivedTypeContractResolver();
        }

        public static HttpContent Serialize<T>(T content)
        {
            string serializedObject = string.Empty;
            serializedObject = JsonConvert.SerializeObject(content, SerializerSettings.Instance);
            return new StringContent(serializedObject, System.Text.Encoding.UTF8, "application/json");
        }

        public static async Task<T> Deserialize<T>(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(jsonString, SerializerSettings.Instance);
        }

        public static async Task<PagingResponse<T>> DeserializePagingResponse<T>(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            PagingResponse<T> responseWrapper = JsonConvert.DeserializeObject<PagingResponse<T>>(jsonString, SerializerSettings.Instance);
            return responseWrapper;
        }

        public static async Task<IEnumerable<T>> DeserializeCollection<T>(HttpResponseMessage response)
        {
            var responseWrapper = await TestHelper.DeserializePagingResponse<T>(response).ConfigureAwait(false);
            return responseWrapper.Value;
        }

        public static async Task<T> DeserializeValue<T>(HttpResponseMessage response)
        {
            var responseWrapper = await Deserialize<ValueResponse<T>>(response).ConfigureAwait(false);
            return responseWrapper.Value;
        }

        /// <summary>
        /// Helper class to deserialize response with a collection of objects.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        public class PagingResponse<T>
        {
            [JsonProperty(PropertyName = "@odata.count")]
            public int Total { get; set; }

            [JsonProperty(PropertyName = "@odata.nextLink")]
            public string NextLink { get; set; }

            [JsonProperty(PropertyName = "value")]
            public IEnumerable<T> Value { get; set; }
        }

        /// <summary>
        /// Helper class to deserialize response with a single value selected.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        public class ValueResponse<T>
        {
            [JsonProperty(PropertyName = "value")]
            public T Value { get; set; }
        }
    }
}
