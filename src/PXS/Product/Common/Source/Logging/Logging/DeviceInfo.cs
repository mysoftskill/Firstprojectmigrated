// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging
{
    using System;
    using System.Globalization;

    using Microsoft.CommonSchema.Services;
    using Microsoft.Telemetry;

    /// <summary>
    ///     Holds information about the device
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        ///     Formats a device id as decribed at https://osgwiki.com/wiki/CommonSchema/device_id
        /// </summary>
        /// <param name="type">The type of the id</param>
        /// <param name="id">The id without any prefix</param>
        /// <returns>The formatted id</returns>
        public static string FormatDeviceId(DeviceIdType type, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            string prefix;
            switch (type)
            {
                case DeviceIdType.GlobalId:
                    prefix = "g";
                    break;
                case DeviceIdType.HardwareId:
                    prefix = "h";
                    break;
                case DeviceIdType.MacAddressId:
                    prefix = "m";
                    break;
                case DeviceIdType.SqmId:
                    prefix = "s";
                    FormatHelpers.VerifyIsGuid(id, "SqmId");
                    break;
                case DeviceIdType.Deidentified:
                    prefix = "d";
                    break;
                case DeviceIdType.Random:
                    prefix = "r";
                    break;
                case DeviceIdType.AndroidId:
                    prefix = "a";
                    break;
                case DeviceIdType.XboxLiveHardwareId:
                    prefix = "x";
                    break;
                case DeviceIdType.AdvertisingId:
                    prefix = "e";
                    break;
                default:
                    throw new ArgumentException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Unexpected DeviceIdType '{0}'",
                            type));
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}:{1}", prefix, id);
        }

        public string Id { get; private set; }

        /// <summary>
        ///     Fills the provided envelope with the device information
        /// </summary>
        /// <param name="envelope">The envelope to be filled</param>
        public void FillEnvelope(Envelope envelope)
        {
            if (!string.IsNullOrWhiteSpace(this.Id))
            {
                var device = envelope.SafeDevice();
                device.id = this.Id;
            }
        }

        /// <summary>
        ///     Sets the device Id
        /// </summary>
        /// <param name="type">The type of the id</param>
        /// <param name="id">The id without any prefix</param>
        public void SetId(DeviceIdType type, string id)
        {
            this.Id = FormatDeviceId(type, id);
        }
    }
}
