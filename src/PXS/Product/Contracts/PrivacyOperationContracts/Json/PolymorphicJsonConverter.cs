// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    ///     JSON converter for polymorphic objects.
    /// </summary>
    /// <remarks>
    ///     This converter works only on types, defined in this assembly (security limitation).
    /// </remarks>
    /// <typeparam name="TInterface">The type of interface.</typeparam>
    internal class PolymorphicJsonConverter<TInterface> : JsonConverter
    {
        /// <summary>
        ///     The type property encoded in JSON.
        /// </summary>
        public const string JsonTypeNameProperty = "__type";

        // Prevent infinite loops by marking ourselves as disabled.
        [ThreadStatic]
        private static bool disabled;

        // Maps Type Name -> CLR Type
        private readonly Dictionary<string, Type> typeNameToTypeMap;

        // Maps CLR type -> Type Name.
        private readonly Dictionary<Type, string> typeToTypeNameMap;

        /// <summary>
        ///     Gets a value that indicates if we can read at the current time.
        /// </summary>
        public override bool CanRead => !disabled;

        /// <summary>
        ///     Gets a value that indicates if we can write at the current time.
        /// </summary>
        public override bool CanWrite => !disabled;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolymorphicJsonConverter{TInterface}" /> class.
        /// </summary>
        public PolymorphicJsonConverter()
        {
            // Find all types that implement TInterface.
            var derivedTypes =
                typeof(PolymorphicJsonConverter<>).Assembly.GetTypes()
                    .Select(t => new { Type = t, Interfaces = t.GetInterfaces() })
                    .Where(t => t.Interfaces.Contains(typeof(TInterface)))
                    .Where(t => !t.Type.IsAbstract && !t.Type.IsInterface);

            this.typeNameToTypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            this.typeToTypeNameMap = new Dictionary<Type, string>();

            foreach (var item in derivedTypes)
            {
                // Find the attribute that allows object serialization.
                AllowSerializationAttribute attribute = GetAttribute(item.Type);
                this.typeNameToTypeMap.Add(item.Type.FullName, item.Type);
                this.typeToTypeNameMap.Add(item.Type, item.Type.FullName);
            }
        }

        /// <summary>
        ///     Gets a value indicating if this converter can convert the given value.
        /// </summary>
        /// <param name="objectType">The type of object.</param>
        /// <returns>A value indicating whether this converter can convert the given object.</returns>
        public override bool CanConvert(Type objectType)
        {
            return typeof(TInterface).IsAssignableFrom(objectType);
        }

        /// <summary>
        ///     Reads the given token.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The type of object.</param>
        /// <param name="existingValue">The object.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The parsed object.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            try
            {
                // Disable this converter to prevent stack overflows.
                disabled = true;
                JObject @object = JObject.Load(reader);

                string typeProperty = @object.Property(JsonTypeNameProperty).Value.Value<string>();

                Type type;
                if (!this.typeNameToTypeMap.TryGetValue(typeProperty, out type))
                {
                    throw new FormatException($"Received unrecognized type name: '{typeProperty}'");
                }

                return @object.ToObject(type);
            }
            finally
            {
                disabled = false;
            }
        }

        /// <summary>
        ///     Writes the given object as JSON.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            string typeName;
            Type objectType = value.GetType();
            if (!this.typeToTypeNameMap.TryGetValue(objectType, out typeName))
            {
                throw new FormatException($"Received {typeof(TInterface).Name} type: '{objectType}', but type was not known.");
            }

            JObject @object;
            try
            {
                // Disable this converter to prevent stack overflows.
                disabled = true;
                @object = JObject.FromObject(value, serializer);
            }
            finally
            {
                disabled = false;
            }

            @object.AddFirst(new JProperty(JsonTypeNameProperty, typeName));
            @object.WriteTo(writer);
        }

        private static AllowSerializationAttribute GetAttribute(Type type)
        {
            var attribute = type.GetCustomAttribute<AllowSerializationAttribute>();
            if (attribute == null)
            {
                throw new InvalidOperationException(
                    $"Type: {type.Name} implemented interface {typeof(TInterface).Name} but did not have [{nameof(AllowSerializationAttribute)}] attribute.");
            }

            return attribute;
        }
    }
}
