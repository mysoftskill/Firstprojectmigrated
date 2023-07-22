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
    ///     A filtering resource source provides a filter on top of another resource source. A function is provided
    ///     that returns true for items that should be filtered out.
    /// </summary>
    public class FilterResourceSource<T> : IResourceSource<T>
        where T : class
    {
        private readonly Func<T, bool> filterFunc;

        private readonly IResourceSource<T> source;

        /// <param name="source">The source to filter</param>
        /// <param name="filterFunc">A function that returns true for items that should be removed</param>
        public FilterResourceSource(IResourceSource<T> source, Func<T, bool> filterFunc)
        {
            this.source = source;
            this.filterFunc = filterFunc;
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
            return this.source.GetNextToken();
        }

        public async Task<IList<T>> PeekAsync(int count)
        {
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
                results.AddRange(additionalResults.Where(i => !this.filterFunc(i)));
            }

            return results;
        }
    }
}
