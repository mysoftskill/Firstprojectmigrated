// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Converters
{
    using System;

    /// <summary>
    /// Helper extension methods
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Throws if the specified parameter is null
        /// </summary>
        /// <param name="o">Parameter</param>
        /// <param name="parameterName">Parameter name</param>
        public static void ThrowOnNull(this object o, string parameterName)
        {
            if (o == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
