namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System.Net;
    using System.Runtime.Remoting.Messaging;

    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;

    /// <summary>
    /// Defines a property bag representing an incoming event.
    /// </summary>
    public class IncomingEvent : OperationEvent
    {
        /// <summary>
        /// Creates a new incoming event from the given source location.
        /// </summary>
        public IncomingEvent(SourceLocation sourceLocation) : base(sourceLocation)
        {
            Current = this;
        }

        /// <summary>
        /// Gets or sets the current incoming event
        /// </summary>
        public static IncomingEvent Current
        {
            get => (IncomingEvent)CallContext.LogicalGetData(typeof(IncomingEvent).FullName);
            set => CallContext.LogicalSetData(typeof(IncomingEvent).FullName, value);
        }

        /// <summary>
        /// The caller IP address.
        /// </summary>
        public string CallerIpAddress { get; set; }

        /// <summary>
        /// Caller Device or Application. Ex: Windows Phone 7, IE9, Xbox, etc. 
        /// </summary>
        public string CallerName { get; set; }

        /// <summary>
        /// The HTTP request method (Get/Post/etc)
        /// </summary>
        public string RequestMethod { get; set; }

        /// <summary>
        /// The status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Forces the instrumentation to log this event as an error, even if the HTTP status code said otherwise.
        /// This is useful for situations where we had a partial error processing an event and want that to appear in our QOS monitoring,
        /// but not to fail the entire client call.
        /// </summary>
        public bool ForceReportAsFailed { get; set; }

        /// <summary>
        /// The URI.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string TargetUri { get; set; }

        /// <summary>
        /// A method to set the ForceReportAsFailed property so that we can use the '?' syntax.
        /// </summary>
        public void SetForceReportAsFailed(bool value)
        {
            this.ForceReportAsFailed = value;
        }

        /// <summary>
        /// Logs this event.
        /// </summary>
        public override void Log(ILogger logger)
        {
            logger.Log(this);
        }
    }
}