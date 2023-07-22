// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.MsaIdentityService
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Adapters.Common;
    using Microsoft.Membership.MemberServices.Contracts.Adapter.MsaTokenProvider;
    using Microsoft.Membership.MemberServices.Contracts.Exposed;

    /// <summary>
    /// Contains helper methods to execute actions that require
    /// S2S authentication
    /// </summary>
    public static class S2SRequestHelper
    {
        /// <summary>
        /// Executes the supplied action and if the response indicates
        /// the failure was due to an invalid token, retries the call with a
        /// refreshed token.
        /// </summary>
        /// <typeparam name="T">Type of response</typeparam>
        /// <param name="tokenProvider">Provider of MSA tokens</param>
        /// <param name="action">Action to execute</param>
        /// <returns>If succesful, returns the result of the executaed action, otherwise returns an error response</returns>
        public static async Task<AdapterResponse<T>> ExecuteMsaTokenAction<T>(IMsaTokenProvider tokenProvider, Func<string, Task<AdapterResponse<T>>> action)
        {
            GetTokenResponse msaTokenResponse = await tokenProvider.GetTokenAsync(forceRefresh: false);

            AdapterResponse<T> response = null;
            ErrorInfo error = msaTokenResponse.Error;

            if (msaTokenResponse.IsSuccess)
            {
                string msaToken = msaTokenResponse.Token;

                response = await action(msaToken);

                if (!response.IsSuccess && response.ErrorInfo.ErrorCode == ErrorCode.PartnerAuthorizationFailureMsaToken)
                {
                    msaTokenResponse = await tokenProvider.GetTokenAsync(forceRefresh: true);
                    
                    if (msaTokenResponse.IsSuccess)
                    {
                        msaToken = msaTokenResponse.Token;
                        response = await action(msaToken);
                    }
                    else
                    {
                        // Clearing the failed response so that the output of this helper represents the failure 
                        // to get the token
                        response = null;
                        error = msaTokenResponse.Error;
                    }
                }
            }

            if (response == null)
            {
                return new AdapterResponse<T>()
                {
                    ErrorInfo = error
                };
            }

            return response;
        }
    }
}