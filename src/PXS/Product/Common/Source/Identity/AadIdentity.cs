// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Security.Principal;

    /// <summary>
    ///     AadIdentity
    /// </summary>
    public class AadIdentity : IIdentity
    {
        /// <summary>
        ///     The Application Display Name
        /// </summary>
        private readonly string applicationDisplayName;

        /// <summary>
        ///     The access token
        /// </summary>
        public string AccessToken { get; }

        /// <summary>
        ///     The Application Identity (aka AppId)
        /// </summary>
        public string ApplicationId { get; }

        /// <inheritdoc />
        public string AuthenticationType { get; }

        /// <summary>
        ///     Gets the caller name formatted
        /// </summary>
        public string CallerNameFormatted
        {
            get { return $"{this.AuthenticationType}_{this.applicationDisplayName}"; }
        }
        
        /// <inheritdoc />
        public bool IsAuthenticated { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        ///     Gets the ObjectId of the request maker.
        /// </summary>
        public Guid ObjectId { get; }

        public long? OrgIdPuid { get; set; }

        public Guid TargetObjectId { get; set; }

        /// <summary>
        ///     Gets the TenantId.
        /// </summary>
        public Guid TenantId { get; }

        /// <inheritdoc />
        public AadIdentity(string inboundAppId, Guid objectId, Guid tenantId, string accessToken, string applicationDisplayName)
        {
            this.AuthenticationType = "AAD";
            this.IsAuthenticated = true;

            this.ApplicationId = inboundAppId;
            this.ObjectId = this.TargetObjectId = objectId;
            this.TenantId = tenantId;
            this.AccessToken = accessToken;
            this.applicationDisplayName = applicationDisplayName;
            this.Name = this.applicationDisplayName;
        }

        public AadIdentity(
            string inboundAppId,
            Guid objectId,
            Guid targetObjectId,
            Guid tenantId,
            string accessToken,
            string applicationDisplayName)
        {
            this.AuthenticationType = "AAD";
            this.IsAuthenticated = true;

            this.ApplicationId = inboundAppId;
            this.ObjectId = objectId;
            this.TargetObjectId = targetObjectId;
            this.TenantId = tenantId;
            this.AccessToken = accessToken;
            this.applicationDisplayName = applicationDisplayName;
            this.Name = this.applicationDisplayName;
        }

        public AadIdentity(Guid objectId, Guid tenantId, long? orgIdPuid)
        {
            this.ObjectId = this.TargetObjectId = objectId;
            this.TenantId = tenantId;
            this.OrgIdPuid = orgIdPuid;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{{AadIdentity: {this.TenantId}}}";
        }
    }
}
