// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http.Controllers;
    using System.Web.Http.Filters;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;

    /// <summary>
    ///     Requires a correlation vector be present in the request to authorize the request
    /// </summary>
    public class CorrelationVectorRequiredAttribute : AuthorizationFilterAttribute
    {
        /// <inheritdoc />
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            if (actionContext.Request?.Headers != null && actionContext.Request.Headers.TryGetValues(CorrelationVector.HeaderName, out IEnumerable<string> correlationVectors))
            {
                string correlationVector = correlationVectors.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(correlationVector))
                {
                    return;
                }
            }

            var missingCvErrorMessage = $"Request header did not contain a CV in the header: {CorrelationVector.HeaderName}";
            var error = new Error(ErrorCode.InvalidInput, missingCvErrorMessage);
            actionContext.Response = actionContext.Request.CreateErrorResponse(error);
        }
    }
}
