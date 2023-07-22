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

    public class SearchHistoryV2Controller : MockCommonV2Controller
    {
        public const string ResourceCollectionName = "searchhistory";

        [HttpPost]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteChildSearchHistory(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteSearchHistory("child", null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMySearchHistory(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteSearchHistory(null, null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyDeviceSearchHistory(
            string deviceId,
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteSearchHistory("device", deviceId, filterQuery, requestBody);
        }

        [HttpGet]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMySearchHistory(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$count")] string count = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            if (this.IsGetCountRequest(this.Request))
            {
                return this.GetCardTypeCountAsync(TimelineCard.CardTypes.SearchCard);
            }

            return this.GetSearchHistory(null, null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyChildSearchHistory(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetSearchHistory("child", null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyDeviceSearchHistory(
            string deviceId,
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetSearchHistory("device", deviceId, filter, search, orderby, maxpagesize, skip);
        }

        private Task<HttpResponseMessage> DeleteSearchHistory(string childOrDevice, string deviceId, string filterQuery, DeleteRequestV2 deleteRequest)
        {
            return RequestHandler.Wrapper(
                this.Request,
                async () =>
                {
                    MsaSelfIdentity identity = this.GetAndValidateIdentity(childOrDevice, deviceId);
                    await Task.Delay(LatencyMilliseconds);
                    return this.DoDelete<SearchResourceV2>(
                        filterQuery,
                        deleteRequest,
                        SearchHistoryStoreV2.EdmProperties,
                        SearchHistoryStoreV2.GetValueByName,
                        () => SearchHistoryStoreV2.Instance.DeleteAllItems(identity.TargetPuid.Value),
                        p => SearchHistoryStoreV2.Instance.DeleteWhere(identity.TargetPuid.Value, p));
                });
        }

        private Task<HttpResponseMessage> GetSearchHistory(
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

                    PagedResponseV2<SearchResourceV2> response = ApplyFilterOrderPaging(
                        SearchHistoryStoreV2.Instance.Get(identity.TargetPuid.Value),
                        filter,
                        search,
                        orderBy,
                        maxpagesize,
                        skip,
                        this.Request.RequestUri,
                        SearchHistoryStoreV2.EdmProperties,
                        SearchHistoryStoreV2.EdmFullTextProperties,
                        SearchHistoryStoreV2.GetValueByName);

                    await Task.Delay(LatencyMilliseconds);
                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }
    }
}
