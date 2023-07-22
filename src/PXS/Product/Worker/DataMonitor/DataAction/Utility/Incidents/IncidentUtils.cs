// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility.Incidents
{
    using Microsoft.AzureAd.Icm.Types;

    using Incident = Microsoft.PrivacyServices.DataManagement.Client.V2.Incident;

    /// <summary>
    ///     utilities for working with incidents
    /// </summary>
    public static class IncidentUtils
    {
        /// <summary>
        ///     To the incident file status
        /// </summary>
        /// <param name="incident">incident</param>
        /// <returns>resulting value</returns>
        public static IncidentFileStatus ToIncidentFileStatus(Incident incident)
        {
            IncidentAddUpdateSubStatus subStatus;

            // if we didn't get an incident back, assume we failed to file it 
            if (incident == null)
            {
                return IncidentFileStatus.FailedToFile;
            }

            // if we don't have response metadata, but do have an incident, then we must have filed it but can't tell any more 
            //  details, so assume a plain successful filing
            if (incident.ResponseMetadata == null)
            {
                return IncidentFileStatus.Created;
            }

            subStatus = (IncidentAddUpdateSubStatus)incident.ResponseMetadata.Substatus;

            switch ((IncidentAddUpdateStatus)incident.ResponseMetadata.Status)
            {
                case IncidentAddUpdateStatus.AddedNew:
                    return (subStatus & IncidentAddUpdateSubStatus.Suppressed) == 0 ?
                        IncidentFileStatus.Created :
                        IncidentFileStatus.CreatedSuppressed;

                case IncidentAddUpdateStatus.Discarded:
                    return (subStatus & IncidentAddUpdateSubStatus.Suppressed) == 0 ?
                        IncidentFileStatus.HitCounted :
                        IncidentFileStatus.Discarded;

                // while techncially no change happened, this is effectively saying that IcM accepted but then discarded the
                //  change because the alert source change date listed in the change was earlier than the last one recorded by 
                //  IcM. For our purposes, reporting this as an update is fine
                case IncidentAddUpdateStatus.DidNotChangeExisting:
                    return (subStatus & IncidentAddUpdateSubStatus.Suppressed) == 0 ?
                        IncidentFileStatus.Updated :
                        IncidentFileStatus.UpdatedSuppressed;
                    
                case IncidentAddUpdateStatus.UpdatedExisting:
                {
                    // there are possible other substatus field values that could have an impact on update, but they are not
                    //  likely to be triggered by a process we use at this time or plan to use (resolve, mitigate, transfer, etc)

                    return 
                        (subStatus & IncidentAddUpdateSubStatus.Activated) != 0 ?
                            IncidentFileStatus.UpdatedActivate :
                            ((subStatus & IncidentAddUpdateSubStatus.Suppressed) != 0 ? 
                                IncidentFileStatus.UpdatedSuppressed : 
                                IncidentFileStatus.Updated);
                }
                
                default:
                    // the other statuses are various forms of "failed to file".  For details,  see the IcM docs at
                    //  https://icmdocs.azurewebsites.net/developers/Connectors/InjectingIncidentsUsingConnectorAPI.html and search
                    //  for the IncidentAddUpdateStatus and IncidentAddUpdateSubStatus enums
                    return IncidentFileStatus.FailedToFile;
            }
        }
    }
}
