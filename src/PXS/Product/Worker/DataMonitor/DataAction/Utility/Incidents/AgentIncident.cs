// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents
{
    using System;

    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Newtonsoft.Json;

    /// <summary>
    ///     an incident that will be filed against a data agent
    /// </summary>
    public class AgentIncident : IValidatable
    {
        [JsonIgnore]
        public const int MinSev = 0;

        [JsonIgnore]
        public const int MaxSev = 4;

        /// <summary>
        ///     Gets or sets asset group id
        /// </summary>
        public string AssetGroupId { get; set; }

        /// <summary>
        ///     Gets or sets agent id
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        ///     Gets or sets owner id
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        ///     Gets or sets event name
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        ///     Gets or sets keywords
        /// </summary>
        public string Keywords { get; set; }

        /// <summary>
        ///     Gets or sets title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        ///     Gets or sets body
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        ///     Gets or sets severity
        /// </summary>
        public int Severity { get; set; }

        /// <summary>
        ///     Validates incident is well formed
        /// </summary>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            if (this.AssetGroupId == null && this.AgentId == null && this.OwnerId == null)
            {
                context.LogError("at least one of agent id, owner id, or asset group id must be non-null");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(this.Title))
            {
                context.LogError("incident title must be non-empty");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(this.Body))
            {
                context.LogError("incident body must be non-empty");
                result = false;
            }

            if (string.IsNullOrWhiteSpace(this.EventName))
            {
                context.LogError("incident event name must be non-empty");
                result = false;
            }

            if (this.Severity < AgentIncident.MinSev || this.Severity > AgentIncident.MaxSev)
            {
                context.LogError($"incident severity must be in the range [{AgentIncident.MinSev}..{AgentIncident.MaxSev}]");
                result = false;
            }

            return result;
        }

        /// <summary>
        ///     converts this class to an incident object to be sent to PDMS
        /// </summary>
        /// <returns>resulting value</returns>
        public Incident ToIncident()
        {
            return new Incident
            {
                InputParameters = new IncidentInputParameters
                {
                    DisableTitleSubstitutions = true,
                    DisableBodySubstitutions = true,
                },

                Routing = new RouteData
                {
                    AssetGroupId = AgentIncident.ConvertToGuid(this.AssetGroupId),
                    AgentId = AgentIncident.ConvertToGuid(this.AgentId),
                    OwnerId = AgentIncident.ConvertToGuid(this.OwnerId),

                    EventName = this.EventName,
                },

                Keywords = this.Keywords,
                Severity = this.Severity,
                Title = this.Title,
                Body = this.Body,
            };
        }

        /// <summary>
        ///     Converts a string to a GUID, emitting an ActionExecuteException if it fails
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>resulting value</returns>
        private static Guid? ConvertToGuid(string s)
        {
            try
            {
                return string.IsNullOrWhiteSpace(s) == false ? Guid.Parse(s) : (Guid?)null;
            }
            catch (FormatException e)
            {
                throw new ActionExecuteException($"[{s}] is not convertible to a GUID: {e.Message}", e);
            }
        }
    }
}
