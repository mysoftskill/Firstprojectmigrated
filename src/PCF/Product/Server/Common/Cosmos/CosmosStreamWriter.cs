namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Common.Cosmos;

    /// <summary>
    /// Defines the class that write batch records to cosmos
    /// </summary>
    public class CosmosStreamWriter
    {

        private static CosmosStreamWriter AuditLogWriter = null;
        private static CosmosStreamWriter PrivacyCommandWriter = null;

        private readonly TimeSpan streamExpirationTime;
        private readonly string streamPathFormat;
        private readonly string completionSignalStreamPathFormat;
        private readonly ICosmosClient cosmosClient;

        private CosmosStreamWriter(
            TimeSpan streamExpirationTime,
            string streamPathFormat,
            string completionSignalStreamPathFormat,
            ICosmosClient cosmosClient)
        {
            this.streamExpirationTime = streamExpirationTime;
            
            this.cosmosClient = cosmosClient;

            // Adjust paths for adls.
            if (this.cosmosClient.ClientTechInUse() == ClientTech.Adls)
            {
                this.streamPathFormat = streamPathFormat.Substring(streamPathFormat.IndexOf("/local", StringComparison.InvariantCulture));
                this.completionSignalStreamPathFormat = completionSignalStreamPathFormat.Substring(completionSignalStreamPathFormat.IndexOf("/local", StringComparison.InvariantCulture));
            }
            else 
            {
                this.streamPathFormat = streamPathFormat;
                this.completionSignalStreamPathFormat = completionSignalStreamPathFormat;
            }
        }

        public static CosmosStreamWriter AuditLogCosmosWriter()
        {
            return AuditLogWriter;
        }

        public static CosmosStreamWriter PrivacyCommandCosmosWriter()
        {
            return PrivacyCommandWriter;
        }

        public static void InitializeStreamWriters(ICosmosClient client)
        {
            if (AuditLogWriter == null && PrivacyCommandWriter == null)
            {
                AuditLogWriter = new CosmosStreamWriter(
                    TimeSpan.FromDays(Config.Instance.Cosmos.Streams.AuditLog.StreamExpirationDays),
                    Config.Instance.Cosmos.Streams.AuditLog.StreamFormat,
                    Config.Instance.Cosmos.Streams.AuditLog.CompletionSignalStreamFormat,
                    client);

                PrivacyCommandWriter = new CosmosStreamWriter(
                    TimeSpan.FromDays(Config.Instance.Cosmos.Streams.PrivacyCommand.StreamExpirationDays),
                    Config.Instance.Cosmos.Streams.PrivacyCommand.StreamFormat,
                    Config.Instance.Cosmos.Streams.PrivacyCommand.CompletionSignalStreamFormat,
                    client);
            }
            else
            {
                throw new InvalidOperationException("Stream writers initialization called multiple times.");
            }
        }

        /// <summary>
        /// Write batch of records to cosmos
        /// </summary>
        /// <param name="hourWindow">Hour window for stream</param>
        /// <param name="serializedBatchRecords">Batch of records as string format</param>
        /// <param name="rowCount">Record count in batch for logging purpose</param>
        public Task WriteBatchRecordsToCosmosAsync(DateTimeOffset hourWindow, string serializedBatchRecords, int rowCount)
        {
            if (string.IsNullOrEmpty(serializedBatchRecords))
            {
                return Task.FromResult(true);
            }

            string streamPath = GetCosmosStreamFullPath(hourWindow, this.streamPathFormat, this.cosmosClient);
            byte[] content = Encoding.UTF8.GetBytes(serializedBatchRecords);

            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    ev["CosmosSteamPath"] = streamPath;
                    ev["AppendRows"] = rowCount.ToString();
                    ev["AppendBytes"] = content.Length.ToString();

                    try
                    {
                        await this.cosmosClient.AppendAsync(streamPath, content).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        DualLogger.Instance.Warning(nameof(CosmosStreamWriter), $"Write to cosmos failed, stream {streamPath} might not yet exist. Error Message: {e.Message}");
                        await this.CreateStreamAndAppendContents(streamPath, content).ConfigureAwait(false);
                    }

                    DualLogger.Instance.Information(nameof(CosmosStreamWriter), $"Batch records appened to stream: {streamPath}. Appened {rowCount} rows");
                });
        }

        /// <summary>
        /// Close given hour window by writing completion signal to cosmos
        /// </summary>
        /// <param name="hourWindow">Hour window to close</param>
        public Task CloseHourWindowAsync(DateTimeOffset hourWindow)
        {
            return Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async ev =>
                {
                    DualLogger.Instance.Information(nameof(CosmosStreamWriter), $"Writing Completion Signal to this hour: {hourWindow}.");
                    string streamPath = GetCosmosStreamFullPath(hourWindow, this.streamPathFormat, this.cosmosClient);
                    string completionSignalStreamPath = GetCosmosStreamFullPath(hourWindow, this.completionSignalStreamPathFormat, this.cosmosClient);

                    ev["streamPath"] = streamPath;
                    ev["CompletionSignalStreamPath"] = completionSignalStreamPath;

                    // If stream did not exist, then create an empty file. 
                    // This will only be executed when worker is down for more than one hour.
                    if (!await this.cosmosClient.StreamExistsAsync(streamPath).ConfigureAwait(false))
                    {
                        await this.cosmosClient.CreateAsync(streamPath, this.streamExpirationTime, CosmosCreateStreamMode.CreateAlways).ConfigureAwait(false);
                    }

                    // Create the completion signal stream to cosmos.
                    try
                    {
                        await this.cosmosClient.CreateAsync(completionSignalStreamPath, this.streamExpirationTime, CosmosCreateStreamMode.ThrowIfExists).ConfigureAwait(false);
                    }
                    catch (StreamExistException)
                    {
                        DualLogger.Instance.Warning(nameof(CosmosStreamWriter), $"Hourly Write Completion Signal stream already exists: {completionSignalStreamPath}");
                    }
                    catch (Exception e)
                    {
                        DualLogger.Instance.Error(nameof(CosmosStreamWriter), e, $"Unable to create Completion Signal stream: {completionSignalStreamPath}");
                        throw;
                    }
                });
        }

        private static string GetCosmosStreamFullPath(
            DateTimeOffset hourWindow,
            string cosmosStreamFormat,
            ICosmosClient client)
        {
            string streamRelativePathWithFormat = string.Format(
                CultureInfo.InvariantCulture,
                cosmosStreamFormat,
                hourWindow);

            return streamRelativePathWithFormat;
        }

        private async Task CreateStreamAndAppendContents(string streamPath, byte[] content)
        {
            try
            {
                await this.cosmosClient.CreateAsync(streamPath, this.streamExpirationTime, CosmosCreateStreamMode.ThrowIfExists).ConfigureAwait(false);
            }
            catch (StreamExistException)
            {
                DualLogger.Instance.Warning(nameof(CosmosStreamWriter), $"Hourly Stream: {streamPath} already exists.");
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(CosmosStreamWriter), ex, $"Failed to create cosmos stream {streamPath}.");
                throw;
            }

            try
            {
                await this.cosmosClient.AppendAsync(streamPath, content).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(CosmosStreamWriter), ex, $"Failed to append to cosmos stream {streamPath}.");
                throw;
            }
        }
    }
}
