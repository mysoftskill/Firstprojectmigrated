#if INCLUDE_TEST_HOOKS

namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.TestHooks
{
    using Microsoft.Azure.ComplianceServices.NonWindowsDeviceDeleteWorker;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal class FakeDeleteRequestsProcessor : IDeleteRequestsProcessor
    {
        public Dictionary<string, string> DeleteEvents { get; set; } = new Dictionary<string, string>();
        public bool PublishDeleteRequestsExecuted = false;

        public void ProcessDeleteRequestsFromJson(string partitionId, string jsonDeleteRequest)
        {
            string platformDeviceId = NonWindowsDeviceDeleteHelpers.GetDeviceIdFromJsonEvent(jsonDeleteRequest);
            this.DeleteEvents[platformDeviceId] = jsonDeleteRequest;
            return;
        }

        public Task PublishDeleteRequests(string partitionId)
        {
            this.PublishDeleteRequestsExecuted = true;
            return Task.CompletedTask;
        }
    }
}

#endif
