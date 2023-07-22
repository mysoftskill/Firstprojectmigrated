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

    public class AppUsageV2Controller : MockCommonV2Controller
    {
        public const string ResourceCollectionName = "appusage";

        [HttpPost]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteChildAppUsage(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteAppUsage("child", null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyAppUsage(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteAppUsage(null, null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyDeviceAppUsage(
            string deviceId,
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteAppUsage("device", deviceId, filterQuery, requestBody);
        }

        [HttpGet]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyAppUsage(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$count")] string count = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            if (this.IsGetCountRequest(this.Request))
            {
                return this.GetCardTypeCountAsync(TimelineCard.CardTypes.AppUsageCard);
            }

            return this.GetAppUsage(null, null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyChildAppUsage(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetAppUsage("child", null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyDeviceAppUsage(
            string deviceId,
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetAppUsage("device", deviceId, filter, search, orderby, maxpagesize, skip);
        }


        private Task<HttpResponseMessage> DeleteAppUsage(string childOrDevice, string deviceId, string filterQuery, DeleteRequestV2 deleteRequest)
        {
            return RequestHandler.Wrapper(
                this.Request,
                async () =>
                {
                    MsaSelfIdentity identity = this.GetAndValidateIdentity(childOrDevice, deviceId);
                    await Task.Delay(LatencyMilliseconds);
                    return this.DoDelete<AppUsageResourceV2>(
                        filterQuery,
                        deleteRequest,
                        AppUsageStoreV2.EdmProperties,
                        AppUsageStoreV2.GetValueByName,
                        () => AppUsageStoreV2.Instance.DeleteAllItems(identity.TargetPuid.Value),
                        p => AppUsageStoreV2.Instance.DeleteWhere(identity.TargetPuid.Value, p));
                });
        }

        private Task<HttpResponseMessage> GetAppUsage(
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

                    PagedResponseV2<AppUsageResourceV2> response = ApplyFilterOrderPaging(
                        AppUsageStoreV2.Instance.Get(identity.TargetPuid.Value),
                        filter,
                        search,
                        orderBy,
                        maxpagesize,
                        skip,
                        this.Request.RequestUri,
                        AppUsageStoreV2.EdmProperties,
                        AppUsageStoreV2.EdmFullTextProperties,
                        AppUsageStoreV2.GetValueByName);

                    await Task.Delay(LatencyMilliseconds);
                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }
    }
}
