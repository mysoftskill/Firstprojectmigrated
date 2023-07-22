using System.Collections.Generic;
using PdmsApiModelsV2 = Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Models.Pdms.RegistrationStatus
{
    /// <summary>
    /// The asset registration status. 
    /// </summary>
    public class AssetRegistrationStatus
    {
        /// <summary>
        /// The id of the asset from DataGrid.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// An overall summary of whether or not the asset registration is complete. 
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// The qualifier for the asset group. 
        /// </summary>
        public AssetGroupQualifier Qualifier { get; set; }

        /// <summary>
        /// Whether or not the NonPersonal tag was found. 
        /// </summary>
        public bool IsNonPersonal { get; set; }

        /// <summary>
        /// Whether or not the LongTail or CustomNonUse tag was found. 
        /// </summary>
        public bool IsLongTailOrCustomNonUse { get; set; }

        /// <summary>
        /// The subject type tags for the asset. 
        /// </summary>
        public IEnumerable<PdmsApiModelsV2.Tag> SubjectTypeTags { get; set; }

        /// <summary>
        /// The subject type tag registration status. 
        /// </summary>
        public PdmsApiModelsV2.RegistrationState SubjectTypeTagsStatus { get; set; }

        /// <summary>
        /// The data type tag registration status. 
        /// </summary>
        public IEnumerable<PdmsApiModelsV2.Tag> DataTypeTags { get; set; }

        /// <summary>
        /// The data type tag registration status.
        /// </summary>
        public PdmsApiModelsV2.RegistrationState DataTypeTagsStatus { get; set; }
    }
}
