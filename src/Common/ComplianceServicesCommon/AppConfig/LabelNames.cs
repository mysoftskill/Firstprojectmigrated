

namespace Microsoft.Azure.ComplianceServices.Common
{
    using Microsoft.Extensions.Configuration.AzureAppConfiguration;

    // Using lables on configs allows us to use different configurations for different environments. 
    // See https://docs.microsoft.com/en-us/azure/azure-app-configuration/howto-labels-aspnet-core?tabs=core5x for details
    // This only applies to NonProd config as Prod config lives in a different store
    public static class LabelNames
    {
        public const string CI = "ci";
        public const string INT = "int";
        public const string PPE = "ppe";
        public const string None = LabelFilter.Null;
    }
}
