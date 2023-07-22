namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using System.Linq;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// Defines the <see cref="IAssetGroupVariantInfoExtension" />
    /// </summary>
    public static class IAssetGroupVariantInfoExtension
    {
        /// <summary>
        /// Converts <see cref="IAssetGroupVariantInfo"/> to <see cref="VariantInfo"/>.
        /// </summary>
        /// <param name="pcfVariantInfo">The pcfVariantInfo <see cref="IAssetGroupVariantInfo"/></param>
        /// <returns>The <see cref="VariantInfo"/></returns>
        public static VariantInfo ToSignalApplicabilityVariantInfo(this IAssetGroupVariantInfo pcfVariantInfo)
        {
            VariantInfo variantInfo = new VariantInfo()
            {
                Capabilities = pcfVariantInfo.ApplicableCapabilities.Select(x => x.ToCapabilityId()).ToList(),
                DataTypes = pcfVariantInfo.ApplicableDataTypeIds.ToList(),
                SubjectTypes = pcfVariantInfo.ApplicableSubjectTypes.Select(x => x.ToSubjectTypeId()).ToList(),
                VariantId = pcfVariantInfo.VariantId.GuidValue,
            };

            return variantInfo;
        }
    }
}
