// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Contracts;
    using Microsoft.Membership.MemberServices.PrivacyMockService.DataSource;
    using Microsoft.Windows.Services.AuthN.Server;

    public class TestHookController : MockCommonController
    {
        private const int LatencyMilliseconds = 10;
        private static Dictionary<string, Action<long>> deleteActionsByResource = new Dictionary<string, Action<long>>
        {
            { BrowseHistoryV2Controller.ResourceCollectionName, puid =>
                {
                    BrowseHistoryStoreV2.Instance.DeleteUser(puid);
                }
            },
            { SearchHistoryV2Controller.ResourceCollectionName, puid =>
                {
                    SearchHistoryStoreV2.Instance.DeleteUser(puid);
                }
            },
            { LocationHistoryV2Controller.ResourceCollectionName, puid =>
                {
                    LocationHistoryStoreV2.Instance.DeleteUser(puid);
                }
            },
            { VoiceHistoryV2Controller.ResourceCollectionName, puid =>
                {
                    VoiceHistoryStoreV2.Instance.DeleteUser(puid);
                }
            },
            { AppUsageV2Controller.ResourceCollectionName, puid =>
                {
                    AppUsageStoreV2.Instance.DeleteUser(puid);
                }
            }
        };


        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("v1/{userId}/testhook/{resource}/{verb}")]
        public async Task<HttpResponseMessage> PostTestHook(string userId, string resource = null, string verb = null)
        {
            long puid = long.Parse(userId);

            switch (verb.ToLowerInvariant())
            {
                case "delete":
                    deleteActionsByResource[resource.ToLowerInvariant()](puid);
                    break;
                default:
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, "Unknown action '{0}' in test hook request.", verb));
            }

            await Task.Delay(LatencyMilliseconds);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("v1/{userId}/testhook/getproxyticket")]
        public async Task<HttpResponseMessage> GetProxyTicket(string userId, [FromBody] ProxyTicketRequest proxyTicketRequest)
        {
            string proxyTicket;
            using (var authServer = new RpsAuthServer())
            {
                var propertyBag = new RpsPropertyBag();

                var authResult = authServer.GetAuthResult("pxstest.api.account.microsoft.com", proxyTicketRequest.UserTicket, RpsTicketType.Compact, propertyBag);
                proxyTicket = authResult[RpsTicketField.ProxyTicket] as string;
            }

            var response = new ProxyTicketResponse
            {
                ProxyTicket = proxyTicket,
            };

            await Task.Delay(LatencyMilliseconds);
            return this.Request.CreateResponse(HttpStatusCode.OK, response);
        }
    }
}
