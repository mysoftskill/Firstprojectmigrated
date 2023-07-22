// <copyright company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.Helpers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;

    public static class RequestHandler
    {
        /// <summary>
        /// Wraps incoming requests and handles errors
        /// </summary>
        /// <param name="request">Request object</param>
        /// <param name="handler">Business logic of the request</param>
        /// <returns>HTTP response</returns>
        public static async Task<HttpResponseMessage> Wrapper(HttpRequestMessage request, Func<Task<HttpResponseMessage>> handler)
        {
            try
            {
                return await handler();
            }
            catch (Exception e)
            {
                var errorBody = new ErrorV2
                {
                    Code = "UnexpectedError",
                    Message = e.Message,
                    ErrorDetails = e.ToString(),
                };

                return request.CreateResponse(HttpStatusCode.InternalServerError, errorBody);
            }
        }
    }
}
