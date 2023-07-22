// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Extensions
{
    using System;

    /// <summary>
    /// Argument Extensions
    /// </summary>
    public static class ArgumentExtensions
    {
        /// <summary>
        /// Throws if the specified parameter is null
        /// </summary>
        /// <param name="value">Parameter</param>
        /// <param name="parameterName">Parameter name</param>
        public static void ThrowOnNull(this object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        /// <summary>
        /// Throws if the specified parameter is null or whitespace
        /// </summary>
        /// <param name="value">Parameter</param>
        /// <param name="parameterName">Parameter name</param>
        public static void ThrowOnNull(this string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}