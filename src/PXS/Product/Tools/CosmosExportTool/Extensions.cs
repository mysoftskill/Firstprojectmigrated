// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace CosmosExportScanner
{
    using System;

    /// <summary>
    ///     Extensions class
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        ///      checks if two strings are equal, ignoring case
        /// </summary>
        /// <param name="s1">string 1</param>
        /// <param name="s2">string 2</param>
        /// <returns>true if equal; false otherwise</returns>
        public static bool EqualsIgnoreCase(
            this string s1,
            string s2)
        {
            return
                (s1 == null && s2 == null) ||
                (s1 != null && s1.Equals(s2, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        ///      determins if the string starts with a specific different string, ignoring case
        /// </summary>
        /// <param name="s1">string to check for a prefix</param>
        /// <param name="s2">prefix to check for</param>
        /// <returns>true if s2 is a prefix of s1; false otherwise</returns>
        public static bool StartsWithIgnoreCase(
            this string s1,
            string s2)
        {
            return
                (s1 == null && s2 == null) ||
                (s1 != null && s2 != null && s1.StartsWith(s2, StringComparison.OrdinalIgnoreCase));
        }
    }
}
