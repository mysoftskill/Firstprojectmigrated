// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations.BaseContexts
{
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Telemetry;

    using Ms.Qos;

    /// <summary>
    ///     Context for the Part B fields of an outgoing logical operation.
    /// </summary>
    public class OutgoingLoggingContext : BaseLoggingContext
    {
        private const LogOption DefaultLogOption = LogOption.Realtime;

        /// <summary>
        ///     Correlation vector as string.
        /// </summary>
        public string CorrelationVector { get; protected set; }

        /// <summary>
        ///     The name of the service. Ex: Outlook, CP, XflowWebService, XflowControlDB, etc.
        /// </summary>
        public string DependencyName { get; set; }

        /// <summary>
        ///     The name of the dependency operation invoked.
        /// </summary>
        public string DependencyOperationName { get; set; }

        /// <summary>
        ///     The version of the dependency operation invoked.
        /// </summary>
        public string DependencyOperationVersion { get; set; }

        /// <summary>
        ///     Type of the called resources. Ex: AzureStore, WebService, SqlAzureL, etc.
        /// </summary>
        public string DependencyType { get; set; }

        /// <summary>
        ///     The response size in bytes.
        /// </summary>
        public int ResponseSizeBytes { get; set; }

        protected override void FillEnvelope(Envelope e)
        {
            e.cV = this.CorrelationVector;
            base.FillEnvelope(e);
        }

        /// <summary>
        ///     Perform operation telemetry completion tasks.
        /// </summary>
        /// <param name="operation">The logical operation.</param>
        protected void Finish(OutgoingApiEvent operation)
        {
            this.FinishOperationBaseData(operation.baseData);
            operation.LogInformational(DefaultLogOption, this.FillEnvelope);
            base.Finish();
        }

        /// <summary>
        ///     Perform operation telemetry completion tasks.
        /// </summary>
        /// <param name="operation">The logical operation.</param>
        protected void Finish(OutgoingCosmosDbEvent operation)
        {
            this.FinishOperationBaseData(operation.baseData);
            operation.LogInformational(DefaultLogOption, this.FillEnvelope);
            base.Finish();
        }

        /// <summary>
        ///     Populates the outgoing-request.
        /// </summary>
        /// <param name="outgoingServiceRequest">The outgoing-request to populate.</param>
        protected void PopulateOutgoingServiceRequest(OutgoingServiceRequest outgoingServiceRequest)
        {
            this.PopulateBaseQos(outgoingServiceRequest);
            outgoingServiceRequest.dependencyOperationName = this.DependencyOperationName;
            outgoingServiceRequest.dependencyOperationVersion = this.DependencyOperationVersion;
            outgoingServiceRequest.dependencyName = this.DependencyName;
            outgoingServiceRequest.dependencyType = this.DependencyType;
            outgoingServiceRequest.responseSizeBytes = this.ResponseSizeBytes;
        }

        private void FinishOperationBaseData(OutgoingServiceRequest outgoingServiceRequest)
        {
            outgoingServiceRequest.succeeded = this.Success;
            outgoingServiceRequest.serviceErrorCode = this.ServiceErrorCode;
            outgoingServiceRequest.latencyMs = this.GetLatencyInMilliseconds();
            outgoingServiceRequest.responseSizeBytes = this.ResponseSizeBytes;
        }
    }
}
