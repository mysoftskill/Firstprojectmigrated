namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening
{
    using System;
    using System.Net.Http;

    using Microsoft.AspNet.OData;
    using Microsoft.AspNet.OData.Query;

    /// <summary>
    /// Wraps the standard OData attribute with error handling.
    /// </summary>
    public class ErrorAwareEnableQuery : EnableQueryAttribute
    {
        /// <summary>
        /// Wraps the method with stronger error handling.
        /// </summary>
        /// <param name="request">The parameter is not used.</param>
        /// <param name="queryOptions">The parameter is not used.</param>
        public override void ValidateQuery(HttpRequestMessage request, ODataQueryOptions queryOptions)
        {
            try
            {
                base.ValidateQuery(request, queryOptions);
            }
            catch (Exception ex)
            {
                throw new InvalidRequestError(ex.Message);
            }
        }
    }
}