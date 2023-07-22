namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Newtonsoft.Json;

    /// <summary>
    /// The set of registration states.
    /// </summary>
    [JsonConverter(typeof(EnumTolerantConverter<RegistrationState>))]
    public enum RegistrationState
    {
        /// <summary>
        /// Indicates an invalid registration.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// Indicates a valid registration.
        /// </summary>
        Valid = 1,

        /// <summary>
        /// Indicates something is deprecated.
        /// </summary>
        Deprecated = 2,

        /// <summary>
        /// Indicates something is missing.
        /// </summary>
        Missing = 3,

        /// <summary>
        /// Indicates something is only partially correct.
        /// </summary>
        Partial = 4,

        /// <summary>
        /// Indicates a valid registration but results were truncated because they were too large.
        /// As such, there may be other invalid data that is hidden.
        /// </summary>
        ValidButTruncated = 5,

        /// <summary>
        /// Indicates a registration that is not subject to DSR requests.
        /// </summary>
        NotApplicable = 6
    }
}