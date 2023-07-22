namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System.Threading.Tasks;

    /// <summary>
    /// Defines an interface that can process privacy commands. This is the location where implementations should put their business logic.
    /// </summary>
    public interface IPrivacyDataAgent
    {
        /// <summary>
        /// Processes a command to delete a given type of data between a range of dates.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A task for completion.</returns>
        Task ProcessDeleteAsync(IDeleteCommand command);

        /// <summary>
        /// Processes a command that exports the given data types to a location in Azure Blob storage.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A task for completion.</returns>
        Task ProcessExportAsync(IExportCommand command);

        /// <summary>
        /// Processes a command that indicates the account of the command's subject is irrevocably closed.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A task for completion.</returns>
        Task ProcessAccountClosedAsync(IAccountCloseCommand command);

        /// <summary>
        /// Processes a command that indicates the account of the command's subject aged out (it is irrevocably closed).
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A task for completion.</returns>
        Task ProcessAgeOutAsync(IAgeOutCommand command);
    }
}
