namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.BackgroundTasks.DeleteExportArchive;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.PXS.Command.CommandStatus;

    internal class PxsDeleteExportArchiveActionResult : BaseHttpActionResult
    {
        private readonly IAzureWorkItemQueuePublisher<DeleteFullExportArchiveWorkItem> publisher;
        private readonly AuthenticationScope authenticationScope;
        private readonly IAuthorizer authorizer;
        private readonly HttpRequestMessage requestMessage;
        private readonly DeleteExportArchiveParameters parameters;

        public PxsDeleteExportArchiveActionResult(
           HttpRequestMessage request,
           IAzureWorkItemQueuePublisher<DeleteFullExportArchiveWorkItem> publisher,
           IAuthorizer authorizer,
           AuthenticationScope authenticationScope,
           DeleteExportArchiveParameters parameters)
        {
            this.requestMessage = request;
            this.publisher = publisher;
            this.parameters = parameters;
            this.authenticationScope = authenticationScope;
            this.authorizer = authorizer;
        }

        protected override async Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken)
        {
            await this.authorizer.CheckAuthorizedAsync(this.requestMessage, this.authenticationScope);
            var commandId = new CommandId(parameters.CommandId);
            IncomingEvent.Current?.SetProperty("CommandId", commandId.Value);
            //  adding some delay to make sure that worker would not pick message from queue before record status updation
            TimeSpan? delay = TimeSpan.FromSeconds(10);

            var record = await CommandFeedGlobals.CommandHistory.QueryAsync(commandId, CommandHistoryFragmentTypes.Core);
            if (record == null)
            {
                DualLogger.Instance.Error(nameof(PxsDeleteExportArchiveActionResult), $"No record found for received commandId - {commandId.Value}");
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            var subject = record.Core.Subject as MsaSubject;
            if(parameters.RequesterPuid!=subject.Puid)
            {
                DualLogger.Instance.Error(nameof(PxsDeleteExportArchiveActionResult), $"Received puid does not match the puid of the record - {commandId.Value}");
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            if (record.Core.ExportArchivesDeleteStatus == ExportArchivesDeleteStatus.DeleteInProgress)
            {
                DualLogger.Instance.Error(nameof(PxsDeleteExportArchiveActionResult), $"Archive deletion is already in progress for commandId - {commandId.Value}");
                return new HttpResponseMessage(HttpStatusCode.Conflict);
            }

            var workItem = new DeleteFullExportArchiveWorkItem(commandId);
            await this.publisher.PublishAsync(workItem, delay);
            DualLogger.Instance.Information(nameof(PxsDeleteExportArchiveActionResult), $"Message pushed successfully to the queue with commandId - {commandId.Value}");
            record.Core.ExportArchivesDeleteStatus = ExportArchivesDeleteStatus.DeleteInProgress;
            record.Core.DeleteRequestedTime = DateTimeOffset.UtcNow;
            record.Core.DeleteRequester = parameters.RequesterPuid.ToString();

            try
            {
                DualLogger.Instance.Information(nameof(PxsDeleteExportArchiveActionResult), $"Updating CommandHistoryDB as DeleteInProgress - {commandId.Value}");
                await CommandFeedGlobals.CommandHistory.ReplaceAsync(record, CommandHistoryFragmentTypes.Core);
            }
            catch (CommandFeedException ex)
            {
                DualLogger.Instance.Error(nameof(PxsDeleteExportArchiveActionResult), $"Error in updating CommandHistoryDB as DeleteInProgress - {commandId.Value}");
                throw ex;
            }
            DualLogger.Instance.Information(nameof(PxsDeleteExportArchiveActionResult), $"Successfully Updated CommandHistoryDB as DeleteinProgress - {commandId.Value}");
            IncomingEvent.Current.StatusCode = HttpStatusCode.OK;
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
