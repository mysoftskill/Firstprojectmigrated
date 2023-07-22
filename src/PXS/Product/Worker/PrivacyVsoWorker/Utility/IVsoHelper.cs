namespace Microsoft.Membership.MemberServices.Privacy.PrivacyVsoWorker.Utility
{
    using System.Threading.Tasks;

    using Newtonsoft.Json.Linq;

    public interface IVsoHelper
    {
        Task<JObject> CreateVsoWorkItemIfNotExistsAsync(Agent agent);
    }
}
