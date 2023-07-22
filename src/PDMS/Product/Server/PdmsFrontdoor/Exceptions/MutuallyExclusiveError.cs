namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using Newtonsoft.Json;

    /// <summary>
    /// Identifies an invalid argument that violates the mutually exclusiveness with the source property.
    /// </summary>
    public class MutuallyExclusiveError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MutuallyExclusiveError" /> class.
        /// </summary>
        /// <param name="source">The source property name that causes the mutual exclusiveness.</param>
        public MutuallyExclusiveError(string source) : base("MutuallyExclusive")
        {
            this.Source = source;
        }

        /// <summary>
        /// Gets the source property name that causes the mutual exclusiveness.
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public string Source { get; private set; }
    }
}