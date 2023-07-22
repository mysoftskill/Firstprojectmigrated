namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.Serialization;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Acquires MSA S2S tickets.
    /// </summary>
    public class MicrosoftAccountAuthClient : IAuthClient
    {
        private readonly object syncRoot = new object();

        private readonly string scheme = "MSAS2S";
        private readonly CommandFeedLogger logger;
        private readonly Uri msaTicketUri;
        private readonly string targetSiteName;
        private readonly IHttpClient httpClient;
        private readonly long clientSiteId;

        private Task renewTicketTask;
        private AppTicket currentTicket;

        /// <summary>
        /// Initializes a new MicrosoftAccountAuthClient. This class can acquire MSA S2S tickets that can be used to authenticate to PCF.
        /// </summary>
        /// <param name="clientSiteId">The client site ID.</param>
        /// <param name="logger">The command feed logger.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="endpointConfiguration">The endpoint configuration</param>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "algorithm")]
        public MicrosoftAccountAuthClient(long clientSiteId, CommandFeedLogger logger, IHttpClient httpClient, CommandFeedEndpointConfiguration endpointConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (endpointConfiguration == null)
            {
                throw new ArgumentNullException(nameof(endpointConfiguration));
            }

            this.msaTicketUri = endpointConfiguration.MsaAuthEndpoint;
            this.targetSiteName = endpointConfiguration.CommandFeedMsaSiteName;
            this.clientSiteId = clientSiteId;

            if (httpClient == null)
            {
                throw new ArgumentNullException(nameof(httpClient));
            }

            if (httpClient.Certificate == null || !httpClient.Certificate.HasPrivateKey)
            {
                throw new ArgumentException("Client certificate must have private key", nameof(httpClient));
            }

            try
            {
                // Throws if there is an error.
                var algorithm = httpClient.Certificate.PrivateKey;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"The given client certificate does not have an accessible private key. Does the current account have access? Please see the inner exception for details.", ex);
            }

            this.httpClient = httpClient;
        }

        /// <inheritdoc />
        public string Scheme => this.scheme;

        /// <summary>
        /// Gets an MSA S2S token asynchronously. Most of the time, this will be a synchronous call.
        /// </summary>
        /// <returns>The token.</returns>
        public async Task<string> GetAccessTokenAsync()
        {
            await this.RefreshTicketIfNecessaryAsync().ConfigureAwait(false);

            var ticket = this.currentTicket;
            if (ticket?.TimeUntilExpiration >= TimeSpan.Zero)
            {
                return ticket.AccessToken;
            }
            
            throw new InvalidOperationException("No valid service to service access token was available for use. Please check the output of the 'UnexpectedException' callback of your CommandFeedLogger instance.");
        }

        /// <summary>
        /// Provides the mechanics of fetching a new app ticket from MSA.
        /// </summary>
        private Task RefreshTicketIfNecessaryAsync()
        {
            if (this.currentTicket == null || this.currentTicket.TimeUntilExpiration <= TimeSpan.FromHours(1))
            {
                Task renewTask;

                lock (this.syncRoot)
                {
                    if (this.renewTicketTask == null)
                    {
                        this.logger.BeginServiceToServiceAuthRefresh(this.targetSiteName, this.clientSiteId);

                        // Start a task to renew the ticket, and clear the task when done.
                        this.renewTicketTask = this.RenewTicketAsync().ContinueWith(t =>
                        {
                            // Reenter the lock here to prevent races with the outside block.
                            lock (this.syncRoot)
                            {
                                this.renewTicketTask = null;
                            }

                            // If we had an exception, bubble it here.
                            if (t.IsFaulted)
                            {
                                throw t.Exception.InnerException ?? t.Exception;
                            }
                        });
                    }

                    renewTask = this.renewTicketTask;
                }

                // If current ticket is invalid, return the renewal task. Otherwise, we just let the renewal proceed in the background.
                if (this.currentTicket == null || this.currentTicket.TimeUntilExpiration <= TimeSpan.Zero)
                {
                    return renewTask;
                }
            }

            return Task.FromResult(true);
        }

        private async Task RenewTicketAsync()
        {
            string responseBody = null;
            HttpStatusCode? responseCode = null;
            try
            {
                string escapedTicketScope = Uri.EscapeDataString($"{this.targetSiteName}::S2S_24HOURS_MUTUALSSL");
                string escapedSiteId = Uri.EscapeDataString(this.clientSiteId.ToString(CultureInfo.InvariantCulture));

                string body = $"grant_type=client_credentials&client_id={escapedSiteId}&scope={escapedTicketScope}";

                StringContent content = new StringContent(body);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, this.msaTicketUri);
                request.Content = content;

                HttpResponseMessage response = await this.httpClient.SendAsync(request, CancellationToken.None).ConfigureAwait(false);
                this.logger.HttpResponseReceived(request, response);

                responseCode = response.StatusCode;
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpRequestException("Unexpected HTTP response code: " + response.StatusCode);
                }

                var ticket = AppTicket.FromJson(responseBody);
                this.currentTicket = ticket;
            }
            catch (Exception ex)
            {
                var httpException = new HttpRequestException(
                    $"Unable to get MSA S2S ticket. Response code = {responseCode}, Message = {responseBody}, Certificate = {this.httpClient?.Certificate?.Thumbprint}, SiteId = {this.clientSiteId}, TargetSite = {this.targetSiteName}. " +
                    $"Certificate has private key = {this.httpClient?.Certificate?.HasPrivateKey}" +
                    $"Please ensure that your process can access the certificate's private key and MSM site is used as Prod MSA Site", ex);

                this.logger.UnhandledException(httpException);
                throw httpException;
            }
        }

        /// <summary>
        /// Data contract for server response
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        private class AppTicket
        {
            private const int FiveMinutesInSeconds = 300;

            [DataMember(Name = "access_token")]
            public string AccessToken
            {
                get;
                set;
            }

            [DataMember(Name = "token_type")]
            public string TokenType
            {
                get;
                set;
            }

            [DataMember(Name = "expires_in")]
            private int ExpiresIn
            {
                get;
                set;
            }

            [DataMember(Name = "error")]
            public string Error
            {
                get;
                set;
            }

            [DataMember(Name = "error_description")]
            public string ErrorMessage
            {
                get;
                set;
            }

            public TimeSpan TimeUntilExpiration
            {
                get
                {
                    return 
                        (this.TokenIssueTimeUtc + TimeSpan.FromSeconds(this.ExpiresIn - FiveMinutesInSeconds)) -
                        DateTimeOffset.UtcNow;                                                 
                }
            }

            public DateTimeOffset TokenIssueTimeUtc
            {
                get;
                set;
            }

            public static AppTicket FromJson(string json)
            {
                if (string.IsNullOrEmpty(json))
                {
                    throw new ArgumentNullException("json");
                }

                AppTicket appTicket = Newtonsoft.Json.JsonConvert.DeserializeObject<AppTicket>(json);
                appTicket.TokenIssueTimeUtc = DateTimeOffset.UtcNow;

                return appTicket;
            }
        }
    }
}
