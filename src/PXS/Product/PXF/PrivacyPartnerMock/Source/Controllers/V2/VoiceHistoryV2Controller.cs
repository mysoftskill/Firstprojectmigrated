// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyMockService.DataSource;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Security;

    public class VoiceHistoryV2Controller : MockCommonV2Controller
    {
        public const string ResourceCollectionName = "voicehistory";

        [HttpPost]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteChildVoiceHistory(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteVoiceHistory("child", null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyVoiceHistory(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteVoiceHistory(null, null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyDeviceVoiceHistory(
            string deviceId,
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteVoiceHistory("device", deviceId, filterQuery, requestBody);
        }

        [HttpGet]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyVoiceHistory(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$count")] string count = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            if (this.IsGetCountRequest(this.Request))
            {
                return this.GetCardTypeCountAsync(TimelineCard.CardTypes.VoiceCard);
            }

            return this.GetVoiceHistory(null, null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyChildVoiceHistory(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetVoiceHistory("child", null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyDeviceVoiceHistory(
            string deviceId,
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetVoiceHistory("device", deviceId, filter, search, orderby, maxpagesize, skip);
        }

        private Task<HttpResponseMessage> DeleteVoiceHistory(string childOrDevice, string deviceId, string filterQuery, DeleteRequestV2 deleteRequest)
        {
            return RequestHandler.Wrapper(
                this.Request,
                async () =>
                {
                    MsaSelfIdentity identity = this.GetAndValidateIdentity(childOrDevice, deviceId);
                    await Task.Delay(LatencyMilliseconds);
                    return this.DoDelete<VoiceResourceV2>(
                        filterQuery,
                        deleteRequest,
                        VoiceHistoryStoreV2.EdmProperties,
                        VoiceHistoryStoreV2.GetValueByName,
                        () => VoiceHistoryStoreV2.Instance.DeleteAllItems(identity.TargetPuid.Value),
                        p => VoiceHistoryStoreV2.Instance.DeleteWhere(identity.TargetPuid.Value, p));
                });
        }

        private Task<HttpResponseMessage> GetVoiceHistory(
            string childOrDevice,
            string deviceId,
            [FromUri(Name = "$filter")] string filter,
            [FromUri(Name = "$search")] string search,
            [FromUri(Name = "$orderBy")] string orderBy,
            [FromUri(Name = "$maxpagesize")] int maxpagesize,
            [FromUri(Name = "$skip")] int skip)
        {
            return RequestHandlerV2.Wrapper(
                this.Request,
                async () =>
                {
                    MsaSelfIdentity identity = this.GetAndValidateIdentity(childOrDevice, deviceId);

                    PagedResponseV2<VoiceResourceV2> response = ApplyFilterOrderPaging(
                        VoiceHistoryStoreV2.Instance.Get(identity.TargetPuid.Value),
                        filter,
                        search,
                        orderBy,
                        maxpagesize,
                        skip,
                        this.Request.RequestUri,
                        VoiceHistoryStoreV2.EdmProperties,
                        VoiceHistoryStoreV2.EdmFullTextProperties,
                        VoiceHistoryStoreV2.GetValueByName);

                    if (response.Items.Count() != 1)
                    {
                        // If this is a filter down to a single item, then return the extra voice audio data
                        // Here, the logic is a bit inverse since it's all in memory, specifically if there is more
                        // than one result, then make a copy of all the results without the audio data so it doesn't
                        // get returned.
                        response.Items = response.Items.Select(
                            i => new VoiceResourceV2()
                            {
                                Application = i.Application,
                                DeviceType = i.DeviceType,
                                DisplayText = i.DisplayText,
                                Id = i.Id,
                                DateTime = i.DateTime,
                                DeviceId = i.DeviceId,
                                Sources = i.Sources
                            }).ToList();
                    }

                    await Task.Delay(LatencyMilliseconds);
                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }
    }
}
