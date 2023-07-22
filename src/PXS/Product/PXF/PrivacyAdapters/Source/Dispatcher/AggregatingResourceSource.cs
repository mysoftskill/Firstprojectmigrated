// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    ///     An aggregating resource source. It contains another <see cref="IResourceSource{T}" /> and aggregates results
    ///     by the aggregation function provided.
    /// </summary>
    /// <remarks>
    ///     This resource source has special properties that make it unsuitable in certain situations. Given the source
    ///     aggregates results up until it has fulfilled the request, the results are different depending on the count
    ///     requested. As such, it is never safe to peek X and consume Y results. Consider a case where you have
    ///     [A, B, A, C, A] underlying resources. If you peek for 3 results, you will get [2A, 1B, 1C] as the source will
    ///     aggregate up until the request is fulfilled. If you then consume 2 results what is getting consumed is *not*
    ///     [2A, 1B] as you might expect. Rather it re-aggregates and then consumes [1A, 1B] because two results are
    ///     fulfilled as soon as the B is encountered. Also note the last A is in neither set, as this source does not
    ///     look ahead greedily.
    ///     Generally, this means this resource source must never be used underneath any resource source pipeline that
    ///     that has this peek/consume behavior where what is consumed is not always what is peeked. Since it is hard to
    ///     know the implementation of any given source or more dangerously, how a source is free to change it's
    ///     implementation, it is generally unsafe for this source to be anywhere but the very top of the resource
    ///     pipeline. This class has some rudimentary safety checks to try to detect if a consume does not match the
    ///     previous peeks, in order to ensure consistent results.
    ///     One interesting idea to consider... building a <see cref="PagingResourceSource{T}" /> on top of an
    ///     <see cref="AggregatingResourceSource{T}" /> to potentially resolve some of the above issues.
    /// </remarks>
    public class AggregatingResourceSource<T> : IResourceSource<T>
        where T : class
    {
        private readonly Func<T, T, bool> aggregateFunc;

        private readonly IResourceSource<T> source;

        private int lastPeekCount;

        /// <summary>
        ///     Constructs an <see cref="AggregatingResourceSource{T}" />
        /// </summary>
        /// <param name="source">The source you want aggregated.</param>
        /// <param name="aggregateFunc">
        ///     The function that aggregates results. The first parameter should be modified to contain the second
        ///     parameter if aggregation needs to happen. If no aggregation needs to happen, this function should
        ///     return false.
        /// </param>
        public AggregatingResourceSource(IResourceSource<T> source, Func<T, T, bool> aggregateFunc)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (aggregateFunc == null)
                throw new ArgumentNullException(nameof(aggregateFunc));
            this.source = source;
            this.aggregateFunc = aggregateFunc;
        }

        public async Task ConsumeAsync(int count)
        {
            if (this.lastPeekCount >= 0)
            {
                // Try to ensure this source is not used badly. See the class remarks for more detail.
                if (count != this.lastPeekCount)
                {
                    // This is not a fully reliable check. It could be I peeked 4, and then 10, and decided to go with the 4
                    // results, and trying to consume them now will give this error when it's really not a problem.
                    throw new ArgumentOutOfRangeException(
                        nameof(count),
                        $"{nameof(AggregatingResourceSource<T>)} last peeked {this.lastPeekCount} results and now attempting to consume {count} results.");
                }

                // Reset the last peeked count
                this.lastPeekCount = 0;
            }

            await this.PeekAsync(count, true).ConfigureAwait(false);
        }

        public async Task<IList<T>> FetchAsync(int count)
        {
            return await this.PeekAsync(count, true).ConfigureAwait(false);
        }

        public ResourceSourceContinuationToken GetNextToken()
        {
            return this.source.GetNextToken();
        }

        public async Task<IList<T>> PeekAsync(int count)
        {
            this.lastPeekCount = count;
            return await this.PeekAsync(count, false).ConfigureAwait(false);
        }

        public void SetNextToken(ResourceSourceContinuationToken token)
        {
            this.source.SetNextToken(token);
        }

        private async Task<IList<T>> PeekAsync(int count, bool andConsume)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            int peekOffset = 0;
            var results = new List<T>();
            while (results.Count < count)
            {
                // This makes stacking aggregating sources particularly bad. You'll note we first request how many we
                // need. Then, we aggregate, which may reduce the set, and so we have to keep asking for smaller and
                // smaller additional data chunks. Any downstream aggregators are then not aggregating well with these
                // smaller subsequent requests.
                IList<T> additionalResults;
                if (andConsume)
                    additionalResults = await this.source.FetchAsync(count == int.MaxValue ? count : count - results.Count).ConfigureAwait(false);
                else
                {
                    additionalResults = await this.source.PeekAsync(count == int.MaxValue ? count : peekOffset + (count - results.Count)).ConfigureAwait(false);
                    additionalResults = additionalResults.Skip(peekOffset).ToList();
                    peekOffset += additionalResults.Count;
                }
                if (additionalResults.Count <= 0)
                    break;
                results.AddRange(additionalResults);

                // Note the nested for loops. O(n-squared) in the case of no aggregation.
                for (int mergeIdx = 1; mergeIdx < results.Count; mergeIdx++)
                {
                    // mergeIdx is the item which we need to consider merging back into earlier in the results
                    for (int i = 0; i < mergeIdx; i++)
                    {
                        if (this.aggregateFunc(results[i], results[mergeIdx]))
                        {
                            // sourceResults[mergeIdx] has been merged into sourceResults[i] and can be removed
                            results.RemoveAt(mergeIdx);
                            mergeIdx--;
                            break;
                        }
                    }
                }
            }

            return results;
        }
    }
}
