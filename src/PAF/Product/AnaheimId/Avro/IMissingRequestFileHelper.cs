namespace Microsoft.PrivacyServices.AnaheimId.Avro
{
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Anaheim Id Missing Request File Reader.
    /// </summary>
    public interface IMissingRequestFileHelper
    {
        /// <summary>
        /// Collect request ids using the <see cref="MissingRequestFileHelper"/> class.
        /// </summary>
        /// <param name="stream">stream for avro formatted file.</param>
        /// <param name="maxRequestIds">the maximum number of request ids to return.</param>
        /// <returns>Collect a list of request ids as longs with a max of ten ids.</returns>
        List<long> CollectRequestIds(Stream stream, int maxRequestIds);
    }
}
