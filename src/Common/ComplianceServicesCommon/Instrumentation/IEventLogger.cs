namespace Microsoft.Azure.ComplianceServices.Common.Instrumentation
{
    using System;

    /// <summary>
    /// Default event logger interface.
    /// </summary>
    public interface IEventLogger
    {
        /// <summary>
        /// Gets or sets the CV of the current context.
        /// </summary>
        string CorrelationVector { get; }

        /// <summary>
        /// Sets the correlation vector to the given value. If the value is invalid, a new vector may be created.
        /// </summary>
        /// <remarks>Declared as a method so we can use ? syntax: Logger.Instance?.SetCorrelationVector("foobar"). Such syntax doesn't work on properties.</remarks>
        void SetCorrelationVector(string vector);

        /// <summary>
        /// Ensures that a correlation vector exists, and creates a new one if it does not.
        /// </summary>
        void EnsureCorrelationVector();

        /// <summary>
        /// Logged when we encounter an exception.
        /// </summary>
        void UnexpectedException(Exception ex);

        /// <summary>
        /// Logged when we report on the worker queue depths.
        /// </summary>
        void AzureWorkerQueueDepth(string storageAccountName, string queueName, long depth);

        /// <summary>
        /// Logged when a distributed lock is acquired.
        /// </summary>
        void DistributedLockAcquiredEvent(string lockName, TimeSpan duration);
        
        /// <summary>
        /// Logs the basic metadata about the current PDMS data set.
        /// </summary>
        void LogPdmsDataSetAgeEvent(string assetGroupInfoStream, string variantInfoStream, DateTimeOffset createdTime, long version);

        /// <summary>
        /// Log Data Agent Validator Errors
        /// </summary>
        void LogDataAgentValidatorError(string cv, string commandId, Exception ex);

        /// <summary>
        /// Log Data Agent UnrecognizedDataType error
        /// </summary>
        void LogDataAgentUnrecognizedDataType(string cv, string commandId, string dataType);

        /// <summary>
        /// Log Data Agent UnrecognizedCommandType error
        /// </summary>
        void LogDataAgentUnrecognizedCommandType(string cv, string commandId, string commandType);

        /// <summary>
        /// Logs commands transferred from various points in the system.
        /// </summary>
        void CommandsTransferred(int commandCount, string agentId, string assetGroupId, string transferPoint);

        /// <summary>
        /// Logs that the given agent/asset group pair doesn't have ICM information registered and that we tried to raise an alert.
        /// </summary>
        void IcmConnectorNotRegistered(string agentId, string assetGroupId, string eventName);

        /// <summary>
        /// Logs when fail to parse lease receipt.
        /// </summary>
        void LogLeaseReceiptFailedToParse(string leaseReceipt);

        /// <summary>
        /// Logs the source location and reason for requesting a restart of the process.
        /// </summary>
        void RestartRequested(string memberName, string fileName, int lineNumber, string reason);
    }
}
