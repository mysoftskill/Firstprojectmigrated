namespace PCF.FunctionalTests
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;
    using Xunit;
    using Xunit.Abstractions;

    using PXSV1 = Microsoft.PrivacyServices.PXS.Command.Contracts.V1;

#if INCLUDE_TEST_HOOKS

    [Trait("Category", "FCT")]
    public class BackgroundTaskTests
    {
        private readonly ITestOutputHelper outputHelper;

        public BackgroundTaskTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        /// <summary>
        /// Ensures that the export cleanup job works.
        /// </summary>
        [Fact]
        public async Task ExportCleanup()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/backgroundtasks/exportstoragecleanup"),
                this.outputHelper);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Ensures that the AAD daily force complete job works.
        /// </summary>
        [Fact]
        public async Task OldExportForceCompleteAad()
        {
            PXSV1.ExportRequest exportRequest = new PXSV1.ExportRequest
            {
                Context = Guid.NewGuid().ToString(),
                IsWatchdogRequest = false,
                IsSyntheticRequest = false,
                PrivacyDataTypes = new[] { "BrowsingHistory", "ProductAndServiceUsage" },
                Requester = Guid.NewGuid().ToString(),
                RequestType = PXSV1.RequestType.Export,
                StorageUri = new Uri("https://a/uri/to/nowhere"),
                ProcessorApplicable = true,
                ControllerApplicable = true,
                RequestGuid = Guid.NewGuid(),
                RequestId = Guid.NewGuid(),
                Subject = new AadSubject
                {
                    ObjectId = Guid.NewGuid(),
                    OrgIdPUID = 123456,
                    TenantId = Guid.NewGuid(),
                },
                Timestamp = DateTimeOffset.UtcNow,
            };

            await OldExportForceComplete(exportRequest);
        }

        /// <summary>
        /// Ensures that the MSA daily force complete job works.
        /// </summary>
        [Fact]
        public async Task OldExportForceCompleteMsa()
        {
            PXSV1.ExportRequest exportRequest = new PXSV1.ExportRequest
            {
                Context = Guid.NewGuid().ToString(),
                IsWatchdogRequest = false,
                IsSyntheticRequest = false,
                PrivacyDataTypes = new[] { "BrowsingHistory", "ProductAndServiceUsage" },
                Requester = Guid.NewGuid().ToString(),
                RequestType = PXSV1.RequestType.Export,
                StorageUri = new Uri("https://a/uri/to/nowhere"),
                ProcessorApplicable = true,
                ControllerApplicable = true,
                RequestGuid = Guid.NewGuid(),
                RequestId = Guid.NewGuid(),
                Subject = new MsaSubject
                {
                    Anid = "anid",
                    Cid = 123,
                    Opid = "opid",
                    Puid = 456,
                },
                Timestamp = DateTimeOffset.UtcNow,
            };

            await OldExportForceComplete(exportRequest);
        }
      
        /// <summary>
        /// Ensures that the daily force complete job works.
        /// </summary>
        internal async Task OldExportForceComplete(PXSV1.ExportRequest exportRequest)
        {
            await TestSettings.InsertPxsCommandAsync(new[] { exportRequest }, this.outputHelper);

            // Wait for the record to be available.
            CommandStatusResponse commandStatus = null;
            Stopwatch sw = Stopwatch.StartNew();
            while (commandStatus?.AssetGroupStatuses.Any(x => x.IngestionTime != null) != true && sw.Elapsed <= TimeSpan.FromSeconds(30))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                commandStatus = await TestSettings.GetCommandStatusAsync(exportRequest.RequestId, this.outputHelper);
            }

            Assert.False(commandStatus?.IsGloballyComplete);
            
            // Wait for the record to appear completed.
            commandStatus = null;
            while (commandStatus?.IsGloballyComplete != true && sw.Elapsed <= TimeSpan.FromSeconds(60))
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                var response = await TestSettings.SendWithS2SAync(
                    new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/backgroundtasks/oldexportforcecomplete/{exportRequest.RequestId:n}"),
                    this.outputHelper);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                commandStatus = await TestSettings.GetCommandStatusAsync(exportRequest.RequestId, this.outputHelper);
            }

            Assert.True(commandStatus?.IsGloballyComplete);
        }

        /// <summary>
        /// Ensures that we can check agent azure storage queue depth.
        /// </summary>
        [Fact]
        public async Task CheckAgentAzureStorageQueueDepth()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/backgroundtasks/checkagentazurestoragequeuedepth"),
                this.outputHelper,
                TimeSpan.FromMinutes(5));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// Ensures that we can read PDMS data into docdb.
        /// </summary>
        [Fact]
        public async Task RefreshPdmsData()
        {
            var response = await TestSettings.SendWithS2SAync(
                new HttpRequestMessage(HttpMethod.Get, $"https://{TestSettings.ApiHostName}/testhooks/backgroundtasks/refreshpdmsdata"),
                this.outputHelper,
                TimeSpan.FromMinutes(5));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

#endif
}
