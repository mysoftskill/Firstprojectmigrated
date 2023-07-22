namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Defines connection details to a docDB. There can be multiple databases in each account.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
   
    public class DatabaseConnectionInfo
    {
        /// <summary>
        /// The ID of the database within the account.
        /// </summary>
        public string DatabaseId { get; set; }

        /// <summary>
        /// The key of the account.
        /// </summary>
        public string AccountKey { get; set; }

        /// <summary>
        /// The friendly name of the database within the account.
        /// </summary>
        public string DatabaseMoniker { get; set; }

        /// <summary>
        /// The account's URI.
        /// </summary>
        public Uri AccountUri { get; set; }

        /// <summary>
        /// The relative weight of this database, for new insertions.
        /// </summary>
        public int Weight { get; set; }

        /// <summary>
        /// Builds and initializes a CosmosDB context from the current configuration.
        /// Feature flags are used while deciding actual weight for Db, local weight can be cleaned up as It is not used
        /// </summary>
        public static List<DatabaseConnectionInfo> GetDatabaseConnectionInfosFromConfig()
        {
            List<DatabaseConnectionInfo> databases = new List<DatabaseConnectionInfo>();
          
            foreach (var configItem in Config.Instance.CosmosDBQueues.Instances)
            {
                var connectionInfo = new DatabaseConnectionInfo
                {
                    AccountKey = configItem.Key,
                    AccountUri = new Uri(configItem.Uri),
                    DatabaseId = configItem.DatabaseId,
                    DatabaseMoniker = configItem.Moniker,
                    Weight = (int)configItem.Weight
                };
                databases.Add(connectionInfo);
            }
            return databases;
        }
    }
}
