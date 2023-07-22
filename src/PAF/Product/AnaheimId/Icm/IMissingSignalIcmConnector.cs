namespace Microsoft.PrivacyServices.AnaheimId.Icm
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Defines method for creating ICM incidents for missing signals.
    /// </summary>
    public interface IMissingSignalIcmConnector
    {
        /// <summary>
        /// Loads the necessary icm connector implementation and populates an incident envelope
        /// with signal information before creating a ticket to the ICM portal.
        /// </summary>
        /// <param name="name">The name of the blob containing missing signals.</param>
        /// <param name="requestIds">A sample of request ids found in the missing signal file.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>An incident create/update success status.</returns>
        bool CreateMissingSignalIncident(string name, List<long> requestIds, ILogger logger);
    }
}