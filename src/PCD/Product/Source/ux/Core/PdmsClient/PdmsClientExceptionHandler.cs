using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Osgs.Infra.Monitoring.AspNetCore;
using PdmsClientBaseException = Microsoft.PrivacyServices.DataManagement.Client.BaseException;
using Microsoft.PrivacyServices.Identity;
using Microsoft.PrivacyServices.DataManagement.Client.V2;

namespace Microsoft.PrivacyServices.UX.Core.PdmsClient
{
    internal class PdmsClientExceptionHandler : AjaxErrorFilter.ICustomErrorHandler
    {
        #region AjaxErrorFilter.ICustomErrorHandler Members

        public bool HandleAjaxError(ExceptionContext context, AjaxErrorFilter.JsonErrorModel errorResult)
        {
            switch (context.Exception)
            {
                case PdmsClientBaseException exception:
                    return HandlePdmsClientBaseException(exception, errorResult);

                case ArgumentValidationException exception:
                    return HandleArgumentValidationException(exception, errorResult);
            }

            return false;
        }

        #endregion

        /// <summary>
        /// Handles <see cref="Microsoft.PrivacyServices.DataManagement.Client.BaseException"/>.
        /// </summary>
        private bool HandlePdmsClientBaseException(PdmsClientBaseException exception, AjaxErrorFilter.JsonErrorModel errorResult)
        {
            switch (exception)
            {
                case ExpiredError.ETagMismatch ex:
                    errorResult.ErrorCode = "eTagMismatch";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    errorResult.Data = new Dictionary<string, string>()
                    {
                        { "value",  ex.Value }
                    };
                    return true;

                case NotAuthenticatedError ex:
                    //  This case is only to provide more useful response status code.
                    errorResult.ErrorCode = "notAuthenticated";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.Unauthorized;
                    return true;

                case NotAuthorizedError.User.SecurityGroup ex:
                    errorResult.ErrorCode = "badSecurityGroups";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.Forbidden;
                    return true;

                case NotAuthorizedError.User.ServiceTree ex:
                    errorResult.ErrorCode = "badServiceTree";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.Forbidden;
                    return true;

                case NotAuthorizedError.User ex:
                    //  Catch-all for all unrecognized user authentication errors.
                    errorResult.ErrorCode = "notAuthorized";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.Forbidden;

                    if (ex.Role == "ServiceViewer")
                    {
                        errorResult.ErrorCode = $"{errorResult.ErrorCode}_serviceViewer";
                    }
                    return true;

                case BadArgumentError.InvalidArgument.UnsupportedCharacter ex:
                    errorResult.ErrorCode = "invalidCharacter";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    errorResult.Data = new Dictionary<string, string>()
                    {
                        { "target",  ex.Target }
                    };
                    return true;

                case BadArgumentError.InvalidArgument ex:
                    errorResult.ErrorCode = "invalidInput";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    errorResult.Data = new Dictionary<string, string>()
                    {
                        { "target",  ex.Target }
                    };
                    return true;

                case ConflictError.AlreadyExists.ClaimedByOwner ex:
                    errorResult.ErrorCode = "alreadyExists";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    errorResult.Data = new Dictionary<string, string>()
                    {
                        { "target",  ex.Target },
                        { "ownerId", ex.OwnerId }
                    };
                    return true;

                case ConflictError.AlreadyExists ex:
                    errorResult.ErrorCode = "alreadyExists";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    errorResult.Data = new Dictionary<string, string>()
                    {
                        { "target",  ex.Target }
                    };
                    return true;

                case ConflictError.DoesNotExist ex:
                    errorResult.ErrorCode = "doesNotExist";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
                    errorResult.Data = new Dictionary<string, string>()
                    {
                        { "target",  ex.Target }
                    };

                    if (ex.Target == "icm.connectorId")
                    {
                        errorResult.ErrorCode = $"{errorResult.ErrorCode}_icm";
                    }
                    return true;

                case ConflictError.InvalidValue.Immutable ex:
                    errorResult.ErrorCode = "immutableValue";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.Conflict;
                    return true;

                case ConflictError.LinkedEntityExists ex:
                    errorResult.ErrorCode = "hasDependentEntity";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.Conflict;
                    return true;

                case ConflictError.PendingCommandsExists ex:
                    errorResult.ErrorCode = "hasPendingCommands";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.Conflict;
                    return true;

                case NotFoundError pdmsException:
                case DataManagement.Client.ServiceTree.NotFoundError serviceTreeException:
                    errorResult.ErrorCode = "notFound";
                    errorResult.HttpStatusCode = System.Net.HttpStatusCode.NotFound;
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Handles <see cref="ArgumentValidationException"/>.
        /// </summary>
        private bool HandleArgumentValidationException(ArgumentValidationException exception, AjaxErrorFilter.JsonErrorModel errorResult)
        {
            errorResult.ErrorCode = "invalidInput";
            errorResult.HttpStatusCode = System.Net.HttpStatusCode.BadRequest;
            errorResult.Data = new Dictionary<string, string>()
            {
                { "target",  exception.ParamName },
                { "message", exception.Message }
            };

            return true;
        }
    }
}
