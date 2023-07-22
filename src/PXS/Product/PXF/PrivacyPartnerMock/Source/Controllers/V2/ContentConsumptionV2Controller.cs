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

    public class ContentConsumptionV2Controller : MockCommonV2Controller
    {
        public const string ResourceCollectionName = "contentconsumption";

        [HttpPost]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteChildContentConsumption(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteContentConsumption("child", null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyContentConsumption(
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteContentConsumption(null, null, filterQuery, requestBody);
        }

        [HttpPost]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> DeleteMyDeviceContentConsumption(
            string deviceId,
            [FromBody] DeleteRequestV2 requestBody,
            [FromUri(Name = "$filter")] string filterQuery = null)
        {
            return this.DeleteContentConsumption("device", deviceId, filterQuery, requestBody);
        }

        [HttpGet]
        [Route(ChildRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyChildContentConsumption(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$count")] string count = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            if (this.IsGetCountRequest(this.Request))
            {
                return this.GetCardTypeCountAsync(TimelineCard.CardTypes.BookConsumptionCard);
            }

            return this.GetContentConsumption("child", null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(MyRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyContentConsumption(
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetContentConsumption(null, null, filter, search, orderby, maxpagesize, skip);
        }

        [HttpGet]
        [Route(DeviceRoutePrefixPdapiV2 + ResourceCollectionName)]
        public Task<HttpResponseMessage> GetMyDeviceContentConsumption(
            string deviceId,
            [FromUri(Name = "$filter")] string filter = null,
            [FromUri(Name = "$search")] string search = null,
            [FromUri(Name = "$orderBy")] string orderby = null,
            [FromUri(Name = "$maxpagesize")] int maxpagesize = -1,
            [FromUri(Name = "$skip")] int skip = -1)
        {
            return this.GetContentConsumption("device", deviceId, filter, search, orderby, maxpagesize, skip);
        }

        private Task<HttpResponseMessage> DeleteContentConsumption(string childOrDevice, string deviceId, string filterQuery, DeleteRequestV2 deleteRequest)
        {
            return RequestHandler.Wrapper(
                this.Request,
                async () =>
                {
                    MsaSelfIdentity identity = this.GetAndValidateIdentity(childOrDevice, deviceId);
                    await Task.Delay(LatencyMilliseconds).ConfigureAwait(false);
                    return this.DoDelete<ContentConsumptionResourceV2>(
                        filterQuery,
                        deleteRequest,
                        ContentConsumptionStoreV2.EdmProperties,
                        ContentConsumptionStoreV2.GetValueByName,
                        () => ContentConsumptionStoreV2.Instance.DeleteAllItems(identity.TargetPuid.Value),
                        p => ContentConsumptionStoreV2.Instance.DeleteWhere(identity.TargetPuid.Value, p));
                });
        }

        private Task<HttpResponseMessage> GetContentConsumption(
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

                    PagedResponseV2<ContentConsumptionResourceV2> response = ApplyFilterOrderPaging(
                        ContentConsumptionStoreV2.Instance.Get(identity.TargetPuid.Value),
                        filter,
                        search,
                        orderBy,
                        maxpagesize,
                        skip,
                        this.Request.RequestUri,
                        ContentConsumptionStoreV2.EdmProperties,
                        ContentConsumptionStoreV2.EdmFullTextProperties,
                        ContentConsumptionStoreV2.GetValueByName);

                    await Task.Delay(LatencyMilliseconds).ConfigureAwait(false);
                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }
    }
}
