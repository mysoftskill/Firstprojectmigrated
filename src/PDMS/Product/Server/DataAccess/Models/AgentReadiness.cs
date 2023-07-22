namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;
    
    /// <summary>
    /// This enum type creates a separation for agents that are truly Production Ready and agents that are
    /// Testing in Production to ensure that the overall system readiness can be measured and launch successfully.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<AgentReadiness>))]
    public enum AgentReadiness
    {
        /// <summary>
        /// A production agent that has not been signed off by owners as production ready.
        /// </summary>
        TestInProd = 0,

        /// <summary>
        /// A production agent that has been signed off by owners as ship quality.
        /// </summary>
        ProdReady = 1
    }
}