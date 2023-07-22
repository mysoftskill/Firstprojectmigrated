namespace Microsoft.Azure.ComplianceServices.Common
{
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CsvHelper;
    using CsvHelper.Configuration;
    using Newtonsoft.Json;

    /// <summary>
    /// Writes JSON data to CSV format.
    /// </summary>
    /// <remarks>
    /// Converts standard JSON to CSV format.
    /// 
    /// Sample JSON:
    /// [
    ///     {
    ///         "time": "2010-07-15",
    ///         "correlationId: "id1",
    ///         "properties": {
    ///             "time": "2010-07-15",
    ///             "location": "0, 0",
    ///             "region": "USA",
    ///             "categories": [
    ///                 {
    ///                     name: "QA"
    ///                 },
    ///                 {
    ///                     name: "Prod"
    ///                 }
    ///             ]
    ///         }
    ///     },
    ///     {
    ///         "time": "2010-07-15T00:45:30",
    ///         "correlationId: "id2",
    ///         "properties": {
    ///             "time": "2010-07-15T00:45:30-07:00",
    ///             "location": "0.0, 4.5",
    ///             "categories": [
    ///                 {
    ///                     name: "Dev"
    ///                 },
    ///                 {
    ///                     name: "Test"
    ///                 }
    ///             ]
    ///         }
    ///     }
    /// ]
    /// 
    /// Resulting CSV output:
    /// time,correlationId,"properties/time","properties/location","properties/region","properties/categories/0/name","properties/categories/1/name",
    /// 2010-07-15,id1,2010-07-15,"0, 0",USA,QA,Prod
    /// 2010-07-15T00:45:30,id2,2009-06-15T13:45:30-07:00,"0.0, 4.5",,Dev,Test
    /// 
    /// </remarks>
    public class JsonToCsvWriter : IDisposable
    {
        private bool disposedValue;
        private Stream sourceStream;
        private Stream destinationStream;
        private readonly bool leaveOpen;

        private readonly CsvConfiguration csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            ShouldQuote = (args) =>
            {
                if (args.Field.Contains("\"") ||
                    args.Field.Contains(",") ||
                    args.Field.Contains("/") ||
                    args.Field.Contains("\n"))
                {
                    return true;
                }

                return false;
            }
        };

        private readonly JsonSerializer serializer = new JsonSerializer()
        {
            DateParseHandling = DateParseHandling.DateTimeOffset
        };

        private readonly List<string> headers = new List<string>();

        public JsonToCsvWriter(Stream source, Stream destination)
        {
            this.sourceStream = source;
            this.destinationStream = destination;
        }

        public JsonToCsvWriter(Stream source, Stream destination, bool leaveOpen)
        {
            this.sourceStream = source;
            this.destinationStream = destination;
            this.leaveOpen = leaveOpen;
        }

        public async Task WriteAsync(CancellationToken cancellationToken)
        {
            using (var reader = new StreamReader(sourceStream, Encoding.UTF8, false, 4096, true))
            using (var destination = new StreamWriter(destinationStream, Encoding.UTF8, 4096, true))
            using (var writer = new CsvWriter(destination, this.csvConfiguration))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    while (await jsonReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        // Compose headers
                        // Make sure at the top level we iterate over each object
                        // as opposed to the array to avoid deserializing the entire payload.
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            void visitor(string key, dynamic val)
                            {
                                if (!this.headers.Contains(key))
                                {
                                    this.headers.Add(key);
                                }
                            }

                            var item = this.serializer.Deserialize<ExpandoObject>(jsonReader);
                            await TraverseObject(string.Empty, item, visitor, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                this.sourceStream.Position = 0;

                using (var jsonReader = new JsonTextReader(reader))
                {
                    while (await jsonReader.ReadAsync(cancellationToken))
                    {
                        if (jsonReader.TokenType == JsonToken.StartObject)
                        {
                            // Build record
                            var item = this.serializer.Deserialize<ExpandoObject>(jsonReader);
                            var record = new ExpandoObject() as IDictionary<string, dynamic>;

                            foreach (var header in this.headers)
                            {
                                dynamic columnValue = null;
                                void visitor(string key, dynamic val)
                                {
                                    if (string.Equals(key, header))
                                    {
                                        columnValue = val;
                                    }
                                }

                                await TraverseObject(string.Empty, item, visitor, cancellationToken).ConfigureAwait(false);
                                record[header] = columnValue;
                            }

                            writer.WriteRecord(record);
                            writer.NextRecord();
                        }
                    }
                }
            }
        }

        public async Task TraverseList(string key, IList<dynamic> items, Action<string, dynamic> visitor, CancellationToken cancellationToken)
        {
            var keyIndex = 0;
            foreach (var item in items)
            {
                var subKey = string.IsNullOrEmpty(key) ? $"{keyIndex}" : $"{key}/{keyIndex}";
                await TraverseObject(subKey, item, visitor, cancellationToken).ConfigureAwait(false);
                keyIndex++;
            }
        }

        public async Task TraverseObject(string key, dynamic item, Action<string, dynamic> visitor, CancellationToken cancellationToken)
        {
            if (!(item is IDictionary<string, dynamic> properties))
            {
                visitor?.Invoke(key, item);
            }
            else
            {
                foreach (var property in properties)
                {
                    if (property.Value is IList<dynamic> listOfValues)
                    {
                        await TraverseList(property.Key, listOfValues, visitor, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var subKey = string.IsNullOrEmpty(key) ? $"{property.Key}" : $"{key}/{property.Key}";
                        await TraverseObject(subKey, property.Value, visitor, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!leaveOpen)
                    {
                        this.sourceStream?.Dispose();
                        this.destinationStream?.Dispose();
                    }
                }

                this.sourceStream = null;
                this.destinationStream = null;
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
