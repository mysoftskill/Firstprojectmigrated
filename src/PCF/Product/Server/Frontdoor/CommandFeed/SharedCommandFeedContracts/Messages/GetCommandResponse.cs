namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Newtonsoft.Json;

    /// <summary>
    /// The result of a call to get commands from command feed.
    /// </summary>
    [JsonObject]
    internal class GetCommandsResponse : IEnumerable<PrivacyCommand>
    {
        [JsonProperty("deleteCommands")]
        public List<DeleteCommand> DeleteCommands { get; set; } = new List<DeleteCommand>();

        [JsonProperty("exportCommands")]
        public List<ExportCommand> ExportCommands { get; set; } = new List<ExportCommand>();

        [JsonProperty("accountCloseCommands")]
        public List<AccountCloseCommand> AccountCloseCommands { get; set; } = new List<AccountCloseCommand>();

        [JsonProperty("ageOutCommands")]
        public List<AgeOutCommand> AgeOutCommands { get; set; } = new List<AgeOutCommand>();

        [JsonIgnore]
        public int Count
        {
            get
            {
                return 
                    (this.ExportCommands?.Count ?? 0) + 
                    (this.AccountCloseCommands?.Count ?? 0) + 
                    (this.DeleteCommands?.Count ?? 0) + 
                    (this.AgeOutCommands?.Count ?? 0);
            }
        }

        /// <summary>
        /// Adds the given command to the appropriate collection.
        /// </summary>
        public void Add(PrivacyCommand command)
        {
            if (command is AccountCloseCommand accountClose)
            {
                this.AccountCloseCommands.Add(accountClose);
            }
            else if (command is DeleteCommand deleteCommand)
            {
                this.DeleteCommands.Add(deleteCommand);
            }
            else if (command is ExportCommand exportCommand)
            {
                this.ExportCommands.Add(exportCommand);
            }
            else if (command is AgeOutCommand ageOutCommand)
            {
                this.AgeOutCommands.Add(ageOutCommand);
            }
            else
            {
                throw new InvalidOperationException("Unexpected command type: " + command?.GetType().FullName);
            }
        }
        
        /// <summary>
        /// Gets an enumerator that returns all of the commands in this object.
        /// </summary>
        public IEnumerator<PrivacyCommand> GetEnumerator()
        {
            if (this.DeleteCommands != null)
            {
                foreach (var deleteCommand in this.DeleteCommands)
                {
                    yield return deleteCommand;
                }
            }

            if (this.ExportCommands != null)
            {
                foreach (var exportCommand in this.ExportCommands)
                {
                    yield return exportCommand;
                }
            }

            if (this.AccountCloseCommands != null)
            {
                foreach (var accountCloseCommand in this.AccountCloseCommands)
                {
                    yield return accountCloseCommand;
                }
            }

            if (this.AgeOutCommands != null)
            {
                foreach (var ageOutCommand in this.AgeOutCommands)
                {
                    yield return ageOutCommand;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
