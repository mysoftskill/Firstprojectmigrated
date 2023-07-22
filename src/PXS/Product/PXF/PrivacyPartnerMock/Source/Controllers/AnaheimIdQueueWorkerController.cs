// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers
{
    using global::Azure.Core;
    using global::Azure.Identity;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.ComplianceServices.Common.Queues;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.PcfAdapter;
    using Microsoft.Membership.MemberServices.Privacy.VortexDeviceDeleteWorker.QueueProcessor;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    /// <inheritdoc />
    /// <summary>
    ///     AnaheimId controller
    /// </summary>
    public class AnaheimIdQueueWorkerController : ApiController
    {
        private const string anaheimidQueueName = "anaheimidtesting";

        private readonly ILogger logger;

        private CloudQueue<AnaheimIdRequest> cloudQueue;

        private IPrivacyConfigurationManager configurationManager;
        private IAppConfiguration appConfiguration;

        private readonly IPcfAdapter pcfAdapter;

        public AnaheimIdQueueWorkerController(
            ILogger logger,
            IPrivacyConfigurationManager configurationManager,
            IPcfAdapter pcfAdapter)
        {
            this.logger = logger;
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager));
            this.appConfiguration = new AppConfiguration(@"local.settings.json");
            this.pcfAdapter = pcfAdapter;
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("anaheimid/processpcferror")]
        public async Task<HttpResponseMessage> ProcessAnaheimIdrequestFailedSendToPcf()
        {
            try
            {
                await this.CleanUpCloudQueue().ConfigureAwait(false);

                var mockPcfAdapter = new Mock<IPcfAdapter>();
                mockPcfAdapter.Setup(m => m.PostCommandsAsync(It.IsAny<IList<PrivacyRequest>>())).ReturnsAsync(new AdapterResponse
                {
                    Error = new AdapterError(
                            AdapterErrorCode.Unknown,
                           "Something bad happened",
                            500)
                });

                var anaheimIdRequest = new AnaheimIdRequest
                {
                    AnaheimIds = new long[] { 1, 2, 3 },
                    DeleteDeviceIdRequest = new DeleteDeviceIdRequest
                    {
                        AuthorizationId = "AuthorizationId",
                        CorrelationVector = "CorrelationVector",
                        GlobalDeviceId = 123456,
                        RequestId = Guid.NewGuid(),
                        CreateTime = DateTimeOffset.UtcNow,
                        TestSignal = true
                    }
                };


                this.cloudQueue.EnqueueAsync(anaheimIdRequest).Wait();

                var worker = new AnaheimIdQueueWorker(
                    this.cloudQueue,
                    mockPcfAdapter.Object,
                    this.configurationManager.VortexDeviceDeleteWorkerConfiguration.QueueProccessorConfig,
                    this.appConfiguration,
                    this.logger);

                // Act
                await worker.DoWorkAsync().ConfigureAwait(false);

                // Assert
                var tryToGetMessageWithNoWait = this.cloudQueue.DequeueAsync().Result;

                Assert.IsNull(tryToGetMessageWithNoWait, "Should be null since visbility timeout hasn't expired yet.");

                // PXS.AnaheimIdQueueWorkerMinVisibilityTimeoutInSeconds and PXS.AnaheimIdQueueWorkerMaxVisibilityTimeoutInSeconds
                // are both set to 4 seconds in local.settings.json for simplicity
                Task.Delay(TimeSpan.FromSeconds(4)).Wait();

                var tryToGetMessageAfterWait = this.cloudQueue.DequeueAsync().Result;
                Assert.IsNotNull(tryToGetMessageAfterWait, "Should get the message since visbility timeout should be expired now.");

                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(AnaheimIdQueueWorkerController), $"error: {e.Message}");
                return this.Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, e);
            }
            finally
            {
                await this.CleanUpCloudQueue().ConfigureAwait(false);
            }
        }

        [HttpGet]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("anaheimid/processthrottledmsg")]
        public async Task<HttpResponseMessage> ProcessThrottledAnaheimIdrequest()
        {
            try
            {
                await this.CleanUpCloudQueue().ConfigureAwait(false);

                // Mock a throttling case
                var mockAppConfiguration = new Mock<IAppConfiguration>();
                mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdQueueWorker_Enabled, It.IsAny<bool>())).ReturnsAsync(true);
                mockAppConfiguration.Setup(c => c.IsFeatureFlagEnabledAsync(FeatureNames.PXS.AnaheimIdRequestToPcfEnabled, It.IsAny<bool>())).ReturnsAsync(false);
                // Throttling VisibilityTimeout
                mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdThrottledRequestsMaxVisibilityTimeoutInMinutes)).Returns(2);
                // Retry VisibilityTimeout
                mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdQueueWorkerMinVisibilityTimeoutInSeconds)).Returns(5);
                mockAppConfiguration.Setup(c => c.GetConfigValue<int>(ConfigNames.PXS.AnaheimIdQueueWorkerMaxVisibilityTimeoutInSeconds)).Returns(5);

                var anaheimIdRequest = new AnaheimIdRequest
                {
                    AnaheimIds = new long[] { 1, 2, 3 },
                    DeleteDeviceIdRequest = new DeleteDeviceIdRequest
                    {
                        AuthorizationId = "AuthorizationId",
                        CorrelationVector = "CorrelationVector",
                        GlobalDeviceId = 123456,
                        RequestId = Guid.NewGuid(),
                        CreateTime = DateTimeOffset.UtcNow,
                        TestSignal = true
                    }
                };

                this.cloudQueue.EnqueueAsync(anaheimIdRequest).Wait();

                var worker = new AnaheimIdQueueWorker(
                    this.cloudQueue,
                    this.pcfAdapter,
                    this.configurationManager.VortexDeviceDeleteWorkerConfiguration.QueueProccessorConfig,
                    mockAppConfiguration.Object,
                    this.logger);

                // Act
                await worker.DoWorkAsync().ConfigureAwait(false);

                // Assert
                // Wait for 2 seconds
                Task.Delay(TimeSpan.FromSeconds(2)).Wait();

                var tryToGetMessageAfterTwoSeconds = this.cloudQueue.DequeueAsync().Result;
                Assert.IsNotNull(tryToGetMessageAfterTwoSeconds, "Don't need to wait for 5 seconds because the RetryVisibilityTimeout should be overwritten by Throttling VisibilityTimeout");

                return this.Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(AnaheimIdQueueWorkerController), $"error: {e.Message}");
                return this.Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, e);
            }
            finally
            {
                await this.CleanUpCloudQueue().ConfigureAwait(false);
            }
        }

        private async Task CleanUpCloudQueue()
        {
            InitiaizeCloudQueue();

            while (await this.cloudQueue.GetQueueSizeAsync() > 0)
            {
                var messages = await this.cloudQueue.DequeueBatchAsync(
                     visibilityTimeout: TimeSpan.FromSeconds(1),
                     maxCount: 32,
                     cancellationToken: CancellationToken.None);

                if (messages.Any())
                {
                    foreach (var message in messages)
                    {
                        await message.DeleteAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        private void InitiaizeCloudQueue()
        {
            if (Program.PartnerMockConfigurations.EnvironmentConfiguration.EnvironmentType == MemberServices.Configuration.EnvironmentType.OneBox)
            {
                this.cloudQueue = new CloudQueue<AnaheimIdRequest>(anaheimidQueueName);
            }
            else
            {
                TokenCredential credential = new DefaultAzureCredential();
                this.cloudQueue = new CloudQueue<AnaheimIdRequest>(
                    Program.PartnerMockConfigurations.PartnerMockConfiguration.AnaheimIdQueueWorkerStorageConfiguration.AccountName,
                    anaheimidQueueName,
                    credential);
            }
            this.cloudQueue.CreateIfNotExistsAsync().Wait();
        }

    }

}
