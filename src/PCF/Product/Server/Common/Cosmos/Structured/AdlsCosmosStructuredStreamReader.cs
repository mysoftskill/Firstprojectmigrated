namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos.Structured
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    using Microsoft.Azure.Management.DataLake.InternalAnalytics.Export;
    using Microsoft.PrivacyServices.Common.Cosmos;
    using Microsoft.Rest;

    /// <summary>
    /// Cosmos Structured StreamReader for Adls
    /// </summary>
    public sealed class AdlsCosmosStructuredStreamReader : ICosmosStructuredStreamReader
    {
        private readonly AdlsConfig adlsConfig;
        private IDataReader dataReader;
        private Dictionary<string, int> columnNameOrdinalMap;
        private readonly Func<Task<string>> getToken;

        /// <summary>
        /// Create AdlsCosmosStructuredStreamReader
        /// </summary>
        public AdlsCosmosStructuredStreamReader(string stream,
            DateTimeOffset lastModifiedTime,
            AdlsConfig config,
            Func<Task<string>> getTokenCallback)
        {
            this.adlsConfig = config;
            this.CosmosStream = stream;
            this.LastModifiedTime = lastModifiedTime;
            this.getToken = getTokenCallback;
        }

        /// <summary>
        /// Stream that is being accessed
        /// </summary>
        public string CosmosStream { get; }

        /// <summary>
        /// The last modified time of the stream.
        /// </summary>
        public DateTimeOffset LastModifiedTime { get; }

        /// <summary>
        /// Dispose Data Reader
        /// </summary>
        public void Dispose()
        {
            if (this.dataReader != null)
            {
                this.dataReader.Close();
                this.dataReader.Dispose();
                this.dataReader = null;
            }
        }

        /// <summary>
        /// Reads the named column at the current cursor.
        /// </summary>
        public T GetValue<T>(string columnName)
        {
            return (T)this.dataReader.GetValue(this.columnNameOrdinalMap[columnName]);
        }

        /// <summary>
        /// Tries to get the column value at the current cursor.
        /// </summary>
        public bool TryGetValue<T>(string columnName, out T value)
        {
            if (!this.columnNameOrdinalMap.ContainsKey(columnName))
            {
                value = default;
                return false;
            }

            value = (T)this.dataReader.GetValue(this.columnNameOrdinalMap[columnName]);
            return true;
        }

        /// <summary>
        /// Reads the Json from the named column at the current cursor and returns the object
        /// </summary>
        public T GetJsonValue<T>(string columnName)
        {
            return JsonConvert.DeserializeObject<T>(this.GetValue<string>(columnName));
        }

        /// <summary>
        /// Tries to get the json column value at the current cursor.
        /// </summary>
        public bool TryGetJsonValue<T>(string columnName, out T value)
        {
            if (!this.TryGetValue(columnName, out string rawValue))
            {
                value = default;
                return false;
            }

            value = JsonConvert.DeserializeObject<T>(rawValue);
            return true;
        }

        /// <summary>
        /// Advances the current cursor.
        /// </summary>
        public bool MoveNext()
        {
            return this.dataReader.Read();
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            var exportClient = new DataLakeAnalyticsExportClient(new TokenCredentials(await this.getToken().ConfigureAwait(false)), this.adlsConfig.AccountName, this.CosmosStream, this.adlsConfig.AccountSuffix);
            var exportResult = exportClient.Export();
            this.dataReader = exportResult.DataReader;

            //// Initialize column names
            DataTable schematable = this.dataReader.GetSchemaTable();

            DataColumn schemacolColname = schematable.Columns["ColumnName"];

            this.columnNameOrdinalMap = new Dictionary<string, int>();
            for (int i = 0; i < schematable.Rows.Count; i++)
            {
                DataRow row = schematable.Rows[i];

                string columnName = (string)row[schemacolColname.Ordinal];
                this.columnNameOrdinalMap[columnName] = i;
            }
        }
    }
}