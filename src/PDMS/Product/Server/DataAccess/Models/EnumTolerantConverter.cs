namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// JSON Converter to help serializing string enum values that may or may not exist.
    /// Unrecognized values will fallback to the default enum value (0).
    /// </summary>
    /// <typeparam name="TEnum">The enum type.</typeparam>
    public class EnumTolerantConverter<TEnum> : StringEnumConverter
    {
        private static readonly Type EnumType = typeof(TEnum);

        private static readonly Lazy<bool> IsFlagsEnum = new Lazy<bool>(() =>
        {
            return EnumType.GetCustomAttributes(typeof(FlagsAttribute), false).Any();
        });

        private static readonly Lazy<ConcurrentDictionary<CaseInsensitiveString, TEnum>> EnumNameMapping = new Lazy<ConcurrentDictionary<CaseInsensitiveString, TEnum>>(() =>
        {
            var dictionary = new ConcurrentDictionary<CaseInsensitiveString, TEnum>();

            foreach (var name in Enum.GetNames(EnumType))
            {
                var value = (TEnum)Enum.Parse(EnumType, name);
                dictionary.GetOrAdd(CaseInsensitiveString.Create(name), value);
            }

            return dictionary;
        });

        /// <summary>
        /// Reads the given value or falls back to a default if the value is unrecognized.
        /// </summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="existingValue">The existing value.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>The parsed value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var name = CaseInsensitiveString.Create(reader.Value.ToString());
                TEnum result;

                if (EnumNameMapping.Value.TryGetValue(name, out result))
                {
                    return result;
                }
                else if (IsFlagsEnum.Value)
                {
                    return EnumNameMapping.Value.GetOrAdd(name, this.HandleFlagsToken);
                }
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                return Enum.ToObject(EnumType, reader.Value);
            }

            return default(TEnum);
        }

        private TEnum HandleFlagsToken(CaseInsensitiveString token)
        {
            if (Enum.GetUnderlyingType(EnumType) == typeof(ulong))
            {
                // Ulong gets special treatment here. Long can handle everything else.
                List<ulong> parts = this.ParseFlags(token.Value, Convert.ToUInt64);
                var result = parts.Aggregate((ulong)0, (a, b) => a | b);
                return (TEnum)Enum.ToObject(EnumType, result);
            }
            else
            {
                List<long> parts = this.ParseFlags(token.Value, Convert.ToInt64);
                var result = parts.Aggregate((long)0, (a, b) => a | b);
                return (TEnum)Enum.ToObject(EnumType, result);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        private List<T> ParseFlags<T>(
            string value,
            Func<object, T> convertCallback) where T : struct
        {
            List<T> parsedParts = new List<T>();
            var parts = 
                value
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(CaseInsensitiveString.Create);

            foreach (var part in parts)
            {
                if (EnumNameMapping.Value.ContainsKey(part))
                {
                    parsedParts.Add(convertCallback(EnumNameMapping.Value[part]));
                }
            }

            return parsedParts;
        }

        /// <summary>
        /// An internal class that ensures dictionary keys are case insensitive.
        /// </summary>
        private class CaseInsensitiveString
        {
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1304:SpecifyCultureInfo", MessageId = "System.String.ToLower")]
            private CaseInsensitiveString(string value)
            {
                this.Value = value.ToLower();
            }

            public string Value { get; private set; }

            public static CaseInsensitiveString Create(string value)
            {
                return new CaseInsensitiveString(value);
            }

            public override bool Equals(object obj)
            {
                var other = obj as CaseInsensitiveString;

                if (other != null)
                {
                    return this.Value.Equals(other.Value);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return this.Value.GetHashCode();
            }
        }
    }
}