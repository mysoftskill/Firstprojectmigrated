namespace Microsoft.PrivacyServices.AzureFunctions.Common.Models
{
    /// <summary>
    /// Models the Data Agent
    /// </summary>
    public class Agent
    {
        /// <summary>
        /// Gets or sets Id of the data agent
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        /// Gets or sets Name of the agent
        /// </summary>
        public string AgentName { get; set; }

        /// <summary>
        /// Gets or sets Contacts that can be notified about the agent
        /// </summary>
        public string AlertContacts { get; set; }

        /// <summary>
        /// Gets or sets Contacts that will recieved agent announcements
        /// </summary>
        public string AnnouncementContacts { get; set; }

        /// <summary>
        /// Gets or sets List of completed commands
        /// </summary>
        public string CompletedCommands { get; set; }

        /// <summary>
        /// Gets or sets Rate that commands are completed
        /// </summary>
        public string CompletedCommandsRate { get; set; }

        /// <summary>
        /// Gets or sets Service Tree Division
        /// </summary>
        public string DivisionName { get; set; }

        /// <summary>
        /// Gets or sets General Contractor in charge of agent
        /// </summary>
        public string GC { get; set; }

        /// <summary>
        /// Gets or sets List of Ingested Commands
        /// </summary>
        public string IngestedCommands { get; set; }

        /// <summary>
        /// Gets or sets Link to filed bug
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// Gets or sets Service Tree Organization
        /// </summary>
        public string OrganizationName { get; set; }

        /// <summary>
        /// Gets or sets PDMS assigned owner Id
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Gets or sets Service Group Name from Service Tree
        /// </summary>
        public string ServiceGroupName { get; set; }

        /// <summary>
        /// Gets or sets Service Id from Service Tree
        /// </summary>
        public string ServiceId { get; set; }

        /// <summary>
        /// Gets or sets Service Name from Service Tree
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets Name of Team
        /// </summary>
        public string TeamGroupName { get; set; }

        /// <summary>
        /// Gets or sets Tenant Id of agent
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets Tenant Name of agent
        /// </summary>
        public string TenantName { get; set; }

        /// <summary>
        /// Gets or sets Data owner vertical
        /// </summary>
        public string Vertical { get; set; }
    }
}
