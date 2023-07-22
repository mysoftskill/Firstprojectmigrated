using System.Collections.Generic;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Models.Pdms.RegistrationStatus
{
    /// <summary>
    /// The asset group registration status.
    /// </summary>
    public class AssetGroupRegistrationStatus
    {
        /// <summary>
        /// The id of the asset group.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The owner id of the asset group.
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// The owner name of the asset group.
        /// </summary>
        public string OwnerName { get; set; }

        /// <summary>
        /// An overall summary of whether or not the asset group registration is complete. 
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// The qualifier for the asset group. 
        /// </summary>
        public AssetGroupQualifier Qualifier { get; set; }

        /// <summary>
        /// The set of asset registration statuses for all assets linked to this asset group. 
        /// </summary>
        public IEnumerable<AssetRegistrationStatus> Assets { get; set; }

        /// <summary>
        /// An overall summary of whether or not all asset registrations are correct. 
        /// </summary>
        public PdmsApiModelsV2.RegistrationState AssetsStatus { get; set; }
    }
}
