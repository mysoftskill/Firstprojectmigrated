// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging
{
    /// <summary>
    ///     Device Id type as documented at https://osgwiki.com/wiki/CommonSchema/device_id
    /// </summary>
    public enum DeviceIdType
    {
        Undefined = 0,

        /// <summary>
        ///     This ID is tied to your installation of Windows. It persists across OS updates &amp; upgrades but not across re-installs. See spec. Not available in plain text on the client,
        ///     but available in an MSA token after it has been read by a service with permission to read that token.
        /// </summary>
        GlobalId = 1,

        /// <summary>
        ///     This ID is tied to your actual hardware. See spec. Not available in plain text on the client, but available in an MSA token after it has been read by a service with permission
        ///     to read that token. Today, this ID is only on a small handful of events. All Census events, OOBE, and Setup360. The best place to find this ID is in the Device Census stream
        ///     curated by DnA.
        /// </summary>
        HardwareId = 2,

        /// <summary>
        ///     Mac Address of the device
        /// </summary>
        MacAddressId = 3,

        /// <summary>
        ///     Stored in the registry at HKLM\Software\Microsoft\SQMClient\MachineId
        /// </summary>
        SqmId = 4,

        /// <summary>
        ///     An anonymized value based on what the client would have used for a local ID.
        /// </summary>
        Deidentified = 5,

        /// <summary>
        ///     A random value generated on the client. Used to uniquely identified events even if they were generated with the DROP flag to omit unique identifiers.
        /// </summary>
        Random = 6,

        /// <summary>
        ///     A 64-bit number (as a hex string) that is randomly generated when the user first sets up the device and should remain constant for the lifetime of the user's device. The value
        ///     may change if a factory reset is performed on the device.
        ///     Note: When a device has multiple users (available on certain devices running Android 4.2 or higher), each user appears as a completely separate device, so the ANDROID_ID value
        ///     is unique to each user.
        /// </summary>
        AndroidId = 7,

        /// <summary>
        ///     XBox Live Hardware ID
        /// </summary>
        XboxLiveHardwareId = 8,

        /// <summary>
        ///     This is the advertising ID known to the application (in other words, campaign targeting id)
        ///     Example: ba99ae784404baeae86dc1a99b33979c
        /// </summary>
        AdvertisingId = 9
    }
}
