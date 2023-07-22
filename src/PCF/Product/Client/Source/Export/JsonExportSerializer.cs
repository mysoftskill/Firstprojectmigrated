// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    /// <summary>
    ///     The JSON.NET export serializer.
    /// </summary>
    public class JsonExportSerializer : IExportSerializer
    {
        /// <summary>
        ///     UTF8 no BOM Encoding
        /// </summary>
        public static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);

        private static readonly byte[] FileEntryDelimiter = Utf8NoBom.GetBytes(",");

        private static readonly byte[] FilePostfix = Utf8NoBom.GetBytes("]");

        private static readonly byte[] FilePrefix = Encoding.UTF8.GetBytes("[");

        private readonly JsonSerializerSettings settings;

        /// <summary>
        ///     Creates a <see cref="JsonExportSerializer" />.
        /// </summary>
        public JsonExportSerializer(CommandFeedLogger logger)
        {
            this.settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Include,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                TypeNameHandling = TypeNameHandling.None,
                Error = logger.SerializationError,
                Formatting = Formatting.Indented,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
        }

        /// <inheritdoc />
        [Obsolete("Use the Serialize() overload that provides timestamp and correlationId from your data")]
        public void Serialize(object value, Stream stream)
        {
            this.Serialize(DateTimeOffset.UtcNow, string.Empty, value, stream);
        }

        /// <inheritdoc />
        public void Serialize(DateTimeOffset timestamp, string correlationId, object value, Stream stream)
        {
            string json = JsonConvert.SerializeObject(new ShoeboxEntry(timestamp, correlationId, value), this.settings);
            byte[] bytes = Utf8NoBom.GetBytes(json);

            stream.Write(bytes, 0, bytes.Length);
        }

        /// <inheritdoc />
        public async Task WriteEntryDelimiterAsync(IExportFile file)
        {
            using (var stream = new MemoryStream(FileEntryDelimiter))
            {
                await file.AppendAsync(stream).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task WriteEntryDelimiterAsync(Stream stream)
        {
            await stream.WriteAsync(FileEntryDelimiter, 0, FileEntryDelimiter.Length).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task WriteFilePostfixAsync(IExportFile file)
        {
            using (var stream = new MemoryStream(FilePostfix))
            {
                await file.AppendAsync(stream).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task WriteFilePostfixAsync(Stream stream)
        {
            await stream.WriteAsync(FileEntryDelimiter, 0, FilePostfix.Length).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task WriteFilePrefixAsync(IExportFile file)
        {
            using (var stream = new MemoryStream(FilePrefix))
            {
                await file.AppendAsync(stream).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task WriteFilePrefixAsync(Stream stream)
        {
            await stream.WriteAsync(FileEntryDelimiter, 0, FilePrefix.Length).ConfigureAwait(false);
        }

        private class ShoeboxEntry
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Serialized member")]
            [JsonProperty("correlationId", Order = 1)]
            public string CorrelationId { get; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Serialized member")]
            [JsonProperty("properties", Order = 2)]
            public object Data { get; }

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Serialized member")]
            [JsonProperty("time", Order = 0)]
            public DateTimeOffset Timestamp { get; }

            public ShoeboxEntry(DateTimeOffset timestamp, string correlationId, object data)
            {
                this.Data = data;
                this.Timestamp = timestamp;
                this.CorrelationId = correlationId;
            }
        }
    }
}
