// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System;
    using System.Collections.Generic;

    public enum ResourceStatus
    {
        /// <summary>
        /// Unknown status
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Active (not deleted and not pending delete)
        /// </summary>
        Active = 1,

        /// <summary>
        /// Pending Delete
        /// </summary>
        PendingDelete = 2,

        /// <summary>
        /// Deleted
        /// </summary>
        Deleted = 3,

        /// <summary>
        /// Error
        /// </summary>
        Error = 4,
    }

    /// <summary>
    /// Base class for representing a privacy resource element
    /// </summary>
    public abstract class Resource
    {
        /// <summary>
        /// Gets or sets the resource identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the resource date time.
        /// </summary>
        public DateTimeOffset DateTime { get; set; }

        /// <summary>
        /// Gets or sets the resource status.
        /// </summary>
        public ResourceStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the device identifier. 
        /// If available, the deviceId of the device where the browse happened.  Ideally, this would be the MSA Global ID value for the device. 
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the sources.
        /// </summary>
        public string[] Sources { get; set; }

        /// <summary>
        /// Gets or sets the partner identifier. This is the name of the partner specified in the PXF Adapter.
        /// </summary>
        public string PartnerId { get; set; }

        /// <summary>
        /// Gets or sets a data source defined property bag that can be used by delete, export, etc processors to filter the data that
        ///  the command is to affect
        /// </summary>
        public IDictionary<string, IList<string>> PropertyBag { get; set; }
    }
}
