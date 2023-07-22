namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using System;
    using System.Threading.Tasks;

    public interface IInternalProcessFactory
    {
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
        Task ExecuteAsync<T>(string name, bool swallowExceptions, Func<T, Task> process);
    }
}