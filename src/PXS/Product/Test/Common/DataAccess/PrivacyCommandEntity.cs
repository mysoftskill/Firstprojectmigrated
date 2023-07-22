// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Test.Common.DataAccess
{
    using System;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Scheduler;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Tables;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.Azure.Cosmos.Table;

    using Newtonsoft.Json;

    /// <summary>
    /// </summary>
    public class PrivacyCommandEntity : TableEntity, ITableEntity, ITableEntityInitializer
    {
        public Guid? AadObjectId { get; set; }

        public Guid? AadTenantId { get; set; }

        public string Data
        {
            get => this.DataActual != null ? JsonConvert.SerializeObject(this.DataActual) : string.Empty;

            set => this.DataActual = value != null ? JsonConvert.DeserializeObject<PrivacyRequest>(value) : default(PrivacyRequest);
        }

        [IgnoreProperty]
        public PrivacyRequest DataActual { get; set; }

        public long? GlobalDeviceId { get; set; }

        public long? MsaPuid { get; set; }

        /// <summary>
        ///     Creates a new PrivacyCommandEntity
        /// </summary>
        /// <param name="privacyCommandRequest"></param>
        public PrivacyCommandEntity(PrivacyRequest privacyCommandRequest)
        {
            if (privacyCommandRequest == null)
            {
                throw new ArgumentNullException(nameof(privacyCommandRequest));
            }

            if (privacyCommandRequest.RequestId.Equals(Guid.Empty))
            {
                throw new ArgumentException($"The ({nameof(privacyCommandRequest.RequestId)}) must not be an empty guid.");
            }

            this.DataActual = privacyCommandRequest;

            // RowKey must be unique to make the primary key (Partition Key + RowKey combination) unique
            this.RowKey = privacyCommandRequest.RequestId.ToString();

            // Partition key differs by subject. Should be something the requester knows, to make querying possible.
            switch (privacyCommandRequest.Subject)
            {
                case AadSubject aadSubject:
                    this.PartitionKey = aadSubject.ObjectId.ToString();
                    this.AadObjectId = aadSubject.ObjectId;
                    this.AadTenantId = aadSubject.TenantId;
                    break;
                case DeviceSubject deviceSubject:
                    this.PartitionKey = deviceSubject.GlobalDeviceId.ToString();
                    this.GlobalDeviceId = deviceSubject.GlobalDeviceId;
                    break;
                case MsaSubject msaSubject:
                    this.PartitionKey = msaSubject.Puid.ToString();
                    this.MsaPuid = msaSubject.Puid;
                    break;
                default:
                    this.PartitionKey = privacyCommandRequest.RequestId.ToString();
                    break;
            }
        }

        public PrivacyCommandEntity()
        {
        }

        public void Initialize(object rawTableObject)
        {
            if (rawTableObject is DynamicTableEntity entity)
            {
                this.PartitionKey = entity.PartitionKey;
                this.Timestamp = entity.Timestamp;
                this.RowKey = entity.RowKey;
                this.ETag = entity.ETag;

                this.Data = entity.GetString("Data") ?? string.Empty;
                this.MsaPuid = entity.GetLong("MsaPuid");
                this.AadObjectId = entity.GetGuid("AadObjectId");
                this.AadTenantId = entity.GetGuid("AadTenantId");
                this.GlobalDeviceId = entity.GetLong("GlobalDeviceId");
            }
        }
    }
}
