// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    public enum PxfAdapterCapability
    {
        View,

        Delete
    }

    /// <summary>
    ///     Fans out resource calls to multiple providers and aggregates the result
    /// </summary>
    public interface IPxfDispatcher
    {

        /// <summary>
        ///     Deletes the AppUsage from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A Collection of deletion responses from all of the partners for this resource.</returns>
        Task<DeletionResponse<DeleteResourceResponse>> DeleteAppUsageAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Deletes the browse history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A Collection of deletion responses from all of the partners for this resource.</returns>
        Task<DeletionResponse<DeleteResourceResponse>> DeleteBrowseHistoryAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Deletes the location history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A Collection of deletion responses from all of the partners for this resource.</returns>
        Task<DeletionResponse<DeleteResourceResponse>> DeleteLocationHistoryAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Deletes the search history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="disableThrottling">if set to <c>true</c>, disable throttling for the request. Only test callerName is allowed to pass <c>true</c> for this.</param>
        /// <returns>A Collection of deletion responses from all of the partners for this resource.</returns>
        Task<DeletionResponse<DeleteResourceResponse>> DeleteSearchHistoryAsync(IPxfRequestContext requestContext, bool disableThrottling);

        /// <summary>
        ///     Deletes the voice history from configured partners.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>A Collection of deletion responses from all of the partners for this resource.</returns>
        Task<DeletionResponse<DeleteResourceResponse>> DeleteVoiceHistoryAsync(IPxfRequestContext requestContext);

        /// <summary>
        ///     Execute a function for each adapter and return all of the results
        /// </summary>
        Task<IList<T>> ExecuteForProvidersAsync<T>(
            IPxfRequestContext requestContext,
            Configuration.ResourceType resourceType,
            PxfAdapterCapability capability,
            Func<PartnerAdapter, Task<T>> execFunc);

        /// <summary>
        ///     Gets the adapters for a given resource type and capability (view, delete)
        /// </summary>
        IEnumerable<PartnerAdapter> GetAdaptersForResourceType(IPxfRequestContext context, Configuration.ResourceType resourceType, PxfAdapterCapability capability);

        /// <summary>
        /// Create DeletePolicyDataTypeTask
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="pxfRequestContext"></param>
        /// <returns></returns>
        Task<DeletionResponse<DeleteResourceResponse>> CreateDeletePolicyDataTypeTask(string dataType, IPxfRequestContext pxfRequestContext);
    }
}
