namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// V2 PrivacyCommand, only fields needed in validation are populated
    /// </summary>
    public class PrivacyCommandV2
    {
        /// <summary>
        /// The unique ID of the command.
        /// </summary>
        [JsonProperty("commandId")]
        public string CommandId { get; set; }

        /// <summary>
        /// The operation type - delete/export
        /// </summary>
        [JsonProperty("operationType")]
        public string OperationType { get; set; }

        /// <summary>
        /// The specific “verb” of the signal and differentiates different flavors of operations, e.g. AccountClose, Export.
        /// </summary>
        [JsonProperty("operation")]
        public string Operation { get; set; }

        /// <summary>
        /// The command type id.
        /// </summary>
        [JsonProperty("commandTypeId")]
        public int CommandTypeId { get; set; }

        /// <summary>
        /// The same properties shared by all commands in the same page, e.g. DataType, SubjectType.
        /// The data come from command page in storage blob
        /// </summary>
        [JsonProperty("commandProperties")]
        public IList<CommandProperty> CommandProperties { get; set; }

        /// <summary>
        /// Predicates for signals
        /// </summary>
        [JsonProperty("rowPredicate")]
        public JToken RowPredicate { get; set; }

        /// <summary>
        /// Time range for which to apply the signal. 
        /// </summary>
        [JsonProperty("timeRangePredicate")]
        public TimeRangePredicateV2 TimeRangePredicate { get; set; }

        /// <summary>
        /// The time at which the command was issued.
        /// </summary>
        [JsonProperty("timestamp")]
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// The subject of the command. This will be a <see cref="MsaSubject"/>, <see cref="AadSubject"/>, <see cref="AadSubject2"/>, <see cref="DeviceSubject"/>, or -- less commonly -- a <see cref="DemographicSubject"/>.
        /// </summary>
        [JsonProperty("subject")]
        public JToken Subject { get; set; }

        /// <summary>
        /// The PCF command verifier.
        /// </summary>
        [JsonProperty("verifier")]
        public string Verifier { get; set; }

        /// <summary>
        /// Indicates whether this command is scoped to processor data.
        /// </summary>
        public bool ProcessorApplicable { get; set; }

        /// <summary>
        /// Indicates whether this command is scoped to controller data.
        /// </summary>
        public bool ControllerApplicable { get; set; }

        /// <summary>
        /// Gets/sets the Azure Blob container to which the exported data should be uploaded.
        /// NOTE: Only export command for continuous agent will have this value set
        /// </summary>
        [JsonProperty("azureBlobUri")]
        public Uri AzureBlobContainerTargetUri { get; set; }

        /// <summary>
        /// Gets/sets the Azure container path the agent should write into. Do not write into any path within the
        /// <see cref="AzureBlobContainerTargetUri" /> than this path.
        /// NOTE: Only export command for continuous agent will have this value set
        /// </summary>
        [JsonProperty("azureBlobPath")]
        public string AzureBlobContainerPath { get; set; }
    }
}
