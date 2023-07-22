namespace Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts
{
    using System.Collections.Generic;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The content of a command page json file.
    /// </summary>
    public class CommandPage
    {
        /// <summary>
        /// Command operation type, e.g. Delete, Export.
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// The specific “verb” of the signal and differentiates different flavors of operations, e.g. AccountClose, Export.
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// The command type id.
        /// </summary>
        public int CommandTypeId { get; set; }

        /// <summary>
        /// The page number.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// The same properties shared by all commands in the same page, e.g. DataType, SubjectType.
        /// The data come from command page in storage blob
        /// </summary>
        public JToken CommandProperties { get; set; }

        /// <summary>
        /// The actual commands.
        /// The data come from command page in storage blob
        /// </summary>
        public IList<PrivacyCommandV2> Commands { get; set; }
    }
}
