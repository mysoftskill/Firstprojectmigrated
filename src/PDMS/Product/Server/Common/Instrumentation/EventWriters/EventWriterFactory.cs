namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using global::Autofac;

    /// <summary>
    /// An event writer factory that can locate event writers using <c>Autofac</c>.
    /// </summary>
    public class EventWriterFactory : IEventWriterFactory
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
        /// Initializes a new instance of the <see cref="EventWriterFactory" /> class.
        /// </summary>
        /// <param name="scope">The current scope.</param>
        /// <param name="properties">The session properties.</param>
        public EventWriterFactory(ILifetimeScope scope, SessionProperties properties)
        {
            this.scope = scope;
            this.properties = properties;
        }

        /// <summary>
        /// Attempts to create an event writer for the given type.
        /// </summary>
        /// <typeparam name="TData">The type whose event writer is requested.</typeparam>
        /// <param name="writer">An event writer that can log data of the given type.</param>
        /// <returns>True if a writer was created, otherwise false.</returns>
        public bool TryCreate<TData>(out IEventWriter<TData> writer)
        {
            writer = null;
            if (this.scope.IsRegistered<IEventWriter<TData>>() || this.scope.IsRegistered(typeof(IEventWriter<>)))
            {
                writer = this.scope.Resolve<IEventWriter<TData>>();
            }

            return writer != null;
        }
    }
}
