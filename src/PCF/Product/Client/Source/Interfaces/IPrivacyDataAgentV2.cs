namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPrivacyDataAgentV2
    {
        /// <summary>
        /// Processes a workitem
        /// </summary>
        /// <param name="commandFeedClient">The PCF SDK client.</param>
        /// <param name="workitem">The workitem.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task for completion.</returns>
        Task ProcessWorkitemAsync(ICommandFeedClient commandFeedClient, Workitem workitem, CancellationToken cancellationToken);
    }
}
