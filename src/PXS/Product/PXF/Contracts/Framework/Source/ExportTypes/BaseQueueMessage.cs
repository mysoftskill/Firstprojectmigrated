// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes
{
    using System;
    using System.Globalization;

    /// <summary>
    ///     Base class for Azure Queue Message
    /// </summary>
    public class BaseQueueMessage
    {
        public string Action { get; set; }

        public int DequeueCount { get; set; }

        public DateTimeOffset? InsertionTime { get; set; }

        public string MessageId { get; set; }

        public DateTimeOffset? NextVisibleTime { get; set; }

        public string PopRecipt { get; set; }

        public string RequestId { get; set; }

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Action {0} ReqId {1}",
                this.Action,
                this.RequestId);
        }
    }
}
