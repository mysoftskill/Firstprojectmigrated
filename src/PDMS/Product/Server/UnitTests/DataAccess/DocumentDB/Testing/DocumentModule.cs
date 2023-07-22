namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;

    using Microsoft.Azure.Documents;

    /// <summary>
    /// Contains helpful static functions for working with Documents in test code.
    /// </summary>
    public static class DocumentModule
    {
        /// <summary>
        /// Creates a Document that contains the provided data.
        /// Also sets the ETag property to simulate DocumentDB behavior.
        /// </summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="data">The data.</param>
        /// <returns>The document object.</returns>
        public static Document Create<T>(T data)
        {
            var serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var document = CreateFromJson(serializedData);

            document.SetPropertyValue("_etag", $"\"{Guid.NewGuid().ToString()}\"");

            return document;
        }

        /// <summary>
        /// Creates a document from the given JSON data. 
        /// The JSON data should be raw data pulled from document DB.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The document.</returns>
        public static Document CreateFromJson(string data)
        {
            var document = new Document();
            var textReader = new System.IO.StringReader(data);

            using (var jsonReader = new Newtonsoft.Json.JsonTextReader(textReader))
            {
                document.LoadFrom(jsonReader);
            }

            return document;
        }
    }
}