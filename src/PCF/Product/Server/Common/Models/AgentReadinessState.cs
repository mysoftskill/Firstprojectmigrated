namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Captures the state agent is in, ProdReady or TestInProd
    /// </summary>
    public enum AgentReadinessState
    {
        /// <summary>
        /// The Agent is in full production state
        /// </summary>
        ProdReady,

        /// <summary>
        /// The agent is not fully production ready
        /// </summary>
        TestInProd,
    }
}
