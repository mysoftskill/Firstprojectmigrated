namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions
{
    using System;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Web.Http.ModelBinding;

    /// <summary>
    /// Represents an error thrown by a web API controller due to a bad model binding.
    /// </summary>
    [Serializable]
    public class InvalidModelError : InvalidRequestError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidModelError" /> class.
        /// </summary>
        /// <param name="modelState">An invalid model state from a controller.</param>
        public InvalidModelError(ModelStateDictionary modelState)
            : base(GetMessage(modelState), new StandardInnerError("InvalidModelBinding"))
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidModelError" /> class.
        /// Required by ISerializable.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The serialization context.</param>
        public InvalidModelError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Finds the error message from an invalid ModelState.
        /// </summary>
        /// <param name="modelState">The invalid model state.</param>
        /// <returns>The error message.</returns>
        public static string GetMessage(ModelStateDictionary modelState)
        {
            var modelErrors =
                from state in modelState
                where state.Value.Errors != null && state.Value.Errors.Any()
                select state.Value.Errors.First();

            var modelErrorExn = modelErrors.FirstOrDefault(m => m.Exception != null);

            return modelErrorExn?.Exception.Message ?? "Invalid model state.";
        }
    }
}