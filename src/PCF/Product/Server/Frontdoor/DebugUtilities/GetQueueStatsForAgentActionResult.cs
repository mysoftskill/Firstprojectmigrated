namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.CommandFeed.Service.Common;

    [ExcludeFromCodeCoverage] // Justification: not product code
    internal class GetQueueStatsForAgentActionResult : BaseHttpActionResult
    {
        private readonly HttpRequestMessage request;
        private readonly IAuthorizer authorizer;
        private readonly ICommandQueue agentCommandQueue;
        private readonly AuthenticationScope authenticationScope;
        private readonly IDataAgentMap dataAgentMap;
        private readonly bool getDetailedStatistics;

        public GetQueueStatsForAgentActionResult(
            HttpRequestMessage requestMessage,
            ICommandQueue agentCommandQueue,
            IDataAgentMap dataAgentMap,
            bool getDetailedStatistics,
            IAuthorizer authorizer,
            AuthenticationScope authenticationScope)
        {
            this.request = requestMessage;
            this.agentCommandQueue = agentCommandQueue;
            this.authorizer = authorizer;
            this.authenticationScope = authenticationScope;
            this.getDetailedStatistics = getDetailedStatistics;
            this.dataAgentMap = dataAgentMap;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.request, this.authenticationScope);

            // Discover asset groups.
            ConcurrentBag<AgentQueueStatistics> resultsBag = new ConcurrentBag<AgentQueueStatistics>();
            await this.agentCommandQueue.AddQueueStatisticsAsync(resultsBag, this.getDetailedStatistics, cancellationToken);

            // Group by asset group and subject type.
            var grouping = resultsBag.GroupBy(x => (x.SubjectType, x.AssetGroupId));

            // Aggregate each assetgroup / subject type into one logical item.
            var response = new PXS.Command.CommandStatus.AgentQueueStatisticsResponse
            {
                AssetGroupQueueStatistics = grouping.Select(this.Aggregate).ToList(),
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new JsonContent(response)
            };
        }

        private PXS.Command.CommandStatus.AssetGroupQueueStatistics Aggregate(IEnumerable<AgentQueueStatistics> grouping)
        {
            var first = grouping.First();

            return new PXS.Command.CommandStatus.AssetGroupQueueStatistics
            {
                AgentId = first.AgentId.GuidValue,
                AssetGroupId = first.AssetGroupId.GuidValue,
                AssetGroupQualifier = this.GetAssetGroupQualifierOrNull(first),
                SubjectType = first.SubjectType.ToString(),
                MinimumLeaseAvailableTime = AggregateNonNullItems(grouping, x => x.MinLeaseAvailableTime, x => x.Min()),
                OldestPendingCommand = AggregateNonNullItems(grouping, x => x.MinPendingCommandCreationTime, x => x.Min()),
                PendingCommandCount = AggregateNonNullItems(grouping, x => x.PendingCommandCount, x => x.Sum()),
                UnleasedCommandCount = AggregateNonNullItems(grouping, x => x.UnleasedCommandCount, x => x.Sum()),
            };
        }

        private static TValue? AggregateNonNullItems<TSource, TValue>(
            IEnumerable<TSource> items, 
            Func<TSource, TValue?> selector, 
            Func<IEnumerable<TValue>, TValue> aggregator) where TValue : struct
        {
            var nonNullItems = items.Select(selector).Where(x => x != null).Select(x => x.Value);
            if (nonNullItems.Any())
            {
                return aggregator(nonNullItems);
            }

            return null;
        }

        private string GetAssetGroupQualifierOrNull(AgentQueueStatistics stats)
        {
            string qualifier = null;

            if (this.dataAgentMap.TryGetAgent(stats.AgentId, out var agentInfo) && 
                agentInfo.TryGetAssetGroupInfo(stats.AssetGroupId, out var assetGroupInfo))
            {
                qualifier = assetGroupInfo.AssetGroupQualifier;
            }

            return qualifier;
        }
    }
}
