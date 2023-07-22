// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System.Security.Principal;

    /// <summary>
    /// </summary>
    public interface IRequestClassifier
    {
        /// <summary>
        ///     Classifies a request as test or not.
        /// </summary>
        /// <param name="requestOriginPortal">The portal where the request originated, if it is PartnerTestPage, then consider it a test request</param>
        /// <param name="identity">The identity</param>
        /// <param name="correlationContextBaseOperationName">
        ///     The correlation context base operation name is defined as 'ms.b.qos.rootOperationName' an an SLL event.
        ///     Documented at https://osgwiki.com/wiki/CorrelationContext
        ///     This allows us to know where an operation originated, for example, from AMC's partner test page.
        /// </param>
        /// <returns>returns <c>true</c> if the authenticated identity is a test request.</returns>
        bool IsTestRequest(string requestOriginPortal, IIdentity identity, string correlationContextBaseOperationName = null);
    }
}
