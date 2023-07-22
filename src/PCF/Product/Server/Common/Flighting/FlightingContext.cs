namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Globalization;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    /// A parameter used to evaluate a flight.
    /// </summary>
    public class FlightingContext
    {
        // Add cache for environment info, it seems to be causing high cpu if called
        // repeatedly.
        private static EnvironmentInfoCache environmentInfoCache = null;

        /// <summary>
        /// Creates a flighting parameter for an integer.
        /// </summary>
        public static ICustomOperatorContext FromIntegerValue(int value)
        {
            ICustomOperatorContext context = CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(value);
            AddEnvironmentInfoToOperatorContext(context);
            return context;
        }

        /// <summary>
        /// Creates a flighting parameter for a string.
        /// </summary>
        public static ICustomOperatorContext FromStringValue(string value)
        {
            ICustomOperatorContext context = CustomOperatorContextFactory.CreateDefaultStringComparisonContext(value);
            AddEnvironmentInfoToOperatorContext(context);
            return context;
        }

        /// <summary>
        /// Creates a flighting parameter for an Asset Group ID.
        /// </summary>
        public static ICustomOperatorContext FromAssetGroupId(AssetGroupId assetGroupId)
        {
            return CreateCustomOperatorContext("AssetGroupId", assetGroupId.Value);
        }

        /// <summary>
        /// Creates a flighting parameter for an Agent ID.
        /// </summary>
        public static ICustomOperatorContext FromAgentId(AgentId agentId)
        {
            return CreateCustomOperatorContext("AgentId", agentId.Value);
        }

        /// <summary>
        /// Creates a flighting parameter for a Tenant ID.
        /// </summary>
        public static ICustomOperatorContext FromTenantId(TenantId tenantId)
        {
            return CreateCustomOperatorContext("TenantId", tenantId.Value);
        }

        /// <summary>
        /// Creates a flighting parameter for a Command ID.
        /// </summary>
        public static ICustomOperatorContext FromCommandId(CommandId commandId)
        {
            return CreateCustomOperatorContext("CommandId", commandId.Value);
        }

        /// <summary>
        /// Creates a flighting parameter for an MSA subject.
        /// </summary>
        public static ICustomOperatorContext FromMsaSubject(MsaSubject subject)
        {
            return CreateCustomOperatorContext("PUID", subject.Puid.ToString());
        }

        /// <summary>
        /// Creates a flighting parameter for an AAD subject.
        /// </summary>
        public static ICustomOperatorContext FromAadSubject(AadSubject subject)
        {
            return CreateCustomOperatorContext("OID", subject.ObjectId.ToString());
        }


        private static ICustomOperatorContext CreateCustomOperatorContext(string key, string value)
        {
            CustomOperatorContextFactory.CustomOperatorContext context = CustomOperatorContextFactory.CreateDefaultStringComparisonContextWithKeyValue(key, value);

            AddEnvironmentInfoToOperatorContext(context);

            return context;

        }

        private static void AddEnvironmentInfoToOperatorContext(ICustomOperatorContext con)
        {
            var current = IncomingEvent.Current;
            if (current != null)
            {
                con.IncomingOperationName = current.OperationName ?? "unknown";
                con.IncomingCallerName = current.CallerName ?? "unknown";
            }

            if(environmentInfoCache is null)
            {
                environmentInfoCache = new EnvironmentInfoCache()
                {
                    MachineName = EnvironmentInfo.NodeName,
                    ServiceName = EnvironmentInfo.ServiceName,
                    EnvironmentName = EnvironmentInfo.EnvironmentName,
                    AssemblyVersion = EnvironmentInfo.AssemblyVersion
                };
            }

            con.MachineName = environmentInfoCache.MachineName;
            con.ServiceName = environmentInfoCache.ServiceName;
            con.EnvironmentName = environmentInfoCache.EnvironmentName;
            con.AssemblyVersion = environmentInfoCache.AssemblyVersion;
        }

        private class EnvironmentInfoCache
        {
            public string MachineName;
            public string ServiceName;
            public string EnvironmentName;
            public string AssemblyVersion;
        }
    }
}
