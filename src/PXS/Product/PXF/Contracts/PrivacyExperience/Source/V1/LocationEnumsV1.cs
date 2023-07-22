// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1
{
    /// <summary>
    /// Enums related to location records
    /// </summary>
    public static class LocationEnumsV1
    {
        /// <summary>
        /// Activity type
        /// </summary>
        public enum LocationActivityTypeV1
        {
            /// <summary>
            /// Unspecified activity type
            /// </summary>
            Unspecified = 0,

            /// <summary>
            /// Hike
            /// </summary>
            Hike = 1,

            /// <summary>
            /// Run
            /// </summary>
            Run = 2,

            /// <summary>
            /// Bike
            /// </summary>
            Bike = 3,
        }

        /// <summary>
        /// Location type
        /// </summary>
        public enum LocationTypeV1
        {
            /// <summary>
            /// Unknown type
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Device
            /// </summary>
            Device = 1,

            /// <summary>
            /// Implicit
            /// </summary>
            Implicit = 2,

            /// <summary>
            /// Fitness
            /// </summary>
            Fitness = 3,

            /// <summary>
            /// Favorite
            /// </summary>
            Favorite = 4,
        }

        /// <summary>
        /// Device Type
        /// </summary>
        public enum LocationDeviceTypeV1
        {
            /// <summary>
            /// Unknownn
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// Phone
            /// </summary>
            Phone = 1,

            /// <summary>
            /// Tablet
            /// </summary>
            Tablet = 2,

            /// <summary>
            /// PC
            /// </summary>
            PC = 3,

            /// <summary>
            /// Console
            /// </summary>
            Console = 4,

            /// <summary>
            /// Laptop
            /// </summary>
            Laptop = 5,

            /// <summary>
            /// Accessory
            /// </summary>
            Accessory = 6,

            /// <summary>
            /// Wearable
            /// </summary>
            Wearable = 7,

            /// <summary>
            /// Surface hub
            /// </summary>
            SurfaceHub = 8,

            /// <summary>
            /// Head mounted display
            /// </summary>
            HeadMountedDisplay = 9,
        }
    }
}
