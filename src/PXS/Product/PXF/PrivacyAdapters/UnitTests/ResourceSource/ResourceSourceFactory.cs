// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.UnitTests
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    public static class ResourceSourceFactory<T>
        where T : class
    {
        /// <summary>
        ///     For creating a source of data where you don't care about how it's provided, for testing other resource sources.
        /// </summary>
        public static IResourceSource<T> Generate(params T[] results)
        {
            return new PagingResourceSource<T>(
                () => Task.FromResult(new PagedResponse<T> { Items = results }),
                uri => { throw new Exception("Shouldn't get here"); });
        }

        /// <summary>
        ///     For creating specific page test patterns
        /// </summary>
        public static PagingResourceSource<T> GeneratePages(T[][] pages)
        {
            return new PagingResourceSource<T>(
                () =>
                {
                    if (pages == null)
                        return Task.FromResult<PagedResponse<T>>(null);

                    var response = new PagedResponse<T> { Items = pages.Length > 0 ? pages[0] : null };
                    if (pages.Length > 1)
                        response.NextLink = new Uri("https://test.com/#1");
                    return Task.FromResult(response);
                },
                uri =>
                {
                    int page = int.Parse(uri.Fragment.Substring(1));
                    var response = new PagedResponse<T> { Items = pages[page] };
                    if ((page + 1) < pages.Length)
                        response.NextLink = new Uri($"https://test.com/#{page + 1}");
                    return Task.FromResult(response);
                });
        }
    }
}
