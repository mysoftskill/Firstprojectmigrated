using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Osgs.Infra.Monitoring.Context;
using Microsoft.Osgs.Web.Core.Configuration;
using Microsoft.PrivacyServices.UX.Configuration;
using Microsoft.PrivacyServices.UX.Core.UhfClient;
using Microsoft.PrivacyServices.UX.Models.AppBootstrap;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ICorrelationVectorContext correlationVectorContext;

        private readonly IAzureADConfig aadConfig;

        private readonly IHostingEnvironment env;

        private readonly IEnvironmentInfo environmentInfo;

        private readonly IMemoryCache memoryCache;

        private readonly IUhfClient uhfClient;

        private readonly IConfiguration configuration;

        private readonly INgpLockdownConfig ngpLockdownConfig;

        private readonly IMocksConfig mocksConfig;

        public HomeController(
            IHostingEnvironment env,
            IEnvironmentInfo environmentInfo,
            ICorrelationVectorContext correlationVectorContext,
            IAzureADConfig aadConfig,
            IUhfClient uhfClient,
            IMemoryCache memoryCache,
            IConfiguration configuration,
            INgpLockdownConfig ngpLockdownConfig,
            IMocksConfig mocksConfig)
        {
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.environmentInfo = environmentInfo ?? throw new ArgumentNullException(nameof(environmentInfo));
            this.correlationVectorContext = correlationVectorContext ?? throw new ArgumentNullException(nameof(correlationVectorContext));
            this.aadConfig = aadConfig ?? throw new ArgumentNullException(nameof(aadConfig));
            this.uhfClient = uhfClient ?? throw new ArgumentNullException(nameof(uhfClient));
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.ngpLockdownConfig = ngpLockdownConfig ?? throw new ArgumentNullException(nameof(ngpLockdownConfig));
            this.mocksConfig = mocksConfig ?? throw new ArgumentNullException(nameof(mocksConfig));
        }
        
        public async Task<IActionResult> Index()
        {
            var requestCultureFeature = HttpContext.Features.Get<IRequestCultureFeature>();
            var requestCulture = requestCultureFeature.RequestCulture;
            var cultureCode = requestCulture.UICulture.Name;

            ViewBag.CorrelationVectorContext = correlationVectorContext;

            ViewBag.UhfData = await memoryCache.GetOrCreateAsync($"Home.UhfData.{cultureCode}", entry =>
            {
                // Cache duration: 24 hours
                double cacheExpirationMins = 60 * 24;

                // The timespan between requests can be extended or shorten depending on
                // our desired frequency.
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpirationMins);
                return uhfClient.LoadUhfModel(cultureCode);
            });

            ViewBag.AppBootstrapModel = new AppBootstrapModel()
            {
                AzureAdAppId = aadConfig.AppId,
                JsllAppId = Request.Host.Host,
                I9nMode = configuration.GetValue<string>("i9nMode").Equals(bool.TrueString),
                EnvironmentType = environmentInfo.EnvironmentType,
                AllowMocks = mocksConfig.AllowMocks,
                NgpLockdown = NgpLockdownModel.CreateFromConfig(ngpLockdownConfig),
                //  NOTE: This field should be loaded from CMS, once integration is back on.
                PreLoadedCmsContentItems = new Dictionary<string, IBaseCompassType>()
            };

            //  Bootstraps SPA.
            return View();
        }
    }
}
