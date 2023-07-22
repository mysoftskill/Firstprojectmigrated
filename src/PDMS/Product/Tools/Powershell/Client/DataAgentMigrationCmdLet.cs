namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.Client;
    using Microsoft.PrivacyServices.DataManagement.Client.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.Client.V2;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// A <c>cmdlet</c> for migrating data agents from V1 to V2 protocol.
    /// Switching the protocol between V1 and V2 agents
    /// </summary>
    [Cmdlet(VerbsCommon.Switch, "PdmsDataAgentProtocol")]
    public class PdmsDataAgentProtocolCmdLet : IHttpResultCmdlet<DeleteAgent>
    {
        /// <summary>
        /// The state of the migration the agent is entering.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public AgentMigrationState AgentMigrationState { get; set; }

        /// <summary>
        /// The PDMS Data Agent Id.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNull]
        public Guid AgentId { get; set; }

        /// <summary>
        /// The ConnectionDetail
        /// </summary>
        [Parameter(Position = 2, Mandatory = false)]
        public ConnectionDetail ConnectionDetail { get; set; }

        protected override Task<IHttpResult<DeleteAgent>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            DeleteAgent deleteAgent = client.DataAgents.ReadAsync<DeleteAgent>(this.AgentId.ToString(), context, DataAgentExpandOptions.None).Result.Response; ;

            if (deleteAgent == null)
            {
                throw new Exception("Agent not found");
            }

            this.ValidateConnectionDetail();
            IDictionary<ReleaseState, ConnectionDetail> migratingConnectionDetails;

            switch (this.AgentMigrationState)
            {
                case AgentMigrationState.PreproductionV1ToV2:
                    // remove V1 PPE connectionDetail and Add V2 ConnectionDetail to MigratingConnectionDetails.
                    deleteAgent.ConnectionDetails.Remove(ReleaseState.PreProd);
                    deleteAgent.MigratingConnectionDetails = new Dictionary<ReleaseState, ConnectionDetail>
                    {
                        { ReleaseState.PreProd, this.ConnectionDetail }
                    };

                    break;

                case AgentMigrationState.ProductionV1ToV2:
                    // Switch V1 ConnectionDetails and MigratingConnectionDetails. Add V2 Prod connectionDetail to ConnectionDetails.
                    migratingConnectionDetails = deleteAgent.MigratingConnectionDetails;
                    deleteAgent.MigratingConnectionDetails = deleteAgent.ConnectionDetails;

                    if (this.ConnectionDetail != null)
                    {
                        if (this.ConnectionDetail.ReleaseState == ReleaseState.Prod)
                        {
                            migratingConnectionDetails.Add(ReleaseState.Prod, this.ConnectionDetail);
                        }
                        else
                        {
                            throw new Exception("ConnectionDetail ReleaseState should be Prod when switching Prod to V2");
                        }
                    }
                    
                    deleteAgent.ConnectionDetails = migratingConnectionDetails;

                    if (deleteAgent.ConnectionDetails[ReleaseState.Prod] == null)
                    {
                        throw new Exception("Prod ConnectionDetail is required when switching Prod to V2");
                    }

                    if (!IsV2Protocol(deleteAgent.ConnectionDetails[ReleaseState.Prod]))
                    {
                        throw new Exception("Agent migrating protocol should either be PCFV2Batch or CommandFeedV2");
                    }

                    break;

                case AgentMigrationState.ProductionV2ToV1:
                    //Switch deleteAgent's ConnectionDetails and MigratingConnectionDetails

                    if (deleteAgent.ConnectionDetails[ReleaseState.Prod] == null)
                    {
                        throw new Exception("Agent is not in production, can't rollback.");
                    }

                    if (!IsV2Protocol(deleteAgent.ConnectionDetails[ReleaseState.Prod]))
                    {
                        throw new Exception("Agent protocol is already V1, can't rollback.");
                    }

                    migratingConnectionDetails = deleteAgent.MigratingConnectionDetails;
                    deleteAgent.MigratingConnectionDetails = deleteAgent.ConnectionDetails;
                    deleteAgent.ConnectionDetails = migratingConnectionDetails;

                    break;
                default:
                    throw new NotImplementedException();
            }

            return client.DataAgents.UpdateAsync(deleteAgent, context);
        }

        private void ValidateConnectionDetail()
        {
            // if AgentMigrationState is PreproductionV1ToV2 then ConnectionDetail should not be null
            if (this.AgentMigrationState is AgentMigrationState.PreproductionV1ToV2)
            {
                if (this.ConnectionDetail == null)
                {
                    throw new Exception("PPE ConnectionDetail is required when switching Preprod to V2");
                }

                if (!IsV2Protocol(this.ConnectionDetail))
                {
                    throw new Exception("Agent migrating protocol should either be PCFV2Batch or CommandFeedV2");
                }

                if (this.ConnectionDetail.ReleaseState is ReleaseState.Prod)
                {
                    throw new Exception("ReleaseState should be Preproduction when switching Preprod to V2");
                }
            }

            // if AgentMigrationState is ProductionV2ToV1 and ConnectionDetail is not null, throw
            if (this.AgentMigrationState is AgentMigrationState.ProductionV2ToV1
                 && this.ConnectionDetail != null)
            {
                throw new Exception("ConnectionDetail should be null when switching Prod to V1");
            }
        }

        private bool IsV2Protocol(ConnectionDetail connectionDetail)
        {
            if (connectionDetail.Protocol == Policies.Current.Protocols.Ids.PCFV2Batch
                || connectionDetail.Protocol == Policies.Current.Protocols.Ids.CommandFeedV2)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// A <c>cmdlet</c> for marking migration as complete.
    /// Clearing up the migrating connection details in the agent config.
    /// </summary>
    [Cmdlet(VerbsCommon.Close, "PdmsDataAgentMigration")]
    public class DataAgentMigrationCmdLet : IHttpResultCmdlet<DeleteAgent>
    {
        /// <summary>
        /// The PDMS Data Agent Id.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public Guid AgentId { get; set; }

        protected override Task<IHttpResult<DeleteAgent>> ExecuteAsync(IDataManagementClient client, RequestContext context)
        {
            DeleteAgent deleteAgent = client.DataAgents.ReadAsync<DeleteAgent>(this.AgentId.ToString(), context, DataAgentExpandOptions.None).Result.Response;

            deleteAgent.MigratingConnectionDetails = null;

            return client.DataAgents.UpdateAsync(deleteAgent, context);
        }
    }
}