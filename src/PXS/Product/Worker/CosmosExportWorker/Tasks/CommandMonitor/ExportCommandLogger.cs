// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Net.Http;

    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.CommandFeed.Client;

    using Newtonsoft.Json.Serialization;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     logging object for the PCF client
    /// </summary>
    public class ExportCommandLogger : CommandFeedLogger
    {
        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the ExportCommandLogger class
        /// </summary>
        /// <param name="logger">Geneva trace logger</param>
        public ExportCommandLogger(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///     Invoked when an unhandled exception is encountered
        /// </summary>
        /// <param name="exception">exception</param>
        public override void UnhandledException(Exception exception)
        {
            if (exception != null)
            {
                this.logger.Error(nameof(CommandMonitor), exception, "Error occurred in PCF client");
            }
        }

        /// <summary>
        ///     Invoked when a data type is received that is not recognized
        /// </summary>
        /// <param name="cv">The correlation vector.</param>
        /// <param name="commandId">The command ID.</param>
        /// <param name="dataType">The data type.</param>
        public override void UnrecognizedDataType(
            string cv, 
            string commandId, 
            string dataType)
        {
            this.logger.Error(
                nameof(CommandMonitor), 
                $"Unexpected data type found in PCF client [cv: {cv}][commandId: {commandId}][type: {dataType}]");
        }

        /// <summary>
        ///     Invoked when an HTTP response has been received
        /// </summary>
        /// <param name="request">HTTP request</param>
        /// <param name="response">HTTP response</param>
        /// <remarks>The implementation MUST NOT modify any details of either the request or response</remarks>
        public override void HttpResponseReceived(
            HttpRequestMessage request, 
            HttpResponseMessage response)
        {
            // TODO: do we care about logging anything here?
        }

        /// <summary>
        /// Invoked when the client begins the process of refreshing its Service to Service auth ticket.
        /// </summary>
        /// <param name="targetSiteName">The target site name for Command Feed.</param>
        /// <param name="siteId">The agent site ID.</param>
        public override void BeginServiceToServiceAuthRefresh(
            string targetSiteName, 
            long siteId)
        {
            this.logger.Information(
                nameof(CommandMonitor),
                $"Refreshing S2S auth ticket [siteName: {targetSiteName}][siteId: {siteId}]");
        }

        /// <summary>Invoked when serialization fails.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The arguments of the serialization error.</param>
        public override void SerializationError(
            object sender, 
            ErrorEventArgs args)
        {
            this.logger.Error(
                nameof(CommandMonitor),
                $"PCF client failed to deserialize object [Path: {args?.ErrorContext?.Path}]: {args?.ErrorContext?.Error}]");
        }
    }
}
