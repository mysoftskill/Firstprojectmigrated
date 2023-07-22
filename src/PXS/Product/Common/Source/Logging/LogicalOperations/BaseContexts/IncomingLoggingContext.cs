// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations.BaseContexts
{
    using Ms.Qos;

    /// <summary>
    ///     Context for the Part B fields of an incoming logical operation.
    /// </summary>
    public class IncomingLoggingContext : BaseLoggingContext
    {
        /// <summary>
        ///     The IP address of the caller.
        /// </summary>
        public string CallerIPAddress { get; set; }

        /// <summary>
        ///     Caller Device or Application. Ex: Windows Phone 7, IE9, Xbox, etc.
        /// </summary>
        public string CallerName { get; set; }

        /// <summary>
        ///     The version of the current operation.
        /// </summary>
        public string OperationVersion { get; set; }

        /// <summary>
        ///     The request size in bytes.
        /// </summary>
        public int RequestSizeBytes { get; set; }

        /// <summary>
        ///     Populates the incoming-request.
        /// </summary>
        /// <param name="incomingServiceRequest">The incoming-request to populate.</param>
        protected void PopulateIncomingServiceRequest(IncomingServiceRequest incomingServiceRequest)
        {
            this.PopulateBaseQos(incomingServiceRequest);
            incomingServiceRequest.operationVersion = this.OperationVersion;
            incomingServiceRequest.callerIpAddress = this.CallerIPAddress;
            incomingServiceRequest.callerName = this.CallerName;
            incomingServiceRequest.requestSizeBytes = this.RequestSizeBytes;
        }
    }
}
