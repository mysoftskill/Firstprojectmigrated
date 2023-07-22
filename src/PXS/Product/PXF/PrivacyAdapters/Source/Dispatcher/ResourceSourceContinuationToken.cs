// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Serialization;

    using Microsoft.Membership.MemberServices.Common.Utilities;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    ///     Abstract class for a continuation token for a <see cref="IResourceSource{T}" />. Any implementor of this class
    ///     must attribute their implementation with a <see cref="ResourceSourceContinuationTokenNameAttribute" />.
    /// </summary>
    public abstract class ResourceSourceContinuationToken
    {
        // According to Json.Net documentation, incoming types validated with a custom SerializationBinder
        // when deserializing will mitigate the risks.
        private static readonly JsonSerializerSettings jsonSerializerSettings =
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new JsonBinder() };     // lgtm[cs/unsafe-type-name-handling]

        /// <summary>
        ///     Deserializes a continuation token into a concrete object.
        /// </summary>
        public static ResourceSourceContinuationToken Deserialize(string token)
        {
            return token == null ? null : Base64UrlSerializer.Decode<ResourceSourceContinuationToken>(token, jsonSerializerSettings);
        }

        /// <summary>
        ///     Serializes a continuation token into a Url-friendly string.
        /// </summary>
        public string Serialize()
        {
            return Base64UrlSerializer.Encode(this, jsonSerializerSettings);
        }

        private class JsonBinder : ISerializationBinder
        {
            private static readonly Dictionary<string, Type> nameToType = new Dictionary<string, Type>();

            private static readonly Dictionary<Type, string> typeToName = new Dictionary<Type, string>();

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                if (!typeToName.TryGetValue(serializedType, out typeName))
                    throw new ArgumentOutOfRangeException(nameof(serializedType), $"Type is unknown: {serializedType.FullName}");
            }

            public Type BindToType(string assemblyName, string typeName)
            {
                if (!nameToType.TryGetValue(typeName, out Type type))
                    throw new ArgumentOutOfRangeException(nameof(typeName), $"Type can not be found: {typeName}");

                return type;
            }

            static JsonBinder()
            {
                Type[] types;
                try
                {
                    types = Assembly.GetExecutingAssembly()
                        .GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                var attributedTypes = types
                    .Where(t => typeof(ResourceSourceContinuationToken).IsAssignableFrom(t) && t != typeof(ResourceSourceContinuationToken))
                    .Select(
                        t => new
                        {
                            Type = t,
                            ((ResourceSourceContinuationTokenNameAttribute)t.GetCustomAttributes(typeof(ResourceSourceContinuationTokenNameAttribute), false).Single()).Name
                        });

                foreach (var pair in attributedTypes)
                {
                    if (nameToType.ContainsKey(pair.Name))
                        throw new Exception("Multiple types with the same ResourceSourceContinuationTokenNameAttribute name");

                    if (pair.Type.FullName == null ||
                        (!pair.Type.FullName.Equals("Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher.MergingResourceSourceContinuationToken") &&
                         !pair.Type.FullName.Equals("Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher.PagingResourceSourceContinuationToken")))
                    {
                        throw new SerializationException("Not a valid ResourceSourceContinuationToken serialized data.");
                    }

                    nameToType.Add(pair.Name, pair.Type);
                    typeToName.Add(pair.Type, pair.Name);
                }
            }
        }
    }
}
