namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// This enum type creates a separation for agents that are truly Production Ready and agents that are
    /// Testing in Production to ensure that the overall system readiness can be measured and launch successfully.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1717:OnlyFlagsEnumsShouldHavePluralNames")]
    [JsonConverter(typeof(EnumTolerantConverter<TransferRequestStates>))]
    public enum TransferRequestStates
    {
        /// <summary>
        /// Invalid transfer request object.
        /// </summary>
        None = 0,

        /// <summary>
        /// Transfer request initiated - approval is pending.
        /// </summary>
        Pending = 1,

        /// <summary>
        /// Transfer request has been approved - executing beginning.
        /// </summary>
        Approving = 2,

        /// <summary>
        /// Transfer request approval execution completed.
        /// </summary>
        Approved = 3,

        /// <summary>
        /// Transfer request cancelled.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Cancelled")]
        Cancelled = 4,

        /// <summary>
        /// Transfer request approval execution failed.
        /// </summary>
        Failed = 5
    }
}