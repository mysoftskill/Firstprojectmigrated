// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker
{
    /// <summary>
    /// Device Id cache interface.
    /// </summary>
    public interface IDeviceIdCache
    {
        /// <summary>
        /// Check if given DeviceId in the cache.
        /// </summary>
        /// <param name="deviceId">DeviceId</param>
        /// <returns></returns>
        bool Contains(string deviceId);

        /// <summary>
        /// Add device id and associated value to the cache.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="value"></param>
        void Add(string deviceId, object value);

        /// <summary>
        /// Update device Id cache.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="value">Value</param>
        void Update(string deviceId, object value);
    }
}
