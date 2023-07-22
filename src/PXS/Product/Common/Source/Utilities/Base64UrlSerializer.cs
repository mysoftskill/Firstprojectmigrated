// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Text;

    using Newtonsoft.Json;

    /// <summary>
    ///     JSON Serialization, GZipping, and Base64 encoding to be URL friendly.
    /// </summary>
    /// <remarks>
    ///     This code was shamelessly lifted from System.IdentityModel.Tokens.Jwt NuGet. But I've added Json
    ///     serialization and gzipping together in a single call to take any object to a url parameter
    ///     representation
    /// </remarks>
    public static class Base64UrlSerializer
    {
        private const char Character62 = '+';

        private const char Character63 = '/';

        private const char PadCharacter = '=';

        private const char UrlCharacter62 = '-';

        private const char UrlCharacter63 = '_';

        /// <summary>
        ///     Decodes a string that comes from <see cref="Base64UrlSerializer.Encode" />
        /// </summary>
        public static T Decode<T>(string input, JsonSerializerSettings settings = null)
        {
            input = input.Replace(UrlCharacter62, Character62);
            input = input.Replace(UrlCharacter63, Character63);
            switch (input.Length % 4)
            {
                case 0:
                    break;
                case 2:
                    input += PadCharacter;
                    input += PadCharacter;
                    break;
                case 3:
                    input += PadCharacter;
                    break;
                default:
                    throw new FormatException($"Unable to decode: '{input}' as Base64Url encoded string.");
            }

            using (var inStream = new MemoryStream(Convert.FromBase64String(input)))
            using (var gzStream = new GZipStream(inStream, CompressionMode.Decompress, true))
            using (var outStream = new MemoryStream())
            {
                gzStream.CopyTo(outStream);

                string json = Encoding.UTF8.GetString(outStream.ToArray());
                return settings == null ? JsonConvert.DeserializeObject<T>(json) : JsonConvert.DeserializeObject<T>(json, settings);
            }
        }

        /// <summary>
        ///     Encodes an object into a url-friendly base64 representation
        /// </summary>
        public static string Encode<T>(T data, JsonSerializerSettings settings = null)
        {
            string json = JsonConvert.SerializeObject(data, typeof(T), settings);

            using (var inStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            using (var outStream = new MemoryStream())
            {
                using (var gzStream = new GZipStream(outStream, CompressionLevel.Optimal, true))
                {
                    inStream.CopyTo(gzStream);
                }

                return Convert.ToBase64String(outStream.ToArray())
                    .Split(PadCharacter)[0]
                    .Replace(Character62, UrlCharacter62)
                    .Replace(Character63, UrlCharacter63);
            }
        }
    }
}
