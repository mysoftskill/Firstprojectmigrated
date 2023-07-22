namespace Microsoft.Membership.MemberServices.PrivacyAdapters.AnaheimIdAdapter
{
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;

    /// <summary>
    ///     Interface for IAnaheimIdAdapter.
    /// </summary>
    public interface IAnaheimIdAdapter
    {
        /// <summary>
        ///     Send DeleteDeviceIdRequest to device azure queue
        /// </summary>
        /// <param name="deleteDeviceIdRequest">The request object</param>
        /// <returns>An Adapter response.</returns>
        Task<AdapterResponse> SendDeleteDeviceIdRequestAsync(DeleteDeviceIdRequest deleteDeviceIdRequest);
    }
}
