namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using Newtonsoft.Json;

    /// <summary>
    /// A custom converter to handle serialization and deserialization of dictionary objects.
    /// </summary>
    /// <typeparam name="Key">The dictionary key type.</typeparam>
    /// <typeparam name="Value">The dictionary value type.</typeparam>
    public class DictionaryConverter<Key, Value> : JsonConverter
    {
        private static readonly Dictionary<Type, Tuple<KeySelector, KeyApplicator>> Cache;

        private readonly KeySelector keySelector;
        private readonly KeyApplicator keyApplicator;

        // Must have a static constructor to ensure proper singleton instantiation.
        // Otherwise, this class is not thread safe.
        static DictionaryConverter()
        {
            Cache = new Dictionary<Type, Tuple<KeySelector, KeyApplicator>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryConverter{Key,Value}"/> class.
        /// </summary>
        /// <param name="delegateType">The class type that contains the delegate.</param>
        /// <param name="keySelectorDelegateName">The name of the key selector delegate.</param>
        /// <param name="keyApplicatorDelegateName">The name of the key applicator delegate.</param>
        public DictionaryConverter(Type delegateType, string keySelectorDelegateName, string keyApplicatorDelegateName)
        {
            if (!Cache.ContainsKey(delegateType))
            {
                // During rare occasions in the test cases, we run into null reference errors
                // when running the tests in parallel. This is an attempt to fix that.
                lock (Cache)
                {
                    var members = delegateType.GetField(keySelectorDelegateName, BindingFlags.NonPublic | BindingFlags.Static);
                    var keySelector = (KeySelector)members.GetValue(null);

                    members = delegateType.GetField(keyApplicatorDelegateName, BindingFlags.NonPublic | BindingFlags.Static);
                    var keyApplicator = (KeyApplicator)members.GetValue(null);

                    Cache[delegateType] = new Tuple<KeySelector, KeyApplicator>(keySelector, keyApplicator);
                }
            }

            this.keySelector = Cache[delegateType].Item1;
            this.keyApplicator = Cache[delegateType].Item2;
        }

        /// <summary>
        /// Defines a delegate for selecting the key from the value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The key.</returns>
        public delegate Key KeySelector(Value value);

        /// <summary>
        /// Defines a delegate for copying the key to the value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public delegate void KeyApplicator(Key key, Value value);

        /// <summary>
        /// Serializes a dictionary as an IEnumerable.
        /// </summary>
        /// <param name="writer">The LSON writer.</param>
        /// <param name="value">The object type that needs to be serialized.</param>
        /// <param name="serializer">The parameter is not used.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(
                writer,
                (value as IDictionary<Key, Value>).Select(d =>
                {
                    this.keyApplicator(d.Key, d.Value);
                    return d.Value;
                }));
        }

        /// <summary>
        /// Reads the current property as a JSON string, and then deserializes it.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The expected object's type.</param>
        /// <param name="existingValue">The parameter is not used.</param>
        /// <param name="serializer">The parameter is not used.</param>
        /// <returns>The deserialized object.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var values = serializer.Deserialize<IEnumerable<Value>>(reader);

            return values?.ToDictionary(this.keySelector.Invoke);
        }

        /// <summary>
        /// This converter is only set on properties, so it is always assumed to be valid.
        /// </summary>
        /// <param name="objectType">The parameter is not used.</param>
        /// <returns>Always returns true.</returns>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}