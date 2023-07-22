namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This adds support for writing the <c>@odata.type</c> property.
    /// This separate converter is necessary because JToken.FromObject
    /// will try to use DerivedTypeConverter for writes, 
    /// and that would make an infinitely recursive loop.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "TypeWriter")]
    public class DerivedTypeWriterConverter : DerivedTypeConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedTypeWriterConverter" /> class.
        /// </summary>
        /// <param name="namespacePrefix">The OData namespace prefix.</param>
        public DerivedTypeWriterConverter(string namespacePrefix) : base(namespacePrefix)
        {
        }

        /// <summary>
        /// Override the fact that writes are disabled in the base class.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Writes the value and injects the <c>@odata.type</c> property.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Remove the DerivedTypeContractResolver to avoid recursive loop.
            var oldResolver = serializer.ContractResolver;
            serializer.ContractResolver = null;

            JObject jobject = JObject.FromObject(value, serializer);

            // Add back the converter for subsequent objects.
            serializer.ContractResolver = oldResolver;

            // Find the appropriate mapping and add the property.
            var mapping = this.GetMapping(value.GetType()).Single(m => m.Item2 == value.GetType());
            jobject.AddFirst(new JProperty("@odata.type", mapping.Item1));

            jobject.WriteTo(writer);
        }
    }
}