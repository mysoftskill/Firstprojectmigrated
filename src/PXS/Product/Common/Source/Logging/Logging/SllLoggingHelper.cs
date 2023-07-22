// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.Extensions
{
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Telemetry;

    /// <summary>
    ///     Miscellaneous extension methods for Sll events
    /// </summary>
    public static class SllLoggingHelper
    {
        private const LogOption DefaultLogOption = LogOption.Realtime;

        /// <summary>
        ///     Creates Device Info
        /// </summary>
        /// <param name="deviceIdType">Device id type</param>
        /// <param name="id">An id</param>
        /// <returns>Device Info</returns>
        public static DeviceInfo CreateDeviceInfo(DeviceIdType deviceIdType, string id)
        {
            DeviceInfo device = new DeviceInfo();

            if (string.IsNullOrWhiteSpace(id))
            {
                return device;
            }

            device.SetId(deviceIdType, id);
            return device;
        }

        /// <summary>
        ///     Creates Device Info from deviceId
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns>Device Info</returns>
        public static DeviceInfo CreateDeviceInfo(long? deviceId)
        {
            DeviceInfo device = new DeviceInfo();

            if (deviceId == null)
            {
                return device;
            }

            device.SetId(DeviceIdType.GlobalId, deviceId.ToString());

            return device;
        }

        /// <summary>
        ///     Creates User Info
        /// </summary>
        /// <param name="userIdType">User id type</param>
        /// <param name="id">An id</param>
        /// <returns>User Info</returns>
        public static UserInfo CreateUserInfo(UserIdType userIdType, string id)
        {
            UserInfo user = new UserInfo();

            if (string.IsNullOrWhiteSpace(id))
            {
                return user;
            }

            user.SetId(userIdType, id);
            return user;
        }

        /// <summary>
        ///     Creates a UserInfo object from an MsaId. UserInfo objects are used for Sll logging purposes.
        ///     This method uses the PUID if present, falls back to CID, or returns an empty UserInfo object if neither PUID or CID are available.
        /// </summary>
        /// <param name="userId">MsaId used to create UserInfo object</param>
        /// <returns>UserInfo object</returns>
        public static UserInfo CreateUserInfo(MsaId userId)
        {
            UserInfo user = new UserInfo();

            if (userId == null)
            {
                return user;
            }

            string cidString = userId.CidString;

            // Use the PUID if available. Otherwise, use the CID
            if (userId.PuidDecimal != 0)
            {
                user.SetId(UserIdType.DecimalPuid, userId.PuidDecimalString);
            }
            else if (!string.IsNullOrEmpty(cidString))
            {
                user.SetId(UserIdType.DecimalCid, cidString);
            }

            return user;
        }

        /// <summary>
        ///     Logs the given telemetry log as error and populates the user info property using the MsaId
        /// </summary>
        /// <param name="log">Log to log as error</param>
        /// <param name="userId">MsaId to populate the user info with</param>
        public static void LogError<T>(this T log, MsaId userId)
            where T : Base
        {
            UserInfo userInfo = CreateUserInfo(userId);

            log.LogError(DefaultLogOption, fillEnvelope: userInfo.FillEnvelope);
        }

        /// <summary>
        ///     Logs the given telemetry log as informational and populates the user info property using the MsaId
        /// </summary>
        /// <param name="log">Log to log as informational</param>
        /// <param name="userId">MsaId to populate the user info with</param>
        public static void LogInformational<T>(this T log, MsaId userId)
            where T : Base
        {
            UserInfo userInfo = CreateUserInfo(userId);

            log.LogInformational(DefaultLogOption, fillEnvelope: userInfo.FillEnvelope);
        }

        /// <summary>
        ///     Logs the given telemetry log as informational and populates the device info property using the DeviceInfo
        /// </summary>
        /// <param name="log">Log to log as informational</param>
        /// <param name="deviceId">Device Id</param>
        public static void LogInformational<T>(this T log, long? deviceId)
            where T : Base
        {
            log.LogInformational(DefaultLogOption, fillEnvelope: CreateDeviceInfo(deviceId).FillEnvelope);
        }

        /// <summary>
        ///     Logs the given telemetry log as warning and populates the user info property using the MsaId
        /// </summary>
        /// <param name="log">Log to log as warning</param>
        /// <param name="userId">MsaId to populate the user info with</param>
        public static void LogWarning<T>(this T log, MsaId userId)
            where T : Base
        {
            UserInfo userInfo = CreateUserInfo(userId);

            log.LogWarning(DefaultLogOption, fillEnvelope: userInfo.FillEnvelope);
        }
    }
}
