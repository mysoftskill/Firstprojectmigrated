namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    /// <summary>
    /// Identifies an invalid argument that has a specific character that is not allowed.
    /// </summary>
    public class UnsupportedCharacterError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnsupportedCharacterError" /> class.
        /// </summary>
        public UnsupportedCharacterError() : base("UnsupportedCharacter")
        {
        }
    }
}