// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.PerfCounters;
    using Microsoft.Membership.MemberServices.Privacy.Core.AadAuthentication;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.DataManagementAdapter;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;

    using Newtonsoft.Json;

    using RequestContext = Microsoft.PrivacyServices.DataManagement.Client.RequestContext;

    /// <summary>
    ///     object to file incidents in IcM
    /// </summary>
    public class IncidentCreator : IIncidentCreator
    {
        private readonly IPrivacyConfigurationManager configMgr;
        private readonly IDataManagementClientFactory factory;
        private readonly ICounterFactory counterFactory;
        private readonly IAadAuthManager authManager;

        /// <summary>
        ///     Initializes a new instance of the IncidentCreator class
        /// </summary>
        public IncidentCreator(
            IDataManagementClientFactory incidentClientFactory,
            IPrivacyConfigurationManager configManager,
            ICounterFactory counterFactory,
            IAadAuthManager authManager)
        {
            this.counterFactory = counterFactory ?? throw new ArgumentNullException(nameof(counterFactory));
            this.authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            this.configMgr = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.factory = incidentClientFactory ?? throw new ArgumentNullException(nameof(incidentClientFactory));
        }

        /// <summary>
        ///     Files the specified incident
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <param name="incident">incident to file</param>
        /// <returns>result of filing the incident</returns>
        public async Task<IncidentCreateResult> CreateIncidentAsync(
            CancellationToken cancellationToken,
            AgentIncident incident)
        {
            IDataManagementClient client = this.factory.Create(this.authManager, this.configMgr, this.counterFactory);

            try
            {
                RequestContext requestContext = new RequestContext { CancellationToken = cancellationToken };
                Incident response;

                response = (await client.Incidents.CreateAsync(incident.ToIncident(), requestContext))?.Response;
                return new IncidentCreateResult(response?.Id, IncidentUtils.ToIncidentFileStatus(response));
            }
            catch (ConflictError.DoesNotExist)
            {
                return new IncidentCreateResult(IncidentFileStatus.ConnectorNotFound);
            }
            catch (NotFoundError.Entity)
            {
                return new IncidentCreateResult(IncidentFileStatus.EntityNotFound);
            }
            catch (Exception e) when (IncidentCreator.IsFatalError(e) == false)
            {
                DataActionException exNew = new DataActionException(
                    $"Failed to submit sev [{incident.Severity}] incident [{incident.Title}] to IcM for agent [{incident.AgentId}]",
                    e,
                    false);

                if (e is JsonException)
                {
                    IncidentCreator.CopyExceptionData(
                        e, 
                        "DataManagmentClient.RawResponse", 
                        exNew,
                        DataActionConsts.ExceptionDataIncidentRawResponse);
                }

                throw exNew;
            }
        }

        /// <summary>
        ///     Copies the contents of the source exception's Data field into the destination exception
        /// </summary>
        /// <param name="source">source</param>
        /// <param name="sourceName">source name</param>
        /// <param name="dest">dest</param>
        /// <param name="destName">dest name</param>
        private static void CopyExceptionData(
            Exception source,
            string sourceName,
            Exception dest,
            string destName)
        {
            if (source.Data.Contains(sourceName))
            {
                // don't insert a null element
                object temp = source.Data[sourceName];
                if (temp != null)
                {
                    dest.Data[destName] = temp;
                }
            }
        }

        /// <summary>
        ///     Determines whether the provided exception is fatal or not.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns>System.Boolean.</returns>
        private static bool IsFatalError(Exception e)
        {
            return
               e is OutOfMemoryException ||
               e is StackOverflowException ||
               e is NullReferenceException ||
               e is SEHException ||
               e is AccessViolationException ||
               e is ThreadAbortException;
        }
    }
}
