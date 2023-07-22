// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Serialization;
    using Newtonsoft.Json;

    internal static class SerializationHelper
    {
        #region JSON

        private static readonly List<MediaTypeFormatter> JsonMediaFormatterSettings =
            new List<MediaTypeFormatter>
            {
                new JsonMediaTypeFormatter
                {
                    SerializerSettings = new JsonSerializerSettings
                    {
                        // Throws exception if a JSON property doesn't match to a member
                        MissingMemberHandling = MissingMemberHandling.Error
                    }
                }
            };

        /// <summary>
        /// Tries to deserialize an HTTP body content to the specified type and returns a result with either the successfully
        /// deserialized object or a populated error.
        /// </summary>
        /// <typeparam name="T">The type to deserialize to.</typeparam>
        /// <param name="responseContent">The HTTP body content to deserialize.</param>
        /// <param name="throwErrorOnMissingMember">Specifies handling of unrecognized information in the stream being deserialized.</param>
        /// <returns>A SerializationResult object with either error or result populated.</returns>
        public static async Task<SerializationResult<T>> DeserializeResponseContent<T>(
            HttpContent responseContent, 
            bool throwErrorOnMissingMember = true)
            where T : class
        {
            if (responseContent == null)
            {
                return new SerializationResult<T>(ErrorHelper.EmptyResponseContentMessage);
            }

            try
            {
                T deserializeResult = throwErrorOnMissingMember
                    ? await responseContent.ReadAsAsync<T>(JsonMediaFormatterSettings)
                    : await responseContent.ReadAsAsync<T>();

                // Deserialization may result in null result
                if (deserializeResult == null)
                {
                    return new SerializationResult<T>(ErrorHelper.EmptyResponseMessage);
                }

                return new SerializationResult<T>(deserializeResult);
            }
            catch (UnsupportedMediaTypeException e)
            {
                return new SerializationResult<T>(string.Format(ErrorHelper.InvalidContentTypeMessage, e));
            }
            catch (JsonSerializationException e)
            {
                return new SerializationResult<T>(string.Format(ErrorHelper.DeserializeErrorMessage, e));
            }
            catch (JsonReaderException e)
            {
                return new SerializationResult<T>(string.Format(ErrorHelper.DeserializeErrorMessage, e));
            }
        }

        /// <summary>
        /// Serializes the object to string HttpContent using Json.NET serializer.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <returns>The HttpContent.</returns>
        public static HttpContent SerializeToHttpContent(object value)
        {
            string serializationResult = JsonConvert.SerializeObject(value);
            return new StringContent(serializationResult, Encoding.UTF8, HeaderValues.AcceptJson);
        }

        #endregion

        #region XML

        /// <summary>
        /// Serializes the object to string using XmlSerializer.
        /// </summary>
        /// <typeparam name="T">Type of object to be serialized.</typeparam>
        /// <param name="input">object instance.</param>
        /// <returns>Serialized string.</returns>
        public static string XmlSerialize<T>(T input)
        {
            string serializedString;
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                serializer.Serialize(stringWriter, input);
                serializedString = stringWriter.ToString();
            }

            return serializedString;
        }

        /// <summary>
        /// Deserializes a string using XmlSerializer.
        /// </summary>
        /// <typeparam name="T">Type of object to be deserialized.</typeparam>
        /// <param name="input">The string to be deserialized.</param>
        /// <returns>Deserialized object.</returns>
        public static T XmlDeserialize<T>(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return default(T);   
            }

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (StringReader stringReader = new StringReader(input))
            {
                XmlReader reader = XmlReader.Create(stringReader, new XmlReaderSettings { XmlResolver = null });
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Serializes the object to string using DataContractSerializer.
        /// </summary>
        /// <typeparam name="T">Typeo of object to be serialized.</typeparam>
        /// <param name="input">object instance.</param>
        /// <returns>Serialized string.</returns>
        public static string DataContractSerialize<T>(T input)
        {
            DataContractSerializer dataContractSerializer = new DataContractSerializer(typeof(T));
            string serializedString;
            MemoryStream memoryStream = null;

            try
            {
                memoryStream = new MemoryStream();
                dataContractSerializer.WriteObject(memoryStream, input);

                memoryStream.Seek(0, SeekOrigin.Begin);
                using (StreamReader streamReader = new StreamReader(memoryStream))
                {
                    memoryStream = null;
                    serializedString = streamReader.ReadToEnd();
                }
            }
            finally
            {
                if (null != memoryStream)
                {
                    memoryStream.Dispose();
                }
            }

            return serializedString;
        }

        #endregion
    }
}
