namespace Microsoft.PrivacyServices.DataManagement.Client.Models.V2
{
    /// <summary>
    /// This enum type identifies the state an delete agent is while migrating from V1 to V2.
    /// </summary>
    public enum AgentMigrationState
    {
        /// <summary>
        /// The agent is migrating Preproduction protocol from V1 to V2.
        /// </summary>
        PreproductionV1ToV2 = 1,

        /// <summary>
        /// The agent is migrating Production protocol from V1 to V2.
        /// </summary>
        ProductionV1ToV2 = 2,

        /// <summary>
        /// The agent is rolling back V2 migration.
        /// The agent is migrating Production protocol from V2 to V1.
        /// </summary>
        ProductionV2ToV1 = 3
    }
}