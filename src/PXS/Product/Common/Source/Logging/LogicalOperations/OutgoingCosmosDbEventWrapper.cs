// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations
{
    using System.Diagnostics;

    using Microsoft.PrivacyServices.Common.Azure;

    using Ms.Qos;

    /// <summary>
    ///     OutgoingCosmosDbEventWrapper
    /// </summary>
    public class OutgoingCosmosDbEventWrapper : OutgoingApiEventWrapper
    {
        private const string ComponentName = nameof(OutgoingCosmosDbEventWrapper);

        private readonly ILogger logger;

        private OutgoingCosmosDbEvent operation;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OutgoingCosmosDbEventWrapper" /> class.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="queryString">The query string.</param>
        /// <param name="logger">The logger.</param>
        public OutgoingCosmosDbEventWrapper(string operationName, string queryString, ILogger logger)
        {
            const string CosmosDbDependencyName = "AzureCosmosDB";

            this.DependencyOperationName = operationName;

            // CosmosDb doesn't expose the target uri, so logging query string instead.
            this.TargetUri = queryString;
            this.PartnerId = CosmosDbDependencyName;
            this.DependencyName = CosmosDbDependencyName;

            this.logger = logger;
        }

        /// <summary>
        ///     Finishes the event.
        /// </summary>
        /// <param name="elapsedMilliseconds">Elapsed milliseconds to use as latency for the operation.</param>
        public override void Finish(double? elapsedMilliseconds = null)
        {
            if (this.operation == null)
            {
                this.logger.Error(ComponentName, "Start() must be called before Finish(). No event logged.");
                return;
            }

            this.operation.RequestCharge = this.RequestCharge;
            this.operation.ActivityId = this.ActivityId;
            this.operation.QueryResultCount = this.QueryResultCount;

            bool isSuccess = true;

            if (!string.IsNullOrWhiteSpace(this.CosmosDbErrorCode) || !string.IsNullOrWhiteSpace(this.CosmosDbErrorMessage))
            {
                this.operation.CosmosDbError = new CosmosDbError
                {
                    Code = this.CosmosDbErrorCode,
                    Message = this.CosmosDbErrorMessage
                };

                isSuccess = false;
            }

            this.operation.CollectionInfo = new CosmosDbCollectionInfo
            {
                CollectionSizeUsage = this.CollectionSizeUsage,
                CollectionSizeQuota = this.CollectionSizeQuota,
                CollectionUsagePercentage = this.CollectionUsagePercentage != default(decimal) ? this.CollectionUsagePercentage.ToString("0.00%") : string.Empty
            };

            this.PopulateFinishEvent(this.operation, elapsedMilliseconds, this.logger);

            TraceOperationHelper.PopupOperationResult(
                isSuccess,
                this.operation.baseData.protocolStatusCode,
                this.ResultDetails,
                this.ResultSignature,
                this.traceOperation);

            base.Finish(this.operation);
            this.operation = null;
        }

        /// <summary>
        ///     Starts a new event.
        ///     Resets all error fields in this wrapper, and increments the AttemptCount and CorrelationVector fields, for retries.
        /// </summary>
        public override void Start()
        {
            this.Start(GetNextCorrelationVector());
        }

        /// <summary>
        ///     Starts a new event using the provided correlation vector.
        ///     Resets all error fields in this wrapper, and increments the AttemptCount, for retries.
        /// </summary>
        /// <param name="correlationVector">Starts a logical operation with this Correlation Vector.</param>
        public override void Start(string correlationVector)
        {
            if (this.operation != null)
            {
                this.logger.Error(ComponentName, "Finish() must be called after Start(). No event started.");
                return;
            }

            this.AttemptCount++;

            this.CorrelationVector = correlationVector;

            this.ServiceErrorCode = 0;
            this.ProtocolStatusCode = null;
            this.ErrorMessage = null;
            string parentOperationName = GetParentOperationName();

            this.Stopwatch = Stopwatch.StartNew();
            this.operation = new OutgoingCosmosDbEvent { baseData = new OutgoingServiceRequest { operationName = parentOperationName } };

            this.InitTraceOperation(parentOperationName);
        }

        protected override string ResultDetails => this.operation.CosmosDbError?.Code;

        protected override string ResultSignature => this.operation.CosmosDbError?.Message;

        #region Part-C

        public double RequestCharge { get; set; }

        public string ActivityId { get; set; }

        public int QueryResultCount { get; set; }

        public decimal CollectionUsagePercentage { get; set; }

        public long CollectionSizeUsage { get; set; }

        public long CollectionSizeQuota { get; set; }

        public string CosmosDbErrorCode { get; set; }

        public string CosmosDbErrorMessage { get; set; }

        #endregion
    }
}
