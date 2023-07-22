namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.Caching;
    using System.Runtime.CompilerServices;
    using Newtonsoft.Json;

    /// <summary>
    /// A generic base class for PCF identifiers. Used to provide some type-safety on top of various IDs
    /// flowing through the system.
    /// </summary>
    [JsonConverter(typeof(GeneralIdJsonConverter))]
    public abstract class Identifier : IEquatable<Identifier>
    {
        private delegate Identifier Constructor(string value);

        // String to GUID cache.
        private static readonly ConcurrentDictionary<string, GuidWrapper> GuidWrapperCache = new ConcurrentDictionary<string, GuidWrapper>(StringComparer.Ordinal);

        private static readonly ConcurrentDictionary<Type, Constructor> Constructors = new ConcurrentDictionary<Type, Constructor>();

        /// <summary>
        /// Protected .ctor.
        /// </summary>
        protected Identifier(string value, bool enableCache = false)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            GuidWrapper guidWrapper = null;
            bool foundInCache = false;
            if (enableCache)
            {
                foundInCache = GuidWrapperCache.TryGetValue(value, out guidWrapper);
            }

            if (guidWrapper == null)
            {
                if (!Guid.TryParse(value, out Guid result))
                {
                    throw new FormatException($"Type {this.GetType().Name} must be a GUID.");
                }

                guidWrapper = new GuidWrapper(result);
            }

            if (enableCache && !foundInCache)
            {
                GuidWrapperCache[value] = guidWrapper;
            }

            this.GuidValue = guidWrapper.Guid;
            this.Value = guidWrapper.Value;
        }

        /// <summary>
        /// Protected .ctor. that takes a Guid
        /// </summary>
        protected Identifier(Guid value)
        {
            this.GuidValue = value;
            this.Value = value.ToString("n");
        }

        /// <summary>
        /// Gets the value of this Identifier.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the Guid value.
        /// </summary>
        public Guid GuidValue { get; }

        /// <summary>
        /// Attempts to parse the given string as an instance of type T.
        /// </summary>
        public static bool TryParse<T>(string value, out T item) where T : Identifier
        {
            if (string.IsNullOrEmpty(value))
            {
                item = null;
                return false;
            }

            try
            {
                item = (T)GetConstructor(typeof(T)).Invoke(value);
                return true;
            }
            catch (FormatException)
            {
                item = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the string representation of this identifier.
        /// </summary>
        public override string ToString()
        {
            return this.Value;
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        public override bool Equals(object obj)
        {
            return this.Equals(obj as Identifier);
        }

        /// <summary>
        /// Gets the hash code of this identifier.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        public bool Equals(Identifier other)
        {
            return this == other;
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        public static bool operator ==(Identifier a, Identifier b)
        {
            if (object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (a?.GetType() != b?.GetType())
            {
                return false;
            }

            return a.Value == b.Value;
        }

        /// <summary>
        /// Tests for inequality.
        /// </summary>
        public static bool operator !=(Identifier a, Identifier b)
        {
            return !(a == b);
        }
        
        /// <summary>
        /// Compiles and caches a delegate to create a new instance of the given type.
        /// </summary>
        private static Constructor GetConstructor(Type objectType)
        {
            if (Constructors.TryGetValue(objectType, out var constructor))
            {
                return constructor;
            }
            else
            {
                ConstructorInfo constructorInfo = objectType.GetConstructor(new[] { typeof(string) });
                if (constructorInfo == null)
                {
                    throw new InvalidOperationException($"The type {objectType} must have a constructor that accepts a single parameter of type {typeof(string)}");
                }

                ParameterExpression param = Expression.Parameter(typeof(string));
                Expression newExpression = Expression.New(constructorInfo, param);
                var lambda = Expression.Lambda<Constructor>(newExpression, param);
                Constructor func = lambda.Compile();
                Constructors[objectType] = func;

                return func;
            }
        }

        // Wrapper class for a GUID and its associated string value.
        private class GuidWrapper
        {
            public GuidWrapper(Guid guid)
            {
                this.Guid = guid;
                this.Value = guid.ToString("n");
            }

            public string Value { get; }

            public Guid Guid { get; }
        }

        /// <summary>
        /// A JSON converter for an identifier.
        /// </summary>
        private class GeneralIdJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(Identifier).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                string item = serializer.Deserialize<string>(reader);
                if (item == null)
                {
                    return null;
                }

                Constructor ctor = GetConstructor(objectType);
                return ctor(item);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                Identifier item = (Identifier)value;
                serializer.Serialize(writer, item.Value);
            }
        }
    }
}
