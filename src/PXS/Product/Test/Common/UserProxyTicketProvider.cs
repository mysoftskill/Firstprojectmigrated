// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Server;
    using Microsoft.WindowsLive.Test.WinLiveUser.AuthInterface;

    public class UserProxyTicketProvider : IUserProxyTicketProvider
    {
        private const string ComponentName = nameof(UserProxyTicketProvider);

        public UserProxyTicketProvider(IRpsConfiguration rpsConfiguration)
            : this(rpsConfiguration.Environment, rpsConfiguration.SiteUri, rpsConfiguration.SiteId, rpsConfiguration.AuthPolicy, rpsConfiguration.SiteName)
        {
        }

        /// <summary>
        ///     Constructs a user proxy ticket provider.
        /// </summary>
        /// <param name="environment">The MSA environment to target (e.g. INT or PROD)</param>
        /// <param name="siteUri">The DNS name of the site the user is logging into. Must match MSM configuration.</param>
        /// <param name="siteId">The ID of the site the user is logging into. Must match MSM configuration.</param>
        /// <param name="authPolicy">The authentication policy of the ticket.</param>
        /// <param name="siteName">The name of the site</param>
        public UserProxyTicketProvider(RpsEnvironment environment, Uri siteUri, string siteId, string authPolicy, string siteName)
        {
            this.Environment = (environment == RpsEnvironment.Int)
                ? LiveIdEnvironment.INT
                : LiveIdEnvironment.Production;

            this.SiteUri = siteUri;

            this.SiteId = siteId;

            this.AuthPolicy = authPolicy;

            this.SiteName = siteName;
        }

        public async Task<UserProxyTicketResult> GetTicket(string userName, string password)
        {
            string userTicket = await this.GetUserTicket(userName, password).ConfigureAwait(false);
            string userProxyTicket = await this.ConvertToProxyTicketAsync(userTicket).ConfigureAwait(false);

            return new UserProxyTicketResult
            {
                Ticket = userProxyTicket
            };
        }

        public async Task<UserProxyTicketAndPuidResult> GetTicketAndPuidAsync(string userName, string password)
        {
            string userTicket = await this.GetUserTicket(userName, password).ConfigureAwait(false);
            Tuple<string, long?, long?> result = await this.CreateTicketAsync(userTicket).ConfigureAwait(false);

            return new UserProxyTicketAndPuidResult
            {
                Ticket = result.Item1,
                Puid = result.Item2,
                Cid = result.Item3
            };
        }

        public async Task<string> GetTicketAsync(string userName, string password)
        {
            UserProxyTicketResult result = await this.GetTicket(userName, password).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                throw new Exception("Retrieving user proxy ticket was not successful. Result: " + result);
            }

            if (string.IsNullOrEmpty(result.Ticket))
            {
                throw new Exception("The retrieved user proxy ticket is null. Please check client certificate.");
            }

            return result.Ticket;
        }

        /// <inheritdoc />
        public async Task<string> GetUserTicket(string userName, string password, int maxRetryCount = 3)
        {
            // Password must be HTML encoded for MSA to accept special characters
            password = WebUtility.HtmlEncode(password);

            var authSettings = new AuthInterfaceSettings
            {
                LiveIdEnv = this.Environment,
                SpecialLogin = new SpecialLogin(
                    this.SiteUri.ToString(),
                    this.SiteId,
                    this.AuthPolicy)
            };

            var authInterface = new AuthInterface(authSettings);

            // MSA INT can have login issues occasionally, so do retries here in the event they fail.
            int retryCount = 0;
            do
            {
                retryCount++;

                try
                {
                    return await Task.Run(
                        () =>
                            authInterface.GetCompactTicket(userName, password, this.SiteUri.ToString(), AuthenticationMode.XML)).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    IfxTraceLogger.Instance.Error(ComponentName, $"Exception: {e}");
                    IfxTraceLogger.Instance.Error(ComponentName, $"Message: {e.Message}");
                    IfxTraceLogger.Instance.Error(ComponentName, $"InnerException: {e.InnerException}");

                    if (retryCount >= maxRetryCount)
                    {
                        throw;
                    }

                    // Fixed retry interval
                    await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
                }
            } while (true);
        }

        private string AuthPolicy { get; }

        private LiveIdEnvironment Environment { get; }

        private string SiteId { get; }

        private string SiteName { get; }

        private Uri SiteUri { get; }

        private async Task<string> ConvertToProxyTicketAsync(string userTicket)
        {
            Tuple<string, long?, long?> result = await this.CreateTicketAsync(userTicket).ConfigureAwait(false);
            return result.Item1;
        }

        private async Task<Tuple<string, long?, long?>> CreateTicketAsync(string userTicket)
        {
            if (userTicket == null)
            {
                throw new ArgumentNullException(nameof(userTicket), "userTicket cannot be null");
            }
            if (string.IsNullOrEmpty(userTicket))
            {
                throw new ArgumentException("userTicket cannot be empty", nameof(userTicket));
            }

            string userProxyTicket;
            long? userPuid;
            long? userCid;

            using (var rpsAuthServer = new RpsAuthServer())
            using (RpsAuthResult result = await Task.Factory.StartNew(
                () =>
                        rpsAuthServer.GetAuthResult(this.SiteName, userTicket, RpsTicketType.Compact)).ConfigureAwait(false))
            {
                object proxyTicketObject = result[RpsTicketField.ProxyTicket];
                userProxyTicket = proxyTicketObject as string;

                userPuid = result.MemberId;
                userCid = result.Cid;
            }

            return new Tuple<string, long?, long?>(userProxyTicket, userPuid, userCid);
        }
    }
}
