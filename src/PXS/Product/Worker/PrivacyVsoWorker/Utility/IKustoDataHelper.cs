namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    ///     Helper to query kusto data
    /// </summary>
    public interface IKustoDataHelper
    {
        Task<List<Agent>> GetAgentsWithNoConnectorIdAsync();
    }
}
