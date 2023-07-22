namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using Newtonsoft.Json;

    /// <summary>
    /// Identifies a security group user not authorized error.
    /// </summary>
    public class SecurityGroupNotAuthorizedError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecurityGroupNotAuthorizedError" /> class.
        /// </summary>
        /// <param name="securityGroups">The security groups that the user is not part of.</param>
        public SecurityGroupNotAuthorizedError(string securityGroups) : base("SecurityGroup")
        {
            this.SecurityGroups = securityGroups;
        }

        /// <summary>
        /// Gets or sets the security groups that the user is not part of.
        /// </summary>
        [JsonProperty(PropertyName = "securityGroups")]
        public string SecurityGroups { get; set; }
    }
}