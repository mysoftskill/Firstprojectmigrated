// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations.BaseContexts;

    using Ms.Qos;

    /// <summary>
    ///     Wrapper for an incoming API logical operation. Defines Part C fields.
    /// </summary>
    public class IncomingApiEventWrapper : IncomingLoggingContext
    {
        private const LogOption DefaultLogOption = LogOption.Realtime;

        private readonly bool isIncomingCall;

        private IncomingApiEvent operation;

        /// <summary>
        ///     Initializes a new instance of the <see cref="IncomingApiEventWrapper" /> class.
        /// </summary>
        public IncomingApiEventWrapper()
        {
            this.ServerActivityId = LogicalWebOperationContext.ServerActivityId.ToString();
            this.ClientActivityId = LogicalWebOperationContext.ClientActivityId.ToString();
            this.ExtraData = new Dictionary<string, string>();
            this.isIncomingCall = true;
        }

        /// <summary>
        ///     Perform operation telemetry completion tasks.
        /// </summary>
        /// <param name="overrideQosImpacting">Boolean that determines if we should set status to non-qos impacting</param>
        public void Finish(bool overrideQosImpacting = false)
        {
            if (this.operation == null)
            {
                throw new InvalidOperationException("Start() must be called before Finish().");
            }

            this.RequestStatusDelegateMethod(overrideQosImpacting, this);
            this.PopulateIncomingServiceRequest(this.operation.baseData);

            this.operation.ServerActivityId = this.ServerActivityId;
            this.operation.ClientActivityId = this.ClientActivityId;
            this.operation.Authentication = this.Authentication;
            this.operation.ErrorMessage = this.ErrorMessage;
            this.operation.ErrorCode = this.ErrorCode;
            this.operation.MemberViewClientVersion = this.MemberViewClientVersion;
            this.operation.FlattenedErrorCode = this.FlattenedErrorCode;
            this.operation.Flights = this.Flights;

            this.operation.baseData.succeeded = this.Success;

            TraceOperationHelper.PopupOperationResult(
                this.Success,
                this.operation.baseData.protocolStatusCode,
                this.ResultSignature,
                this.ResultDetails,
                this.traceOperation,
                this.isIncomingCall);

            this.operation.baseData.serviceErrorCode = this.ServiceErrorCode;
            this.operation.baseData.latencyMs = this.GetLatencyInMilliseconds();

            // Populate Part B CC
            Sll.Context?.CorrelationContext?.FillCorrelationContextDictionary(this.operation.baseData.cC);

            if (this.ExtraData != null)
                this.operation.ExtraData = this.ExtraData;

            this.operation.LogInformational(DefaultLogOption, this.FillEnvelope);
            this.operation = null;

            base.Finish();
        }

        /// <summary>
        ///     Perform operation telemetry commencement tasks.
        /// </summary>
        /// <param name="frontEndApiName">Http web controller method name.</param>
        public void Start(string frontEndApiName)
        {
            if (this.operation != null)
            {
                throw new InvalidOperationException("Finish() must be called after Start().");
            }

            this.Stopwatch = Stopwatch.StartNew();
            this.operation = new IncomingApiEvent();
            this.operation.baseData = new IncomingServiceRequest();
            this.operation.baseData.operationName = frontEndApiName;
            this.traceOperation = new Tracer(frontEndApiName);

            Sll.Context.ChangeIncomingEvent(this.operation);
        }

        protected override string ResultDetails => this.ErrorMessage;

        protected override string ResultSignature => this.ErrorCode;

        #region Part-C

        /// <summary>
        ///     Gets or sets the server activity id.
        /// </summary>
        internal string ServerActivityId { get; set; }

        /// <summary>
        ///     Gets or sets the client activity id.
        /// </summary>
        public string ClientActivityId { get; set; }

        /// <summary>
        ///     Gets or sets the authentication information.
        /// </summary>
        public string Authentication { get; set; }

        /// <summary>
        ///     Gets or sets the error-message.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        ///     Gets or sets the error-code.
        /// </summary>
        public string ErrorCode { get; set; }

        /// <summary>
        ///     Gets or sets the flattened-error-code.
        /// </summary>
        public string FlattenedErrorCode { get; set; }

        /// <summary>
        ///     Gets or sets the member-view client version.
        /// </summary>
        public string MemberViewClientVersion { get; set; }

        /// <summary>
        ///     Gets or sets the flights of the request
        /// </summary>
        public string Flights { get; set; }

        /// <summary>
        ///     Gets the extra data, property bag style.
        /// </summary>
        public Dictionary<string, string> ExtraData { get; }

        /// <summary>
        ///     Gets or sets Part C Correlation context.
        /// </summary>
        public Dictionary<string, string> CorrelationContext { get; set; }

        #endregion
    }
}
