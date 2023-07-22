// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementConfig
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     DataManagementConfigurationManager
    /// </summary>
    public class DataManagementConfig : IDataManagementConfig
    {
        /// <summary>
        ///     Gets Mapping of Ring -> PartnerConfigMapping.
        /// </summary>
        public IDictionary<string, IRingPartnerConfigMapping> RingPartnerConfigMapping { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DataManagementConfig" /> class.
        /// </summary>
        public DataManagementConfig()
        {
            this.RingPartnerConfigMapping = new Dictionary<string, IRingPartnerConfigMapping>();
        }
    }
}
