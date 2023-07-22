namespace Microsoft.PrivacyServices.CommandFeed.Service.Telemetry.Repository
{
    /// <summary>
    /// Telemetry Kusto queries <see cref="KustoQuery" />
    /// TODO: remove V1 queries after migration.
    /// </summary>
    public static class KustoQuery
    {
        /// <summary>
        /// Defines the KustoInterpolationQueryStatV2
        /// </summary>
        public const string KustoInterpolationQuery = @"
            declare query_parameters (AgentId:string);
            PCFAgentStatInterpolationView(AgentId)";

        public const string KustoAggregationQuery = @"
            declare query_parameters (AgentId:string);
            PCFAgentAggregationView(AgentId)";
    }
}
