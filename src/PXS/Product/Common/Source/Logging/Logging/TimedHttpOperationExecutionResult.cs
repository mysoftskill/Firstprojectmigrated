//--------------------------------------------------------------------------------
// <copyright file="TimedHttpOperationExecutionResult.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common.Logging
{
    using System.Net.Http;

    /// <summary>
    /// Times and stores execution result of an HTTP delegate.
    /// </summary>
    public class TimedHttpOperationExecutionResult : TimedOperationExecutionResult<HttpResponseMessage>
    {
        /// <summary>
        /// Gets or sets a value indicating whether the operation was successful, and the <see cref="TimedOperationExecutionResult{TResult}.Response"/> isn't null.
        /// </summary>
        public override bool IsSuccess
        {
            get
            {
                return base.IsSuccess && this.Response != null && this.Response.IsSuccessStatusCode;
            }
        }
    }
}
