namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    /// <summary>
    /// Identifies a service tree data owner not found error.
    /// </summary>
    public class DataOwnerNotFoundError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataOwnerNotFoundError" /> class.
        /// </summary>
        public DataOwnerNotFoundError() : base("DataOwner")
        {
        }
    }
}