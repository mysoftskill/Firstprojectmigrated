namespace Microsoft.PrivacyServices.DataManagement.Common
{
    using System.Threading.Tasks;

    /// <summary>
    /// A component that can initialize dependencies.
    /// Register this with <c>Autofac</c> and then retrieve all using Resolve{IEnumerable{IInitializer}}.
    /// </summary>
    public interface IInitializer
    {
        /// <summary>
        /// Initializes the component.
        /// </summary>
        /// <returns>A task that executes the initialization.</returns>
        Task InitializeAsync();
    }
}