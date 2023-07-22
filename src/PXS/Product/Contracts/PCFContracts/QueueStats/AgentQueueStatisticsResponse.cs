// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.CommandStatus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     PCF's response about an agent's queue.
    /// </summary>
    public class AgentQueueStatisticsResponse
    {
        /// <summary>
        ///     A list of statistics for each individual asset group / subject type. These may be aggregated to produce a logical "overall" picture of an agent's health.
        /// </summary>
        public IList<AssetGroupQueueStatistics> AssetGroupQueueStatistics { get; set; }
    }
}
