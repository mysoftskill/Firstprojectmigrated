namespace Microsoft.Membership.MemberServices.PrivacyExperience.SyntheticsTests.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure.Storage.Blob;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.GraphApis;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common.Utilities;

    /// <summary>
    /// Create an remove personal data request as a home tenant admin
    /// TODO: add config info so that this can be used for both the home tenant and resource tenant scenarios
    /// </summary>
    public static class RemovePersonalDataTask
    {
        public static async Task RunAsync(TelemetryClient telemetryClient, RemovePersonalDataConfig config)
        {
            string TokenForUserInHomeTenant = await GraphAuthenticationHelper.GetGraphAccessTokenAsync(config.UserNameForHomeTenant, config.PasswordForUserInHomeTenant, config.HomeTenantClientId);
            string TokenForUserInResourceTenant = await GraphAuthenticationHelper.GetGraphAccessTokenAsync(config.UserNameForResourceTenant, config.PasswordForUserInResourceTenant, config.ResourceTenantClientId, "CrossTenantUserProfileSharing.ReadWrite.All");

            if (TokenForUserInHomeTenant != null && TokenForUserInResourceTenant != null)
            { 
                var httpClient = new HttpClient(); // lgtm [cs/httpclient-checkcertrevlist-disabled]
                var apiCaller = new ProtectedApiCallHelper(httpClient, telemetryClient);
                try
                {
                    string userId = "02862820-d675-4f23-a2cc-2e917ab5b98d"; // Resource Tenant User
                    // First the user functional-test-admin@meepxsresource.onmicrosoft.com tries to get a token for traversing to tenant meepxs.onmicrosoft.com
                    // with his/her native identity using one of the cross-tenant flows (B2B connect, Multi-tenant collaboration, etc).
                    // More on this can be found at https://microsoftgraph.visualstudio.com/onboarding/_git/onboarding?path=/reviews/8883-consent-to-cross-tenant-boundary/api.md&_a=preview&version=GBmaster
                    using (HttpResponseMessage httpResponseMessage = await apiCaller.CallPostAsync<ResourceTenantDataSharingConsent>($"https://graph.microsoft.com/v1.0/users/{userId}/resourceTenantDataSharingConsents", TokenForUserInResourceTenant,
                        new ResourceTenantDataSharingConsent { ResourceTenantId = "7bdb2545-6702-490d-8d07-5cc0a5376dd9" }).ConfigureAwait(false))
                    {
                        httpResponseMessage.EnsureSuccessStatusCode();
                    }
                    string apiUrlPath = string.Format(config.ApiPathTemplate, userId);
                    using (HttpResponseMessage httpResponseMessage = await apiCaller.CallPostAsync<StringContent>($"{config.ApiUrlEndpoint}/{apiUrlPath}", TokenForUserInHomeTenant,
                        null).ConfigureAwait(false)) {

                        string content = httpResponseMessage.Content != null ? await (httpResponseMessage.Content.ReadAsStringAsync()).ConfigureAwait(false) : string.Empty;
                        var responseDetails = $"Status Code: {httpResponseMessage.StatusCode}, Content: {content}";
                        telemetryClient.TrackTrace(responseDetails);
                    }
                }
                catch (Exception ex)
                {
                    telemetryClient.TrackTrace($"Error creating resource tenant data sharing consent: {ex}", SeverityLevel.Error);
                }
            }
            else
            {
                StringBuilder errorString = new StringBuilder("Unable to get Graph Access Token for UserName: ");
                bool IsTokenGeneratedForHomeTenantUser = TokenForUserInHomeTenant != null;
                bool IsTokenGeneratedForResourceTenantUser = TokenForUserInResourceTenant != null;
                if(!IsTokenGeneratedForHomeTenantUser) // If an error occurred while generating the token for the home tenant user
                {
                    errorString.Append(config.UserNameForHomeTenant);
                }
                if(!IsTokenGeneratedForResourceTenantUser) // If an error occurred while generating the token for the resource tenant user
                {
                    if(!IsTokenGeneratedForHomeTenantUser) // If we already encountered an error while generating the token for the home tenant user token
                    {
                        errorString.Append(" and ").Append(config.UserNameForResourceTenant);
                    }
                    else
                    {
                        errorString.Append(config.UserNameForResourceTenant); // We only encountered error while generating the token for the resource tenant user 
                    }
                }
                telemetryClient.TrackTrace($"{errorString}", SeverityLevel.Error);
            }
        }
    }
}

