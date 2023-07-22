// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Helpers;

    /// <summary>
    ///     A merging resource source takes a list of <see cref="IResourceSource{T}" /> and merges them into
    ///     a single source. It requires a compare function as the chooser for which of the sources to pull
    ///     each next item from.
    /// </summary>
    public class MergingResourceSource<T> : IResourceSource<T>
        where T : class
    {
        private readonly Func<T, T, int> compareFunc;

        private readonly HashSet<IResourceSource<T>> finishedSources = new HashSet<IResourceSource<T>>();

        private readonly IDictionary<string, IResourceSource<T>> sources;

        /// <summary>
        ///     Constructs a <see cref="MergingResourceSource{T}" />
        /// </summary>
        /// <param name="compareFunc">The comparison function to use for choosing which resource source to pull a resource from</param>
        /// <param name="sources">
        ///     A dictionary of sources. Each source must be named uniquely and repeatable across calls in the dictionary key so that
        ///     continuation tokens work correctly.
        /// </param>
        public MergingResourceSource(Func<T, T, int> compareFunc, IDictionary<string, IResourceSource<T>> sources)
        {
            if (compareFunc == null)
                throw new ArgumentNullException(nameof(compareFunc));
            if (sources == null)
                throw new ArgumentNullException(nameof(sources));

            this.sources = sources;
            this.compareFunc = compareFunc;
        }

        public async Task ConsumeAsync(int count)
        {
            await this.PeekAsync(count, true).ConfigureAwait(false);
        }

        public async Task<IList<T>> FetchAsync(int count)
        {
            return await this.PeekAsync(count, true).ConfigureAwait(false);
        }

        public ResourceSourceContinuationToken GetNextToken()
        {
            var token = new MergingResourceSourceContinuationToken
            {
                SubTokens = this.sources.ToDictionary(s => s.Key, s => this.finishedSources.Contains(s.Value) ? null : s.Value.GetNextToken())
            };
            return token.SubTokens.Values.Any(t => t != null) ? token : null;
        }

        public async Task<IList<T>> PeekAsync(int count)
        {
            return await this.PeekAsync(count, false).ConfigureAwait(false);
        }

        public void SetNextToken(ResourceSourceContinuationToken token)
        {
            if (token == null)
                return;
            var myToken = token as MergingResourceSourceContinuationToken;
            if (myToken == null)
                throw new ArgumentOutOfRangeException(nameof(token));

            foreach (KeyValuePair<string, IResourceSource<T>> sourcePair in this.sources)
            {
                ResourceSourceContinuationToken subToken;
                if (!myToken.SubTokens.TryGetValue(sourcePair.Key, out subToken))
                {
                    // Better to continue anyway, I think. This is getting a token for a source we no longer have.
                    // throw new ArgumentOutOfRangeException(nameof(token), "Tokens do not match");
                }
                else
                {
                    if (subToken != null)
                        sourcePair.Value.SetNextToken(subToken);
                    else
                        this.finishedSources.Add(sourcePair.Value);
                    myToken.SubTokens.Remove(sourcePair.Key);
                }
            }

            if (myToken.SubTokens.Count > 0)
            {
                // Better to continue anyway, I think. This is if we have no token for a source we have.
                //throw new ArgumentOutOfRangeException(nameof(token), "Excess tokens");
            }
        }

        private async Task<IList<T>> PeekAsync(int count, bool andConsume)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            var comparer = new DelegatedComparer<T>(this.compareFunc);

            // First, get a full page from each source. This is over-querying, as we are asking for count*sources amount of data, but
            // we have no way of knowing ahead of time if one source will fulfill the entire count or not. If we don't over-query then
            // if any of our sources are aggregation sources, they will aggregate incorrectly, so we have no choice but to over-query
            // here.
            Dictionary<string, Task<IList<T>>> sourceTasks = this.sources.Where(s => !this.finishedSources.Contains(s.Value))
                .ToDictionary(s => s.Key, s => s.Value.PeekAsync(count));
            await Task.WhenAll(sourceTasks.Values).ConfigureAwait(false);
            Dictionary<string, IList<T>> sourceResults = sourceTasks.ToDictionary(s => s.Key, s => s.Value.Result);

            // Consume the results until we have fulfilled the request or are out of results
            var results = new List<T>();
            while (results.Count < count && sourceResults.Any(r => r.Value.Count > 0))
            {
                var bestEntry = sourceResults
                    .Where(s => s.Value.Count > 0)
                    .Select(s => new { s.Key, TopResult = s.Value[0] })
                    .OrderBy(e => e.TopResult, comparer)
                    .First();

                results.Add(bestEntry.TopResult);
                sourceResults[bestEntry.Key].RemoveAt(0);
                if (andConsume)
                    await this.sources[bestEntry.Key].ConsumeAsync(1).ConfigureAwait(false);
            }

            return results;
        }
    }
}
