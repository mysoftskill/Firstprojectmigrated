namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// An age out command from the privacy command feed.
    /// </summary>
    public partial class AgeOutCommand : PrivacyCommand
    {
        /// <summary>
        /// The type of this command.
        /// </summary>
        public const string CommandTypeName = "ageOut";

        /// <summary>
        /// Initializes a new Age out command.
        /// </summary>
        public AgeOutCommand() : base(CommandTypeName)
        {
        }

        /// <summary>
        /// Gets or sets the last time the account was active.
        /// </summary>
        [JsonProperty("lastActive", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset? LastActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if the subject of the command was suspended or not.
        /// </summary>
        [JsonProperty("isSuspended", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool IsSuspended { get; set; }
    }
}
