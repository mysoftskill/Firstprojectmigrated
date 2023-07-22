
namespace Microsoft.PrivacyServices.PXS.Command.Contracts.Helpers
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.Policy;
    using System;

    /// <summary>
    /// PXS Contract Helpers.
    /// </summary>
    public static class PxsContractsHelpers
    {
        /// <summary>
        /// Supported DataTypeIds by Device Delete signals.
        /// </summary>
        public static readonly DataTypeId[] DeviceDeleteDataTypeIds =
        {
                Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration,
                Policies.Current.DataTypes.Ids.ProductAndServiceUsage,
                Policies.Current.DataTypes.Ids.ProductAndServicePerformance,
                Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory,
                Policies.Current.DataTypes.Ids.BrowsingHistory,
                Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance
        };

        /// <summary>
        ///     Creates the predicate.
        /// </summary>
        /// <param name="dataTypeId">Type of the privacy data.</param>
        /// <returns>the predicate</returns>
        /// <exception cref="ArgumentOutOfRangeException">Received an unexpected privacy data type</exception>
        public static IPrivacyPredicate CreatePrivacyPredicate(DataTypeId dataTypeId)
        {
            IPrivacyPredicate predicate;

            if (dataTypeId == Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration)
            {
                predicate = new DeviceConnectivityAndConfigurationPredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.ProductAndServiceUsage)
            {
                predicate = new ProductAndServiceUsagePredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.ProductAndServicePerformance)
            {
                predicate = new ProductAndServicePerformancePredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory)
            {
                predicate = new SoftwareSetupAndInventoryPredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.BrowsingHistory)
            {
                predicate = new BrowsingHistoryPredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else if (dataTypeId == Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance)
            {
                predicate = new InkingTypingAndSpeechUtterancePredicate { WindowsDiagnosticsDeleteOnly = true };
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Unsupported privacy data type: {dataTypeId.Value}");
            }

            return predicate;
        }
    }
}
