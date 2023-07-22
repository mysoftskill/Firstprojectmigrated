// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///    Retrieves a list of excluded agents for Action References from Azure app configuration.
    /// </summary>
    public class ActionRefExcludedAgentsOverrides : IActionRefExcludedAgentsOverrides
    {
        private const string ComponentName = nameof(ActionRefExcludedAgentsOverrides);
        private readonly IAppConfiguration appConfiguration;
        private readonly ILogger logger;

        /// <summary>
        ///    Initializes a new instance of the ActionRefExcludedAgentsOverrides class.
        /// </summary>
        /// <param name="appConfiguration">Azure App configuration isntance</param>
        /// <param name="logger">Geneva trace logger</param>
        public ActionRefExcludedAgentsOverrides(
            IAppConfiguration appConfiguration,
            ILogger logger)
        {
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Merge original ActionRef Json file with excluded agents overrides from Azure app configuration.
        /// </summary>
        /// <param name="contents">Json content of original ActionRef.</param>
        /// <returns>Merged ActionRef</returns>
        public string MergeExcludedAgentsOverrides(string contents)
        {
            var agents = this.GetExcludedAgentsOverrides();
            if (agents == null || !agents.Any())
            {
                // There are no excluded agents in Azure App Configuration, log error and use existing config.
                this.logger.Error(ComponentName, $"There are no excluded agents in Azure App Configuration. Use existing config from ActionRefs.json.");
                return contents;
            }

            try
            {
                JArray jsonContents = JArray.Parse(contents);
                foreach (JToken token in jsonContents.Children())
                {
                    string id = (string)token["Id"];

                    var kustoParameters = token.SelectToken("$..KustoParameters");
                    if (kustoParameters == null)
                    {
                        continue;
                    }

                    ICollection<ExcludedAgent> overrides = null;
                    agents.TryGetValue(id, out overrides);
                    if (overrides == null || !overrides.Any())
                    {
                        this.logger.Information(ComponentName, $"ActionRef with Id: {id} contains no excluded agents in Azure App Configuration.");
                        overrides = new List<ExcludedAgent>();
                    }
                    else
                    {
                        this.logger.Information(ComponentName, $"{overrides.Count()} excluded agents are added to ActionRef with Id: {id}.");
                        overrides.Where(a => string.IsNullOrWhiteSpace(a.Expires)).ToList().ForEach(a => a.Expires = null);
                    }

                    kustoParameters["ExcludedAgentsJson"] = JArray.FromObject(overrides);
                }

                return jsonContents.ToString();
            }
            catch (Exception e)
            {
                // Swallow exceptions during merge.
                this.logger.Error(ComponentName, $"Cannot merge ExcludedAgentsJson from Azure app configuration and error message is: {e.Message}.");
            }

            return contents;
        }

        private Dictionary<string, ICollection<ExcludedAgent>> GetExcludedAgentsOverrides()
        {
            var agents = new Dictionary<string, ICollection<ExcludedAgent>>();
            var overrides = this.appConfiguration.GetConfigValues<ExcludedAgentsFromAppConfig>(ConfigNames.PXS.DataActionRunner_ActionRefsOverrides)?.ToList();
            if (overrides != null)
            {
                agents = overrides.ToDictionary(a => a.Id, a => a.ExcludedAgentsJson, StringComparer.OrdinalIgnoreCase);
            }

            return agents;
        }
    }
}
