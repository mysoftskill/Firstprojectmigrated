// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.XboxAccounts
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts.Models;

    public class XboxAccountsController : ApiController
    {
        private const string GetUserLookupInfoApiRelativePath = @"users/{0}({1})/lookup";

        private const string XassTokenRelativeUrl = @"service/authenticate";

        private const string XasuTokenRelativeUrl = @"user/authenticate";

        private const string XstsTokenRelativeUrl = @"xsts/authorize";

        private const string GetUsersLookupInfoApiRelativePath = @"users/puid/lookup";

        [HttpPost]
        [Route(XassTokenRelativeUrl)]
        public HttpResponseMessage GetXassToken()
        {
            var response = new XassResponse
            {
                Token = "xassToken",
                IssueInstant = DateTime.UtcNow,
                NotAfter = DateTime.UtcNow.AddDays(1)
            };
            return this.Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [HttpPost]
        [Route(XasuTokenRelativeUrl)]
        public HttpResponseMessage GetXasuToken()
        {
            var response = new XasuResponse
            {
                Token = "xasuToken",
                IssueInstant = DateTime.UtcNow,
                NotAfter = DateTime.UtcNow.AddDays(1)
            };
            return this.Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [HttpPost]
        [Route(XstsTokenRelativeUrl)]
        public HttpResponseMessage GetXstsToken([FromBody] XstsRequest request)
        {
            XasuResponse response = null;
            if (request.Properties?.ServiceToken != null)
            {
                response = new XasuResponse
                {
                    Token = "xstsToken",
                    IssueInstant = DateTime.UtcNow,
                    NotAfter = DateTime.UtcNow.AddDays(1)
                };
            }
            else
            {
                response = new XasuResponse
                {
                    Token = "xstsToken",
                    IssueInstant = DateTime.UtcNow,
                    NotAfter = DateTime.UtcNow.AddDays(1),
                    DisplayClaims = new XboxAuthDisplayClaims
                    {
                        XuiClaims = new[] { new XuiClaims { Xuid = "999" } }
                    }
                };
            }

            return this.Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [HttpGet]
        [Route(GetUserLookupInfoApiRelativePath)]
        public HttpResponseMessage GetXboxLiveUserLookupInfo()
        {
            var response = new XboxLiveUserLookupInfo
            {
                Xuid = "999"
            };
            return this.Request.CreateResponse(HttpStatusCode.OK, response);
        }

        [HttpPost]
        [Route(GetUsersLookupInfoApiRelativePath)]
        public HttpResponseMessage GetXboxLiveUsersLookupInfo([FromBody] GetXboxLiveUsersLookupInfoRequest request)
        {
            var response = new XboxLiveUsersLookupInfo();
            var users = new List<XboxLiveUserLookupInfo>();
            if (request.Puids != null)
            {
                foreach (long puid in request.Puids)
                {
                    users.Add(
                        new XboxLiveUserLookupInfo()
                        {
                            Puid = puid,
                            Xuid = "123"
                        });
                }

                response.Users = users;
            }
            return this.Request.CreateResponse(HttpStatusCode.OK, response);
        }
    }
}
