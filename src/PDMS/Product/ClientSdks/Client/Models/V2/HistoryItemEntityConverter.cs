namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A converter for history item entity values.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class HistoryItemEntityConverter : JsonConverter
    {
        private static readonly Lazy<IEnumerable<Type>> ConcreteEntityTypes = new Lazy<IEnumerable<Type>>(() =>
        {
            return Assembly.GetAssembly(typeof(Entity)).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(Entity)));
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="HistoryItemEntityConverter" /> class.
        /// </summary>
        public HistoryItemEntityConverter()
        {
        }
        
        /// <summary>
        /// This is not used for writes.
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Determines whether or not the type can be converted.
        /// </summary>
        /// <param name="objectType">The type to convert.</param>
        /// <returns>True if it can be converted.</returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Entity);
        }

        /// <summary>
        /// Deserializes the JSON to the correct entity type.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="existingValue">This parameter is not used.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The deserialized derived object.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);

            Entity entity;

            var typeName = jsonObject["@odata.type"]?.ToString();

            if (typeName != null)
            {
                var entityType = ConcreteEntityTypes.Value.Single(t => typeName.EndsWith(t.Name));

                entity = (Entity)Activator.CreateInstance(entityType);
            }
            else
            {
                return null;
            }

            serializer.Populate(jsonObject.CreateReader(), entity);

            return entity;
        }

        /// <summary>
        /// This method is not implemented.
        /// </summary>
        /// <param name="writer">The parameter is not used.</param>
        /// <param name="value">The parameter is not used.</param>
        /// <param name="serializer">The parameter is not used.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}