// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     A base definition for a WCF pipelined component. Defines how executors can be pipelined together in a chain.
    /// </summary>
    public abstract class DelegatingWcfHandler : IWcfRequestHandler
    {
        public IWcfRequestHandler InnerHandler { get; set; }

        /// <summary>
        ///     Executes the passed in WCF operation in a pipelined manner. Logic can be added before and after executing
        ///     the operation. Execution may use the inner executor or directly respond.
        /// </summary>
        /// <typeparam name="T">Specifies the contract type the WCF operation returns.</typeparam>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>
        ///     Result of the WCF operation.
        /// </returns>
        /// <exception cref="ArgumentNullException">action</exception>
        /// <exception cref="PartnerException">Thrown if partner is unavailable.</exception>
        public virtual async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (this.InnerHandler != null)
            {
                return await this.InnerHandler.ExecuteAsync(action).ConfigureAwait(false);
            }

            return await action().ConfigureAwait(false);
        }

        public virtual async Task ExecuteAsync(Func<Task> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (this.InnerHandler != null)
            {
                await this.InnerHandler.ExecuteAsync(action).ConfigureAwait(false);
                return;
            }

            await action().ConfigureAwait(false);
        }
    }
}
