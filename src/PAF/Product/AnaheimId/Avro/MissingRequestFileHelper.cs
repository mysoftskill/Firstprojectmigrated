namespace Microsoft.PrivacyServices.AnaheimId.Avro
{
    using System.Collections.Generic;
    using System.IO;
    using global::Avro.File;
    using global::Avro.Generic;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// Anaheim Id Missing Request File Reader.
    /// </summary>
    public class MissingRequestFileHelper : IMissingRequestFileHelper
    {
        /// <summary>
        /// Collect request ids using the <see cref="MissingRequestFileHelper"/> class.
        /// </summary>
        /// <param name="stream">stream for avro formatted file.</param>
        /// <param name="maxRequestIds">the maximum number of request ids to return.</param>
        /// <returns>Collect a list of request ids as longs with a max of ten ids.</returns>
        public List<long> CollectRequestIds(Stream stream, int maxRequestIds)
        {
            List<long> values = new List<long>();
            using (var reader = DataFileReader<GenericRecord>.OpenReader(stream))
            {
                int i = 0;
                while (reader.HasNext() && i < maxRequestIds)
                {
                    GenericRecord record = reader.Next();
                    string body = (string)record["Body"];
                    DeleteDeviceIdRequest anaheimIdRequest = JsonConvert.DeserializeObject<DeleteDeviceIdRequest>(body);
                    values.Add(anaheimIdRequest.GlobalDeviceId);
                    i++;
                }
            }

            return values;
        }
    }
}
