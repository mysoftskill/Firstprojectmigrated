namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System.Net.Http;
    using System.Text;
    using Newtonsoft.Json;

    internal class JsonContent : StringContent
    {
        public JsonContent(object item) : this(item, Encoding.UTF8)
        {
        }

        public JsonContent(object content, Encoding encoding)
            : base(JsonConvert.SerializeObject(content), encoding, "application/json")
        {
        }
    }
}
