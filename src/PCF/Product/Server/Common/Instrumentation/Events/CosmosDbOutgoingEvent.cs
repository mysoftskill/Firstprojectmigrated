namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;

    /// <summary>
    /// Defines an outgoing request to CosmosDB.
    /// </summary>
    public class CosmosDbOutgoingEvent : OutgoingEvent
    {
        /// <summary>
        /// Creates a new OutgoingEvent from the given location.
        /// </summary>
        public CosmosDbOutgoingEvent(SourceLocation sourceLocation, string moniker, string collectionId, string partition) : base(sourceLocation)
        {
            this.Moniker = moniker;
            this.CollectionId = collectionId;
            this.PartitionKey = partition;
        }

        /// <summary>
        /// The request charge of this operation.
        /// </summary>
        public double RequestCharge { get; set; }

        /// <summary>
        /// The partition key of the request.
        /// </summary>
        public string PartitionKey { get; set; }

        /// <summary>
        /// The moniker.
        /// </summary>
        public string Moniker { get; set; }

        /// <summary>
        /// The collection ID.
        /// </summary>
        public string CollectionId { get; set; }

        /// <summary>
        /// The number of rows affected by this operation.
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// A delimited list of returned command IDs.
        /// </summary>
        public string CommandIds { get; set; }

        /// <summary>
        /// Indicates whether we were throttled or not.
        /// </summary>
        public bool IsThrottled { get; set; }
        
        /// <summary>
        /// Logs this event.
        /// </summary>
        public override void Log(ILogger logger)
        {
            logger.Log(this);
        }
    }
}