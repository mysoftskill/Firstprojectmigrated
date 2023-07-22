namespace PCF.UnitTests.Pdms
{
    using Microsoft.PrivacyServices.Policy;
    using System;

    public static class PcfTestDataTypeExtentions
    {
        public static DataTypeId GetDataTypeId(this PcfTestDataType pcfTestDataType)
        {
            DataTypeId dataTypeId;

            switch (pcfTestDataType)
            {
                case PcfTestDataType.BrowsingHistory:
                    dataTypeId = Policies.Current.DataTypes.Ids.BrowsingHistory;
                    break;
                case PcfTestDataType.ContentConsumption:
                    dataTypeId = Policies.Current.DataTypes.Ids.ContentConsumption;
                    break;
                case PcfTestDataType.DeviceConnectivityAndConfiguration:
                    dataTypeId = Policies.Current.DataTypes.Ids.DeviceConnectivityAndConfiguration;
                    break;
                case PcfTestDataType.InkingTypingAndSpeechUtterance:
                    dataTypeId = Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance;
                    break;
                case PcfTestDataType.PreciseUserLocation:
                    dataTypeId = Policies.Current.DataTypes.Ids.PreciseUserLocation;
                    break;
                case PcfTestDataType.ProductAndServicePerformance:
                    dataTypeId = Policies.Current.DataTypes.Ids.ProductAndServicePerformance;
                    break;
                case PcfTestDataType.ProductAndServiceUsage:
                    dataTypeId = Policies.Current.DataTypes.Ids.ProductAndServiceUsage;
                    break;
                case PcfTestDataType.SearchRequestsAndQuery:
                    dataTypeId = Policies.Current.DataTypes.Ids.SearchRequestsAndQuery;
                    break;
                case PcfTestDataType.SoftwareSetupAndInventory:
                    dataTypeId = Policies.Current.DataTypes.Ids.SoftwareSetupAndInventory;
                    break;
                case PcfTestDataType.Any:
                    dataTypeId = Policies.Current.DataTypes.Ids.Any;
                    break;
                default:
                    throw new ArgumentException($"Unknown pcf data type: {pcfTestDataType}.");
            }

            return dataTypeId;
        }
    }
}
