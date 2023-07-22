namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Defines a command from PXS that has had filtering applied to it.
    /// </summary>
    public sealed class PxsFilteredCommandRequest
    {
        /// <summary>
        /// The raw command from PXS.
        /// </summary>
        public JObject RawPxsCommand { get; set; }
        
        /// <summary>
        /// A hint about what type of command this actually is.
        /// </summary>
        public PrivacyCommandType CommandType { get; set; }

        /// <summary>
        /// The version of the PDMS data set used to generate this request.
        /// </summary>
        public long PdmsDataSetVersion { get; set; }

        /// <summary>
        /// The set of destinations for this command.
        /// </summary>
        public List<PxsFilteredCommandDestination> Destinations { get; set; }
    }
}
