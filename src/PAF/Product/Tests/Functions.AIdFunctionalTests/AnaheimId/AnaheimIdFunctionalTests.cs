namespace Microsoft.PrivacyServices.AzureFunctions.AIdFunctionalTests.AnaheimId
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Newtonsoft.Json;

    /// <summary>
    /// AnaheimId Functional Tests.
    /// </summary>
    [TestClass]
    public class AnaheimIdFunctionalTests
    {
        private const string ComponentName = nameof(AnaheimIdFunctionalTests);
        private readonly ILogger logger;

        private readonly AidFctConfig aidFctConfig;
        private readonly AnaheimIdRequest anaheimIdRequest = new AnaheimIdRequest()
        {
            DeleteDeviceIdRequest = new DeleteDeviceIdRequest()
            {
                AuthorizationId = "AuthorizationId",
                CorrelationVector = "CorrelationVector",
                CreateTime = DateTimeOffset.UtcNow,
                GlobalDeviceId = 123456,
                RequestId = Guid.NewGuid(),
                TestSignal = true
            },
            AnaheimIds = new long[] { 1, 2, 3 }
        };

        // We use this preifx to identify requests sent from end to end test
        private const string CorrelationVectorPrefix = "AidEndToEnd";
        private const int NumOfExpectedDeviceDeleteCommands = 6;
        private const int NumOfExpectedEdgeBrowserCommands = 18;
        private readonly string GetCommandsUri;
        private readonly string CheckPointCommandUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnaheimIdFunctionalTests"/> class.
        /// </summary>
        public AnaheimIdFunctionalTests()
        {
            this.logger = DualLogger.Instance;
            this.aidFctConfig = AidFctConfig.Build();
            this.GetCommandsUri = $"https://{aidFctConfig.PcfApiHost}:443/pcf/v1/{AidTestHelpers.AIdAgentId}/commands";
            this.CheckPointCommandUri = $"https://{aidFctConfig.PcfApiHost}:443/pcf/v1/{AidTestHelpers.AIdAgentId}/checkpoint";
        }

        /// <summary>
        /// Insert AnaheimId into the queue and receive PCF commands back.
        /// </summary>
        [TestMethod]
        public async Task AidEventHubTestAsync()
        {
            var fullyQualifiedNamespace = $"{this.aidFctConfig.EventHubHostName}.servicebus.windows.net";
            var credential = AidTestHelpers.GetClientCredentials(this.aidFctConfig);

            // Create a producer client that you can use to send events to an event hub
            var producerClient = new EventHubProducerClient(
                fullyQualifiedNamespace: fullyQualifiedNamespace,
                eventHubName: this.aidFctConfig.EventHubName,
                credential: credential);

            // Create a batch of events
            EventDataBatch eventBatch = await producerClient.CreateBatchAsync().ConfigureAwait(false);
            string messageText = JsonConvert.SerializeObject(this.anaheimIdRequest);

            if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(messageText))))
            {
                // if it is too large for the batch
                string errorMsg = "Event is too large for the batch and cannot be sent.";
                this.logger.Error(ComponentName, errorMsg);
                throw new Exception(errorMsg);
            }

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await producerClient.SendAsync(eventBatch).ConfigureAwait(false);
            }
            finally
            {
                await producerClient.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Check Device Delete and Edge Browser signals end to end.
        /// PXS.Frontdoor -> PXS.Worker -> PXS.Mock (routing) -> PCF.Frontdoor -> agent queue
        /// </summary>
        [TestMethod]
        public async Task ValidateDeviceDeleteAndEdgeBrowserEndToEnd()
        {
            var deviceId = GlobalDeviceIdGenerator.Generate();
            var deviceIdString = $"g:{deviceId}";
            Console.WriteLine($"ValidateDeviceDeleteAndEdgeBrowserEndToEnd: GlobalDeviceId={deviceIdString}");
            var endToEndTestCV = string.Concat(CorrelationVectorPrefix, deviceId);

            // Send Vortex events to PXS
            var pxsTestClient = PxsHttpHelper.CreatePxsHttpClient(aidFctConfig);
            var pxsResponse = await AidTestHelpers.SendDeviceDeleteRequest(pxsTestClient, deviceIdString, endToEndTestCV, "p:123456").ConfigureAwait(false);
            Assert.AreEqual(HttpStatusCode.OK, pxsResponse.StatusCode);

            // Receive commands from PCF
            var factory = new PcfHttpClientFactory();
            var s2sCert = CertificateFinder.FindCertificateByName(aidFctConfig.CloudTestCertSubjectName, false);
            var httpClient = factory.CreateHttpClient(s2sCert);
            var authClient = new MicrosoftAccountAuthClient(AidTestHelpers.TestSiteId, AidTestHelpers.DummyPcfLogger, httpClient, AidTestHelpers.CreateTestEndpointConfig(aidFctConfig));
            List<PrivacyCommand> allCommands = await AidTestHelpers.PollToReceiveCommandsAsync(this.GetCommandsUri, endToEndTestCV, maxPollRetry: 10, httpClient, authClient).ConfigureAwait(false);

            // Filtering and checkpointing
            var tasks = new List<Task>();
            var deviceDeleteCommands = new List<PrivacyCommand>();
            var edgeBrowserCommands = new List<PrivacyCommand>();

            foreach (var command in allCommands)
            {
                Console.WriteLine($"All Commands: CommandId={command.CommandId},CV={command.CorrelationVector},Subject={command.Subject}");
                if (command.CorrelationVector == endToEndTestCV)
                {
                    if (command.Subject is DeviceSubject)
                    {
                        deviceDeleteCommands.Add(command);
                    }

                    if (command.Subject is EdgeBrowserSubject)
                    {
                        edgeBrowserCommands.Add(command);
                    }

                    // Checkpoint all the commands from current run
                    tasks.Add(AidTestHelpers.CheckPointCommandAsync(this.CheckPointCommandUri, command, CommandStatus.Complete, 0, httpClient, authClient));
                }
            }
            await Task.WhenAll(tasks);

            // Validate Device Delete commands
            Assert.IsTrue(deviceDeleteCommands.Any());
            Console.WriteLine($"Received {deviceDeleteCommands.Count} Device Delete commands from the current round. Should receive {NumOfExpectedDeviceDeleteCommands} commands.");
            foreach (var command in deviceDeleteCommands)
            {
                Console.WriteLine($"CommandId={command.CommandId}, CV={command.CorrelationVector}, Subject={command.Subject}");
            }
            Assert.AreEqual(NumOfExpectedDeviceDeleteCommands, deviceDeleteCommands.Count);

            // Validate EdgeBrowser commands
            Assert.IsTrue(edgeBrowserCommands.Any());
            Console.WriteLine($"Received {edgeBrowserCommands.Count} Edge Browser commands from the current round. Should receive {NumOfExpectedEdgeBrowserCommands} commands.");
            foreach (var command in edgeBrowserCommands)
            {
                Console.WriteLine($"CommandId={command.CommandId}, CV={command.CorrelationVector}, Subject={command.Subject}");
            }
            Assert.AreEqual(NumOfExpectedEdgeBrowserCommands, edgeBrowserCommands.Count);
        }
    }
}
