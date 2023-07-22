namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    /// <summary>
    /// A factory to create SLL specific sessions.
    /// </summary>
    public class SessionFactory : ISessionFactory
    {
        /// <summary>
        /// The sessionWriterFactory.
        /// </summary>
        private readonly ISessionWriterFactory sessionWriterFactory;
        private readonly SessionProperties sessionProperties;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionFactory" /> class.
        /// </summary>
        /// <param name="sessionWriterFactory">A factory to create session writer instances.</param>
        /// <param name="sessionProperties">The session properties.</param>
        public SessionFactory(ISessionWriterFactory sessionWriterFactory, SessionProperties sessionProperties)
        {
            this.sessionWriterFactory = sessionWriterFactory;
            this.sessionProperties = sessionProperties;
        }

        /// <summary>
        /// Creates a new session and starts calculating its duration.
        /// </summary>
        /// <param name="name">The name of the session.</param>
        /// <param name="sessionType">The type of the session.</param>
        /// <returns>A session object.</returns>
        public ISession StartSession(string name, SessionType sessionType)
        {
            return new Session(this.sessionWriterFactory, name, sessionType, this.sessionProperties);
        }
    }
}
