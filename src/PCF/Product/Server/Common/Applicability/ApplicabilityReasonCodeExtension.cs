namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// Defines the <see cref="ApplicabilityReasonCodeExtension" /> class.
    /// </summary>
    public static class ApplicabilityReasonCodeExtension
    {
        /// <summary>
        /// Check if result is applicable.
        /// </summary>
        /// <param name="applicabilityResult">SAL Applicability Result.</param>
        /// <returns>True if applicable.</returns>
        public static bool IsApplicable(this ApplicabilityResult applicabilityResult)
        {
            return applicabilityResult.Status != ApplicabilityStatus.DoesNotApply;
        }

        /// <summary>
        /// Get PCF applicable variants.
        /// </summary>
        /// <param name="applicabilityResult">Applicability result.</param>
        /// <param name="pcfVariants">PCF variants.</param>
        /// <returns>Collection of PCF variants.</returns>
        public static IEnumerable<IAssetGroupVariantInfo> GetPcfApplicableVariants(this ApplicabilityResult applicabilityResult, IEnumerable<IAssetGroupVariantInfo> pcfVariants)
        {
            if (pcfVariants.IsNullOrEmpty() || applicabilityResult.ApplicableVariantInfos.IsNullOrEmpty())
            {
                return new List<IAssetGroupVariantInfo>();
            }

            return pcfVariants.Where(v => applicabilityResult.ApplicableVariantIds.Contains(v.VariantId.GuidValue));
        }
    }
}
