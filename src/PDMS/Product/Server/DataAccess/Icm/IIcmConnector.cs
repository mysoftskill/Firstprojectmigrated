namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Icm
{
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Models.V2;

    /// <summary>
    /// Defines methods for sending ICM incidents.
    /// </summary>
    public interface IIcmConnector
    {
        /// <summary>
        /// Given some incident information, loads the necessary connector ID from storage, 
        /// populates an incident envelope and logs the incident to ICM.
        /// </summary>
        /// <param name="incident">The incident to send.</param>
        /// <returns>The incident with data updated.</returns>
        Task<Incident> CreateIncidentAsync(Incident incident);

        /// <summary>
        /// Sends the owner registration confirmation incident to the given owner.
        /// If the registration is not valid, then it throws an exception.
        /// </summary>
        /// <param name="owner">The owner that has ICM properties set.</param>
        void SendOwnerRegistrationConfirmationAsync(DataOwner owner);

        /// <summary>
        /// Sends the agent registration confirmation incident to the given agent.
        /// If the registration is not valid, then it throws an exception.
        /// </summary>
        /// <param name="owner">The agent's owner.</param>
        /// <param name="agent">The agent that has ICM properties set.</param>
        void SendAgentRegistrationConfirmationAsync(DataOwner owner, DataAgent agent);
    }
}