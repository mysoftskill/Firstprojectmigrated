// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.DDS
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Security;
    using Microsoft.Windows.Services.Devices.ApiContract;

    /// <summary>
    ///     Mock controller for DDS endpoint
    /// </summary>
    public class MockGetUserDevicesController : MockCommonController
    {
        private static readonly Random random = new Random();

        /// <summary>
        ///     GET: /DeviceStore/self/Users('{puid}')/?$expand=UserDevices($expand=Device($select=DeviceInfo,DeviceId))
        ///     GET the devices associated with the user.
        /// </summary>
        /// <param name="puid">The puid of the user. Must be in hex. Must match the puid from proxy ticket.</param>
        [Route("DeviceStore/self/Users('{puid}')")]
        public HttpResponseMessage GetUserDevices(string puid)
        {
            if (!Regex.IsMatch(puid, "^[a-fA-F0-9]{16}$"))
            {
                return this.Request.CreateErrorResponse(HttpStatusCode.BadRequest, "PUID must be hex and 16-digits.");
            }

            MsaSelfIdentity identity = this.GetAndValidateIdentity("me");

            if (long.Parse(puid, NumberStyles.HexNumber) != identity.TargetPuid)
            {
                return this.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not authorized for this resource");
            }

            User mockResponse = CreateMockDdsUserDevicesResponse();
            mockResponse.Puid = puid;
            return this.Request.CreateResponse(mockResponse);
        }

        private static User CreateMockDdsUserDevicesResponse()
        {
            // generate a random number of device ids.
            int maxDevices = random.Next(1, 10);
            var userDevices = new List<UserDevice>();

            for (int i = 0; i < maxDevices; i++)
            {
                var userDevice = new UserDevice();
                userDevice.DeviceId = GenerateRandomGlobalDeviceId();
                userDevices.Add(userDevice);
            }

            User user = new User { UserDevices = userDevices };
            return user;
        }

        private static string GenerateRandomGlobalDeviceId()
        {
            // Needs to be 16 digits to be considered a valid device id
            return $"global[{GlobalDeviceIdGenerator.Generate():X16}]";
        }
    }
}
