// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// JSON Converter to help serializing enums that we may or may not understand.
    /// </summary>
    public class TolerantEnumConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, Dictionary<string, object>> EnumTypeMapping = new ConcurrentDictionary<Type, Dictionary<string, object>>();

        /// <summary>
        /// Tests if the conversion is possible.
        /// </summary>
        public override bool CanConvert(Type objectType)
        {
            objectType = Nullable.GetUnderlyingType(objectType) ?? objectType;
            return objectType.IsEnum;
        }

        /// <summary>
        /// Writes the given value.
        /// </summary>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value);
        }

        /// <summary>
        /// Reads the given value.
        /// </summary>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;

            Dictionary<string, object> memberNames;
            if (!EnumTypeMapping.TryGetValue(enumType, out memberNames))
            {
                memberNames = Enum.GetNames(enumType).ToDictionary(name => name, name => Enum.Parse(enumType, name));
                EnumTypeMapping[enumType] = memberNames;
            }

            object result = null;
            if (reader.TokenType == JsonToken.String)
            {
                result = HandleStringToken(reader.Value.ToString(), enumType, memberNames);
            }
            else if (reader.TokenType == JsonToken.Integer)
            {
                result = HandleIntegerToken(reader.Value, enumType);
            }

            if (result != null)
            {
                return result;
            }
            else if (Nullable.GetUnderlyingType(objectType) != null)
            {
                // is nullable
                return null;
            }
            else
            {
                // assume 0.
                return Enum.ToObject(enumType, 0);
            }
        }

        private static object HandleIntegerToken(object token, Type enumType)
        {
            return Enum.ToObject(enumType, token);
        }

        private static object HandleStringToken(string token, Type enumType, Dictionary<string, object> memberNames)
        {
            token = token ?? string.Empty;

            if (enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Any())
            {
                if (Enum.GetUnderlyingType(enumType) == typeof(ulong))
                {
                    // Ulongs get special treatment here. Long can handle everything else.
                    List<ulong> parts = ParseFlags<ulong>(token, memberNames, Convert.ToUInt64);
                    var result = parts.Aggregate((ulong)0, (a, b) => a | b);
                    return Enum.ToObject(enumType, result);
                }
                else
                {
                    List<long> parts = ParseFlags<long>(token, memberNames, Convert.ToInt64);
                    var result = parts.Aggregate((long)0, (a, b) => a | b);
                    return Enum.ToObject(enumType, result);
                }
            }
            else if (memberNames.ContainsKey(token))
            {
                return memberNames[token];
            }

            return null;
        }

        private static List<T> ParseFlags<T>(
            string value,
            Dictionary<string, object> enumNameMap,
            Func<object, T> convertCallback) where T : struct
        {
            List<T> parsedParts = new List<T>();
            string[] parts = value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (enumNameMap.ContainsKey(part))
                {
                    parsedParts.Add(convertCallback(enumNameMap[part]));
                }
            }

            return parsedParts;
        }
    }
}
