// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Adapters.Common.DelegatingExecutors
{
    public static class WcfClientFactory
    {
        /// <summary>
        /// Creates an execution pipeline with handlers chained together and ending with the base executor. The chain
        /// will process operations from first handler to base executor and then the reverse on the return.
        /// </summary>
        /// <param name="handlers">The handlers to pipeline together. Chained from first to last element.</param>
        /// <returns>An execution pipeline.</returns>
        public static IWcfRequestHandler Create(params DelegatingWcfHandler[] handlers)
        {
            // Daisy chain handlers together
            int totalHandlers = handlers.Length;
            for (int i = 0; i < totalHandlers - 1; i++)
            {
                handlers[i].InnerHandler = handlers[i + 1];
            }

            // First handler is top of chain
            return handlers[0];
        }
    }
}
