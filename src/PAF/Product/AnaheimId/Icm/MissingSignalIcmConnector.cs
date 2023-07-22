namespace Microsoft.PrivacyServices.AnaheimId.Icm
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.ServiceModel;
    using System.Text.RegularExpressions;
    using Microsoft.AzureAd.Icm.Types;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Defines method for creating ICM incidents for missing signals.
    /// </summary>
    public class MissingSignalIcmConnector : IMissingSignalIcmConnector
    {
        private const string ComponentName = nameof(MissingSignalIcmConnector);
        private readonly int severity;
        private readonly Guid connectorId;
        private readonly string connectorName;
        private readonly string serviceLocation;
        private readonly string containerEndpoint;
        private readonly IConnectorIncidentManager icmClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MissingSignalIcmConnector" /> class.
        /// </summary>
        /// <param name="icmClient">The ICM client instance.</param>
        /// <param name="connectorId">The ICM guid value for the connector id.</param>
        /// <param name="connectorName">The ICM connector name.</param>
        /// <param name="serviceLocation">The alert environemnt.</param>
        /// <param name="severity">The severity of the alert.</param>
        /// <param name="containerEndpoint">The blob storage container url.</param>
        public MissingSignalIcmConnector(IConnectorIncidentManager icmClient, Guid connectorId, string connectorName, string serviceLocation, int severity, string containerEndpoint)
        {
            this.icmClient = icmClient;
            this.connectorId = connectorId;
            this.connectorName = connectorName;
            this.serviceLocation = serviceLocation;
            this.severity = severity;
            this.containerEndpoint = containerEndpoint;
        }

        /// <summary>
        /// Loads the necessary icm connector implementation and populates an incident envelope
        /// with signal information before creating a ticket to the ICM portal.
        /// </summary>
        /// <param name="name">The name of the blob containing missing signals.</param>
        /// <param name="requestIds">A sample of request ids found in the missing signal file.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>An incident create/update success status.</returns>
        public bool CreateMissingSignalIncident(string name, List<long> requestIds, ILogger logger)
        {
            if (name == null)
            {
                logger.Error(ComponentName, "A valid blob url must be provided.");
                return false;
            }
            else
            {
                // Attempt to extract the datetime object from the blob filename
                DateTime dt = ExtractDate(name);
                if (dt != DateTime.MinValue)
                {
                    // Print all extracted fields in the datetime object
                    logger.Information(ComponentName, $"Creating alert for Year={dt.Year},Month={dt.Month},Day={dt.Day},Hour={dt.Hour},Minute={dt.Minute},Second={dt.Second}");
                }
                else
                {
                    logger.Error(ComponentName, "Could not parse the date from the blob url.");
                    return false;
                }

                // Populate the ICM and attempt to send it to the portal
                AlertSourceIncident incidentEnvelope = this.PopulateIncident(name, dt.ToString("MM/dd/yyyy h:mm tt"), requestIds, logger);
                if (incidentEnvelope != null)
                {
                    return this.SendIncident(incidentEnvelope, logger);
                }
                else
                {
                    logger.Error(ComponentName, "Could not create ICM incident.");
                    return false;
                }
            }
        }

        /// <summary>
        /// Extract the date from a blob url name.
        /// </summary>
        /// <param name="blobName">The url link to the blob.</param>
        /// <returns>The extracted datetime object.</returns>
        private static DateTime ExtractDate(string blobName)
        {
            try
            {
                // Expected Filename Format - MissingSignal_2021-11-23_22_34_16-00004.avro or MissingSignal_2021-11-23_22_34_16.avro
                string blobUrlDate = Regex.Replace(blobName.Replace("MissingSignal_", string.Empty).Replace(".avro", string.Empty), @"-\d{5}", string.Empty);

                // Parsed Numeric Date - 2021-11-23_22_34_16 (for both)
                return DateTime.ParseExact(blobUrlDate, "yyyy-MM-dd_H_m_s", CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                // Use the minimum value to indicate that the parsing did not work
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// This functions attempts to sends an incident to the icm portal.
        /// </summary>
        /// <param name="incidentEnvelope">The Alert information for the incident.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>The incident create/update success status.</returns>
        private bool SendIncident(AlertSourceIncident incidentEnvelope, ILogger logger)
        {
            try
            {
                logger.Information(ComponentName, "Routing Message to connector id: " + this.connectorId.ToString());
                IncidentAddUpdateResult result = this.icmClient.AddOrUpdateIncident2(this.connectorId, incidentEnvelope, RoutingOptions.None);
                return result.IncidentId != null;
            }
            catch (FaultException<IcmFault> icmException)
            {
                if (icmException.Detail != null)
                {
                    logger.Error(ComponentName, $"ICM Exception Code: {icmException.Detail.Code} \nICM Exception Message: {icmException.Detail.Message} \nICM ConnectorId: {this.connectorId}");
                }
                else
                {
                    logger.Error(ComponentName, $"ICM Exception Message: {icmException.Message} \nICM ConnectorId: {this.connectorId}");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ComponentName, ex.Message);
            }

            return false;
        }

        /// <summary>
        /// This functions creates an ICM incident for a specific time.
        /// </summary>
        /// <param name="name">The name of the blob containing missing signals.</param>
        /// <param name="discoveryDate">The detection date to populate in the icm ticket.</param>
        /// <param name="requestIds">A sample of request ids from the missing signal file.</param>
        /// <param name="logger">The logger instance.</param>
        /// <returns>An icm incident object with all details included.</returns>
        private AlertSourceIncident PopulateIncident(string name, string discoveryDate, List<long> requestIds, ILogger logger)
        {
            // Values to populate in the alert for the incident
            DateTime now = DateTime.Now;
            string owner = "Automated";
            string userName = "Automated";
            string keywords = "Anaheim Missing Signals";
            string title = $"Anaheim Missing Signals on {discoveryDate}";
            string commaSeperatedIds = string.Join(", ", requestIds);
            string blobUrl = this.containerEndpoint + "/" + name;
            string message = $"There are missing device delete signals on {discoveryDate} including request ids: {commaSeperatedIds}. \nLink to the file: {blobUrl}. \nNote: This file is available for investigation, but will be deleted in 90 days.";

            // Log the ICM information
            logger.Information(ComponentName, $"Creating ICM \nTitle: {title} \nMessage: {message}");

            return new AlertSourceIncident
            {
                Source = new AlertSourceInfo
                {
                    CreatedBy = userName,
                    Origin = this.connectorName,
                    CreateDate = now,
                    ModifiedDate = now,
                    IncidentId = Guid.NewGuid().ToString("N")
                },
                CorrelationId = Guid.NewGuid().ToString("N"),
                RoutingId = $"ownerId:{owner}", // Intentionally cause a NullReference exception if OwnerId is missing.
                OccurringLocation = new IncidentLocation
                {
                    Environment = this.serviceLocation,
                    DeviceName = null,
                    DeviceGroup = null,
                    ServiceInstanceId =
                        "NGPAnaheimLivesite" // A fixed value that OSOC can use for correlation rules. This will appear as the Slice value in the UI.
                },
                RaisingLocation = new IncidentLocation
                {
                    Environment = this.serviceLocation,
                    DeviceName = "Unknown",
                    DeviceGroup = "Unknown",
                    ServiceInstanceId = null
                },
                Status = IncidentStatus.Active,
                Severity = this.severity,
                Title = title,
                Keywords = keywords,
                DescriptionEntries = new[]
                {
                    new DescriptionEntry
                    {
                        Cause = DescriptionEntryCause.Created,
                        RenderType = DescriptionTextRenderType.Html,
                        Text = message,
                        Date = now,
                        ChangedBy = userName,
                        SubmitDate = now,
                        SubmittedBy = this.connectorName
                    }
                }
            };
        }
    }
}