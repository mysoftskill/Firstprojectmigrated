// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;

    /// <summary>
    ///     AadIdentityWithMsaUserProxyTicket.
    /// </summary>
    public class AadIdentityWithMsaUserProxyTicket : AadIdentity
    {
        /// <summary>
        ///     Gets the target cid.
        /// </summary>
        public long? TargetCid { get; }

        /// <summary>
        ///     Gets the target puid.
        /// </summary>
        public long TargetPuid { get; }

        /// <summary>
        ///     Gets the MSA user proxy ticket.
        /// </summary>
        public string UserProxyTicket { get; }

        /// <inheritdoc />
        public AadIdentityWithMsaUserProxyTicket(
            string inboundAppId,
            Guid objectId,
            Guid tenantId,
            string accessToken,
            string applicationDisplayName,
            long targetPuid,
            string userProxyTicket,
            long? targetCid)
            : base(inboundAppId, objectId, tenantId, accessToken, applicationDisplayName)
        {
            this.TargetCid = targetCid;
            this.TargetPuid = targetPuid;
            this.UserProxyTicket = userProxyTicket;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{AadIdentityWithMsaUserProxyTicket: {this.TenantId}}}";
        }
    }
}
