// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.RecurringDeletes
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.MsaIdentityService;
    using Microsoft.Membership.MemberServices.Test.Common;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;
    using Moq;

    public class MsaIdentityServiceAdapterTestController : ApiController
    {
        private MsaIdentityServiceAdapter adapter = null;
        private IPxfRequestContext requestContext = null;
        private IValidationService validationService = null;
        private ILogger Logger { get; } = IfxTraceLogger.Instance;

        public MsaIdentityServiceAdapterTestController()
        {
            adapter = CreateMsaIdentityServiceAdapter();
            validationService = new ValidationService(PcvEnvironment.Preproduction);
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("msaRvs/GetGdprUserDeleteVerifierWithRefreshClaim")]
        public async Task<AdapterResponse<string>> GetGdprUserDeleteVerifierWithRefreshClaimAsync()
        {
            string content = Request.Content.ReadAsStringAsync().Result;
            var kvps = GetRequestContent(content);
            kvps.TryGetValue("userProxyTicket", out string userProxyTicket);
            kvps.TryGetValue("userPuid", out string userPuid);
            requestContext = CreatePxfRequestContext(userProxyTicket, userPuid, null);

            return await adapter.GetGdprUserDeleteVerifierWithRefreshClaimAsync(requestContext).ConfigureAwait(false);
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("msaRvs/GetGdprUserDeleteVerifierUsingPreVerifier")]
        public async Task<AdapterResponse<string>> GetGdprUserDeleteVerifierUsingPreVerifierAsync()
        {
            string content = Request.Content.ReadAsStringAsync().Result;
            var kvps = GetRequestContent(content);
            requestContext = CreatePxfRequestContext(null, null, null);
            kvps.TryGetValue("preVerifier", out string preVerifier);

            return await adapter.RenewGdprUserDeleteVerifierUsingPreverifierAsync(requestContext, preVerifier).ConfigureAwait(false);
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("msaRvs/GetGdprUserDeleteVerifierForCommand")]
        public async Task<AdapterResponse<string>> GetGdprUserDeleteVerifierForCommandsAsync()
        {
            string content = Request.Content.ReadAsStringAsync().Result;
            var kvps = GetRequestContent(content);
            requestContext = CreatePxfRequestContext(null, null, null);
            kvps.TryGetValue("xuid", out string xuid);
            kvps.TryGetValue("preVerifier", out string preVerifier);
            kvps.TryGetValue("commandId", out string commandId);
            kvps.TryGetValue("predicate", out string predicate);
            kvps.TryGetValue("datatype", out string datatype);

            return await adapter.GetGdprUserDeleteVerifierWithPreverifierAsync(
                Guid.Parse(commandId),
                requestContext,
                preVerifier,
                xuid,
                predicate,
                datatype).ConfigureAwait(false);
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("msaRvs/VerifyUserDeleteVerifier")]
        public async Task<HttpResponseMessage> VerifyUserDeleteVerifierAsync()
        {
            string content = Request.Content.ReadAsStringAsync().Result;
            var kvps = GetRequestContent(content);

            long value;
            if (!long.TryParse(kvps["userPuid"], out value))
            {
                value = 0;
            }

            kvps.TryGetValue("xuid", out string xuid);
            kvps.TryGetValue("preVerifier", out string preVerifier);
            kvps.TryGetValue("commandId", out string commandId);
            kvps.TryGetValue("datatype", out string datatypeStr);

            var msaSubject = new MsaSubject
            {
                Anid = null,
                Cid = 0,
                Opid = null,
                Puid = value,
                Xuid = xuid
            };

            try
            {
                DataTypeId dataType = null;
                Policies.Current?.DataTypes.TryCreateId(datatypeStr, out dataType);

                await validationService.EnsureValidAsync(
                    preVerifier,
                    new CommandClaims
                    {
                        CommandId = commandId,
                        Subject = msaSubject,
                        Operation = ValidOperation.Delete,
                        CloudInstance = "Public",
                        ControllerApplicable = true,
                        ProcessorApplicable = false,
                        DataType = dataType
                    },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, ex.Message);
            }

            return Request.CreateResponse(HttpStatusCode.OK, "Valid verifier token");
        }

        private PxfRequestContext CreatePxfRequestContext(
            string userProxyTicket,
            string puid,
            string familyJsonWebToken = null)
        {
            long value;
            if (!long.TryParse(puid, out value))
            {
                value = 0;
            }

            var requestContext = new PxfRequestContext(
                   userProxyTicket,
                   familyJsonWebToken,
                   value,
                   value,
                   null,
                   null,
                   false,
                   null);

            return requestContext;
        }

        private Dictionary<string, string> GetRequestContent(string content)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(content))
            {
                return result;
            }

            try
            {
                var kvps = content.Split('&');
                if (kvps == null)
                {
                    return result;
                }

                foreach (string str in kvps)
                {
                    var kvp = str.Split('=');
                    result.Add(kvp[0], kvp[1]);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(MsaIdentityServiceAdapterTestController), ex, "An exception occurred while trying to parse request content.");
                throw;
            }

            return result;
        }

        private MsaIdentityServiceAdapter CreateMsaIdentityServiceAdapter()
        {
            var certProvider = new CertificateProvider(Logger);
            string certSubject = "pdos.aadclient.pxs.privacy.microsoft-int.com";
            X509Certificate2 certInfo = null;

            try
            {
                certInfo = certProvider.GetClientCertificate(certSubject, StoreLocation.LocalMachine);
            }
            catch (Exception ex)
            {
                Logger.Error(nameof(MsaIdentityServiceAdapterTestController), ex, "An exception occurred while trying to get PDOS cert.");
                throw;
            }

            var clientCertificateConfig = new Mock<ICertificateConfiguration>(MockBehavior.Strict);
            clientCertificateConfig.SetupGet(c => c.Subject).Returns(certInfo.Subject);
            clientCertificateConfig.SetupGet(c => c.Issuer).Returns(certInfo.Issuer);
            clientCertificateConfig.SetupGet(c => c.Thumbprint).Returns(certInfo.Thumbprint);
            clientCertificateConfig.SetupGet(c => c.CheckValidity).Returns(false);

            var msaIdentityServiceConfig = new Mock<IMsaIdentityServiceConfiguration>(MockBehavior.Strict);
            msaIdentityServiceConfig.SetupGet(c => c.Endpoint).Returns("https://login.live-int.com/pksecure/oauth20_clientcredentials.srf");
            msaIdentityServiceConfig.SetupGet(c => c.ClientId).Returns("295218");
            msaIdentityServiceConfig.SetupGet(c => c.Policy).Returns("S2S_24HOURS_MUTUALSSL");
            msaIdentityServiceConfig.SetupGet(c => c.CertificateConfiguration).Returns(clientCertificateConfig.Object);

            var mockServicePointConfiguration = new Mock<IServicePointConfiguration>(MockBehavior.Strict);
            mockServicePointConfiguration.Setup(c => c.ConnectionLimit).Returns(42);
            mockServicePointConfiguration.Setup(c => c.ConnectionLeaseTimeout).Returns(39);
            mockServicePointConfiguration.Setup(c => c.MaxIdleTime).Returns(98);

            var partnerConfig = new Mock<IMsaIdentityServiceAdapterConfiguration>(MockBehavior.Strict);
            partnerConfig.SetupGet(c => c.BaseUrl).Returns("https://api.login.live-int.com");
            partnerConfig.SetupGet(c => c.CounterCategoryName).Returns("MSA");
            partnerConfig.SetupGet(c => c.PartnerId).Returns("MSA");
            partnerConfig.SetupGet(c => c.RetryStrategyConfiguration).Returns(TestMockFactory.CreateFixedIntervalRetryStrategyConfiguration().Object);
            partnerConfig.SetupGet(c => c.TimeoutInMilliseconds).Returns(10000);
            partnerConfig.SetupGet(c => c.ServicePointConfiguration).Returns(mockServicePointConfiguration.Object);
            partnerConfig.SetupGet(c => c.IgnoreErrors).Returns(false);
            partnerConfig.SetupGet(c => c.EnableAdapter).Returns(true);

            var configurationManager = new Mock<IPrivacyConfigurationManager>(MockBehavior.Strict);
            configurationManager.SetupGet(c => c.MsaIdentityServiceConfiguration).Returns(msaIdentityServiceConfig.Object);
            configurationManager.SetupGet(c => c.AdaptersConfiguration.MsaIdentityServiceAdapterConfiguration).Returns(partnerConfig.Object);

            var counterFactory = new Mock<ICounterFactory>(MockBehavior.Strict);
            counterFactory.Setup(c => c.GetCounter(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CounterType>())).Returns(new Mock<ICounter>(MockBehavior.Loose).Object);

            var msaIdentityServiceClientFactory = new MsaIdentityServiceClientFactory();
            return new MsaIdentityServiceAdapter(configurationManager.Object, certProvider, Logger, counterFactory.Object, msaIdentityServiceClientFactory);
        }
    }
}
