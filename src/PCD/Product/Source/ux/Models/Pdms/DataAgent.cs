using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PdmsApiModels = Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents data agent.
    /// </summary>
    public abstract class DataAgent : IEntityWithIcmInformation
    {
        /// <summary>
        /// Gets or sets data agent frontend kind.
        /// </summary>
        public abstract string Kind { get; }

        /// <summary>
        /// Gets or sets data agent ID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets data agent name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets data agent description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets owner ID.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets owner.
        /// </summary>
        public DataOwner Owner { get; set; }

        /// <summary>
        /// Gets or sets connection details.
        /// </summary>
        public IDictionary<PdmsApiModels.ReleaseState, ConnectionDetail> ConnectionDetails { get; set; }

        /// <summary>
        /// Gets or sets operational readiness.
        /// </summary>
        public OperationalReadiness OperationalReadiness { get; set; }

        /// <summary>
        /// Gets or sets IcM connector ID.
        /// </summary>
        public string IcmConnectorId { get; set; }

        /// <summary>
        /// Known kinds of data agents (maps to frontend's DataAgentKind).
        /// </summary>
        protected static partial class Kinds
        {
            public static string DeleteAgent = "delete-agent";
        }
    }
}
