// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters
{
    using Microsoft.CommonSchema.Services.Logging;

    /// <summary>
    ///     Request Context
    /// </summary>
    public class PxfRequestContext : IPxfRequestContext
    {
        /// <summary>
        ///     Gets the puid that is authorizing the request
        /// </summary>
        public long AuthorizingPuid { get; }

        /// <summary>
        ///     Gets the authenticated user's country/region.
        /// </summary>
        public string Country { get; }

        /// <summary>
        ///     Correlation vector for the request (should already be extended)
        /// </summary>
        public CorrelationVector CV { get; }

        /// <summary>
        ///     Family JWT
        /// </summary>
        public string FamilyJsonWebToken { get; }

        /// <summary>
        ///     List of flights the user is on.
        /// </summary>
        public string[] Flights { get; }

        /// <summary>
        ///     Gets the watchdog request flag.
        /// </summary>
        public bool IsWatchdogRequest { get; }

        /// <summary>
        ///     Gets the cid of the target of the request
        /// </summary>
        public long? TargetCid { get; set; }

        /// <summary>
        ///     Get the puid of the target of the request
        /// </summary>
        public long TargetPuid { get; set; }

        /// <summary>
        ///     Gets the user proxy ticket
        /// </summary>
        public string UserProxyTicket { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PxfRequestContext" /> class.
        /// </summary>
        /// <param name="userProxyTicket">The user proxy ticket.</param>
        /// <param name="familyJsonWebToken">The family JWT (can be null)</param>
        /// <param name="authorizingPuid">The user puid.</param>
        /// <param name="targetPuid">The target puid.</param>
        /// <param name="targetCid">The target cid.</param>
        /// <param name="countryRegion">The authenticated user's country/region.</param>
        /// <param name="isWatchdogRequest">Is this a watchdog request?</param>
        /// <param name="flights">The list of flights.</param>
        public PxfRequestContext(
            string userProxyTicket,
            string familyJsonWebToken,
            long authorizingPuid,
            long targetPuid,
            long? targetCid,
            string countryRegion,
            bool isWatchdogRequest,
            string[] flights)
        {
            this.UserProxyTicket = userProxyTicket;
            this.FamilyJsonWebToken = familyJsonWebToken;
            this.CV = Sll.Context.Vector;
            this.AuthorizingPuid = authorizingPuid;
            this.TargetPuid = targetPuid;
            this.TargetCid = targetCid;
            this.Country = countryRegion;
            this.IsWatchdogRequest = isWatchdogRequest;
            this.Flights = flights;
        }
    }
}
