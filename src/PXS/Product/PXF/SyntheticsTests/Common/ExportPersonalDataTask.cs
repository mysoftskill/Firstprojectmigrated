namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;

    /// <summary>
    /// Create an export request as a home tenant admin
    /// TODO: add config info so that this can be used for both the home tenant and resource tenant scenarios
    /// </summary>
    public static class ExportPersonalDataTask
    {
        public static async Task RunAsync(TelemetryClient telemetryClient, ExportPersonalDataConfig config)
        {
            string token = await GraphAuthenticationHelper.GetGraphAccessTokenAsync(config.UserName, config.Password, config.ClientId, config.Authority);

            if (token != null)
            { 
                var httpClient = new HttpClient(); // lgtm [cs/httpclient-checkcertrevlist-disabled]
                var apiCaller = new ProtectedApiCallHelper(httpClient, telemetryClient);

                // Get the list of users
                List<GraphUser> result = (List<GraphUser>)await apiCaller.CallGetAsync<GraphUser>($"{config.ApiUrlEndpoint}/users?$select=id,userPrincipalName", token);

                // Find Test Export User
                // TODO: consider creating a new user for each request or maybe make this configurable?
                string tenant = config.UserName.Substring(config.UserName.IndexOf('@') + 1);
                GraphUser exportUser = result.Where(u => u.UserPrincipalName == $"testexport@{tenant}").FirstOrDefault();

                CloudBlobContainer container = null;
                try
                {
                    string userId = exportUser.Id; // Test Export User

                    container = await BlobStorageHelper.GetCloudBlobContainerAsync(
                        config.BlobStorageConnectionString,
                        "synthetics-export-" + Guid.NewGuid().ToString("N")).ConfigureAwait(false);
                    Uri targetStorageLocation = await BlobStorageHelper.GetSharedAccessSignatureAsync(container).ConfigureAwait(false);
                    string apiUrlPath = string.Format(config.ApiPathTemplate, userId);
                    HttpResponseMessage httpResponseMessage = await apiCaller.CallPostAsync<ExportPersonalDataBody>($"{config.ApiUrlEndpoint}/{apiUrlPath}", token,
                        new ExportPersonalDataBody { StorageLocation = targetStorageLocation }).ConfigureAwait(false);

                    string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                    var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                    telemetryClient.TrackTrace(responseDetails);

                    if (httpResponseMessage.Headers.TryGetValues("Location", out IEnumerable<string> operationLocationValues))
                    {
                        IEnumerable<string> locationValues = operationLocationValues?.ToList();
                        string statusUrl = locationValues?.FirstOrDefault();
                        if (Uri.IsWellFormedUriString(statusUrl, UriKind.Absolute))
                        {
                            telemetryClient.TrackTrace($"Status Location: {statusUrl}.");

                            // TODO: save the status url in a table (or queue) that we can use in
                            // a dataPolicyOperation synthetics task
                        }
                        else
                        {
                            telemetryClient.TrackTrace($"Should be absolute uri: {statusUrl}.", SeverityLevel.Error);
                        }
                    }
                    else
                    {
                        telemetryClient.TrackTrace("Response did not contain status location", SeverityLevel.Warning);
                    }
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackTrace($"Error getting SAS token for blob storage: {ex}", SeverityLevel.Error);
                }
            }
            else
            {
                telemetryClient.TrackTrace($"Unable to get Graph Access Token for UserName: {config.UserName} with ClientId {config.ClientId}.", SeverityLevel.Error);
            }
        }
    }
}

