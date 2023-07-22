namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using Microsoft.AspNet.OData.Routing;
    using Microsoft.AspNet.OData.Routing.Template;

    /// <summary>
    /// Wraps OData path management with controlled error handling.
    /// </summary>
    public class ErrorAwareODataPathHandler : DefaultODataPathHandler
    {
        /// <summary>
        /// Wraps path parsing with controlled error handling.
        /// </summary>
        /// <param name="serviceRoot">The service root.</param>
        /// <param name="odataPath">The OData path.</param>
        /// <param name="requestContainer">The request container.</param>
        /// <returns>The parsed path.</returns>
        public override ODataPath Parse(string serviceRoot, string odataPath, IServiceProvider requestContainer)
        {
            try
            {
                var path = base.Parse(serviceRoot, odataPath, requestContainer);
                return path;
            }
            catch (Exception ex)
            {
                throw new InvalidRequestError(ex.Message);
            }
        }

        /// <summary>
        /// Wraps template parsing with controlled error handling.
        /// </summary>
        /// <remarks>
        /// This function is excluded from code coverage because it is not known how to execute it.
        /// </remarks>
        /// <param name="odataPathTemplate">The path template.</param>
        /// <param name="requestContainer">The request container.</param>
        /// <returns>The parsed template.</returns>
        [ExcludeFromCodeCoverage]
        public override ODataPathTemplate ParseTemplate(string odataPathTemplate, IServiceProvider requestContainer)
        {
            try
            {
                var template = base.ParseTemplate(odataPathTemplate, requestContainer);
                return template;
            }
            catch (Exception ex)
            {
                throw new InvalidRequestError(ex.Message);
            }
        }
    }
}