// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.NgpProxy.PcfDataAgent
{
    using System;
    using System.Diagnostics;
    using System.Net.Http;

    using Microsoft.Cloud.InstrumentationFramework;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.Common.Azure;

    using Newtonsoft.Json.Serialization;

    public class TraceCommandFeedLogger : CommandFeedLogger
    {
        private readonly ILogger logger;

        private readonly string agentId;

        private readonly string componentName = "PcfDataAgent";

        public TraceCommandFeedLogger(string agentId, ILogger logger)
        {
            this.agentId = agentId;
            this.logger = logger;
        }

        public override void BeginServiceToServiceAuthRefresh(string targetSiteName, long siteId)
        {
            var messageFmt = "BeginServiceToServiceAuthRefresh in TraceCommandFeedLogger.\r\nagentId: {0}, targetSiteName: {1}, siteId: {2}, siteId: {3}";

            logger.Information(
                this.componentName,
                messageFmt,
                this.agentId,
                targetSiteName,
                siteId.ToString(),
                TraceLevel.Info.ToString());
        }

        public override void HttpResponseReceived(HttpRequestMessage request, HttpResponseMessage response)
        {
            var traceLevel = response.IsSuccessStatusCode ? IfxTracingLevel.Informational : IfxTracingLevel.Error;

            var messageFmt = "HttpResponseReceived in TraceCommandFeedLogger.\r\nagentId: {0}, requestMethod: {1}, requestUri: {2}, statusCode: {3}, traceLevel: {4}";

            logger.Log(
                traceLevel,
                this.componentName,
                messageFmt,
                this.agentId,
                request.Method.ToString(),
                request.RequestUri.ToString(),
                response.StatusCode.ToString("D"),
                traceLevel.ToString());
        }

        public override void SerializationError(object sender, ErrorEventArgs args)
        {
            var errorMessage =
                "SerializationError in TraceCommandFeedLogger.\r\nagentId: {0}, sender: {1}, currentObject: {2}, handled: {3}, member: {4}, originalObject: {5}, path: {6}, traceLevel: {7}";

            logger.Error(
                this.componentName,
                errorMessage,
                this.agentId,
                sender.ToString(),
                args?.CurrentObject.ToString() ?? "",
                args?.ErrorContext?.Handled.ToString() ?? "",
                args?.ErrorContext?.Member.ToString() ?? "",
                args?.ErrorContext?.OriginalObject.ToString() ?? "",
                args?.ErrorContext?.Path ?? "",
                TraceLevel.Error.ToString());
        }

        public override void UnhandledException(Exception ex)
        {
            logger.Error(
                this.componentName,
                ex,
                "UnhandledException in TraceCommandFeedLogger.");
        }

        public override void UnrecognizedDataType(string cv, string commandId, string dataType)
        {
            var erroMessage = "UnrecognizedDataType in TraceCommandFeedLogger.\r\nagentId: {0}, cv: {1}, commandId: {2}, dataType: {3}, traceLevel: {4}";

            logger.Error(
                this.componentName,
                erroMessage,
                this.agentId,
                cv,
                commandId,
                dataType,
                TraceLevel.Error.ToString());
        }
    }
}
