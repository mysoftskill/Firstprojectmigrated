namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;

    /// <summary>
    /// An event that measures the latency of a named operation.
    /// </summary>
    public class OperationEvent
    {
        private readonly DateTimeOffset startTime;
        private readonly ConcurrentDictionary<string, string> properties;

        public OperationEvent(SourceLocation sourceLocation)
        {
            this.properties = new ConcurrentDictionary<string, string>();
            this.OperationName = $"{sourceLocation.FileName}.{sourceLocation.MemberName}";
            this.startTime = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets or sets the named property on this event.
        /// </summary>
        public string this[string key]
        {
            get { return this.properties[key]; }
            set { this.properties[key] = value; }
        }

        /// <summary>
        /// The elasped time of the operation.
        /// </summary>
        public TimeSpan ElapsedTime => DateTimeOffset.UtcNow - this.startTime;

        /// <summary>
        /// The name of the operation.
        /// </summary>
        public string OperationName { get; set; }

        /// <summary>
        /// The status of the operation.
        /// </summary>
        public OperationStatus OperationStatus { get; set; }

        /// <summary>
        /// Indicates an exception.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Any custom properties associated with this event.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties => this.properties;

        /// <summary>
        /// Sets the given property to the given value.
        /// </summary>
        public void SetProperty(string key, string value)
        {
            this.properties[key] = value;
        }

        /// <summary>
        /// Sets the exception property. Useful since we can't use the null-coalescing operator with property assignments.
        /// </summary>
        public void SetException(Exception ex)
        {
            this.Exception = ex;
        }

        /// <summary>
        /// Logs this even to the given logger.
        /// </summary>
        public virtual void Log(ILogger logger)
        {
            logger.Log(this);
        }
    }
}