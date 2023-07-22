// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public static class StringExtensions
    {
        /// <summary>
        ///      Concatenates value with the string and returns the resulting string
        /// </summary>
        /// <param name="content">base string</param>
        /// <param name="value">value to append</param>
        /// <returns>resulting string</returns>
        public static string Append(
            this string content, 
            string value)
        {
            return content.Insert(content.Length, value);
        }

        /// <summary>
        ///     Formats a string, appends it to the StringBuilder, and then appends a newline to the StringBuilder
        /// </summary>
        /// <param name="builder">The collection of items to search through.</param>
        /// <param name="format">format string</param>
        /// <param name="args">replacement variables</param>
        /// <returns>resulting StringBuilder</returns>
        public static StringBuilder AppendLineFormat(this StringBuilder builder, string format, params object[] args)
        {
            return builder.AppendLine(format.FormatInvariant(args));
        }

        /// <summary>
        ///     Formats a string and then appends it to the StringBuilder
        /// </summary>
        /// <param name="builder">StringBuilder to append to</param>
        /// <param name="format">format string</param>
        /// <param name="args">replacement variables</param>
        /// <returns>resulting StringBuilder</returns>
        public static StringBuilder AppendFormatInvariant(
            this StringBuilder builder, 
            string format, 
            params object[] args)
        {
            return builder.AppendFormat(CultureInfo.InvariantCulture, format, args); // lgtm[cs/uncontrolled-format-string] Suppressing warning because format is controlled internally.
        }

        /// <summary>
        ///     Returns whether the provided string contains the specified value
        /// </summary>
        /// <param name="content">string to search for value</param>
        /// <param name="value">value to find in the stirng</param>
        /// <param name="comparisonType">type of comparison to perform when searching</param>
        /// <returns>true if the collection contains the value; false otherwise</returns>
        public static bool Contains(
            this string content, 
            string value, 
            StringComparison comparisonType)
        {
            return content.IndexOf(value, comparisonType) >= 0;
        }

        /// <summary>
        ///     Returns whether the provided collection contains the specified value
        /// </summary>
        /// <param name="collection">collection of items to search</param>
        /// <param name="value">value to find</param>
        /// <param name="comparisonType">The type of comparison to perform on each item in the collection</param>
        /// <returns>true if the collection contains the value; false otherwise</returns>
        public static bool Contains(
            this ICollection<string> collection, 
            string value, 
            StringComparison comparisonType)
        {
            foreach (string item in collection)
            {
                if (item.Equals(value, comparisonType))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///      determins if the two strings are equal (ignoring case)
        /// </summary>
        /// <param name="s1">base string</param>
        /// <param name="s2">string to compare to base string</param>
        /// <returns>resulting value</returns>
        public static bool EqualsIgnoreCase(
            this string s1, 
            string s2)
        {
            return (s1 == null && s2 == null) ||
                   (s1 != null && s1.Equals(s2, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///      determins if the base string starts with the prefix (ignoring case)
        /// </summary>
        /// <param name="fullString">base string</param>
        /// <param name="prefix">string to compare to base string</param>
        /// <returns>resulting value</returns>
        public static bool StartsWithIgnoreCase(
            this string fullString, 
            string prefix)
        {
            return (fullString == null && prefix == null) ||
                   (fullString != null && 
                    prefix != null &&
                    fullString.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///      formats a string using the invariant culture
        /// </summary>
        /// <param name="format">format string</param>
        /// <param name="args">replacement variables</param>
        /// <returns>resulting formatted string</returns>
        public static string FormatInvariant(
            this string format, 
            params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }   
    }
}
