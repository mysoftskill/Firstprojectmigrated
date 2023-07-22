namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Predicates;
    using Microsoft.PrivacyServices.Policy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Delete command from the command feed API.
    /// </summary>
    public partial class DeleteCommand : PrivacyCommand
    {
        /// <summary>
        /// The type of this command.
        /// </summary>
        public const string CommandTypeName = "delete";

        /// <summary>
        /// Initializes a new delete command.
        /// </summary>
        public DeleteCommand() : base(CommandTypeName)
        {
        }

        /// <inheritdoc/>
        [JsonProperty("privacyDataType")]
        public DataTypeId PrivacyDataType { get; set; }

        /// <inheritdoc/>
        [JsonProperty("predicate")]
        public IPrivacyPredicate DataTypePredicate { get; set; }

        /// <inheritdoc/>
        [JsonProperty("timeRangePredicate")]
        public TimeRangePredicate TimeRangePredicate { get; set; }
    }
}
