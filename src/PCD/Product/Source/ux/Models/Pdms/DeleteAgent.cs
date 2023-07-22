using System.Collections.Generic;
using Microsoft.PrivacyServices.Policy;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents delete agent.
    /// </summary>
    public class DeleteAgent : DataAgent
    {
        /// <summary>
        /// Gets or sets data agent kind.
        /// </summary>
        public override string Kind => Kinds.DeleteAgent;

        /// <summary>
        /// Gets or sets sharing enabled boolean.
        /// </summary>
        public bool SharingEnabled { get; set; }

        /// <summary>
        /// Gets or sets is third party agent boolean.
        /// </summary>
        public bool IsThirdPartyAgent { get; set; }

        /// <summary>
        /// Gets or sets sharing requests boolean.
        /// </summary>
        public bool? HasSharingRequests { get; set; }

        /// <summary>
        /// Gets or sets asset groups.
        /// </summary>
        public IEnumerable<AssetGroup> AssetGroups { get; set; }

        /// <summary>
        /// Gets or sets the deployment location of the agent.
        /// </summary>
        public CloudInstanceId DeploymentLocation
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets the Data Residency Boundary of the agent.
        /// </summary>
        public DataResidencyInstanceId DataResidencyBoundary
        {
            get; set;
        }

        /// <summary>
        /// Gets or sets supported clouds.
        /// </summary>
        public IEnumerable<CloudInstanceId> SupportedClouds
        {
            get; set;
        }
    }
}
