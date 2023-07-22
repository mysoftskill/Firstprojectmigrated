// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Service.CommandQueue
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;

    using Microsoft.Azure.Storage.Queue;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    using Newtonsoft.Json;

    internal class QueueStorageCommandConverter
    {
        private static readonly ICompressionAlgorithm BrotliCompressionAlgorithm = new BrotliCompressionAlgorithm();

        private const CompressionLevel DefaultCompressionLevel = CompressionLevel.Fastest;
        
        // Reserve bytes to tell us the compression algorithm.
        private const int ReservedBytes = sizeof(int);

        private const CompressionType DefaultCompressionType = CompressionType.Brotli;

        public static StoragePrivacyCommand FromCloudQueueMessage(CloudQueueMessage message)
        {
            byte[] messageBytes = message.AsBytes;

            // Reserved bytes inform compression type used.
            CompressionType compressionType = (CompressionType)(BitConverter.ToInt32(messageBytes.Take(ReservedBytes).ToArray(), 0));

            using (MemoryStream stream = new MemoryStream(message.AsBytes.Skip(ReservedBytes).ToArray()))
            {
                StoragePrivacyCommand storageCommand;
                switch (compressionType)
                {
                    case CompressionType.Brotli:
                        storageCommand = BrotliCompressionAlgorithm.DecompressJson<StoragePrivacyCommand>(stream);
                        break;

                    case CompressionType.None:
                    default:
                        using (StreamReader sr = new StreamReader(stream))
                        using (JsonReader jr = new JsonTextReader(sr))
                        {
                            storageCommand = new JsonSerializer().Deserialize<StoragePrivacyCommand>(jr);
                        }

                        break;
                }

                // This 'NextVisibleTime' property is nullable. Not sure when it can be null, couldn't find Azure documentation
                if (message.NextVisibleTime.HasValue)
                {
                    storageCommand.UnixNextVisibleTimeSeconds = message.NextVisibleTime.Value.ToUnixTimeSeconds();
                }

                return storageCommand;
            }
        }

        public static CloudQueueMessage ToCloudQueueMessage(StoragePrivacyCommand storagePrivacyCommand, string messageId = null, string popReceipt = null, CompressionType compressionType = DefaultCompressionType)
        {
            byte[] messageContent = ConvertToBytes(storagePrivacyCommand, compressionType);

            if (string.IsNullOrWhiteSpace(messageId) && string.IsNullOrWhiteSpace(popReceipt))
            {
                return new CloudQueueMessage(messageContent);
            }
            else if (string.IsNullOrWhiteSpace(messageId) || string.IsNullOrWhiteSpace(popReceipt))
            {
                DualLogger.Instance.Error(nameof(QueueStorageCommandConverter), $"One of these values was null or whitespace. {nameof(messageId)}: {messageId}, {nameof(popReceipt)}:{popReceipt}. " +
                                  $"Conversion will create a new {nameof(CloudQueueMessage)} with neither of the provided {nameof(messageId)} nor {nameof(popReceipt)}");
                return new CloudQueueMessage(messageContent);
            }
            else
            {
                var message = new CloudQueueMessage(messageId, popReceipt);
                message.SetMessageContent2(messageContent);
                return message;
            }
        }

        private static byte[] ConvertToBytes(StoragePrivacyCommand storagePrivacyCommand, CompressionType compressionType)
        {
            byte[] reservedBytesPrefix;
            byte[] messageContent;
            switch (compressionType)
            {
                case CompressionType.Brotli:
                    reservedBytesPrefix = BitConverter.GetBytes((int)compressionType);
                    messageContent = BrotliCompressionAlgorithm.CompressJson(storagePrivacyCommand, DefaultCompressionLevel);
                    return reservedBytesPrefix.Concat(messageContent).ToArray();

                case CompressionType.None:
                default:
                    reservedBytesPrefix = BitConverter.GetBytes((int)compressionType);
                    string serializedCommand = JsonConvert.SerializeObject(storagePrivacyCommand);
                    messageContent = Encoding.UTF8.GetBytes(serializedCommand);
                    return reservedBytesPrefix.Concat(messageContent).ToArray();
            }
        }
    }

    public enum CompressionType : int
    {
        None,
        Brotli
    }
}
