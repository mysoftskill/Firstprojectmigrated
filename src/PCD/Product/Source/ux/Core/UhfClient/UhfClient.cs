using System;
using System.Xml.Linq;
using Microsoft.PrivacyServices.UX.Models.Uhf;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.AspNetCore.Localization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Osgs.Infra.Monitoring;

namespace Microsoft.PrivacyServices.UX.Core.UhfClient
{
    /// <summary>
    /// Provides functionality to load UHF information like Css styles,
    /// JS code, and the html code for the header and footer.
    /// </summary>
    public class UhfClient: IUhfClient
    {
        private readonly IUhfClientConfig config;
        private readonly IUhfHttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UhfClientProvider"/> class.
        /// </summary>
        /// <param name="culture">Access to the culture and location information</param>
        /// <param name="config">Uhf configuration values</param>
        public UhfClient(
            IUhfClientConfig config,
            IUhfHttpClient httpClient)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<Uhf> LoadUhfModel(string cultureCode)
        {
            var serviceUrl = $"{config.ServiceEndpoint}/{cultureCode}/shell/xml/{config.PartnerId}?headerId={config.HeaderId}&footerId={config.FooterId}";
            var requestMessage = new HttpRequestMessage();
            requestMessage.RequestUri = new Uri(serviceUrl);
            requestMessage.Properties[MonitoringRequestProperties.OperationNameKey] = "LoadUhfModel";

            var responseMessage = await httpClient.HttpClient.SendAsync(requestMessage);
            string xml = await responseMessage.Content.ReadAsStringAsync();
            return ConvertXmlToModel(xml);
        }

        /// <summary>
        /// Converts the resulting xml string into a Uhf object
        /// </summary>
        private Uhf ConvertXmlToModel(string xml)
        {
            var document = XDocument.Parse(xml);
            var root = document.Element("shell");
            return new Uhf
            {
                CssIncludes = ConvertToStringValue(root, "cssIncludes"),
                JavaScriptIncludes = ConvertToStringValue(root, "javascriptIncludes"),
                FooterHtml = ConvertToStringValue(root, "footerHtml"),
                HeaderHtml = ConvertToStringValue(root, "headerHtml"),
            };
        }

        /// <summary>
        /// Grabs a string value in the xml document associated with an elementName 
        /// and ensures the elementName is present in the xml document.
        /// </summary>
        private System.Web.HtmlString ConvertToStringValue(XElement root, string elementName)
        {
            var element = root.Element(elementName);
            return new System.Web.HtmlString(element != null ? (string)element : string.Empty);
        }
    }
}
