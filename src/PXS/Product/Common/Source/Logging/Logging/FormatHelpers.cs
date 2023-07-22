// <copyright>
// Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.Membership.MemberServices.Common.Logging
{
    public static class FormatHelpers
    {
        private static readonly Regex HexPuidRegex = new Regex(@"[\da-fA-F]{16}", RegexOptions.Compiled);
        private static readonly Regex HexRegex = new Regex(@"^[\da-fA-F]+$", RegexOptions.Compiled);

        public static void VerifyIsDecimal(string value, string parameterName)
        {
            long output;
            if (!long.TryParse(value, out output))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} must be in decimal format", parameterName));
            }
        }

        public static void VerifyIsGuid(string value, string parameterName)
        {
            Guid output;
            if (!Guid.TryParse(value, out output))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} must be a valid GUID string", parameterName));
            }
        }

        public static void IsValidHexPuid(string value, string parameterName)
        {
            if (!HexPuidRegex.IsMatch(value))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} must be in hexadecimal format", parameterName));
            }
        }

        public static void IsValidHex(string value, string parameterName)
        {
            if (!HexRegex.IsMatch(value))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} must be in hexadecimal format", parameterName));
            }
        }

        public static long ConvertHexToDecimal(string value, string parameterName)
        {
            long decimalValue;
            if (!long.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out decimalValue))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} could not be converted from hexadecimal to decimal", parameterName));
            }
            return decimalValue;
        }

        public static string ConvertDecimalToHex(string value, string parameterName)
        {
            if (!long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out long decValue))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} must be in decimal format", parameterName));
            }
            return decValue.ToString("X", CultureInfo.InvariantCulture);
        }
    }
}
