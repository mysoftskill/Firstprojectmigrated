// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication
{
    using System;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Microsoft.Identity.Client;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     An instrumented instance of <see cref="AadTokenManager" /> that writes events to SLL outgoing events.
    /// </summary>
    public class InstrumentedAadTokenManager : ITokenManager
    {
        private readonly ITokenManager tokenManager;

        /// <summary>
        ///     Creates a new instance of <see cref="InstrumentedAadTokenManager" />
        /// </summary>
        public InstrumentedAadTokenManager(ITokenManager aadTokenManager)
        {
            this.tokenManager = aadTokenManager;
        }

        public async Task<string> GetAppTokenAsync(string authority, string clientId, string resource, X509Certificate2 certificate, bool cacheable = true, ILogger logger = null)
        {
            OutgoingApiEventWrapper apiEvent = OutgoingApiEventWrapper.CreateBasicOutgoingEvent(
                partnerId: nameof(AadTokenManager),
                operationName: nameof(this.GetAppTokenAsync),
                operationVersion: string.Empty,
                targetUri: authority,
                requestMethod: HttpMethod.Post,
                dependencyType: "AzureAD");

            try
            {
                apiEvent.Start();

                return await this.tokenManager.GetAppTokenAsync(authority, clientId, resource, certificate, cacheable, logger).ConfigureAwait(false);
            }
            catch (MsalException e)
            {
                apiEvent.ProtocolStatusCode = e.ErrorCode.ToString();
                apiEvent.Success = false;
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            catch (Exception e)
            {
                apiEvent.ProtocolStatusCode = "500";
                apiEvent.Success = false;
                apiEvent.ErrorMessage = e.ToString();
                throw;
            }
            finally
            {
                apiEvent?.Finish();
            }
        }
    }
}
