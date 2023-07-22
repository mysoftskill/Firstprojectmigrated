// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    ///     A resource source is a generic source of resources. Think of it as a queue you can
    ///     dequeue or peek from until you start getting null meaning you're done. You can also
    ///     get a continuation token with which to continue later where you left off. Other than
    ///     that, the rest is up to each implementation, some of which are transforms, aggregations,
    ///     merging, and actual remote data sources.
    /// </summary>
    public interface IResourceSource<T>
        where T : class
    {
        /// <summary>
        ///     Consumes resources from the source. Generally only <see cref="IResourceSource{T}" />
        ///     implementers should be calling this, regular consumers of a source should be using the
        ///     method <see cref="FetchAsync" /> instead.
        /// </summary>
        Task ConsumeAsync(int count);

        /// <summary>
        ///     This is the preferred way to interact with an <see cref="IResourceSource{T}" /> unless you are implementing
        ///     one yourself. This fetches up to <paramref name="count" /> results, but may return less or no results. The return
        ///     value will never be null. This is not an idempotent call as it consumes resources from the source.
        /// </summary>
        Task<IList<T>> FetchAsync(int count);

        /// <summary>
        ///     Gets a continuation token that can be used in another instance of this same <see cref="IResourceSource{T}" />
        ///     to continue where you left off. This is used for paging, and generally should be serialized with
        ///     <see cref="ResourceSourceContinuationToken.Serialize" /> to pass it on a Url safely, or even more
        ///     safely you can use <see cref="ResourceSourceExtensions.BuildNextLink" /> to handle all your Url
        ///     re-writing needs.
        /// </summary>
        ResourceSourceContinuationToken GetNextToken();

        /// <summary>
        ///     Fetches results from the source. Note that aggregators may return different results depending
        ///     on the count you use here so when calling <see cref="ConsumeAsync" /> even if you get less results
        ///     back always consume the same quantity you asked for, rather than what you received. Generally
        ///     only <see cref="IResourceSource{T}" /> implementers should be calling this, regular consumers
        ///     of a source should be using the method <see cref="FetchAsync" />
        ///     instead.
        /// </summary>
        Task<IList<T>> PeekAsync(int count);

        /// <summary>
        ///     Sets the continuation token to continue using the source where you last left off. To get a token see
        ///     <see cref="ResourceSourceContinuationToken.Deserialize" />. There is also an extension method to not
        ///     have to bother with this if you use <see cref="ResourceSourceExtensions.SetNextToken{T}" /> to
        ///     directly pass a string.
        /// </summary>
        /// <param name="token"></param>
        void SetNextToken(ResourceSourceContinuationToken token);
    }
}
