namespace PCF.FunctionalTests
{
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public class CustomJsonSerializer : IExportSerializer
    {
        /// <summary>
        ///     UTF8 no BOM Encoding
        /// </summary>
        public static readonly Encoding Utf8NoBom = new UTF8Encoding(false, true);

        private static readonly byte[] FileEntryDelimiter = Utf8NoBom.GetBytes(",");

        private static readonly byte[] FilePostfix = Utf8NoBom.GetBytes(string.Empty);

        private static readonly byte[] FilePrefix = Encoding.UTF8.GetBytes(string.Empty);

        public CustomJsonSerializer()
        {
        }

        /// <inheritdoc />
        public void Serialize(DateTimeOffset timestamp, string correlationId, object value, Stream stream)
        {
            byte[] bytes = Utf8NoBom.GetBytes(timestamp.ToString());
            bytes = Utf8NoBom.GetBytes(correlationId.ToString());
            bytes = Utf8NoBom.GetBytes(value.ToString());

            stream.Write(bytes, 0, bytes.Length);
        }

        /// <inheritdoc />
        [Obsolete("Use the Serialize() overload that provides timestamp and correlationId from your data")]
        public void Serialize(object value, Stream stream)
        {
            this.Serialize(DateTimeOffset.UtcNow, string.Empty, value, stream);
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
    }
}
