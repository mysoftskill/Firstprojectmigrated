namespace Microsoft.PrivacyServices.CommandFeed.Service.DataAgent
{
    using System;
    using System.Net.Http;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    internal class DataAgentLogger : CommandFeedLogger
    {
        /// <inheritdoc />
        public override void UnhandledException(Exception ex)
        {
            DualLogger.Instance.Error(nameof(DataAgentLogger), ex, "Unhandled exception: ");
            Logger.Instance?.UnexpectedException(ex);
        }

        /// <inheritdoc />
        public override void HttpResponseReceived(HttpRequestMessage request, HttpResponseMessage response)
        {
            DualLogger.Instance.Verbose(nameof(DataAgentLogger), $"HTTP request completed: {request.Method} {request.RequestUri} Response: {response.StatusCode}");

            string content = response.Content.ReadAsStringAsync().Result;
            DualLogger.Instance.Verbose(nameof(DataAgentLogger), content);
        }

        /// <inheritdoc />
        public override void BeginServiceToServiceAuthRefresh(string targetSiteName, long siteId)
        {
            DualLogger.Instance.Information(nameof(DataAgentLogger), $"Service to service auth refresh initiated. Target Site = {targetSiteName}, Client Site Id = {siteId}");
        }

        /// <inheritdoc />
        public override void UnrecognizedDataType(string cv, string commandId, string dataType)
        {
            DualLogger.Instance.Error(nameof(DataAgentLogger), $"UnrecognizedDataType. cv={cv}, commandid={commandId}, datatype={dataType}");
            Logger.Instance?.LogDataAgentUnrecognizedDataType(cv, commandId, dataType);
        }

        /// <inheritdoc />
        public override void UnrecognizedCommandType(string cv, string commandId, string commandType)
        {
            DualLogger.Instance.Error(nameof(DataAgentLogger), $"UnrecognizedCommandType. cv={cv}, commandid={commandId}, commandType={commandType}");
            Logger.Instance?.LogDataAgentUnrecognizedCommandType(cv, commandId, commandType);
        }

        /// <inheritdoc />
        public override void CommandValidationException(string cv, string commandId, Exception ex)
        {
            DualLogger.Instance.Error(nameof(DataAgentLogger), ex, $"CommandValidationException. cv={cv}, commandid={commandId} ");
            Logger.Instance?.LogDataAgentValidatorError(cv, commandId, ex);
        }

        /// <summary>
        /// Invoked when one or more checkpoint fails a bulk checkpoint complete request.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="error">The error message.</param>
        public override void BatchCompleteError(string commandId, string error)
        {
            DualLogger.Instance.Error(nameof(DataAgentLogger), $"BulkCheckpointCompleteFailed. commandid={commandId}, error={error} ");
        }
    }
}