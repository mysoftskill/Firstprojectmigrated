namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;

    /// <summary>
    /// Data contract for the export expectation records.
    /// </summary>
    public class ExportExpectationEventEntry
    {
        /// <summary>
        /// The unique id made up of the agent id, asset group id, and command id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The Command Id
        /// </summary>
        public Guid CommandId { get; set; }

        /// <summary>
        /// The partition key is the agent ID and Asset Group Id.
        /// </summary>
        public string CompoundKey { get; set; }

        /// <summary>
        /// The time slice derived from the time window.
        /// </summary>
        public string Timeslice { get; set; }

        /// <summary>
        /// The command type id.
        /// </summary>
        public int CommandTypeId { get; set; }

        /// <summary>
        /// The page number.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The final container uri
        /// </summary>
        public Uri FinalContainerUri { get; set; }

        /// <summary>
        /// The final container path
        /// </summary>
        public string FinalDestinationPath { get; set; }

        /// <summary>
        /// expectation completion status.
        /// </summary>
        public int ExportStatus { get; set; }
    }
}
