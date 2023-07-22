namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.PrivacyServices.SignalApplicability;

    /// <summary>
    /// Defines the <see cref="AssetGroupInfoExtension" />
    /// </summary>
    public static class AssetGroupInfoExtension
    {
        /// <summary>
        /// Converts the <see cref="IAssetGroupInfo"/> to the <see cref="DataAsset"/>.
        /// </summary>
        /// <param name="assetGroupInfo">The assetGroupInfo<see cref="IAssetGroupInfo"/></param>
        /// <returns>The <see cref="DataAsset"/></returns>
        public static DataAsset ToDataAsset(this IAssetGroupInfo assetGroupInfo)
        {
            var dataAsset = new DataAsset()
            {
                AssetGroupId = assetGroupInfo.AssetGroupId.GuidValue,
                Capabilities = assetGroupInfo.SupportedCommandTypes.Select(x => x.ToCapabilityId()),
                DataTypes = assetGroupInfo.SupportedDataTypes,
                ExtendedProps = (Dictionary<string, string>)assetGroupInfo.ExtendedProps,
                IsDeprecated = assetGroupInfo.IsDeprecated,
                SubjectTypes = assetGroupInfo.PdmsSubjectTypes.Select(x => Policies.Current.SubjectTypes.CreateId(x.ToString())),
                TenantIds = assetGroupInfo.TenantIds.Select(x => x.GuidValue),
                DeploymentLocation = assetGroupInfo.DeploymentLocation,
                SupportedCloudInstances = assetGroupInfo.SupportedCloudInstances,

                Variants = assetGroupInfo.VariantInfosAppliedByPcf.Select(x => x.ToSignalApplicabilityVariantInfo()),
            };

            return dataAsset;
        }
    }
}