using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdmsApiModels = Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents connection details for data agent.
    /// </summary>
    public class ConnectionDetail
    {
        /// <summary>
        /// Gets or sets connection protocol.
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Gets or sets authentication type.
        /// </summary>
        public PdmsApiModels.AuthenticationType? AuthenticationType { get; set; }

        /// <summary>
        /// Gets or sets MSA site ID (MSA site-based auth).
        /// </summary>
        public long? MsaSiteId { get; set; }

        /// <summary>
        /// Gets or sets AAD app ID (AAD app-based auth).
        /// </summary>
        public string AadAppId { get; set; }

        /// <summary>
        /// Gets or sets multiple AAD app ID (AAD app-based auth for PCF v2 batch agents).
        public IEnumerable<string> AadAppIds { get; set; }

        /// <summary>
        /// Gets or sets Prod readiness state of the agent.
        /// </summary>
        public PdmsApiModels.AgentReadiness AgentReadiness { get; set; }

        /// <summary>
        /// Gets or sets release state.
        /// </summary>
        public PdmsApiModels.ReleaseState ReleaseState { get; set; }
    }
}
