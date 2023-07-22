namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Net;
    using System.Reflection;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    using Newtonsoft.Json;

    /// <summary>
    /// Contains helpful static functions for working with ResourceResponse in test code.
    /// </summary>
    public static class ResourceResponseModule
    {
        /// <summary>
        /// Uses reflection to create a resource response with specific headers.
        /// Only use this if you need to set headers, otherwise, rely on the existing constructors.
        /// </summary>
        /// <typeparam name="T">Data type to deserialize the response.</typeparam>
        /// <param name="data">The data to serialize.</param>
        /// <param name="headers">The headers.</param>
        /// <returns>The ResourceResponse with the correct headers.</returns>
        public static ResourceResponse<T> Create<T>(object data, NameValueCollection headers)
            where T : Resource, new()
        {
            var resourceResponseType = Type.GetType("Microsoft.Azure.Documents.Client.ResourceResponse`1, Microsoft.Azure.Documents.Client");
            var serviceResponse = CreateServiceResponse(Serialize(data), headers, HttpStatusCode.OK);
            var arguments = new object[] { serviceResponse, null };

            var t = resourceResponseType.MakeGenericType(typeof(T));
            var resourceResponse = t.GetTypeInfo().GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0].Invoke(arguments);
            return (ResourceResponse<T>)resourceResponse;
        }

        private static object CreateServiceResponse(Stream body, NameValueCollection headers, HttpStatusCode statusCode)            
        {
            var documentServiceType = Type.GetType("Microsoft.Azure.Documents.DocumentServiceResponse, Microsoft.Azure.Documents.Client");
            var headersDictionaryType = Type.GetType("Microsoft.Azure.Documents.Collections.DictionaryNameValueCollection, Microsoft.Azure.Documents.Client");
            var headersDictionaryInstance = Activator.CreateInstance(headersDictionaryType, headers);
            var arguments = new object[] { body, headersDictionaryInstance, statusCode, null };

            var documentServiceResponse = documentServiceType.GetTypeInfo().GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0].Invoke(arguments);
            return documentServiceResponse;
        }

        private static Stream Serialize(object value)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(writer);
            
            JsonSerializer ser = new JsonSerializer();
            ser.Serialize(jsonWriter, value);
            jsonWriter.Flush();            

            return stream;
        }
    }
}