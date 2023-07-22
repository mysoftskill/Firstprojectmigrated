// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.Http;

    using Microsoft.Data.Edm;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Security;
    using Microsoft.PrivacyServices.Common.Azure;

    public class MockCommonV2Controller : ApiController
    {
        protected const string ChildRoutePrefixPdapiV2 = "v2/my/child/";

        protected const string DeviceRoutePrefixPdapiV2 = "v2/my/device/{deviceId}/";

        protected const string MyRoutePrefixPdapiV2 = "v2/my/";

        protected const int LatencyMilliseconds = 10;

        protected HttpResponseMessage DoDelete<T>(
            string filterQuery,
            DeleteRequestV2 deleteRequest,
            IDictionary<string, EdmPrimitiveTypeKind> properties,
            Func<object, string, object> getValueByName,
            Action deleteAll,
            Action<Predicate<T>> deleteSome)
        {
            if (deleteRequest == null)
                deleteRequest = new DeleteRequestV2();
            if (filterQuery != null)
            {
                if (deleteRequest.Filters == null)
                    deleteRequest.Filters = new List<string>();
                deleteRequest.Filters.Add(filterQuery);
            }

            HttpResponseMessage errorResponse;
            if ((errorResponse = ValidateDeleteRequest(this.Request, deleteRequest)) != null)
                return errorResponse;

            if (deleteRequest.Filters == null)
                deleteAll();
            else
            {
                foreach (string filter in deleteRequest.Filters)
                {
                    var odataFilter = new ODataFilter(properties, filter, getValueByName);
                    deleteSome(r => odataFilter.Matches(r));
                }
            }

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        ///     Validates that the the user identity has been authenticated and returns the identity object
        /// </summary>
        /// <param name="childOrDevice">'child', 'devices', or null</param>
        /// <param name="deviceId">DeviceId if childOrDevice == 'devices'</param>
        /// <returns>Msa self-auth identity object</returns>
        protected MsaSelfIdentity GetAndValidateIdentity(string childOrDevice, string deviceId)
        {
            bool oboAuth = childOrDevice?.ToUpperInvariant() == "CHILD";
            var identity = this.User.Identity as MsaSelfIdentity;
            if (identity == null)
            {
                throw new SecurityException("Identity not found in User context.");
            }

            if (!identity.IsAuthenticated)
            {
                // For unauthenticated users, since this is a mock service, pretend an auth with random constant id
                return new MsaSelfIdentity { TargetPuid = 4657298745, AuthorizingPuid = oboAuth ? new long?(68296724) : null };
            }

            if (oboAuth && identity.AuthorizingPuid == identity.TargetPuid)
            {
                throw new SecurityException("OBO auth must have family token with child target.");
            }

            return identity;
        }

        /// <summary>
        ///     Applies filtering to the item list
        /// </summary>
        /// <typeparam name="T">Collection type</typeparam>
        /// <param name="items">List of items</param>
        /// <param name="filter">Filter expression</param>
        /// <param name="fullTextSearch">Full text search</param>
        /// <param name="orderBy">Order by expression</param>
        /// <param name="maxpagesize">Max page size</param>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="uri">Query uri</param>
        /// <param name="properties">Properties on the object</param>
        /// <param name="fullTextProperties">Properties on the object valid for full text search</param>
        /// <param name="getPropertyFunc">Function to retrieve the value of a property</param>
        /// <returns>Filtered list of items</returns>
        protected static PagedResponseV2<T> ApplyFilterOrderPaging<T>(
            IEnumerable<T> items,
            string filter,
            string fullTextSearch,
            string orderBy,
            int maxpagesize,
            int skip,
            Uri uri,
            IDictionary<string, EdmPrimitiveTypeKind> properties,
            IDictionary<string, EdmPrimitiveTypeKind> fullTextProperties,
            Func<object, string, object> getPropertyFunc)
        {
            DualLogger.Instance?.Information(nameof(MockCommonV2Controller), $"ApplyFilterOrderPaging parameter filter={filter} fullTextSearch={fullTextSearch} orderBy={orderBy} maxpagesize={maxpagesize} skip={skip}");
            DualLogger.Instance?.Information(nameof(MockCommonV2Controller), $"ApplyFilterOrderPaging Input items {items.Count()}, Type {typeof(T).Name}");

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var odataFilter = new ODataFilter(properties, filter, getPropertyFunc);

                items = items.Where(i => odataFilter.Matches(i));
                DualLogger.Instance?.Information(nameof(MockCommonV2Controller), $"ApplyFilterOrderPaging Items after filtering {items.Count()}");
            }
            if (!string.IsNullOrWhiteSpace(fullTextSearch))
            {
                items = items.Where(
                    i => fullTextProperties
                        .Any(p => getPropertyFunc(i, p.Key)?.ToString().ToUpper().Contains(fullTextSearch.ToUpper()) ?? false));
            }
            if (orderBy != null)
            {
                if (orderBy.Equals("date", StringComparison.InvariantCultureIgnoreCase) || orderBy.Equals("dateTime", StringComparison.InvariantCultureIgnoreCase))
                    items = items.OrderByDescending(r => (DateTimeOffset)getPropertyFunc(r, orderBy));
                else
                    throw new NotSupportedException($"Cannot order by {orderBy}");
                DualLogger.Instance?.Information(nameof(MockCommonV2Controller), $"ApplyFilterOrderPaging Items after orderBy {items.Count()}");
            }
            if (skip > 0)
                items = items.Skip(skip);

            Uri nextLink = null;
            List<T> itemsList = items.ToList();

            DualLogger.Instance?.Information(nameof(MockCommonV2Controller), $"ApplyFilterOrderPaging found {itemsList.Count} matches");

            if (maxpagesize > -1 && itemsList.Count > maxpagesize)
            {
                itemsList = itemsList.Take(maxpagesize).ToList();

                NameValueCollection queryParams = uri.ParseQueryString();
                queryParams["$skip"] = (skip > 0 ? skip + maxpagesize : maxpagesize).ToString();

                var sb = new StringBuilder();
                foreach (string key in queryParams.AllKeys)
                {
                    if (sb.Length > 0)
                        sb.Append("&");
                    sb.Append(HttpUtility.UrlEncode(key) + "=");
                    sb.Append(HttpUtility.UrlEncode(queryParams[key]));
                }

                var builer = new UriBuilder(uri) { Query = sb.ToString() };
                nextLink = builer.Uri;
            }

            return new PagedResponseV2<T>
            {
                Items = itemsList,
                NextLink = nextLink
            };
        }

        protected static HttpResponseMessage ValidateDeleteRequest(HttpRequestMessage requestMessage, DeleteRequestV2 deleteRequest)
        {
            if (!requestMessage.RequestUri.ParseQueryString().AllKeys.Contains("delete", StringComparison.OrdinalIgnoreCase))
            {
                return requestMessage.CreateErrorResponse(HttpStatusCode.BadRequest, "URL must contain the 'delete' query flag.");
            }

            if (deleteRequest == null)
            {
                return requestMessage.CreateErrorResponse(HttpStatusCode.BadRequest, "Body did not contain a DeleteRequest object.");
            }

            return null;
        }

        protected bool IsGetCountRequest(HttpRequestMessage request)
        {
            var queryStrings = request.GetQueryNameValuePairs();
            if (queryStrings == null || queryStrings.Count() == 0)
            {
                return false;
            }

            foreach(var kvp in queryStrings)
            {
                if (string.Compare(kvp.Key, "$count", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        protected Task<HttpResponseMessage> GetCardTypeCountAsync(string cardType)
        {
            return RequestHandlerV2.Wrapper(
                this.Request,
                async () =>
                {
                    this.ValidateIdentity();

                    // Mock PDOS get aggregate count API result.
                    var response = new AggregateCountResponse()
                    {
                        AggregateCounts = new Dictionary<string, int> { { cardType, 6 } }
                    };

                    await Task.Delay(LatencyMilliseconds);
                    return this.Request.CreateResponse(HttpStatusCode.OK, response);
                });
        }

        private bool ValidateIdentity()
        {
            var identity = this.User.Identity as MsaSelfIdentity;
            if (identity == null)
            {
                throw new SecurityException("Identity not found in User context.");
            }

            return true;
        }
    }
}
