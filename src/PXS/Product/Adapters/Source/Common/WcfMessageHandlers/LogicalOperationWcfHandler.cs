// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Adapters.Common;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;

    /// <summary>
    /// A WCF executor pipelined component which handles logical operations.
    /// </summary>
    public class LogicalOperationWcfHandler : DelegatingWcfHandler
    {
        private readonly OutgoingApiEventWrapper apiEvent;

        public LogicalOperationWcfHandler(
            OutgoingApiEventWrapper apiEvent)
        {
            this.apiEvent = apiEvent;
        }

        /// <summary>
        /// Execute and track a WCF operation. Unhandled exceptions and non-success responses are considered failures.
        /// </summary>
        /// <typeparam name="T">Specifies the contract type the WCF operation returns.</typeparam>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>An asynchronous task executing the delegate.</returns>
        public override async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
        {
            // Pre-Execution
            this.apiEvent.Start();

            // Execute
            T response;
            try
            {
                response = await base.ExecuteAsync(action);
            }
            catch (Exception ex)
            {
                this.apiEvent.Success = false;
                this.apiEvent.ErrorMessage = ex.Message;

                PartnerException pex = ex as PartnerException;
                if (pex != null)
                {
                    this.apiEvent.ServiceErrorCode = pex.ErrorCode;
                }

                throw;
            }
            finally
            {
                // Post-Execution
                this.apiEvent.Finish();
            }

            return response;
        }

        /// <summary>
        /// Execute and track a WCF operation. Unhandled exceptions and non-success responses are considered failures.
        /// </summary>
        /// <param name="action">A function which executes a WCF operation.</param>
        /// <returns>A asynchronous task executing the delegate.</returns>
        public override async Task ExecuteAsync(Func<Task> action)
        {
            // Pre-Execution
            this.apiEvent.Start();

            // Execute
            try
            {
                await base.ExecuteAsync(action);
            }
            catch (Exception ex)
            {
                this.apiEvent.Success = false;
                this.apiEvent.ErrorMessage = ex.Message;
                throw;
            }
            finally
            {
                // Post-Execution
                this.apiEvent.Finish();
            }
        }
    }
}
