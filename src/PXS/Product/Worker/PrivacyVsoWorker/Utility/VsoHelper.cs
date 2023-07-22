namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.Membership.MemberServices.Configuration;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     Vso Helper to create work items
    /// </summary>
    public class VsoHelper : IVsoHelper
    {
        private readonly IFileSystemProcessor fileSystemProcessor;

        private readonly ILogger logger;

        private readonly string projectUrl;

        private readonly Task<string> taskGetVsoAccessToken;

        private readonly IVSOConfig vsoConfig;

        private string vsoAccessToken;

        public VsoHelper(ILogger logger, IVSOConfig vsoConfig, IFileSystemProcessor fileSystemProcessor, ISecretStoreReader secretReader)
        {
            if (vsoConfig == null)
                throw new ArgumentNullException(nameof(vsoConfig));
            if (secretReader == null)
                throw new ArgumentNullException(nameof(secretReader));
            if (string.IsNullOrWhiteSpace(vsoConfig.VSOProjectUrl))
                throw new ArgumentNullException(nameof(vsoConfig.VSOProjectUrl));
            if (string.IsNullOrWhiteSpace(vsoConfig.VSOAccessKeyName))
                throw new ArgumentNullException(nameof(vsoConfig.VSOAccessKeyName));

            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.projectUrl = vsoConfig.VSOProjectUrl;
            this.vsoConfig = vsoConfig;
            this.taskGetVsoAccessToken = secretReader.ReadSecretByNameAsync(vsoConfig.VSOAccessKeyName);
            this.fileSystemProcessor = fileSystemProcessor ?? throw new ArgumentNullException(nameof(fileSystemProcessor));
        }

        public async Task<JObject> CreateVsoWorkItemIfNotExistsAsync(Agent agent)
        {
            if (!this.CheckIfItemPresentAsync(agent.GenerateTitle()).GetAwaiter().GetResult())
            {
                if (!this.vsoConfig.EnableVsoWorkItemCreation)
                {
                    this.logger.Information(nameof(VsoHelper), $"EnableVsoWorkItemCreation: ${this.vsoConfig.EnableVsoWorkItemCreation}");
                    return null;
                }

                if (!this.vsoConfig.EnableWorkItemAssignment)
                {
                    agent.AlertContacts = string.Empty;
                }

                JObject workItem = await this.CreateVsoWorkItemAsync(
                    agent.BuildWorkItemObjectForMissingIcmInfo(
                        this.vsoConfig.WorkItemType,
                        this.vsoConfig.WorkItemAreaPath,
                        this.vsoConfig.WorkItemTeamProject,
                        $"{this.vsoConfig.WorkItemTeamProject}\\\\{DateTime.UtcNow:yy}{DateTime.UtcNow:MM}",
                        this.fileSystemProcessor.GetAgentsWithNoConnectorsItemDesc(),
                        this.vsoConfig.WorkItemTags.Trim('"'))).ConfigureAwait(false);
                this.logger.Information(nameof(VsoHelper), $"Created work item: ${workItem}");
                return workItem;
            }

            return null;
        }

        private async Task<string> EnsureVsoTokenFetchedAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(this.vsoAccessToken))
                    this.vsoAccessToken = await this.taskGetVsoAccessToken.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(VsoHelper), ex, $"Could not fetch Vso access token. {vsoConfig.VSOAccessKeyName}");
                throw;
            }

            return this.vsoAccessToken;
        }

        private async Task<bool> CheckIfItemPresentAsync(string title)
        {
            string requestJson =
                "{\"query\": \"Select [System.Id], [System.Title], [System.State], [Microsoft.VSTS.Build.FoundIn] From WorkItems Where [System.WorkItemType] == 'Bug' AND [State] <> 'Closed' AND [State] <> 'Removed' AND [System.Title] == '" +
                title + "' order by [Microsoft.VSTS.Common.Priority] asc, [System.CreatedDate] desc\"}";

            using (var client = new HttpClient())
            {
                //set our headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes(
                            $":{await this.EnsureVsoTokenFetchedAsync().ConfigureAwait(false)}")));

                var method = new HttpMethod("POST");
                var request = new HttpRequestMessage(method, this.projectUrl + "_apis/wit/wiql?api-version=4.1");
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                HttpResponseMessage response = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    var workItemResponse = JsonConvert.DeserializeObject<WorkItemResponse>(responseBody);

                    if (workItemResponse?.WorkItems?.Length > 0)
                    {
                        string workItemId = workItemResponse?.WorkItems?.FirstOrDefault()?.Id;
                        this.logger.Information(
                            nameof(VsoHelper),
                            @"Item with same title already present, Title:[{0}] Item ID: [{1}]",
                            title,
                            workItemId);
                        return true;
                    }
                }
                else
                {
                    throw new Exception($"Failure checking VSO: {response.StatusCode} : {response.ReasonPhrase}");
                }

                return false;
            }
        }

        private async Task<JObject> CreateVsoWorkItemAsync(object[] patchDocument)
        {
            if (patchDocument == null)
                throw new ArgumentNullException(nameof(patchDocument));

            //use the httpclient
            using (var client = new HttpClient())
            {
                //set our headers
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Basic",
                    Convert.ToBase64String(
                        Encoding.ASCII.GetBytes(
                            $":{await this.EnsureVsoTokenFetchedAsync().ConfigureAwait(false)}")));

                //serialize the fields array into a json string
                var patchValue = new StringContent(JsonConvert.SerializeObject(patchDocument), Encoding.UTF8, "application/json-patch+json");

                var method = new HttpMethod("POST");
                var request = new HttpRequestMessage(method, this.projectUrl + "_apis/wit/workitems/$Bug?api-version=4.1") { Content = patchValue };
                HttpResponseMessage response = client.SendAsync(request).Result;

                if (response.IsSuccessStatusCode)
                {
                    return this.ParseWebResponse(response.Content.ReadAsStringAsync().Result);
                }
                else
                {
                    throw new Exception(response.Content.ReadAsStringAsync().Result);
                }
            }
        }

        private JObject ParseWebResponse(string response)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            try
            {
                return JObject.Parse(response);
            }
            catch (Exception ex)
            {
                this.logger.Error(nameof(VsoHelper), ex, ex.Message);
                throw;
            }
        }
    }

    internal class WorkItemResponse
    {
        public WorkItems[] WorkItems = null;
    }

    internal class WorkItems
    {
        public string Id = null;

        public string Url = null;
    }
}
