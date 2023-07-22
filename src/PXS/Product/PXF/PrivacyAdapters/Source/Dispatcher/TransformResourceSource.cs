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
    ///     A transform <see cref="IResourceSource{T}" /> takes an existing resource source and transforms it
    ///     as defined by the provided transformation function.
    /// </summary>
    public class TransformResourceSource<TFrom, TTo> : IResourceSource<TTo>
        where TFrom : class
        where TTo : class
    {
        private readonly IResourceSource<TFrom> source;

        private readonly Func<TFrom, TTo> transformFunc;

        public TransformResourceSource(IResourceSource<TFrom> source, Func<TFrom, TTo> transformFunc)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (transformFunc == null)
                throw new ArgumentNullException(nameof(transformFunc));

            this.source = source;
            this.transformFunc = transformFunc;
        }

        public async Task ConsumeAsync(int count)
        {
            await this.source.ConsumeAsync(count).ConfigureAwait(false);
        }

        public async Task<IList<TTo>> FetchAsync(int count)
        {
            return (await this.source.FetchAsync(count).ConfigureAwait(false)).Select(e => this.transformFunc(e)).ToList();
        }

        public ResourceSourceContinuationToken GetNextToken()
        {
            return this.source.GetNextToken();
        }

        public async Task<IList<TTo>> PeekAsync(int count)
        {
            return (await this.source.PeekAsync(count).ConfigureAwait(false)).Select(e => this.transformFunc(e)).ToList();
        }

        public void SetNextToken(ResourceSourceContinuationToken token)
        {
            this.source.SetNextToken(token);
        }
    }
}
