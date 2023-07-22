// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Test.Common
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.WindowsLive.Test.WinLiveUser.AuthInterface;

    public static class UserPartnerTicketProvider
    {
        public static async Task<UserProxyTicketResult> GetTicket(string userName, string password, RpsEnvironment rpsEnvironment, string siteName, string siteUri)
        {
            return await Task.Run(() =>
            {
                string errorMessage = null;
                string compactRpsTicket = null;

                LiveIdEnvironment environment = rpsEnvironment == RpsEnvironment.Int ? LiveIdEnvironment.INT : LiveIdEnvironment.Production;

                // Retrieve the RPS ticket (user ticket)
                try
                {
                    AuthInterfaceSettings authSettings = new AuthInterfaceSettings
                    {
                        LiveIdEnv = environment,
                        SpecialLogin = new SpecialLogin(siteUri, siteName, "MBI_SSL"),
                    };

                    AuthInterface authInterface = new AuthInterface(authSettings);
                    compactRpsTicket = authInterface.GetCompactTicket(
                        userName, password, siteUri, AuthenticationMode.Web);
                    if (string.IsNullOrWhiteSpace(compactRpsTicket))
                    {
                        errorMessage = "Null or empty RPS ticket received";
                    }
                }
                catch (Exception exception)
                {
                    errorMessage = string.Format(
                        CultureInfo.InvariantCulture,
                        "Exception thrown while retrieving RPS ticket: {0}", exception);
                }

                return new UserProxyTicketResult
                {
                    ErrorMessage = errorMessage,
                    Ticket = "WLID1.0=t=" + compactRpsTicket
                };
            });
        }
    }
}
