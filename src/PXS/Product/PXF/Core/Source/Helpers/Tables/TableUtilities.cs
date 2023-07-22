// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables
{
    using System;
    using System.Text;

    /// <summary>
    ///     Table utilities class
    /// </summary>
    public static class TableUtilities
    {
        /// <summary>
        ///     Escapes a string used as a key in the Azure Table Store APIs
        /// </summary>
        /// <param name="key">value to escape</param>
        /// <returns>escaped string</returns>
        public static string EscapeKey(string key)
        {
            const string Hex = "0123456789abcdef";

            if (key != null)
            {
                StringBuilder result = null;
                int idxStart = 0;

                for (int i = 0; i < key.Length; ++i)
                {
                    char c = key[i];

                    if (TableUtilities.ShouldEscape(c))
                    {
                        int length = i - idxStart;

                        result = result ?? new StringBuilder();

                        if (length > 0)
                        {
                            result.Append(key, idxStart, length);
                        }

                        idxStart = i + 1;

                        result.Append('$');
                        result.Append(Hex[(c & 0xf0) >> 4]);
                        result.Append(Hex[c & 0x0f]);
                    }
                }

                if (result != null)
                {
                    if (idxStart < key.Length)
                    {
                        result.Append(key, idxStart, key.Length - idxStart);
                    }

                    return result.ToString();
                }

                return key;
            }

            return null;
        }

        /// <summary>
        ///     Unescapes a string previously escaped with EscapeKey
        /// </summary>
        /// <param name="key">value to unescape</param>
        /// <returns>unescaped string</returns>
        public static string UnescapeKey(string key)
        {
            if (key != null)
            {
                StringBuilder result = null;
                int idxStart = 0;

                while (idxStart < key.Length)
                {
                    int idxCurrent = key.IndexOf('$', idxStart);
                    if (idxCurrent < 0)
                    {
                        break;
                    }

                    if (idxCurrent + 2 >= key.Length)
                    {
                        throw new ArgumentException("Invalid escape sequence found", nameof(key));
                    }

                    result = result ?? new StringBuilder();

                    if (idxStart < idxCurrent)
                    {
                        result.Append(key, idxStart, idxCurrent - idxStart);
                    }

                    result.Append(
                        (char)((TableUtilities.GetHexDigitAsDecimal(key[idxCurrent + 1]) << 4) |
                               (TableUtilities.GetHexDigitAsDecimal(key[idxCurrent + 2]))));

                    idxStart = idxCurrent + 3;
                }

                if (result != null)
                {
                    if (idxStart < key.Length)
                    {
                        result.Append(key, idxStart, key.Length - idxStart);
                    }

                    return result.ToString();
                }

                return key;
            }

            return null;
        }

        /// <summary>
        ///     Converts a single hex digit into the decimal equivalent
        /// </summary>
        /// <param name="hexDigit">hexadecimal digit</param>
        /// <returns>resulting value</returns>
        private static byte GetHexDigitAsDecimal(char hexDigit)
        {
            if (hexDigit >= '0' && hexDigit <= '9')
            {
                return (byte)(hexDigit - '0');
            }

            if (hexDigit >= 'a' && hexDigit <= 'f')
            {
                return (byte)(10 + hexDigit - 'a');
            }

            if (hexDigit >= 'A' && hexDigit <= 'F')
            {
                return (byte)(10 + hexDigit - 'A');
            }

            throw new ArgumentException("Invalid hex digit found in escape sequence");
        }
        
        /// <summary>
        ///     determines if a given character should be escaped    
        /// </summary>
        /// <param name="c">character to test</param>
        /// <returns>true if it should be escaped; false otherwise</returns>
        private static bool ShouldEscape(char c)
        {
            // list obtained from 
            //  https://docs.microsoft.com/en-us/rest/api/storageservices/Understanding-the-Table-Service-Data-Model
            // note that '$' is not a forbidden character, but is included here because that is the escape sequence character
            return c == '#' || c == '/' || c == '\\' || c == '?' || c == '$' ||
                   (c >= 0 && c <= 0x1f) ||
                   (c >= 0x7f && c <= 0x9f);
        }
    }
}
