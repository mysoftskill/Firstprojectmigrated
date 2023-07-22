namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    /// <summary>
    /// This is the standard inner error format.
    /// It does not include any service defined values.
    /// </summary>    
    public class StandardInnerError : InnerError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardInnerError" /> class.
        /// </summary>
        /// <param name="code">The more specific error code.</param>
        public StandardInnerError(string code) : base(code)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardInnerError" /> class.
        /// </summary>
        /// <param name="code">The more specific error code.</param>
        /// <param name="innerError">A nested inner error.</param>
        public StandardInnerError(string code, InnerError innerError) : base(code)
        {
            this.NestedError = innerError;
        }
    }
}