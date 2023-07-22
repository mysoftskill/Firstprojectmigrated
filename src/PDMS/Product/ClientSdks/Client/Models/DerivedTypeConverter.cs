namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// A converter for abstract classes that have the derived type attribute applied.
    /// </summary>
    public class DerivedTypeConverter : JsonConverter
    {
        // Reflection is expensive, so we cache the values statically.
        private static readonly ConcurrentDictionary<Type, IEnumerable<Tuple<string, Type>>> DerivedTypeMapping = new ConcurrentDictionary<Type, IEnumerable<Tuple<string, Type>>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DerivedTypeConverter" /> class.
        /// </summary>
        /// <param name="namespacePrefix">The OData namespace prefix.</param>
        public DerivedTypeConverter(string namespacePrefix)
        {
            this.NamespacePrefix = namespacePrefix;
        }

        /// <summary>
        /// Gets the OData namespace prefix.
        /// </summary>
        public string NamespacePrefix { get; private set; }

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
            return true;
        }

        /// <summary>
        /// Deserializes the JSON to the derived type.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="existingValue">This parameter is not used.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The deserialized derived object.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (JToken.Load(reader) is JObject jsonObject)
            {
                var typeName = jsonObject["@odata.type"]?.ToString();

                object target;

                if (typeName != null)
                {
                    var mapping = this.GetMapping(objectType).Single(t => typeName.EndsWith(t.Item1));

                    target = Activator.CreateInstance(mapping.Item2);
                }
                else if (!objectType.IsAbstract)
                {
                    target = Activator.CreateInstance(objectType);
                }
                else
                {
                    return null;
                }

                serializer.Populate(jsonObject.CreateReader(), target);

                return target;
            }
            else
            {
                return null;
            }
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

        /// <summary>
        /// Gets the mapping information for the given type.
        /// </summary>
        /// <param name="objectType">The object type.</param>
        /// <returns>The mapping information.</returns>
        protected IEnumerable<Tuple<string, Type>> GetMapping(Type objectType)
        {
            return DerivedTypeMapping.GetOrAdd(
                objectType, 
                (t) =>
                {
                    var attributes = GetDerivedTypesAttributes(objectType);
                    if (attributes != null && attributes.Any())
                    {
                        return attributes.Select(a => Tuple.Create($"{this.NamespacePrefix}.{a.DerivedType.Name}", a.DerivedType));
                    }
                    else
                    {
                        return null;
                    }
                });
        }

        private IEnumerable<DerivedTypeAttribute> GetDerivedTypesAttributes(Type objectType)
        {
            var attributes = Enumerable.Empty<DerivedTypeAttribute>();

            if (objectType.BaseType != null)
            {
                attributes = this.GetDerivedTypesAttributes(objectType.BaseType);
            }

            return attributes.Concat(objectType.GetCustomAttributes<DerivedTypeAttribute>());
        }
    }
}