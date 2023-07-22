namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.Authentication
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using System.Web.Http;
    using System.Web.Http.Controllers;

    public class ProductionAuthorize : AuthorizeAttribute
    {
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            bool isProduction = Config.Instance.Common.IsProductionEnvironment;
            return !isProduction || base.IsAuthorized(actionContext);
        }
    }
}
