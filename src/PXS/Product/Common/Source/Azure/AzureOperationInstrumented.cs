// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>


namespace Microsoft.Membership.MemberServices.Common.Azure
{
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Rest.Azure;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    ///     Helper class for wrapping external code in instrumentation.
    /// </summary>
    public abstract class AzureOperationInstrumented
    {
        protected string DependencyName { get; set; }

        protected string PartnerId { get; set; }

        protected string DependencyOperationVersion { get; set; }

        protected string DependencyType { get; set; }

        protected Action<OutgoingApiEventWrapper, Exception> ExceptionHandler { get; set; } = (evt, ex) => evt.ErrorMessage = ex.Message;

        /// <summary>
        ///     Gets a new <see cref="OutgoingApiEventWrapper"/>.
        /// </summary>
        /// <param name="operationName">The name of the operation being wrapped.</param>
        /// <returns>A new <see cref="OutgoingApiEventWrapper"/>.</returns>
        private OutgoingApiEventWrapper GetApiEvent(string operationName) =>
            new OutgoingApiEventWrapper
            {
                DependencyOperationName = operationName,
                DependencyOperationVersion = this.DependencyOperationVersion ?? string.Empty,
                DependencyName = this.DependencyName ?? this.GetType().Name,
                DependencyType = this.DependencyType ?? "WebService",
                PartnerId = this.PartnerId ?? this.GetType().Name,
                Success = false
            };

        /// <summary>
        ///     Instruments an asynchronous outgoing event that doesn't return a value
        /// </summary>
        /// <param name="operationName">Name of the operation</param>
        /// <param name="method">Wrapping function.</param>
        /// <param name="detailedLogs"></param>
        protected async Task<AzureOperationResponse> InstrumentOutgoingCallAsync(string operationName, Func<Task<AzureOperationResponse>> method, bool detailedLogs = false)
        {
            OutgoingApiEventWrapper eventWrapper = this.GetApiEvent(operationName);
            eventWrapper.Start();

            try
            {
                var response = await method().ConfigureAwait(false);
                await eventWrapper.PopulateFromResponseAsync(response.Response, detailedLogs).ConfigureAwait(false);

                return response;
            }
            catch (Exception e)
            {
                this.ExceptionHandler(eventWrapper, e);
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }

        protected async Task<AzureOperationResponse<TResult>> InstrumentOutgoingCallAsync<TResult>(string operationName, Func<Task<AzureOperationResponse<TResult>>> method, bool detailedLogs = false)
        {
            OutgoingApiEventWrapper eventWrapper = this.GetApiEvent(operationName);
            eventWrapper.Start();

            try
            {
                var result = await method().ConfigureAwait(false);
                await eventWrapper.PopulateFromResponseAsync(result.Response, detailedLogs).ConfigureAwait(false);

                return result;
            }
            catch (Exception e)
            {
                this.ExceptionHandler(eventWrapper, e);
                throw;
            }
            finally
            {
                eventWrapper.Finish();
            }
        }
    }
}
