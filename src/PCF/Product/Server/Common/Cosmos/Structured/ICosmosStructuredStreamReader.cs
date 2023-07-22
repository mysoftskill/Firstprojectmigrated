namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Cosmos.Structured
{
    using System;

    /// <summary>
    /// Defines a Cosmos Structured StreamReader
    /// </summary>
    public interface ICosmosStructuredStreamReader : IDisposable
    {
        /// <summary>
        /// Reads the named column at the current cursor.
        /// </summary>
        T GetValue<T>(string columnName);

        /// <summary>
        /// Tries to get the column value at the current cursor.
        /// </summary>
        bool TryGetValue<T>(string columnName, out T value);

        /// <summary>
        /// Reads the Json from the named column at the current cursor and returns the object
        /// </summary>
        T GetJsonValue<T>(string columnName);

        /// <summary>
        /// Tries to get the Json from the named column at the current cursor and returns the object
        /// </summary>
        bool TryGetJsonValue<T>(string columnName, out T value);

        /// <summary>
        /// Advances the current cursor.
        /// </summary>
        bool MoveNext();

        /// <summary>
        /// Stream that is being accessed
        /// </summary>
        string CosmosStream { get; }

        /// <summary>
        /// Gets the last modified time of the stream.
        /// </summary>
        DateTimeOffset LastModifiedTime { get; }
    }
}
