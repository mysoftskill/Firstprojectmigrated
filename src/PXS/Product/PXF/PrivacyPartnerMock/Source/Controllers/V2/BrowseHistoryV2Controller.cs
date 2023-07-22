// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyMockService.DataSource;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Security;

    public class BrowseHistoryV2Controller : MockCommonV2Controller
    {
        public const string ResourceCollectionName = "browsehistory";

        [HttpPost]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteChildBrowseHistory(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteBrowseHistory("child", null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyBrowseHistory(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteBrowseHistory(null, null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyDeviceBrowseHistory(
            string deviceId,
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteBrowseHistory("device", deviceId, filterQuery, requestBody);
        }

        [HttpGet]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyBrowseHistory(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$count")] string count = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            if (this.IsGetCountRequest(this.Request))
            {
                return this.GetCardTypeCountAsync(TimelineCard.CardTypes.BrowseCard);
            }

            return this.GetBrowseHistory(null, null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyChildBrowseHistory(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetBrowseHistory("child", null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyDeviceBrowseHistory(
            string deviceId,
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetBrowseHistory("device", deviceId, filter, search, orderby, maxpagesize, skip);
        }

        private Task<HttpResponseMessage> DeleteBrowseHistory(string childOrDevice, string deviceId, string filterQuery, DeleteRequestV2 deleteRequest)
        {
            return RequestHandler.Wrapper(
                this.Request,
                async () =>
                {
                    MsaSelfIdentity identity = this.GetAndValidateIdentity(childOrDevice, deviceId);
                    await Task.Delay(LatencyMilliseconds);
                    return this.DoDelete<BrowseResourceV2>(
                        filterQuery,
                        deleteRequest,
                        BrowseHistoryStoreV2.EdmProperties,
                        BrowseHistoryStoreV2.GetValueByName,
                        () => BrowseHistoryStoreV2.Instance.DeleteAllItems(identity.TargetPuid.Value),
                        p => BrowseHistoryStoreV2.Instance.DeleteWhere(identity.TargetPuid.Value, p));
                });
        }

        private Task<HttpResponseMessage> GetBrowseHistory(
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

                    PagedResponseV2<BrowseResourceV2> response = ApplyFilterOrderPaging(
                        BrowseHistoryStoreV2.Instance.Get(identity.TargetPuid.Value),
                        filter,
                        search,
                        orderBy,
                        maxpagesize,
                        skip,
                        this.Request.RequestUri,
                        BrowseHistoryStoreV2.EdmProperties,
                        BrowseHistoryStoreV2.EdmFullTextProperties,
                        BrowseHistoryStoreV2.GetValueByName);

                    await Task.Delay(LatencyMilliseconds);
                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }
    }
}
