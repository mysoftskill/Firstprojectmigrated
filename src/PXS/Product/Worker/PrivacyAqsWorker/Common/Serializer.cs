// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Common
{
    using System.IO;
    using System.Text;
    using System.Xml.Serialization;

    internal static class Serializer
    {
        /// <summary>
        ///     Deserializes an XML string to an Object
        /// </summary>
        /// <typeparam name="T"> The XML object to deserialize to </typeparam>
        /// <param name="xmlString"> The XML string </param>
        /// <returns> The deserialized object </returns>
        internal static T Deserialize<T>(string xmlString)
        {
            return Deserialize<T>(Encoding.UTF8.GetBytes(xmlString));
        }

        /// <summary>
        ///     Deserializes an XML byte encoded string to an Object
        /// </summary>
        /// <typeparam name="T"> The XML object to deserialize to </typeparam>
        /// <param name="bytes"> The XML string </param>
        /// <returns> THe deserialized object </returns>
        internal static T Deserialize<T>(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                var xmlSerializer = new XmlSerializer(typeof(T));
                return (T)xmlSerializer.Deserialize(stream);
            }
        }
    }
}
