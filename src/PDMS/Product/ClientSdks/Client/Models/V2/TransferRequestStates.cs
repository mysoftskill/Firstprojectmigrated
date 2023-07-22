namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// This enum type represents the state of a transfer request.
    /// </summary>
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
        Cancelled = 4,

        /// <summary>
        /// Transfer request approval execution failed.
        /// </summary>
        Failed = 5
    }
}