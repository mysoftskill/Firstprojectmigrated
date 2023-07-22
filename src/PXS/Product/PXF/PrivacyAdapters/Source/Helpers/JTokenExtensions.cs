// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers
{
    using Newtonsoft.Json.Linq;

    internal static class JTokenExtensions
    {
        public static T ValueOrDefault<T>(this JToken jToken)
        {
            return jToken == null ? default(T) : jToken.Value<T>();
        }
    }
}