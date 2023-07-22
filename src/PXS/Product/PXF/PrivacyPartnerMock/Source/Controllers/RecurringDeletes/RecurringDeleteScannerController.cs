namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.RecurringDeletes
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Azure.ComplianceServices.Common.DistributedLocking;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleScanner;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleWorker;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Moq;
    using Newtonsoft.Json;

    public class RecurringDeleteSannerController : RecurringDeleteBaseController
    {
        private readonly ILogger logger;

        public RecurringDeleteSannerController(
            ILogger logger,
            IPrivacyConfigurationManager configurationManager,
            IScheduleDbClient scheduleDbClient)
            : base (logger, configurationManager, scheduleDbClient)
        {
            this.logger = logger;
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("recurringdelete/scannerworkere2e")]
        public async Task<HttpResponseMessage> ScannerWorkerE2ESuccess()
        {
            this.InitializeLockPrimitive();
            await this.CleanUpCloudQueue().ConfigureAwait(false);

            // Construct schedule db doc
            var scheduleDbDocArgs = JsonConvert.DeserializeObject<RecurrentDeleteScheduleDbDocument>(Request.Content.ReadAsStringAsync().Result);

            // add a clean up step for more stability
            this.DeleteRecurringDeletesByPuidAsync(scheduleDbDocArgs.Puid).Wait();

            var scheduleDbDoc = await this.CreateOrUpdateRecurringDeletesScheduleDbAsync(scheduleDbDocArgs).ConfigureAwait(false);
            if (scheduleDbDoc == null)
            {
                return this.Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, "Creating testing record in schedule db failed.");
            }
            this.logger.Information(nameof(RecurringDeleteSannerController), $"Created a schedule db doc for testing: {JsonConvert.SerializeObject(scheduleDbDoc)}");

            // Create distributed lock
            DistributedLock<RecurrentDeleteScheduleScanner.LockState> distributedLock = new DistributedLock<RecurrentDeleteScheduleScanner.LockState>("recurringdeletetestlock", lockPrimitives);
            RecurrentDeleteScheduleScanner.LockState lockState = new RecurrentDeleteScheduleScanner.LockState
            {
                LockAcquiredTime = DateTimeOffset.MinValue,
                MinLeaseTime = TimeSpan.FromSeconds(3),
                TaskRunFrequency = TimeSpan.FromSeconds(1),
                MaxExtensionTtl = TimeSpan.FromSeconds(6),
                ExtensionThreshold = TimeSpan.FromSeconds(1),
                NextStartTime = DateTimeOffset.MinValue,
            };

            // mock RecurrentDeleteScheduleWorker dependencies
            var mockPcfProxyService = new Mock<IPcfProxyService>();
            mockPcfProxyService.Setup(x => x.PostMsaRecurringDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<DeleteRequest>(), It.IsAny<string>())).Returns(Task.FromResult(new ServiceResponse()));
            var mockPxfDispatcher = new Mock<IPxfDispatcher>();
            var deleteResponse = new DeleteResourceResponse
            {
                Status = ResourceStatus.Deleted
            };
            List<DeleteResourceResponse> deleteResourceResponses = new List<DeleteResourceResponse>() { deleteResponse };
            mockPxfDispatcher.Setup(x => x.CreateDeletePolicyDataTypeTask(It.IsAny<string>(), It.IsAny<IPxfRequestContext>())).Returns(Task.FromResult(new DeletionResponse<DeleteResourceResponse>(deleteResourceResponses)));


            var scanner = new RecurrentDeleteScheduleScanner(
                distributedLock: distributedLock,
                lockPrimitives: this.lockPrimitives,
                lockState: lockState,
                cancellationToken: new CancellationTokenSource().Token,
                cloudQueue: this.cloudQueue,
                appConfiguration: this.appConfiguration,
                configuration: Program.PartnerMockConfigurations,
                scheduleDbClient: this.scheduleDbClient,
                logger: this.logger);
            var worker = new RecurrentDeleteScheduleWorker(
                this.cloudQueue,
                this.recurringDeleteWorkerConfiguration.CloudQueueConfig,
                this.recurringDeleteWorkerConfiguration.ScheduleDbConfig,
                this.appConfiguration,
                this.scheduleDbClient,
                mockPcfProxyService.Object,
                mockPxfDispatcher.Object,
                this.logger);


            int[] queueDepths = new int[3];
            int queueSizeBeforeScan = await this.cloudQueue.GetQueueSizeAsync();
            queueDepths[0] = queueSizeBeforeScan;

            try
            {
                // start scanner's job
                Task scannerTask = scanner.DoWorkAsync();
                scannerTask.Wait();

                // record the queue size after scanner's job
                int queueSizeAfterScan = await this.cloudQueue.GetQueueSizeAsync();
                queueDepths[1] = queueSizeAfterScan;

                // start worker's job
                Task workerTask = worker.DoWorkAsync();
                workerTask.Wait();

                // record the queue size after worker's job
                int queueSizeAtLast = await this.cloudQueue.GetQueueSizeAsync();
                queueDepths[2] = queueSizeAtLast;
                return this.Request.CreateResponse(HttpStatusCode.OK, queueDepths);
            }
            catch (Exception e)
            {
                return this.Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
            finally
            {
                await this.DeleteRecurringDeletesScheduleDbAsync(scheduleDbDoc).ConfigureAwait(false);
            }
        }
    }
}
