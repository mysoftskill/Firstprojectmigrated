// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities
{
    using System;
    using System.Linq;

    /// <summary>
    ///     GlobalDeviceIdGenerator
    /// </summary>
    public static class GlobalDeviceIdGenerator
    {
        private static readonly Random random = new Random();

        /// <summary>
        ///     Generates a valid global device id
        /// </summary>
        /// <returns>A valid global device id per MSA rules</returns>
        public static long Generate()
        {
            // Per MSA team, all global device id's have this hex prefix
            const string DeviceIdHexPrefix = "18";

            // And the hex value is this # of digits
            ushort HexDigits = 14;

            var buffer = new byte[HexDigits / 2];
            random.NextBytes(buffer);
            string result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());

            // Drop the first two digits, and replace with the hex prefix that makes something a valid device id.
            return Convert.ToInt64(DeviceIdHexPrefix + result.Substring(2), 16);
        }
    }
}
