// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.DataContracts.V2
{
    using System;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    /// <summary>
    ///     Privacy resource record V2
    /// </summary>
    public abstract class PrivacyResourceV2
    {
        /// <summary>
        ///     Gets or sets the resource date time.
        /// </summary>
        [JsonProperty("dateTime")]
        [JsonRequired]
        public DateTimeOffset DateTime { get; set; }

        /// <summary>
        ///     Gets or sets the device identifier.
        ///     If available, the deviceId of the device where the browse happened.  Ideally, this would be the MSA Global ID value for the device.
        /// </summary>
        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        ///     Gets or sets the sources of the data
        /// </summary>
        [JsonProperty("sources")]
        public string[] Sources { get; set; }

        /// <summary>
        ///     Gets or sets a data source defined property bag that can be used by delete, export, etc processors to filter the data that
        ///      the command is to affect
        /// </summary>
        [JsonProperty("propertyBag")]
        public IDictionary<string, IList<string>> PropertyBag { get; set; }
    }
}