// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Web.Http;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyMockService.DataSource;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Security;

    public class CustomerMasterController : ApiController
    {
        private const string JarvisCustomerMasterRoutePrefix = "JarvisCM/{userId}/";
        private const string ProfilesRelativePath = "profiles";
        private const string PutPrivacyProfilesRelativePath = ProfilesRelativePath + "/{profileId}";
        
        private MsaSelfIdentity GetAndValidateIdentity(string userId)
        {
            switch (userId.ToUpperInvariant())
            {
                case "ME":
                    break;
                case "MY-FAMILY":
                    break;
                default:
                    throw new SecurityException("Must use 'me' or 'my-family' for userId.");
            }

            if (!(this.User.Identity is MsaSelfIdentity identity))
            {
                throw new SecurityException("Identity not found in User context.");
            }

            if (!identity.IsAuthenticated)
            {
                throw new SecurityException("Identity is not marked as authenticated.");
            }

            return identity;
        }

        [HttpGet]
        [Route(JarvisCustomerMasterRoutePrefix + ProfilesRelativePath)]
        public HttpResponseMessage GetPrivacyProfile(string userId, string type = null)
        {
            return Wrapper(
                this.Request,
                () =>
                {
                    var identity = this.GetAndValidateIdentity(userId);

                    var response = ProfileSettingsStore.Instance.Get(identity.TargetPuid.Value, type);

                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }

        [HttpPost]
        [Route(JarvisCustomerMasterRoutePrefix + ProfilesRelativePath)]
        public HttpResponseMessage CreatePrivacyProfile(string userId, [FromBody] PrivacyProfile profileContent)
        {
            return Wrapper(
                this.Request,
                () =>
                {
                    var identity = this.GetAndValidateIdentity(userId);

                    // CM accepts the 'create' type like this, but actually sets the type differently. This mocks their behavior.
                    if (string.Equals("MsaPrivacy", profileContent.Type))
                    {
                        profileContent.Type = "msa_privacy";
                    }

                    var response = ProfileSettingsStore.Instance.Create(identity.TargetPuid.Value, profileContent);

                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }

        [HttpPut]
        [Route(JarvisCustomerMasterRoutePrefix + PutPrivacyProfilesRelativePath)]
        public HttpResponseMessage UpdatePrivacyProfile(string userId, string profileId, [FromBody] PrivacyProfile profileContent)
        {
            return Wrapper(
                this.Request,
                () =>
                {
                    var identity = this.GetAndValidateIdentity(userId);

                    string etag = this.Request.Headers.GetValues("If-Match").SingleOrDefault();

                    var response = ProfileSettingsStore.Instance.Update(identity.TargetPuid.Value, profileContent, profileId, etag);

                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }

        private static HttpResponseMessage Wrapper(HttpRequestMessage request, Func<HttpResponseMessage> handler)
        {
            CustomerMasterError errorBody;

            try
            {
                return handler();
            }
            catch(CustomerMasterException e)
            {
                errorBody = e.Error;

                if (string.Equals(e.Error.ErrorCode, CustomerMasterErrorCode.ConcurrencyFailure.ToString()))
                {
                    return request.CreateResponse(HttpStatusCode.Conflict, errorBody);
                }
            }
            catch (Exception e)
            {
                errorBody = new CustomerMasterError
                {
                    ErrorCode = "UnexpectedError",
                    Message = e.ToString(),
                    ObjectType = "Error"
                };
            }

            return request.CreateResponse(HttpStatusCode.InternalServerError, errorBody);
        }
    }
}