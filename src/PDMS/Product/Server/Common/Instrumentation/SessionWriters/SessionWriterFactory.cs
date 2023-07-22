namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using global::Autofac;

    /// <summary>
    /// A session writer factory that can locate event writers using <c>Autofac</c>.
    /// </summary>
    public class SessionWriterFactory : ISessionWriterFactory
    {
        /// <summary>
        /// The current scope.
        /// </summary>
        private readonly ILifetimeScope scope;

        /// <summary>
        /// The session properties.
        /// </summary>
        private readonly SessionProperties properties;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionWriterFactory" /> class.
        /// </summary>
        /// <param name="scope">The current scope.</param>
        /// <param name="properties">The session properties.</param>
        public SessionWriterFactory(ILifetimeScope scope, SessionProperties properties)
        {
            this.scope = scope;
            this.properties = properties;
        }

        /// <summary>
        /// Tries to create a session writer that can log the given type.
        /// </summary>
        /// <remarks>
        /// SessionType is provided so that you can register different events for different session types.
        /// For example, Incoming and Outgoing API calls need different events for XPERT to register properly.
        /// </remarks>
        /// <typeparam name="TData">The type for the session writer.</typeparam>
        /// <param name="sessionType">The session type.</param>
        /// <param name="writer">The discovered session writer.</param>
        /// <returns>True if a session writer could be created, otherwise false.</returns>
        public bool TryCreate<TData>(SessionType sessionType, out ISessionWriter<TData> writer)
        {
            writer = null;

            if (this.scope.IsRegisteredWithKey<ISessionWriter<TData>>(sessionType))
            {
                writer = this.scope.ResolveKeyed<ISessionWriter<TData>>(sessionType);
            }
            else if (this.scope.IsRegistered<ISessionWriter<TData>>() || this.scope.IsRegistered(typeof(ISessionWriter<>)))
            {
                writer = this.scope.Resolve<ISessionWriter<TData>>();
            }

            return writer != null;
        }
    }
}
