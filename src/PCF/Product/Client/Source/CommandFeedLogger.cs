namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;

    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;

    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Exposes events that listeners may subscribe to. Consumers may override some or all of the methods in this class.
    /// </summary>
    public abstract class CommandFeedLogger
    {
        /// <summary>
        /// Invoked when an unhandled exception is encountered.
        /// </summary>
        /// <param name="ex">The exception.</param>
        public abstract void UnhandledException(Exception ex);

        /// <summary>
        /// Invoked when a datatype is received that is not recognized.
        /// </summary>
        /// <param name="cv">The correlation vector.</param>
        /// <param name="commandId">The command ID.</param>
        /// <param name="dataType">The data type.</param>
        public virtual void UnrecognizedDataType(string cv, string commandId, string dataType)
        {
        }

        /// <summary>
        /// Invoked when a command type is received that is not recognized.
        /// </summary>
        /// <param name="cv">The correlation vector.</param>
        /// <param name="commandId">The command ID.</param>
        /// <param name="commandType">The command type.</param>
        public virtual void UnrecognizedCommandType(string cv, string commandId, string commandType)
        {
        }

        /// <summary>
        /// Invoked when an HTTP response has been received. The implementation MUST NOT modify any details
        /// of either the request or response.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        public virtual void HttpResponseReceived(HttpRequestMessage request, HttpResponseMessage response)
        {
        }

        /// <summary>
        /// Invoked when the client begins the process of refreshing its Service to Service auth ticket.
        /// </summary>
        /// <param name="targetSiteName">The target site name for Command Feed.</param>
        /// <param name="siteId">The agent site ID.</param>
        public virtual void BeginServiceToServiceAuthRefresh(string targetSiteName, long siteId)
        {
        }

        /// <summary>
        /// Invoked when serialization fails.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The arguments of the serialization error.</param>
        public virtual void SerializationError(object sender, ErrorEventArgs args)
        {
        }

        /// <summary>
        /// Invoked when the verifier fails validation
        /// </summary>
        /// <param name="cv">The correlation vector.</param>
        /// <param name="commandId">The command ID.</param>
        /// <param name="ex">The exception thrown during the validation</param>
        public virtual void CommandValidationException(string cv, string commandId, Exception ex)
        {
        }

        /// <summary>
        /// Invoked when one or more checkpoint fails a bulk checkpoint complete request.
        /// </summary>
        /// <param name="commandId">The command ID.</param>
        /// <param name="error">The error message.</param>
        public virtual void BatchCompleteError(string commandId, string error)
        {
        }

        /// <summary>
        /// Invoked when the thread cancellation requested.
        /// </summary>
        /// <param name="ex">Cancellation exception.</param>
        public virtual void CancellationException(Exception ex)
        {
        }
    }
}
