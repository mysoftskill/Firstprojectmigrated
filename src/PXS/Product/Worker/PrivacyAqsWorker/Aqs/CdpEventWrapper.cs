// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.AqsWorker.Aqs
{
    using Live.Mesh.Service.AsyncQueueService.Interface;
    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using System.Globalization;

    /// <summary>
    ///     Wraps a CDPEvent2 to have it contain it's aggregation key
    /// </summary>
    public class CdpEventWrapper
    {
        private ulong? puid;

        public CdpEventWrapper(CDPEvent2 evt) => this.Event = evt;

        /// <summary> Gets or sets the Aggregation ID for Completing/Releasing the work with AQS </summary>
        public string AggregationId { get; set; }

        public CDPEvent2 Event { get; }

        public WorkItem ParentWorkItem { get; set; }

        public ulong Puid => this.puid ?? (ulong)(this.puid = ulong.Parse(this.Event.AggregationKey, NumberStyles.AllowHexSpecifier));
    }
}
