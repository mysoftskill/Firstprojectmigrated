namespace Microsoft.PrivacyServices.CommandFeed.Client.SharedCommandFeedContracts.Partials
{
    using System;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Privacy-Command-Feed-Parser
    /// </summary>
    public static class PrivacyCommandFeedParser
    {
        /// <summary>
        /// Parses a JSON blob into a command. 
        /// </summary>
        public static PrivacyCommand ParseObject(JObject item)
        {
            // Look at the "type" property to figure out how to interpet the rest of it.
            string type = item.Property("type").Value.Value<string>();

            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.AccountCloseCommand.CommandTypeName))
            {
                return item.ToObject<Client.AccountCloseCommand>();
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.DeleteCommand.CommandTypeName))
            {
                return item.ToObject<Client.DeleteCommand>();
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.ExportCommand.CommandTypeName))
            {
                return item.ToObject<Client.ExportCommand>();
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(type, Client.AgeOutCommand.CommandTypeName))
            {
                return item.ToObject<Client.AgeOutCommand>();
            }

            throw new ArgumentOutOfRangeException($"The type '{type}' was out of range.");
        }
    }
}
