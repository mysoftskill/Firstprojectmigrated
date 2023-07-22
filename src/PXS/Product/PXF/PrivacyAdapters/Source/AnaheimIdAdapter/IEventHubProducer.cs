
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter
{
    using System.Threading.Tasks;

    /// <summary>
    /// Eventhub producer interface.
    /// </summary>
    public interface IEventHubProducer
    {
        /// <summary>
        /// Send data to eventhub.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync(string message);
    }
}
