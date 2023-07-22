namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening
{
    using System;
    using System.Web.Http.Controllers;

    /// <summary>
    /// An action selector that catches any routing errors and converts them into handled errors.
    /// </summary>
    public class ErrorAwareActionSelector : ApiControllerActionSelector
    {
        /// <summary>
        /// Wraps standard action selection with an error handler.
        /// </summary>
        /// <param name="controllerContext">The controller information.</param>
        /// <returns>The action.</returns>
        public override HttpActionDescriptor SelectAction(HttpControllerContext controllerContext)
        {
            try
            {
                return base.SelectAction(controllerContext);
            }
            catch (Exception ex)
            {
                throw new InvalidRequestError(ex.Message);                
            }
        }
    }
}