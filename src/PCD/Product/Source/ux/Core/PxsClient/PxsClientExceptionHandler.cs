using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;

namespace Microsoft.PrivacyServices.UX.Core.PxsClient
{
    public class PxsClientExceptionHandler : AjaxErrorFilter.ICustomErrorHandler
    {
        public bool HandleAjaxError(ExceptionContext context, AjaxErrorFilter.JsonErrorModel errorResult)
        {
            switch (context.Exception)
            {
                case PrivacyOperation.Contracts.PrivacySubject.PrivacySubjectInvalidException exception:
                    errorResult.ErrorCode = "invalidInput";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    errorResult.Data = new Dictionary<string, string>()
                    {
                        { "target", exception.PropertyName }
                    };
                    return true;

                case PrivacyOperation.Contracts.PrivacySubject.PrivacySubjectIncompleteException exception:
                    errorResult.ErrorCode = "incompleteInput";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    return true;

                case PrivacyOperation.Client.PrivacyOperationClientException exception:
                    if (exception.Error.Code == PrivacyOperation.Contracts.ErrorCode.TimeWindowExpired.ToString())
                    {
                        errorResult.ErrorCode = "expiredMsaProxyTicket";
                        errorResult.HttpStatusCode = System.Net.HttpStatusCode.Forbidden;
                        errorResult.Data = new Dictionary<string, string>()
                        {
                            { "target",  "ProxyTicket" }
                        };
                        return true;
                    }
                    else if (exception.Error.Code == PrivacyOperation.Contracts.ErrorCode.InvalidClientCredentials.ToString())
                    {
                        errorResult.ErrorCode = "invalidMsaProxyTicket";
                        errorResult.HttpStatusCode = System.Net.HttpStatusCode.Forbidden;
                        errorResult.Data = new Dictionary<string, string>()
                        {
                            { "target",  "ProxyTicket" }
                        };
                        return true;
                    }
                    else if (exception.Error.Code == PrivacyOperation.Contracts.ErrorCode.Unauthorized.ToString())
                    {
                        errorResult.ErrorCode = "notAuthorized";
                        errorResult.HttpStatusCode = System.Net.HttpStatusCode.Forbidden;
                        return true;
                    }
                    return false;
            }

            return false;
        }
    }
}
