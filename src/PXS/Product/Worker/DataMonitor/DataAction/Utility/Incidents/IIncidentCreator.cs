// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     status of the incident filed
    /// </summary>
    public enum IncidentFileStatus
    {
        /// <summary>
        ///     invalid option
        /// </summary>
        Invalid = 0,

        /// <summary>
        ///     the incident was created as a new incident
        /// </summary>
        Created,

        /// <summary>
        ///     the incident was filed as a suppressed incident
        /// </summary>
        /// <remarks>
        ///     the id may be null in the case of a suppressed incident if the suppression rule is set to discard
        /// </remarks>
        CreatedSuppressed,

        /// <summary>
        ///     the incident was determined to be suppressed and discarded
        /// </summary>
        Discarded,

        /// <summary>
        ///     the incident hitcounted an existing incident
        /// </summary>
        /// <remarks>the id field contains the id of the incident hitcounted</remarks>
        HitCounted,

        /// <summary>
        ///     the incident was treated as an update to an existing incident
        /// </summary>
        Updated,

        /// <summary>
        ///     the incident was treated as an update to an existing incident and the incident was activated in the process
        /// </summary>
        UpdatedActivate,

        /// <summary>
        ///     the incident was treated as an update to an existing suppressed incident
        /// </summary>
        UpdatedSuppressed,

        /// <summary>
        ///     no connector could be found for the entity specified
        /// </summary>
        ConnectorNotFound,

        /// <summary>
        ///     the entity specified could not be found
        /// </summary>
        EntityNotFound,

        /// <summary>
        ///     the incident failed to be filed for an unknown reason
        /// </summary>
        FailedToFile,

        /// <summary>
        ///     simulated filing an incident but did not actually file one
        /// </summary>
        SimulatedFiling,
    }

    /// <summary>
    ///     the result of filing an incident
    /// </summary>
    public class IncidentCreateResult
    {
        /// <summary>
        ///     Initializes a new instance of the IncidentCreateResult class
        /// </summary>
        /// <param name="id">incident id</param>
        /// <param name="status">incident creation status</param>
        public IncidentCreateResult(
            long? id, 
            IncidentFileStatus status)
        {
            this.Status = status;
            this.Id = id;
        }

        /// <summary>
        ///     Initializes a new instance of the IncidentCreateResult class
        /// </summary>
        /// <param name="status">incident creation status</param>
        public IncidentCreateResult(IncidentFileStatus status)
        {
            this.Status = status;
            this.Id = null;
        }

        /// <summary>
        ///     Gets or sets status
        /// </summary>
        public IncidentFileStatus Status { get; }

        /// <summary>
        ///     Gets or sets the created incident's id or the id of the parent incident for hitcounted incidents
        /// </summary>
        public long? Id { get; }
    }

    /// <summary>
    ///     contract for objects filing incidents
    /// </summary>
    public interface IIncidentCreator
    {
        /// <summary>
        ///     Files the specified incident
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="incident">incident to file</param>
        /// <returns>result of filing the incident</returns>
        Task<IncidentCreateResult> CreateIncidentAsync(
            CancellationToken cancellationToken,
            AgentIncident incident);
    }
}
