#if INCLUDE_TEST_HOOKS

namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.TestHooks
{
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// NonWindows Device Delete Command Test Hooks.
    /// A collection of test hooks, used for helping our test automation out. These hooks are NOT compiled 
    /// for builds that go to production.
    /// </summary>
    [RoutePrefix("testhooks")]
    [ExcludeFromCodeCoverage]
    public class NonWindowsDeviceTestHooksController : ApiController
    {
        /// <summary>
        /// Initializes a new instance of the class <see cref="NonWindowsDeviceTestHooksController" />.
        /// </summary>
        public NonWindowsDeviceTestHooksController()
        {
            ProductionSafetyHelper.EnsureNotInProduction();
        }

        /// <summary>
        /// Can run EventHub Processor.
        /// </summary>
        [HttpPost]
        [Route("nonwindowsdevice/canruneventhubprocessor")]
        [IncomingRequestActionFilter("TestHooks", "CanRunEventHubProcessor", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> CanRunEventHubProcessorAsync()
        {
            await Logger.InstrumentAsync(
                new IncomingEvent(SourceLocation.Here()),
                async ev =>
                {
                    IEventHubConfig eventHubConfig = Config.Instance.Worker.Tasks.NonWindowsDeviceWorker.EventHubConfig;

                    ev["EventHubName"] = eventHubConfig.EventHubName;
                    ev["EventHubMoniker"] = eventHubConfig.Moniker;
                    ev["BlobContainerName"] = eventHubConfig.BlobContainerName;


                    IDeleteRequestsProcessor deleteRequestsProcessor = new FakeDeleteRequestsProcessor();
                    IEventHubProcessorHandler eventHubProcessorHandler = new FakeEventHubProcessorHandler();
                    IEventHubProcessor eventHubProcessor = new EventHubProcessor(eventHubConfig);

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await eventHubProcessor.RunAsync(eventHubProcessorHandler, cts.Token);
                });


            return this.Ok();
        }

        /// <summary>
        /// Inject delete request.
        /// </summary>
        [HttpPost]
        [Route("nonwindowsdevice/senddeleterequest")]
        [IncomingRequestActionFilter("TestHooks", "SendDeleteRequest", "1.0")]
        [DisallowPort80ActionFilter]
        public async Task<IHttpActionResult> SendDeleteRequestAsync()
        {
            await Logger.InstrumentAsync(
                new IncomingEvent(SourceLocation.Here()),
                async ev =>
                {
                    IEventHubConfig eventHubConfig = Config.Instance.Worker.Tasks.NonWindowsDeviceWorker.EventHubConfig;

                    ev["EventHubName"] = eventHubConfig.EventHubName;
                    ev["EventHubMoniker"] = eventHubConfig.Moniker;
                    ev["BlobContainerName"] = eventHubConfig.BlobContainerName;

                    IEventHubProcessor eventHubProcessor = new EventHubProcessor(eventHubConfig);

                    string content = await this.Request.Content.ReadAsStringAsync();

                    ev["DeviceId"] = NonWindowsDeviceDeleteHelpers.GetDeviceIdFromJsonEvent(content);
                    ev["Event"] = content;

                    string jsonEvents = "{'EventFormat': 2, 'Events': []}";

                    JObject parsedJson = JObject.Parse(jsonEvents);
                    ((JArray)parsedJson["Events"]).Add(content);

                    await eventHubProcessor.SendAsync(parsedJson.ToString());
                });

            return this.Ok();
        }
    }
}

#endif
