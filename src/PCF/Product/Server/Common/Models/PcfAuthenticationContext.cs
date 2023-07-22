namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// Describes an authentication context for an HTTP request.
    /// </summary>
    public class PcfAuthenticationContext
    {
        /// <summary>
        /// The authenticated MSA Site Id.
        /// </summary>
        public long? AuthenticatedMsaSiteId { get; set; }

        /// <summary>
        /// The authenticated AAD App Id.
        /// </summary>
        public Guid? AuthenticatedAadAppId { get; set; }
    }
}
