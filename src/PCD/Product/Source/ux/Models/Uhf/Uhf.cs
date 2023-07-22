using System.Web;

namespace Microsoft.PrivacyServices.UX.Models.Uhf
{
    public class Uhf
    {
        public HtmlString CssIncludes { get; set; }
        public HtmlString JavaScriptIncludes { get; set; }
        public HtmlString HeaderHtml { get; set; }
        public HtmlString FooterHtml { get; set; }
    }
}
