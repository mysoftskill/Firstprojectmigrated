// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    ///     This represents a particular kind of serialization, such as JSON.
    /// </summary>
    public interface IExportSerializer
    {
        /// <summary>
        ///     Serializes data into a stream.
        /// </summary>
        /// <param name="value">The object to be serialized.</param>
        /// <param name="stream">The stream to serialize to.</param>
        [Obsolete("Use the Serialize() overload that provides timestamp and correlationId from your data")]
        void Serialize(object value, Stream stream);

        /// <summary>
        ///     Serializes data into a stream.
        /// </summary>
        /// <param name="timestamp">The timestamp of the value.</param>
        /// <param name="correlationId">The correlation id of the value.</param>
        /// <param name="value">The object to be serialized.</param>
        /// <param name="stream">The stream to serialize to.</param>
        void Serialize(DateTimeOffset timestamp, string correlationId, object value, Stream stream);

        /// <summary>
        ///     Writes a delimiter between entries, if one is required.
        /// </summary>
        /// <param name="file">The file to write the delimiter to.</param>
        /// <returns>A task that completes when the delimiter has been written.</returns>
        Task WriteEntryDelimiterAsync(IExportFile file);

        /// <summary>
        ///     Writes a delimiter between entries, if one is required.
        /// </summary>
        /// <param name="stream">The stream to write the delimiter to.</param>
        /// <returns>A task that completes when the delimiter has been written.</returns>
        Task WriteEntryDelimiterAsync(Stream stream);

        /// <summary>
        ///     Writes the file closing postfix, such as the end of array marker, if this serialization requires it.
        /// </summary>
        /// <param name="file">The file to write the postfix to.</param>
        /// <returns>A task that completes when the postfix has been written.</returns>
        Task WriteFilePostfixAsync(IExportFile file);

        /// <summary>
        ///     Writes the file closing postfix, such as the end of array marker, if this serialization requires it.
        /// </summary>
        /// <param name="stream">The stream to write the postfix to.</param>
        /// <returns>A task that completes when the postfix has been written.</returns>
        Task WriteFilePostfixAsync(Stream stream);

        /// <summary>
        ///     Writes the file opening prefix, such as the beginning of array marker, if this serialization requires it.
        /// </summary>
        /// <param name="file">The file to write the prefix to.</param>
        /// <returns>A task that completes when the prefix has been written.</returns>
        Task WriteFilePrefixAsync(IExportFile file);

        /// <summary>
        ///     Writes the file opening prefix, such as the beginning of array marker, if this serialization requires it.
        /// </summary>
        /// <param name="stream">The stream to write the prefix to.</param>
        /// <returns>A task that completes when the prefix has been written.</returns>
        Task WriteFilePrefixAsync(Stream stream);
    }
}
