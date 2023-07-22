// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    public static class JsonExtensions
    {
        /// <summary>
        /// Get a string JSON property, or null if not present
        /// </summary>
        /// <param name="json">Json object</param>
        /// <param name="propertyName">Property name</param>
        public static string SafeGetJsonPropertyString(this JObject json, string propertyName)
        {
            string value;
            GetJsonProperty<string>(json, propertyName, out value);
            return value;
        }

        /// <summary>
        /// Get a bool JSON property, or null if not present
        /// </summary>
        /// <param name="json">Json object</param>
        /// <param name="propertyName">Property name</param>
        public static bool? SafeGetJsonPropertyBool(this JObject json, string propertyName)
        {
            bool? value;
            GetJsonProperty<bool?>(json, propertyName, out value);
            return value;
        }

        /// <summary>
        /// Get an integer JSON property, or null if not present
        /// </summary>
        /// <param name="json">Json object</param>
        /// <param name="propertyName">Property name</param>
        public static int? SafeGetJsonPropertyInt(this JObject json, string propertyName)
        {
            int? value;
            GetJsonProperty<int?>(json, propertyName, out value);
            return value;
        }

        /// <summary>
        /// Get a string JSON property, or null if not present
        /// </summary>
        /// <param name="json">Json object</param>
        /// <param name="containerName">Container name</param>
        /// <param name="propertyName">Property name</param>
        public static string SafeGetJsonPropertyString(this JObject json, string containerName, string propertyName)
        {
            string value;
            GetJsonProperty<string>(json, containerName, propertyName, out value);
            return value;
        }

        /// <summary>
        /// Get a boolean JSON property, or null if not present
        /// </summary>
        /// <param name="json">Json object</param>
        /// <param name="containerName">Container name</param>
        /// <param name="propertyName">Property name</param>
        public static bool? SafeGetJsonPropertyBool(this JObject json, string containerName, string propertyName)
        {
            bool? value;
            GetJsonProperty<bool?>(json, containerName, propertyName, out value);
            return value;
        }

        /// <summary>
        /// Get an integer JSON property, or null if not present
        /// </summary>
        /// <param name="json">Json object</param>
        /// <param name="containerName">Container name</param>
        /// <param name="propertyName">Property name</param>
        public static int? SafeGetJsonPropertyInt(this JObject json, string containerName, string propertyName)
        {
            int? value;
            GetJsonProperty<int?>(json, containerName, propertyName, out value);
            return value;
        }

        /// <summary>
        /// Get an unsigned integer JSON property, or null if not present
        /// </summary>
        /// <param name="json">Json object</param>
        /// <param name="containerName">Container name</param>
        /// <param name="propertyName">Property name</param>
        public static uint? SafeGetJsonPropertyUInt(this JObject json, string containerName, string propertyName)
        {
            uint? value;
            GetJsonProperty<uint?>(json, containerName, propertyName, out value);
            return value;
        }

        /// <summary>
        /// Get a JSON property value
        /// </summary>
        /// <typeparam name="T">Expected type of the property</typeparam>
        /// <param name="json">Json object</param>
        /// <param name="propertyName">JSON property name</param>
        /// <param name="value">The value of the property, or default(T)</param>
        /// <returns>Set to true if the property was found</returns>
        public static bool GetJsonProperty<T>(this JObject json, string propertyName, out T value)
        {
            value = default(T);
            bool success = false;

            try
            {
                if (json.Property(propertyName) != null)
                {
                    JToken token = json[propertyName];
                    value = token.ToObject<T>();
                    success = true;
                }
            }
            catch (FormatException)
            {
            }

            return success;
        }

        /// <summary>
        /// Get a JSON property value
        /// </summary>
        /// <typeparam name="T">Expected type of the property</typeparam>
        /// <param name="json">Json object</param>
        /// <param name="containerName">Outer JSON container name</param>
        /// <param name="propertyName">Inner JSON property name</param>
        /// <param name="value">The value of the property, or default(T)</param>
        /// <returns>Set to true if the property was found</returns>
        public static bool GetJsonProperty<T>(this JObject json, string containerName, string propertyName, out T value)
        {
            value = default(T);
            bool success = false;

            try
            {
                if (json.Property(containerName) != null)
                {
                    JToken property = json[containerName][propertyName];
                    if (property != null)
                    {
                        value = property.Value<T>();
                        success = true;
                    }
                }
            }
            catch (FormatException)
            {
            }

            return success;
        }
    }
}

