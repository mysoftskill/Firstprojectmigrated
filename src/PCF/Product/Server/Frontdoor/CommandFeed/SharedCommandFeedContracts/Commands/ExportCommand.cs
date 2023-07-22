namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.PrivacyServices.Policy;
    using Newtonsoft.Json;

    /// <summary>
    /// An export command from the privacy command feed.
    /// </summary>
    public partial class ExportCommand : PrivacyCommand
    {
        /// <summary>
        /// The type of this command.
        /// </summary>
        public const string CommandTypeName = "export";

        /// <summary>
        /// Initializes a new Export command.
        /// </summary>
        public ExportCommand()
            : base(CommandTypeName)
        {
        }

        /// <inheritdoc/>
        [JsonProperty("privacyDataTypes")]
        public IEnumerable<DataTypeId> PrivacyDataTypes { get; set; }

        /// <inheritdoc/>
        [JsonProperty("azureBlobUri")]
        public Uri AzureBlobContainerTargetUri { get; set; }

        /// <inheritdoc/>
        [JsonProperty("azureBlobPath")]
        public string AzureBlobContainerPath { get; set; }
    }
}
