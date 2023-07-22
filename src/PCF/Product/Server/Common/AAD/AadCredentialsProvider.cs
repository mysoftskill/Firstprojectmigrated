namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Rest;
    using Microsoft.Azure.ComplianceServices.Common;

    /// <summary>
    /// Fetches AAD credentials.
    /// </summary>
    public class AadCredentialProvider : IAadCredentialProvider
    {
        private readonly ConcurrentDictionary<string, SingleResourceCredentialProvider> providerMap = new ConcurrentDictionary<string, SingleResourceCredentialProvider>();

        /// <summary>
        /// Fetches a token from the named resource.
        /// </summary>
        public async Task<TokenCredentials> GetCredentialsAsync(string resource)
        {
            if (!this.providerMap.TryGetValue(resource, out SingleResourceCredentialProvider provider))
            {
                provider = new SingleResourceCredentialProvider(resource);
                this.providerMap[resource] = provider;
            }

            var authResult = await provider.GetAuthenticationResultAsync();
            return new TokenCredentials(authResult.AccessToken);
        }

        private class SingleResourceCredentialProvider
        {
            private readonly object syncRoot = new object();
            private readonly string resource;

            private Task renewTicketTask;
            private AuthenticationResult currentResult;
            private ConfidentialCredential credentialClient;

            public SingleResourceCredentialProvider(string resource)
            {
                this.resource = resource;

                var config = Config.Instance.AzureManagement;
                string authority = $"https://login.microsoftonline.com/{config.TenantId}";

                this.credentialClient = new ConfidentialCredential(config.ApplicationId,
                    Config.Instance.Common.ServiceToServiceCertificate,
                    new Uri(authority));
            }
            
            /// <summary>
            /// Gets an MSA S2S token asynchronously. Most of the time, this will be a synchronous call.
            /// </summary>
            /// <returns>The token.</returns>
            public async Task<AuthenticationResult> GetAuthenticationResultAsync()
            {
                // Invariant: once this method returns there should always be a valid ticket.
                await this.RefreshTicketIfNecessaryAsync();

                var ticket = this.currentResult;
                if (ticket != null)
                {
                    return ticket;
                }

                throw new InvalidOperationException("Failed to get valid ticket");
            }

            /// <summary>
            /// Provides the mechanics of fetching a new app ticket from MSA.
            /// </summary>
            private Task RefreshTicketIfNecessaryAsync()
            {
                bool needsRefresh = this.currentResult == null || this.currentResult.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(5);
                if (needsRefresh)
                {
                    Task renewTask;

                    lock (this.syncRoot)
                    {
                        if (this.renewTicketTask == null)
                        {
                            // Start a task to renew the ticket, and clear the task when done.
                            this.renewTicketTask = this.RenewTicketAsync().ContinueWith(t =>
                            {
                                // Reenter the lock here to prevent races with the outside block.
                                lock (this.syncRoot)
                                {
                                    this.renewTicketTask = null;
                                }
                            });
                        }

                        renewTask = this.renewTicketTask;
                    }

                    // If current ticket is invalid, return the renewal task. Otherwise, we just let the renewal proceed in the background.
                    bool isInvalid = this.currentResult == null || this.currentResult.ExpiresOn <= DateTimeOffset.UtcNow.AddMinutes(1);
                    if (isInvalid)
                    {
                        return renewTask;
                    }
                }

                return Task.FromResult(true);
            }

            private Task RenewTicketAsync()
            {
                return Logger.InstrumentAsync(
                    new OutgoingEvent(SourceLocation.Here()),
                    async (ev) =>
                    {
                        var scopes = new[] { $"{this.resource}/.default" };

                        var result = await this.credentialClient.GetTokenAsync(scopes);

                        ev["AccessTokenExpiration"] = result.ExpiresOn.ToString();
                        ev["AccessTokenDurationMinutes"] = (result.ExpiresOn - DateTimeOffset.UtcNow).TotalMinutes.ToString();

                        this.currentResult = result;
                    });
            }
        }
    }
}
