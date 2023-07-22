namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection;

    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// Contains helpful static functions for working with FeedResponse in test code.
    /// </summary>
    public static class FeedResponseModule
    {
        /// <summary>
        /// Uses reflection to create a feed response with specific headers.
        /// Only use this if you need to set headers, otherwise, rely on the existing constructors.
        /// </summary>
        /// <typeparam name="T">Data type to for the response.</typeparam>
        /// <param name="data">The data to inject.</param>
        /// <param name="headers">The headers.</param>
        /// <returns>The FeedResponse with the correct headers.</returns>
        public static FeedResponse<T> Create<T>(IEnumerable<T> data, NameValueCollection headers)
        {
            var feedResponseType = Type.GetType("Microsoft.Azure.Documents.Client.FeedResponse`1, Microsoft.Azure.Documents.Client");
            var headersDictionaryType = Type.GetType("Microsoft.Azure.Documents.Collections.DictionaryNameValueCollection, Microsoft.Azure.Documents.Client");

            var headersDictionaryInstance = Activator.CreateInstance(headersDictionaryType, headers);
            var arguments = new object[] { data, data.Count(), headersDictionaryInstance, false, null, null, null, 0 };

            var t = feedResponseType.MakeGenericType(typeof(T));
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var feedResponse = t.GetTypeInfo().GetConstructors(flags)[0].Invoke(arguments);
            return (FeedResponse<T>)feedResponse;
        }
    }
}