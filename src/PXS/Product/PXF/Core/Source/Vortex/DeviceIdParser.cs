// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;

    internal static class DeviceIdParser
    {
        /// <summary>
        ///     Determines whether the device identifier is a global device identifier
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>
        ///     <c>true</c> if the device identifier is global device identifier; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsGlobalDeviceId(string deviceId) => deviceId.StartsWith("g:", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Parses the device identifier as int64.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>The device identifier as int64</returns>
        public static long ParseDeviceIdAsInt64(string deviceId)
        {
            return ParseDeviceId(deviceId, long.Parse);
        }

        /// <summary>
        ///     Try to parse the device Id as int64.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="value">The output parameter.</param>
        /// <returns>Whether the attempt succeeds or not.</returns>
        public static bool TryParseDeviceIdAsInt64(string deviceId, out long value)
        {
            value = 0;
            try
            {
                value = ParseDeviceIdAsInt64(deviceId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Try parse the device Id as uint64
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="value">The output parameter.</param>
        /// <returns>Whether the attempt succeeds or not.</returns>
        public static bool TryParseDeviceIdAsUInt64(string deviceId, out ulong value)
        {
            value = 0;
            try
            {
                value = ParseDeviceIdAsUInt64(deviceId);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        ///     Parses the device identifier as uint64.
        /// </summary>
        /// <param name="deviceId">The device identifier.</param>
        /// <returns>Device identifier as a uint64</returns>
        public static ulong ParseDeviceIdAsUInt64(string deviceId)
        {
            return ParseDeviceId(deviceId, ulong.Parse);
        }

        /// <summary>
        ///     Parses the device identifier.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="deviceId">The device identifier.</param>
        /// <param name="parser">The parser.</param>
        /// <returns>The device identifier</returns>
        /// <exception cref="Microsoft.Membership.MemberServices.Privacy.Core.Vortex.DeviceIdFormatException">
        ///     Expected global device id in the format of g:123456 - deviceId
        ///     or
        ///     Expected global device id in the format of g:123456 - deviceId
        /// </exception>
        private static T ParseDeviceId<T>(string deviceId, Func<string, T> parser)
        {
            try
            {
                if (IsGlobalDeviceId(deviceId))
                {
                    string[] parts = deviceId.Split(new[] { ':' }, 2);

                    return parser(parts[1]);
                }
            }
            catch (Exception e)
            {
                throw new DeviceIdFormatException("Expected global device id in the format of g:123456", nameof(deviceId), e);
            }

            throw new DeviceIdFormatException("Expected global device id in the format of g:123456", nameof(deviceId));
        }
    }
}
