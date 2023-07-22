// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Handlers
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     PerfCounterHandler
    /// </summary>
    public class PerfCounterHandler : DelegatingHandler
    {
        private readonly ICounterFactory counterFactory;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PerfCounterHandler" /> class.
        /// </summary>
        /// <param name="counterFactory">The counter factory.</param>
        public PerfCounterHandler(ICounterFactory counterFactory)
        {
            this.counterFactory = counterFactory;
        }

        /// <summary>
        ///     This method will wrap around the processing pipeline and perform perf counter tracking. The tracking is
        ///     configurable per-action.
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        ///     The response from processing the request.
        /// </returns>
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string apiName = request.GetApiName();
            if (apiName == ApiRouteMapping.DefaultApiName)
            {
                IfxTraceLogger logger = IfxTraceLogger.Instance;
                logger.Warning(nameof(PerfCounterHandler), $"No API Name found for {request.RequestUri.AbsolutePath}");
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            TimedHttpOperationExecutionResult result = await RequestExecutionHelper.ExecuteTimedHttpActionAsync(
                this.counterFactory,
                PrivacyCounterCategoryNames.PrivacyExperienceService,
                apiName,
                async () => await base.SendAsync(request, cancellationToken)).ConfigureAwait(false);

            if (result.Exception != null)
            {
                throw result.Exception;
            }

            return result.Response;
        }
    }
}
