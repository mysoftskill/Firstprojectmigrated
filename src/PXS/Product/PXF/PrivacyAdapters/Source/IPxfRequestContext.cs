// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters
{
    using Microsoft.CommonSchema.Services.Logging;

    /// <summary>
    ///     Context of the user request
    /// </summary>
    public interface IPxfRequestContext
    {
        /// <summary>
        ///     Gets the puid that is authorizing the request
        /// </summary>
        long AuthorizingPuid { get; }

        /// <summary>
        ///     Gets the authenticated user's country/region.
        /// </summary>
        string Country { get; }

        /// <summary>
        ///     Correlation vector for the request (should already be extended)
        /// </summary>
        CorrelationVector CV { get; }

        /// <summary>
        ///     Family JWT
        /// </summary>
        string FamilyJsonWebToken { get; }

        /// <summary>
        ///     List of flights
        /// </summary>
        string[] Flights { get; }

        /// <summary>
        ///     Is the request a watchdog request?
        /// </summary>
        bool IsWatchdogRequest { get; }

        /// <summary>
        ///     Gets the cid of the target of the request
        /// </summary>
        long? TargetCid { get; set; }

        /// <summary>
        ///     Get the puid of the target of the request
        /// </summary>
        long TargetPuid { get; set; }

        /// <summary>
        ///     Gets the user proxy ticket
        /// </summary>
        string UserProxyTicket { get; }
    }
}
