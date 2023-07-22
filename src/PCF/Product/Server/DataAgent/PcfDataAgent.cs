namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using Microsoft.Azure.ComplianceServices.Common.Instrumentation;
    using Microsoft.Identity.Client;
    using Microsoft.PrivacyServices.CommandFeed.Client;
    using Microsoft.PrivacyServices.CommandFeed.Service.DataAgent;
    using Microsoft.PrivacyServices.Common.Azure;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// PCF Test Data Agent
    /// </summary>
    public class PcfDataAgent : IPrivacyDataAgent
    {
        private readonly Guid agentId;
        private const int NumberOfReceivers = 3;
        private readonly string aadAppId;
        private readonly CommandFeedEndpointConfiguration endpointConfig;
        private readonly DataAgentLogger logger;

        /// <summary>
        /// Create data agent
        /// </summary>
        public PcfDataAgent(Guid agentId, string aadAppId, CommandFeedEndpointConfiguration endpointConfig)
        {
            this.agentId = agentId;
            this.aadAppId = aadAppId;
            this.endpointConfig = endpointConfig;

            // Unhandled exception logger
            this.logger = new DataAgentLogger();
        }

        /// <inheritdoc />
        public Task ProcessAccountClosedAsync(IAccountCloseCommand command)
        {
            return this.ProcessAsync(command);
        }

        public Task ProcessAgeOutAsync(IAgeOutCommand command)
        {
            return this.ProcessAsync(command);
        }

        /// <inheritdoc />
        public Task ProcessDeleteAsync(IDeleteCommand command)
        {
            return this.ProcessAsync(command);
        }

        /// <inheritdoc />
        public Task ProcessExportAsync(IExportCommand command)
        {
            return this.ProcessAsync(command);
        }


        /// <summary>
        /// Command feed end point name.
        /// </summary>
        /// <returns></returns>
        public string GetCommandFeedEndpointName()
        {
            string endPointName = "UNKNOWN";

            // PCF Test Agent app is currently present only in Microsoft PPE and PROD tenants.
            if (this.endpointConfig == CommandFeedEndpointConfiguration.Preproduction)
            {
                endPointName = "PPE";
            }
            else if (this.endpointConfig == CommandFeedEndpointConfiguration.Production)
            {
                endPointName = "PROD";
            }

            return endPointName;
        }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            // Create command feed client
            var client = new CommandFeedClient(
                agentId: this.agentId,
                aadClientId: this.aadAppId,
                Config.Instance.Common.ServiceToServiceCertificate,
                this.logger,
                null,
                endpointConfig,
                sendX5c: true,
                azureRegion: Environment.GetEnvironmentVariable("MONITORING_DATACENTER") ?? ConfidentialClientApplication.AttemptRegionDiscovery);

            for (int i = 0; i < PcfDataAgent.NumberOfReceivers; i++)
            {
                var receiver = new PrivacyCommandReceiver(
                        dataAgent: this,
                        commandFeedClient: client,
                        logger: this.logger);
                receiver.ConcurrencyLimit = 100;
                tasks.Add(receiver.BeginReceivingAsync(cancellationToken));

                var batchCompleteReceiver = new BatchCompleteReceiver(
                    client,
                    cancellationToken);
                tasks.Add(receiver.BeginReceivingAsync(cancellationToken));
            }

            return Task.WhenAll(tasks);
        }

        private async Task ProcessAsync(IPrivacyCommand command)
        {
            DualLogger.Instance.Information($"{nameof(PcfDataAgent)}.{nameof(ProcessAsync)}", 
                $"EndpointName={this.GetCommandFeedEndpointName()};SubjectType={command.Subject.GetType().Name};CommandId={command.CommandId}");

            // Report ProcessXXX call as incoming event
            await Logger.InstrumentAsync(
                new IncomingEvent(SourceLocation.Here()),
                async (ev) =>
                {
                    ev["AgentId"] = this.agentId.ToString();
                    ev["AssetGroupId"] = command.AssetGroupId;
                    ev["CommandType"] = command.GetType().Name;
                    ev["CommandId"] = command.CommandId;
                    ev["RequestBatchId"] = command.RequestBatchId;
                    ev["CorrelationVector"] = command.CorrelationVector;
                    ev["CloudInstance"] = command.CloudInstance;
                    ev["ProcessorApplicable"] = command.ProcessorApplicable.ToString();
                    ev["ControllerApplicable"] = command.ControllerApplicable.ToString();
                    ev["LeaseReceipt"] = command.LeaseReceipt;
                    ev["IssuedTimestamp"] = command.Timestamp.ToString("s");
                    ev["SubjectType"] = command.Subject.GetType().Name;
                    ev["EndpointName"] = this.GetCommandFeedEndpointName();
                    
                    ev.StatusCode = HttpStatusCode.OK;

                    if (command is Client.AgeOutCommand ageOutCommand)
                    {
                        ev["LastActive"] = ageOutCommand.LastActive?.ToString("s");
                    }

                    await Task.Yield();
                });

            // Report CheckpointAsync as outgoing event
            await Logger.InstrumentAsync(
                new OutgoingEvent(SourceLocation.Here()),
                async (ev) =>
                {
                    ev["AgentId"] = this.agentId.ToString();
                    ev["AssetGroupId"] = command.AssetGroupId;
                    ev["CommandType"] = command.GetType().Name;
                    ev["CommandId"] = command.CommandId;
                    ev["RequestBatchId"] = command.RequestBatchId;
                    ev["CorrelationVector"] = command.CorrelationVector;
                    ev["CloudInstance"] = command.CloudInstance;
                    ev["ProcessorApplicable"] = command.ProcessorApplicable.ToString();
                    ev["ControllerApplicable"] = command.ControllerApplicable.ToString();
                    ev["LeaseReceipt"] = command.LeaseReceipt;
                    ev["IssuedTimestamp"] = command.Timestamp.ToString("s");
                    ev["SubjectType"] = command.Subject.GetType().Name;
                    ev["EndpointName"] = this.GetCommandFeedEndpointName();

                    if (FlightingUtilities.IsEnabled(FlightingNames.SyntheticAgentDisableCompleteCommands))
                    {
                        await command.CheckpointAsync(CommandStatus.Pending, affectedRowCount: 0, leaseExtension: TimeSpan.FromDays(1));
                    }
                    else
                    {
                        await command.CheckpointAsync(CommandStatus.Complete, affectedRowCount: 0);
                    }
                });
       }
    }
}
