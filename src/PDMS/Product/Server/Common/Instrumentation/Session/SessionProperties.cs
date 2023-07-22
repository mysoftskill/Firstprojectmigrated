namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using Newtonsoft.Json;

    /// <summary>
    /// A set of standard properties that are persisted across an entire session.
    /// They are also passed into any nested sessions and are available to the IWriter classes.
    /// The value is passed using Dependency Injection. To access the current session properties
    /// the class should receive them as a constructor parameter.
    /// </summary>
    public class SessionProperties
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionProperties" /> class.
        /// </summary>
        /// <param name="cv">The CV object.</param>
        public SessionProperties(ICorrelationVector cv)
        {
            this.CV = cv;
        }

        public SessionProperties(ICorrelationVector cv, string user, string partner)
        {
            this.CV = cv;
            this.User = user;
            this.PartnerName = partner;
        }

        /// <summary>
        /// Gets or sets the user information.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the partner name.
        /// </summary>
        public string PartnerName { get; set; }

        /// <summary>
        /// Gets or sets the Correlation Context.
        /// </summary>
        public string CC { get; set; }

        /// <summary>
        /// Gets the CV.
        /// </summary>
        [JsonIgnore]
        public ICorrelationVector CV { get; private set; }
    }
}
