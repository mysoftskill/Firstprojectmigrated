namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.RecurringDeletes
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Privacy.RecurrentDeleteWorker.ScheduleWorker;
    using Microsoft.Membership.MemberServices.Privacy.Core.PCF;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Dispatcher;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.Contracts.V1;
    using Moq;
    using Newtonsoft.Json;

    public class RecurringDeleteWorkerController : RecurringDeleteBaseController
    {
        private readonly ILogger logger;

        private readonly IPxfDispatcher pxfDispatcher;

        private readonly IPcfProxyService pcfProxyService;

        public RecurringDeleteWorkerController(
        ILogger logger,
        IPrivacyConfigurationManager configurationManager,
        IScheduleDbClient scheduleDbClient,
        IPxfDispatcher pxfDispatcher,
        IPcfProxyService pcfProxyService) :
            base(logger, configurationManager, scheduleDbClient)
        {
            this.logger = logger;
            this.pxfDispatcher = pxfDispatcher ?? throw new ArgumentNullException(nameof(pxfDispatcher));
            this.pcfProxyService = pcfProxyService ?? throw new ArgumentNullException(nameof(pcfProxyService));
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("recurringdelete/processscheduledbdoc")]
        public async Task<HttpResponseMessage> ProcessScheduleDbDocSuccess()
        {
            this.logger.Information(nameof(RecurringDeleteWorkerController), "ProcessScheduleDbDocSuccess...");
            await this.CleanUpCloudQueue().ConfigureAwait(false);

            var scheduleDbDocArgs = JsonConvert.DeserializeObject<RecurrentDeleteScheduleDbDocument>(Request.Content.ReadAsStringAsync().Result);

            // add a clean up step for more stability
            this.DeleteRecurringDeletesByPuidAsync(scheduleDbDocArgs.Puid).Wait();

            var scheduleDbDoc = await this.CreateOrUpdateRecurringDeletesScheduleDbAsync(scheduleDbDocArgs).ConfigureAwait(false);
            if (scheduleDbDoc == null)
            {
                return this.Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, "Creating testing record in schedule db failed.");
            }
            this.logger.Information(nameof(RecurringDeleteWorkerController), $"Created a schedule db doc for testing: {JsonConvert.SerializeObject(scheduleDbDoc)}");

            try
            {
                var mockOutgoingApi = new Mock<OutgoingApiEventWrapper>();
                var worker = new RecurrentDeleteScheduleWorker(
                    this.cloudQueue,
                    this.recurringDeleteWorkerConfiguration.CloudQueueConfig,
                    this.recurringDeleteWorkerConfiguration.ScheduleDbConfig,
                    this.appConfiguration,
                    this.scheduleDbClient,
                    this.pcfProxyService,
                    this.pxfDispatcher,
                    this.logger);

                await worker.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(scheduleDbDoc, mockOutgoingApi.Object);
                this.logger.Information(nameof(RecurringDeleteWorkerController), "After call ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync..");

                var lattestScheduleDbDoc = await this.GetRecurringDeletesScheduleDbDocumentAsync(scheduleDbDoc).ConfigureAwait(false);

                return this.Request.CreateResponse<RecurrentDeleteScheduleDbDocument>(HttpStatusCode.OK, lattestScheduleDbDoc);
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(RecurringDeleteWorkerController), $"error: {e.Message}");
                return this.Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, e);
            }
            finally
            {
                await this.DeleteRecurringDeletesScheduleDbAsync(scheduleDbDoc).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [OverrideAuthentication]
        [AllowAnonymous]
        [Route("recurringdelete/processdocpcferror")]
        public async Task<HttpResponseMessage> ProcessScheduleDbDocCallPcfError()
        {
            this.logger.Information(nameof(RecurringDeleteWorkerController), "ProcessScheduleDbDocErrorAndRetry...");
            await this.CleanUpCloudQueue().ConfigureAwait(false);

            var scheduleDbDocArgs = JsonConvert.DeserializeObject<RecurrentDeleteScheduleDbDocument>(Request.Content.ReadAsStringAsync().Result);
            // add a clean up step for more stability
            this.DeleteRecurringDeletesByPuidAsync(scheduleDbDocArgs.Puid).Wait();

            var scheduleDbDoc = await this.CreateOrUpdateRecurringDeletesScheduleDbAsync(scheduleDbDocArgs).ConfigureAwait(false);
            if (scheduleDbDoc == null)
            {
                return this.Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, "Creating testing record in schedule db failed.");
            }
            this.logger.Information(nameof(RecurringDeleteWorkerController), $"Created a schedule db doc for testing: {JsonConvert.SerializeObject(scheduleDbDoc)}");

            var errorServiceResponse = new ServiceResponse();
            errorServiceResponse.Error = new Error
            {
                Code = (ErrorCode.PartnerError).ToString(),
                Message = "An error occured"
            };
            var mockPcfProxyService = new Mock<IPcfProxyService>();
            mockPcfProxyService.Setup(x => x.PostMsaRecurringDeleteRequestsAsync(It.IsAny<IRequestContext>(), It.IsAny<DeleteRequest>(), It.IsAny<string>())).Returns(Task.FromResult(errorServiceResponse));

            try
            {
                var mockOutgoingApi = new Mock<OutgoingApiEventWrapper>();
                var worker = new RecurrentDeleteScheduleWorker(
                    this.cloudQueue,
                    this.recurringDeleteWorkerConfiguration.CloudQueueConfig,
                    this.recurringDeleteWorkerConfiguration.ScheduleDbConfig,
                    this.appConfiguration,
                    this.scheduleDbClient,
                    mockPcfProxyService.Object,
                    this.pxfDispatcher,
                    this.logger);

                await worker.ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync(scheduleDbDoc, mockOutgoingApi.Object);
                this.logger.Information(nameof(RecurringDeleteWorkerController), "After call ProcessRecurrentDeleteScheduleDbDocumentInstrumentedAsync..");
                return this.Request.CreateResponse(HttpStatusCode.ExpectationFailed);
            }
            catch (InvalidOperationException)
            {
                // expect to receive this exception
                var lattestScheduleDbDoc = await this.GetRecurringDeletesScheduleDbDocumentAsync(scheduleDbDoc).ConfigureAwait(false);
                return this.Request.CreateResponse<RecurrentDeleteScheduleDbDocument>(HttpStatusCode.OK, lattestScheduleDbDoc);
            }
            catch (Exception e)
            {
                this.logger.Error(nameof(RecurringDeleteWorkerController), $"error: {e.Message}");
                return this.Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, e);
            }
            finally
            {
                await this.DeleteRecurringDeletesScheduleDbAsync(scheduleDbDoc).ConfigureAwait(false);
            }
        }
    }
}
