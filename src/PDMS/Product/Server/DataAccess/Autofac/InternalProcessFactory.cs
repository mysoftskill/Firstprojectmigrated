namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using System;
    using System.Threading.Tasks;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// A class to spawn instrumented internal processes.
    /// This class should be created as a singleton.
    /// </summary>
    public sealed class InternalProcessFactory : IInternalProcessFactory
    {
        /// <summary>
        /// The current scope.
        /// </summary>
        private readonly ILifetimeScope scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalProcessFactory" /> class.
        /// </summary>
        /// <param name="scope">The current scope.</param>
        public InternalProcessFactory(ILifetimeScope scope)
        {
            this.scope = scope;
        }

        /// <summary>
        /// Instruments and executes the given process.
        /// </summary>
        /// <typeparam name="T">A dependency type required by the process.</typeparam>
        /// <param name="name">The session name for instrumenting the action.</param>
        /// <param name="swallowExceptions">
        /// Whether or not this should swallow exceptions or bubble them up. 
        /// The exception will be logged regardless.
        /// </param>
        /// <param name="process">The process to execute.</param>
        /// <returns>Task that runs the process.</returns>
        public async Task ExecuteAsync<T>(string name, bool swallowExceptions, Func<T, Task> process)
        {
            using (var childScope = this.scope.BeginLifetimeScope())
            {
                var dependency = childScope.Resolve<T>();

                var sessionFactory = childScope.Resolve<ISessionFactory>();

                var session = sessionFactory.StartSession(name, SessionType.Internal);

                try
                {
                    await process(dependency).ConfigureAwait(false);
                    session.Success();
                }
                catch (Exception ex)
                {
                    session.Fault(ex);
                    if (!swallowExceptions)
                    {
                        throw;
                    }
                }
            }
        }
    }
}