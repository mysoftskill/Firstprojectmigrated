namespace Microsoft.PrivacyServices.AzureFunctions.Common.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Models the Variant Request workitem for ADO
    /// </summary>
    public class VariantRequestWorkItem
    {
        /// <summary>
        /// List of Variants in the variant request
        /// </summary>
        public IEnumerable<ExtendedAssetGroupVariant> Variants { get; set; }

        /// <summary>
        /// List of AssetGroups in the variant request workitem
        /// </summary>
        public IEnumerable<VariantRelationship> AssetGroups { get; set; }

        /// <summary>
        /// General Contractor alias that will be added to the request
        /// </summary>
        public string GeneralContractorAlias { get; set; }

        /// <summary>
        /// CELA Contractor alias that will be added to the request
        /// </summary>
        public string CelaContactAlias { get; set; }

        /// <summary>
        /// Requester alias that will be added to the request
        /// </summary>
        public string RequesterAlias { get; set; }

        /// <summary>
        /// General Contractor alias that approved the request
        /// </summary>
        public string GcApproverAlias { get; set; }

        /// <summary>
        /// CELA alias that will that approved the request
        /// </summary>
        public string CelaApproverAlias { get; set; }

        /// <summary>
        /// Alias of the GC that approved the request in place of CELA
        /// </summary>
        public string GcOnBehalfOfCelaAlias { get; set; }

        /// <summary>
        /// Variant Request Id from PDMS
        /// </summary>
        public string VariantRequestId { get; set; }

        /// <summary>
        /// Title of the work item
        /// </summary>
        public string WorkItemTitle { get; set; }

        /// <summary>
        /// Description of the work item
        /// </summary>
        public string WorkItemDescription { get; set; }
    }
}
