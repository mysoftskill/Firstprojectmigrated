namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.DeviceDeleteHelper
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Vortex.Event;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.ClientLibrary.Helpers;
    using Microsoft.OSGS.HttpClientCommon;
    using Newtonsoft.Json;

    public static class DeviceDeleteHelper
    {
        /// <summary>
        ///     Creates a device delete request message.
        /// </summary>
        /// <param name="jsonEvents">string containing all vortex events in json format</param>
        /// <returns>Task of HttpRequestMessage</returns>
        private static async Task<HttpRequestMessage> CreateDeviceDeleteRequestAsync(string jsonEvents)
        {
            HttpRequestMessage request = PrivacyExperienceRequestHelper.CreateBasicRequest(
                HttpMethod.Post,
                new Uri(RouteNames.VortexIngestionDeviceDeleteV1, UriKind.Relative),
                "diagdelete",
                Guid.NewGuid().ToString(),
                null);
            request.Headers.Add(HeaderNames.VortexServedBy, "functestserver");
            request.Headers.Add(HeaderNames.WatchdogRequest, new[] { true.ToString() });
            HttpContent contents = new ByteArrayContent(Encoding.UTF8.GetBytes(jsonEvents));
            request.Content = await contents.CompressGZip().ConfigureAwait(false);
            return request;
        }

        /// <summary>
        ///     Creates a json list of vortex events
        /// </summary>
        /// <param name="correlationVector">unique identifier for requests</param>
        /// <param name="deviceIds">list of device ids for the event</param>
        /// <param name="userId">user id for the event</param>
        /// <returns>String of vortex events</returns>
        private static string CreateJsonVortexEvents(string correlationVector, List<string> deviceIds, string userId)
        {
            var testEvents = new VortexEvent[deviceIds.Count];
            for (int i = 0; i < deviceIds.Count; i++)
            {
                testEvents[i] = new VortexEvent
                {
                    Time = DateTimeOffset.UtcNow,
                    CorrelationVector = correlationVector,
                    Ext = new VortexEvent.Extensions
                    {
                        Device = new VortexEvent.Device
                        {
                            Id = deviceIds[i],
                        },
                        User = new VortexEvent.User
                        {
                            Id = userId,
                        },
                    },
                    Data = new VortexEvent.VortexData
                    {
                        IsInitiatedByUser = RandomHelper.Next(0, 2),
                    }
                };
            }

            var vortexEvents = new VortexEvents
            {
                Events = testEvents
            };

            return JsonConvert.SerializeObject(vortexEvents);
        }

        /// <summary>
        ///     Sends a device delete request for a given device id.
        /// </summary>
        /// <param name="testHttpClient">a test http client</param>
        /// <param name="deviceIdString">device id string</param>
        /// <returns>Task of HttpResponseMessage</returns>
        public static async Task<HttpResponseMessage> SendDeviceDeleteRequest(IHttpClient testHttpClient, string deviceIdString)
        {
            string jsonEvents = CreateJsonVortexEvents("correlationvector", new List<string> { deviceIdString }, "p:123456");
            HttpRequestMessage request = await CreateDeviceDeleteRequestAsync(jsonEvents).ConfigureAwait(false);
            return await testHttpClient.SendAsync(request).ConfigureAwait(false); ;
        }

        /// <summary>
        ///     Sends a device delete request for a list of vortex events.
        /// </summary>
        /// <param name="testHttpClient">a test http client</param>
        /// <param name="vortexEvents">a list of vortex device delete events</param>
        /// <returns>Task of HttpResponseMessage</returns>
        public static async Task<HttpResponseMessage> SendDeviceDeleteRequest(IHttpClient testHttpClient, string[] vortexEvents)
        {
            HttpRequestMessage request = await CreateDeviceDeleteRequestAsync(@"{ ""Events"": [" + string.Join("", vortexEvents) + @"] }").ConfigureAwait(false);
            return await testHttpClient.SendAsync(request).ConfigureAwait(false);
        }

        /// <summary>
        ///     Sends a specified number of random device delete requests.
        /// </summary>
        /// <param name="testHttpClient">a test http client</param>
        /// <param name="deviceIds">the device id strings</param>
        /// <returns>Task of HttpResponseMessage</returns>
        public static async Task<HttpResponseMessage> SendRandomDeviceDeleteRequests(IHttpClient testHttpClient, List<string> deviceIds)
        {
            string jsonEvents = CreateJsonVortexEvents("correlationvector", deviceIds, "p:123456");
            HttpRequestMessage request = await CreateDeviceDeleteRequestAsync(jsonEvents).ConfigureAwait(false);
            return await testHttpClient.SendAsync(request).ConfigureAwait(false);
        }
    }
}
