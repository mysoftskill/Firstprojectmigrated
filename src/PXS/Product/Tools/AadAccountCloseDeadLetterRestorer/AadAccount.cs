// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.PrivacyServices.AadAccountCloseDeadLetterRestorer
{
    using System;

    internal class AadAccount
    {
        public Guid ObjectId { get; }

        public Guid TenantId { get; }

        internal AadAccount(Guid objectId, Guid tenantId)
        {
            this.ObjectId = objectId;
            this.TenantId = tenantId;
        }

        public override string ToString() => $"Object-id: {this.ObjectId.ToString()}, Tenant-id: {this.TenantId.ToString()}";
    }
}
