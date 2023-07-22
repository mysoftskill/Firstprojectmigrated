// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Globalization;

    /// <summary>
    ///     Extension methods for converting values into strings
    /// </summary>
    public static class ValueExtensions
    {
        /// <summary>Converts a value, including value types, into a string using the invariant locale</summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="target">The value to convert into a string</param>
        /// <returns>The converted value</returns>
        public static string ToStringInvariant<TValue>(this TValue target) 
            where TValue : struct, IConvertible
        {
            return target.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Converts a nullable value, into a string using the invariant locale</summary>
        /// <typeparam name="TValue">Type of the value</typeparam>
        /// <param name="target">The nullable value to convert into a string</param>
        /// <returns>The converted value, or an empty string if the target was null</returns>
        public static string ToStringInvariant<TValue>(this TValue? target) 
            where TValue : struct, IConvertible
        {
            return target?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
