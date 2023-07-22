namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System.Diagnostics;

    /// <summary>
    /// Base implementation of the ISession interface.
    /// Primarily responsible for calculating the duration of a session.
    /// </summary>
    public abstract class BaseSession : ISession
    {
        /// <summary>
        /// The type of the session.
        /// </summary>
        protected readonly SessionType SessionType;

        /// <summary>
        /// A factory for retrieving type specific session writers.
        /// </summary>
        protected readonly ISessionWriterFactory SessionWriterFactory;

        /// <summary>
        /// Tracks the duration of the session.
        /// </summary>
        private readonly Stopwatch stopWatch;

        /// <summary>
        /// The snapped CV value.
        /// </summary>
        private readonly string cv;

        /// <summary>
        /// The name of the session for instrumentation purposes.
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseSession" /> class.
        /// Starts tracking the duration of the session.
        /// </summary>
        /// <param name="sessionWriterFactory">A factory for retrieving type specific session writers.</param>
        /// <param name="sessionType">The type of the session.</param>
        /// <param name="name">The name of the session for instrumentation purposes.</param>
        /// <param name="sessionProperties">The session properties.</param>
        public BaseSession(ISessionWriterFactory sessionWriterFactory, SessionType sessionType, string name, SessionProperties sessionProperties)
        {
            this.stopWatch = Stopwatch.StartNew();
            this.name = name;
            this.SessionType = sessionType;
            this.SessionWriterFactory = sessionWriterFactory;

            if (sessionType == SessionType.Outgoing)
            {
                // For outgoing calls we must increment the CV and persist it.
                this.cv = sessionProperties.CV.Increment();
            }
            else
            {
                // For all others, we take the current value.
                this.cv = sessionProperties.CV.Get();
            }
        }

        /// <summary>
        /// Gets the correlation vector that was snapped when the session started.
        /// </summary>
        public string CorrelationVector
        {
            get
            {
                return this.cv;
            }
        }

        /// <summary>
        /// Changes the name of the session. This is necessary in places where the name is not known until after some code has started to execute.
        /// </summary>
        /// <param name="name">The new value for the session name.</param>
        public void SetName(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Completes the session and logs the given status.
        /// </summary>
        /// <param name="status">Success status of the session.</param>
        public void Done(SessionStatus status)
        {
            this.Done(status, this.name, this.Stop());
        }

        /// <summary>
        /// Logs the completed session information with some additional metadata.
        /// </summary>
        /// <typeparam name="TResult">The data type that corresponds to a registered ISessionWriter.</typeparam>
        /// <param name="status">Success status of the session.</param>
        /// <param name="result">Additional metadata to log.</param>
        public void Done<TResult>(SessionStatus status, TResult result)
        {
            ISessionWriter<TResult> writer;
            if (this.SessionWriterFactory.TryCreate(this.SessionType, out writer))
            { 
                writer.WriteDone(status, this.name, this.Stop(), this.CorrelationVector, result);
            }
            else
            {
                // If we fail to find a custom session writer, then log it as a generic session.
                this.Done(status, this.name, this.Stop());
            }
        }

        /// <summary>
        /// Stops the timer and returns the final duration.
        /// </summary>
        /// <returns>The duration of the session.</returns>
        public long Stop()
        {
            this.stopWatch.Stop();

            return this.stopWatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Logs the completed session information.
        /// </summary>
        /// <param name="status">Success status of the session.</param>
        /// <param name="name">Name of the session.</param>
        /// <param name="totalMilliseconds">Duration of the session.</param>
        protected abstract void Done(SessionStatus status, string name, long totalMilliseconds);
    }
}
