// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    /// <summary>
    ///     This is a resource source for pulling items from a paging source.
    /// </summary>
    public class PagingResourceSource<T> : IResourceSource<T>
        where T : class
    {
        private readonly List<PagedResponse<T>> cachedPages = new List<PagedResponse<T>>();

        private readonly Func<Task<PagedResponse<T>>> firstPageFunc;

        private readonly Func<Uri, Task<PagedResponse<T>>> nextPageFunc;

        private Uri continuationNextLink;

        private int fetchOffset;

        private int fetchPageOffset;

        /// <summary>
        ///     This flag is needed since there are two conditions that look the same. Either
        ///     we haven't fetched anything, or we're completely done
        /// </summary>
        private bool haveFetched;

        public PagingResourceSource(
            Func<Task<PagedResponse<T>>> firstPageFunc,
            Func<Uri, Task<PagedResponse<T>>> nextPageFunc)
        {
            if (firstPageFunc == null)
                throw new ArgumentNullException(nameof(firstPageFunc));
            if (nextPageFunc == null)
                throw new ArgumentNullException(nameof(nextPageFunc));

            this.firstPageFunc = firstPageFunc;
            this.nextPageFunc = nextPageFunc;
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
            // Should never be here with our fetch page past the first page
            Debug.Assert(this.fetchPageOffset == 0);

            if (!this.haveFetched)
            {
                // If we haven't done anything yet, we can't know that we're done, so
                // just return the continuation token with defaults, for first page, or
                // whatever we were already passed.
                return new PagingResourceSourceContinuationToken
                {
                    NextLink = this.continuationNextLink,
                    Offset = this.fetchOffset
                };
            }

            if (this.cachedPages.Count <= 0 && this.continuationNextLink == null)
            {
                // We seem to have consumed everything, so we're done.
                return null;
            }

            // Or lastly, we're inside a page, and so have to return the link that got us here, and our current offset
            return new PagingResourceSourceContinuationToken
            {
                NextLink = this.continuationNextLink,
                Offset = this.fetchOffset
            };
        }

        public async Task<IList<T>> PeekAsync(int count)
        {
            return await this.PeekAsync(count, false).ConfigureAwait(false);
        }

        public void SetNextToken(ResourceSourceContinuationToken token)
        {
            if (token == null)
                return;
            if (this.haveFetched)
                throw new InvalidOperationException("Too late to set continuation token, already fetched results");
            var continuationToken = token as PagingResourceSourceContinuationToken;
            if (continuationToken == null)
                throw new ArgumentOutOfRangeException(nameof(token), "Token is the wrong type");

            this.fetchOffset = continuationToken.Offset;
            this.continuationNextLink = continuationToken.NextLink;
        }

        private async Task<bool> AddPageAsync()
        {
            PagedResponse<T> page;

            if (this.cachedPages.Count <= 0)
            {
                // We know for sure we're done when we have no pages, we have fetched some previously, and we have no next link
                if (this.haveFetched && this.continuationNextLink == null)
                    return false;

                if (this.continuationNextLink == null)
                    page = await this.firstPageFunc().ConfigureAwait(false);
                else
                    page = await this.nextPageFunc(this.continuationNextLink).ConfigureAwait(false);

                this.haveFetched = true;
            }
            else
            {
                // If we have pages, but the last page has no next link, we also cannot fetch more
                if (this.cachedPages[this.cachedPages.Count - 1].NextLink == null)
                    return false;
                page = await this.nextPageFunc(this.cachedPages[this.cachedPages.Count - 1].NextLink).ConfigureAwait(false);
            }

            this.cachedPages.Add(page ?? new PagedResponse<T> { Items = null, NextLink = null });
            return true;
        }

        private async Task<IList<T>> PeekAsync(int count, bool andConsume)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            // Should never be here with our fetch page past the first page
            Debug.Assert(this.fetchPageOffset == 0);

            var results = new List<T>();

            int peekPageOffset = this.fetchPageOffset;
            int peekOffset = this.fetchOffset;

            while (results.Count < count)
            {
                // if we need a page, grab it
                if (peekPageOffset >= this.cachedPages.Count)
                {
                    if (!await this.AddPageAsync().ConfigureAwait(false))
                        break; // if we can't get another page, then return whatever we got so far
                }

                if (peekOffset < (this.cachedPages[peekPageOffset].Items?.Count ?? 0))
                {
                    // If our index is into this page, we can return the result
                    T result = this.cachedPages[peekPageOffset].Items?[peekOffset];
                    if (result != null)
                        results.Add(result);

                    peekOffset++;
                }
                else
                {
                    // Otherwise, we need to go to the next page
                    peekOffset = 0;
                    peekPageOffset++;
                }
            }

            if (andConsume)
            {
                this.fetchOffset = peekOffset;
                this.fetchPageOffset = peekPageOffset;
            }

            // For as long as our page offset is past the first page, we can erase the first page
            while (this.fetchPageOffset > 0)
            {
                // Remove excess pages
                this.continuationNextLink = this.cachedPages[0].NextLink;
                this.cachedPages.RemoveAt(0);
                this.fetchPageOffset--;
            }

            // If we're off the end of the first page, also erase it
            while (this.cachedPages.Count > 0 && this.fetchOffset >= (this.cachedPages[0].Items?.Count ?? 0))
            {
                this.continuationNextLink = this.cachedPages[0].NextLink;
                this.cachedPages.RemoveAt(0);
                this.fetchOffset = 0;
            }

            return results;
        }
    }
}
