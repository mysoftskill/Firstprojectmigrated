namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;

    using Newtonsoft.Json;

    /// <summary>
    /// Utility class to help compress and decompress data.
    /// </summary>
    /// <remarks>
    /// Stolen from DDS.
    /// </remarks>
    public static class CompressionTools
    {
        private static readonly JsonSerializer JsonSerializer = new JsonSerializer();

        private static readonly ConcurrentQueue<MemoryStream> MemoryStreamPool = new ConcurrentQueue<MemoryStream>();

        /// <summary>
        /// The brotli compression algorithm. Brotli gives better performance and faster compression than Gzip.
        /// Howerver, Brotli does not come with a framing format, so it may be necessary to record the encoding
        /// elsewhere.
        /// </summary>
        public static ICompressionAlgorithm Brotli { get; } = new BrotliCompressionAlgorithm();

        /// <summary>
        /// The Gzip compression format, based on the DEFLATE algorithm. Gzip data contains a checksum
        /// as well as a framing format header to allow for quick detection of non-gzip data.
        /// </summary>
        public static ICompressionAlgorithm Gzip { get; } = new GzipCompressionAlgorithm();

        /// <summary>
        /// Compresses the given string, and returns the value as a compressed base 64-encoded byte array.
        /// </summary>
        public static string CompressString(
            this ICompressionAlgorithm algorithm,
            string data, 
            CompressionLevel compressionLevel = CompressionLevel.Fastest)
        {
            return Convert.ToBase64String(Compress(algorithm, Encoding.UTF8.GetBytes(data), compressionLevel));
        }

        /// <summary>
        /// Serializes the given object as JSON, then compresses and returns a byte array.
        /// </summary>
        public static byte[] CompressJson(
            this ICompressionAlgorithm algorithm,
            object data,
            CompressionLevel compressionLevel = CompressionLevel.Fastest)
        {
            MemoryStream ms = null;
            try
            {
                ms = TakeStream();
                CompressJson(algorithm, data, ms, compressionLevel);
                return ms.ToArray();
            }
            finally
            {
                if (ms != null)
                {
                    ReturnStream(ms);
                }
            }
        }

        /// <summary>
        /// Serializes the given object as JSON, then compresses to the given stream.
        /// </summary>
        public static void CompressJson(
            this ICompressionAlgorithm algorithm,
            object data,
            Stream destinationStream,
            CompressionLevel compressionLevel = CompressionLevel.Fastest)
        {
            using (var compressor = algorithm.GetCompressionStream(destinationStream, compressionLevel))
            using (var streamWriter = new StreamWriter(compressor, Encoding.UTF8, 4096, true))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                JsonSerializer.Serialize(jsonWriter, data);
            }
        }

        /// <summary>
        /// Decompresses the given byte array into <typeparamref name="T"/> using JSON.NET.
        /// </summary>
        public static T DecompressJson<T>(
            this ICompressionAlgorithm algorithm,
            byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            {
                return DecompressJson<T>(algorithm, memoryStream);
            }
        }

        /// <summary>
        /// Decompresses the given stream into <typeparamref name="T"/> using JSON.NET.
        /// </summary>
        public static T DecompressJson<T>(
            this ICompressionAlgorithm algorithm,
            Stream sourceStream)
        {
            using (var decompressor = algorithm.GetDecompressionStream(sourceStream))
            using (var streamReader = new StreamReader(decompressor))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                T value = JsonSerializer.Deserialize<T>(jsonReader);
                return value;
            }
        }

        /// <summary>
        /// Compresses the data.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static byte[] Compress(
            this ICompressionAlgorithm algorithm,
            byte[] data, 
            CompressionLevel compressionLevel = CompressionLevel.Fastest)
        {
            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "CompressionOperations").Increment(algorithm.Name);

            MemoryStream outputStream = null;
            try
            {
                outputStream = TakeStream();
                using (Stream compressor = algorithm.GetCompressionStream(outputStream, compressionLevel))
                {
                    compressor.Write(data, 0, data.Length);
                    compressor.Close();
                    return outputStream.ToArray();
                }
            }
            finally
            {
                ReturnStream(outputStream);
            }
        }

        /// <summary>
        /// Tries to decompress the data if possible.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Can't predict the type of exception.")]
        public static bool TryDecompress(this ICompressionAlgorithm algorithm, byte[] data, out byte[] decompressedData)
        {
            decompressedData = null;

            try
            {
                decompressedData = Decompress(algorithm, data);
                return true;
            }
            catch (Exception ex)
            {
                PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DecompressionErrors")
                                  .Increment($"{algorithm.Name}.{ex.GetType().Name}");

                return false;
            }
        }

        /// <summary>
        /// Treats the given string as base64-encoded, and decompresses.
        /// </summary>
        public static string DecompressString(this ICompressionAlgorithm algorithm, string data)
        {
            return Encoding.UTF8.GetString(Decompress(algorithm, Convert.FromBase64String(data)));
        }

        /// <summary>
        /// Decompresses the data.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public static byte[] Decompress(this ICompressionAlgorithm algorithm, byte[] data)
        {
            PerfCounterUtility.GetOrCreate(PerformanceCounterType.Rate, "DecompressionOperations").Increment(algorithm.Name);

            MemoryStream outputStream = null;
            try
            {
                outputStream = TakeStream();

                using (MemoryStream inputStream = new MemoryStream(data))
                using (Stream decompressor = algorithm.GetDecompressionStream(inputStream))
                {
                    decompressor.CopyTo(outputStream);
                    return outputStream.ToArray();
                }
            }
            finally
            {
                ReturnStream(outputStream);
            }
        }

        private static MemoryStream TakeStream()
        {
            MemoryStream stream;
            if (!MemoryStreamPool.TryDequeue(out stream))
            {
                stream = new MemoryStream();
            }

            return stream;
        }

        private static void ReturnStream(MemoryStream ms)
        {
            if (ms != null)
            {
                ms.Position = 0;
                ms.SetLength(0);

                MemoryStreamPool.Enqueue(ms);
            }
        }
    }
}
