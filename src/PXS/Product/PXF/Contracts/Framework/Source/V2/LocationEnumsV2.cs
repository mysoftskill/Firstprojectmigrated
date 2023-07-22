//--------------------------------------------------------------------------------
// <copyright file="LocationEnumsV2.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    public static class LocationEnumsV2
    {
        public enum LocationActivityTypeV2
        {
            Unspecified = 0,
            Hike = 1,
            Run = 2,
            Bike = 3,
        }

        public enum LocationTypeV2
        {
            Unknown = 0,
            Device = 1,
            Implicit = 2,
            Fitness = 3,
            Favorite = 4,
        }

        public enum LocationDeviceTypeV2
        {
            Unknown = 0,
            Phone = 1,
            Tablet = 2,
            PC = 3,
            Console = 4,
            Laptop = 5,
            Accessory = 6,
            Wearable = 7,
            SurfaceHub = 8,
            HeadMountedDisplay = 9,
        }
    }
}
