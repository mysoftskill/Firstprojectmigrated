using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;
using Newtonsoft.Json;

namespace Microsoft.PrivacyServices.UX.Models.AppBootstrap
{
    /// <summary>
    /// Model used for main.bootstrapApp() arguments.
    /// </summary>
    public sealed class AppBootstrapModel
    {
        /// <summary>
        /// Gets or sets AAD app ID used by the site.
        /// </summary>
        public string AzureAdAppId { get; set; }

        /// <summary>
        /// Gets or sets JSLL app ID.
        /// </summary>
        public string JsllAppId { get; set; }

        /// <summary>
        /// Gets or sets the state of integration testing mode.
        /// </summary>
        public bool I9nMode { get; set; }

        /// <summary>
        /// Gets or sets the kill switch which allows mocking throughout app.
        /// </summary>
        public bool AllowMocks { get; set; }

        /// <summary>
        /// Gets or sets the environment type (e.g., Int, PPE, PROD).
        /// </summary>
        public string EnvironmentType { get; set; }

        /// <summary>
        /// Gets or sets NGP lockdown configuration.
        /// </summary>
        [JsonProperty("lockdown")]
        public NgpLockdownModel NgpLockdown { get; set; } = new NgpLockdownModel();

        /// <summary>
        /// Gets or sets pre-loaded CMS content items to be burned on the page, to prime the
        /// frontend CMS cache at the app startup.
        /// </summary>
        public IDictionary<string, IBaseCompassType> PreLoadedCmsContentItems { get; internal set; }
    }
}
