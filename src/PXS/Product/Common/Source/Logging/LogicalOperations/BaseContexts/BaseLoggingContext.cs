// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations.BaseContexts
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Net.Http;

    using Microsoft.Membership.MemberServices.Common.Logging.Extensions;
    using Microsoft.Membership.MemberServices.Common.Logging.Logging;
    using Microsoft.Telemetry;

    using Ms.Qos;

    /// <summary>
    ///     Context for the Part B fields of a logical operation.
    /// </summary>
    public abstract class BaseLoggingContext
    {
        protected ITraceOperation traceOperation;

        /// <summary>
        ///     Gets or sets the device information.
        /// </summary>
        public DeviceInfo DeviceInfo { get; private set; }

        /// <summary>
        ///     Gets or sets the transport protocol: HTTP, TCP, etc.
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        ///     Gets or sets the HTTP protocol status: 400, 400.1
        /// </summary>
        public string ProtocolStatusCode { get; set; }

        /// <summary>
        ///     Gets or sets the HTTP method: GET, POST, PUT, DELETE, etc.
        /// </summary>
        public string RequestMethod { get; set; }

        /// <summary>
        ///     Gets or sets the request status: Success, CallerError, TransportError, ServiceError
        /// </summary>
        public ServiceRequestStatus RequestStatus { get; set; }

        /// <summary>
        ///     Delegate for the preferred method to setting request status
        /// </summary>
        public Action<bool, BaseLoggingContext> RequestStatusDelegateMethod { get; set; }

        /// <summary>
        ///     Gets or sets the HTTP content type: gzip, img, xml.
        /// </summary>
        public string ResponseContentType { get; set; }

        /// <summary>
        ///     Gets or sets the error code returned by service.
        /// </summary>
        public int ServiceErrorCode { get; set; }

        /// <summary>
        ///     True if the operation was a success; false otherwise.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        ///     Gets or sets the target Uri.
        /// </summary>
        public string TargetUri { get; set; }

        /// <summary>
        ///     Gets or sets the user information such as PUID, CID, etc.
        /// </summary>
        public UserInfo UserInfo { get; private set; }

        /// <summary>
        ///     Determines if the HTTP <see cref="ProtocolStatusCode" /> signifies a client-error.
        /// </summary>
        /// <returns>true if client-error; otherwise false.</returns>
        public bool IsProtocolStatusCodeClientError()
        {
            if (float.TryParse(this.ProtocolStatusCode, out float httpStatusCode))
            {
                return httpStatusCode >= 400 && httpStatusCode < 500;
            }

            return false;
        }

        /// <summary>
        ///     Set user information on this event.
        /// </summary>
        /// <param name="aadId">Aad ID of the user.</param>
        public void SetAadUserId(string aadId)
        {
            if (!string.IsNullOrEmpty(aadId))
            {
                this.UserInfo = new UserInfo();
                this.UserInfo.SetId(UserIdType.AzureAdId, aadId);
            }
        }

        /// <summary>
        ///     Sets the TargetUri, RequestMethod and Protocol based on the request.
        /// </summary>
        /// <param name="request">Request to use to set properties.</param>
        public void SetRequestProperties(HttpRequestMessage request)
        {
            this.TargetUri = request.RequestUri.ToString();
            this.RequestMethod = request.Method.ToString();
            this.Protocol = request.RequestUri.Scheme;
        }

        /// <summary>
        ///     Set user information on this event.
        /// </summary>
        /// <param name="puidDecimal">Puid of user in decimal</param>
        public void SetUserId(long puidDecimal)
        {
            if (puidDecimal != 0)
            {
                this.UserInfo = new UserInfo();
                string puidDecimalString = puidDecimal.ToString(CultureInfo.InvariantCulture);
                this.UserInfo.SetId(UserIdType.DecimalPuid, puidDecimalString);
            }
        }

        /// <summary>
        ///     When specified, this value will be used instead of the Stopwatch value.
        /// </summary>
        protected double? ElapsedMilliseconds { get; set; }

        /// <summary>
        ///     Result details for logging of tracer (geneva operation event).
        /// </summary>
        protected virtual string ResultDetails => string.Empty;

        /// <summary>
        ///     Result signature for logging of tracer (geneva operation event).
        /// </summary>
        protected virtual string ResultSignature => string.Empty;

        /// <summary>
        ///     Gets or sets the stopwatch used to calculate duration
        /// </summary>
        protected Stopwatch Stopwatch { get; set; }

        /// <summary>
        ///     Initializes a new instance of hte <see cref="BaseLoggingContext" /> class.
        /// </summary>
        protected BaseLoggingContext()
        {
            this.Success = true;
            this.RequestStatusDelegateMethod = this.SetRequestStatus;
        }

        /// <summary>
        ///     Fill the envelope
        /// </summary>
        /// <param name="e"></param>
        protected virtual void FillEnvelope(Envelope e)
        {
            // Populate user information if present
            this.UserInfo?.FillEnvelope(e);

            // Populate device information if present
            this.DeviceInfo?.FillEnvelope(e);

            // fill in the basic envelope information
            LoggingInfo.FillEnvelope(e);
        }

        protected virtual void Finish()
        {
            // Dispose the trace operation to log the operation.
            // This is base on the fact that ITraceOperation is designed for using keyword majorly for Geneva operation.
            // So we have to Dispose it for this BaseLoggingContext to finish. Although they are different pattern here.
            this.traceOperation?.Dispose();
        }

        /// <summary>
        ///     Stops the running operation timer and returns the elapsed milliseconds
        /// </summary>
        /// <returns>Elapsed time in milliseconds</returns>
        protected int GetLatencyInMilliseconds()
        {
            if (this.Stopwatch == null || !this.Stopwatch.IsRunning)
            {
                throw new InvalidOperationException("Stopwatch was not started, can't get elapsed milliseconds");
            }

            this.Stopwatch.Stop();
            double elapsedMilliseconds = this.ElapsedMilliseconds ?? this.Stopwatch.ElapsedMilliseconds;

            return elapsedMilliseconds > int.MaxValue ? int.MaxValue : (int)elapsedMilliseconds;
        }

        /// <summary>
        ///     Populates the incoming-request.
        /// </summary>
        /// <param name="baseQos">The incoming-request to populate.</param>
        protected void PopulateBaseQos(IncomingServiceRequest baseQos)
        {
            baseQos.targetUri = this.TargetUri;
            baseQos.serviceErrorCode = this.ServiceErrorCode;
            baseQos.requestMethod = this.RequestMethod;
            baseQos.responseContentType = this.ResponseContentType;
            baseQos.protocol = this.Protocol;
            baseQos.protocolStatusCode = this.ProtocolStatusCode;
            baseQos.requestStatus = this.RequestStatus;
        }

        /// <summary>
        ///     Populates the outgoing-request.
        /// </summary>
        /// <param name="baseQos">The outgoing-request to populate.</param>
        protected void PopulateBaseQos(OutgoingServiceRequest baseQos)
        {
            baseQos.targetUri = this.TargetUri;
            baseQos.serviceErrorCode = this.ServiceErrorCode;
            baseQos.requestMethod = this.RequestMethod;
            baseQos.responseContentType = this.ResponseContentType;
            baseQos.protocol = this.Protocol;
            baseQos.protocolStatusCode = this.ProtocolStatusCode;
            baseQos.requestStatus = this.RequestStatus;
        }

        /// <summary>
        ///     Set device information on this event.
        ///     Does nothing if DeviceId is null.
        /// </summary>
        /// <param name="deviceId">The device id.</param>
        protected void SetDeviceId(long? deviceId)
        {
            if (deviceId != null)
            {
                this.DeviceInfo = SllLoggingHelper.CreateDeviceInfo(deviceId);
            }
        }

        /// <summary>
        ///     Populates RequestStatus based on success flag and ProtocolStatusCodes, or the provided override.
        /// </summary>
        /// <param name="overrideQosImpacting">Boolean that determines if we should set status to non-qos impacting</param>
        /// <param name="loggingContext">The outgoingAPIEvent instance</param>
        protected void SetRequestStatus(bool overrideQosImpacting = false, BaseLoggingContext loggingContext = null)
        {
            if (this.Success)
            {
                this.RequestStatus = ServiceRequestStatus.Success;
            }
            else if (overrideQosImpacting || this.IsProtocolStatusCodeClientError())
            {
                this.RequestStatus = ServiceRequestStatus.CallerError;
            }
            else
            {
                this.RequestStatus = ServiceRequestStatus.ServiceError;
            }
        }

        /// <summary>
        ///     Set user information on this event.
        ///     Does nothing if userId is null.
        /// </summary>
        /// <param name="userId">The MsaId containing the user information.</param>
        protected void SetUserId(MsaId userId)
        {
            if (userId != null)
            {
                this.UserInfo = SllLoggingHelper.CreateUserInfo(userId);
            }
        }
    }
}
