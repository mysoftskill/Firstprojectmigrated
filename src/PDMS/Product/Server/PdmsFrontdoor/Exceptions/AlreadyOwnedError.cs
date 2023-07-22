namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using Newtonsoft.Json;

    /// <summary>
    /// Identifies an invalid argument that violates the mutually exclusiveness with the source property.
    /// </summary>
    public class AlreadyOwnedError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlreadyOwnedError" /> class.
        /// </summary>
        /// <param name="ownerId">The current owner of the entity.</param>
        public AlreadyOwnedError(string ownerId) : base("ClaimedByOwner")
        {
            this.OwnerId = ownerId;
        }

        /// <summary>
        /// Gets the owner of the entity.
        /// </summary>
        [JsonProperty(PropertyName = "ownerid")]
        public string OwnerId { get; private set; }
    }
}
