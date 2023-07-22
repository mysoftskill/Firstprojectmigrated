namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility
{
    /// <summary>
    ///     Agent Class to store all the Agent info
    /// </summary>
    public class Agent
    {
        public string AgentId;

        public string AgentName;

        public string AlertContacts;

        public string AnnouncementContacts;

        public string CompletedCommands;

        public string CompletedCommandsRate;

        public string DivisionName;

        public string GC;

        public string IngestedCommands;

        public string Link;

        public string OrganizationName;

        public string OwnerId;

        public string ServiceGroupName;

        public string ServiceId;

        public string ServiceName;

        public string TeamGroupName;

        public string TenantId;

        public string TenantName;

        public string Vertical;

        public Agent()
        {
            this.DivisionName = string.Empty;
            this.OrganizationName = string.Empty;
            this.ServiceGroupName = string.Empty;
            this.TeamGroupName = string.Empty;
            this.ServiceName = string.Empty;
            this.AgentName = string.Empty;
            this.CompletedCommandsRate = string.Empty;
            this.CompletedCommands = string.Empty;
            this.IngestedCommands = string.Empty;
            this.Link = string.Empty;
            this.AgentId = string.Empty;
            this.Vertical = string.Empty;
            this.AnnouncementContacts = string.Empty;
            this.AlertContacts = string.Empty;
            this.ServiceId = string.Empty;
            this.OwnerId = string.Empty;
        }

        /// <summary>
        ///     Build work item object for missing ICM Info, which is used to create work item
        /// </summary>
        /// <param name="type">Type of work item. Eg.Bug</param>
        /// <param name="areaPath">Area Path where work item needs to be created</param>
        /// <param name="teamProject">Team Project where work item needs to be created</param>
        /// <param name="iterationPath">Iteration Path where work item needs to be created</param>
        /// <param name="description">Description of work item that needs to be created</param>
        /// <param name="tags">Tags for work item that needs to be created</param>
        /// <returns>object array</returns>
        public object[] BuildWorkItemObjectForMissingIcmInfo(string type, string areaPath, string teamProject, string iterationPath, string description, string tags)
        {
            var patchDocument = new object[8];

            patchDocument[0] = new { op = "add", path = "/fields/System.Title", value = this.GenerateTitle() };
            patchDocument[1] = new { op = "add", path = "/fields/Microsoft.VSTS.TCM.ReproSteps", value = description };
            patchDocument[2] = new
            {
                op = "add",
                path = "/fields/System.Tags",
                value = tags + this.Vertical
            };

            patchDocument[3] = new { op = "add", path = "/fields/System.WorkItemType", value = type };
            patchDocument[4] = new { op = "add", path = "/fields/System.AreaPath", value = areaPath };
            patchDocument[5] = new { op = "add", path = "/fields/System.TeamProject", value = teamProject };
            patchDocument[6] = new { op = "add", path = "/fields/System.IterationPath", value = iterationPath };
            patchDocument[7] = string.IsNullOrEmpty(this.AlertContacts)
                ? new { op = "add", path = "/fields/System.AssignedTo", value = "" }
                : new { op = "add", path = "/fields/System.AssignedTo", value = this.AlertContacts };

            //The guid in the first link is the corresponding OwnerID
            //PCD Team: Education Services(https://manage.privacy.microsoft.com/data-owners/edit/c3b68a97-7779-4829-a44f-595319ea475c)
            //The guid in the 2nd link is the corresponding ServiceID
            //Service Tree: Education Services(https://servicetree.msftcloudes.com/#/ServiceModel/Home/d7ed92fb-e1b4-4901-9ada-6d911e8945ce)
            //patchDocument[8] = new { op = "add", path = "/fields/Microsoft.VSTS.Common.CustomString01", value = this.AgentId };
            //patchDocument[9] = new { op = "add", path = "/fields/Microsoft.VSTS.Common.CustomString02", value = this.Vertical };

            return patchDocument;
        }

        public string GenerateTitle()
        {
            return "[" + this.ServiceGroupName + "][" + this.OrganizationName + "] - [" + this.AgentId + "]";
        }
    }
}
