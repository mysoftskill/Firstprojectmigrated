// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes
{
    using System;

    /// <summary>
    ///     Status of a particular data type's export
    /// </summary>
    public class ExportDataResourceStatus
    {
        /// <summary>
        ///     Whether or not the export of the data type is complete
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        ///     When the data type was last completed processing
        /// </summary>
        public DateTimeOffset LastSessionEnd { get; set; }

        /// <summary>
        ///     When the data type was last started processing
        /// </summary>
        public DateTimeOffset LastSessionStart { get; set; }

        /// <summary>
        ///     The data type
        /// </summary>
        public string ResourceDataType { get; set; }
    }
}
