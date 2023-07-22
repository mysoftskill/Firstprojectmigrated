// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Principal;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;

    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Common.Logging.LogicalOperations;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.PrivacyServices.Common.Azure;

    using HeaderNames = Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.HeaderNames;

    /// <summary>
    ///     Helper for logging incoming logical operations handler
    /// </summary>
    public class IncomingLogicalOperationHandler : DelegatingHandler
    {
        private const string ComponentName = nameof(IncomingLogicalOperationHandler);

        private readonly IMachineIdRetriever machineIdRetriever;

        /// <summary>
        ///     Creates a new IncomingLogicalOperationHandler
        /// </summary>
        /// <param name="machineIdRetriever"></param>
        public IncomingLogicalOperationHandler(IMachineIdRetriever machineIdRetriever)
        {
            this.machineIdRetriever = machineIdRetriever;
        }

        /// <summary>
        ///     Handler for the operation
        /// </summary>
        /// <param name="request">Request message</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Response</returns>
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string apiName = request.GetApiName();
            if (apiName == ApiRouteMapping.DefaultApiName)
            {
                IfxTraceLogger logger = IfxTraceLogger.Instance;
                logger.Warning(nameof(IncomingLogicalOperationHandler), $"No API Name found for {request.RequestUri.AbsolutePath}");
            }

            HttpResponseMessage response;

            // If an API is not named or if it's KeepAlive, then we do not track it
            if (HttpRequestExtensions.IsDefaultOrKeepAlive(apiName))
            {
                // Immediately execute the rest of the pipeline, skipping the tracking logic
                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                AddDiagnosticInformation(response, this.machineIdRetriever.ReadMachineId());
                return response;
            }

            // Activity Id must be set prior to creating the SLL Api Event, otherwise a new Activity Id is generated.
            Guid? clientActivityId = GetClientActivityId(request);
            SetTraceActivityId(clientActivityId);

            IncomingApiEventWrapper apiEvent = CreateAndStartApiEvent(
                request,
                apiName,
                clientActivityId != null ? clientActivityId.ToString() : string.Empty);

            try
            {
                InitializeSllContextVector(request);
                InitializeSllCorrelationContext(request);

                response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                AddDiagnosticInformation(response, this.machineIdRetriever.ReadMachineId());

                EndApiEventNormally(apiEvent, response);
            }
            catch (Exception exception)
            {
                EndApiEventWithException(apiEvent, request, exception);
                throw;
            }

            return response;
        }

        private static void AddDiagnosticInformation(HttpResponseMessage response, string machineId)
        {
            if (response == null)
            {
                return;
            }

            response.Headers.Add(HeaderNames.ServerVersion, GetServiceVersion());

            if (!string.IsNullOrWhiteSpace(machineId))
            {
                response.Headers.Add(HeaderNames.MachineId, machineId);
            }
        }

        private static IncomingApiEventWrapper CreateAndStartApiEvent(HttpRequestMessage request, string apiName, string clientActivityId)
        {
            var apiEvent = new IncomingApiEventWrapper();
            apiEvent.TargetUri = request.RequestUri.ToString();
            apiEvent.RequestMethod = request.Method.ToString();
            apiEvent.Protocol = request.RequestUri.Scheme;
            if (request.Headers?.Contains(HeaderNames.Flights) ?? false)
                apiEvent.Flights = string.Join(",", request.Headers?.GetValues(HeaderNames.Flights) ?? Enumerable.Empty<string>());
            else
                apiEvent.Flights = "(NotProvided)";

            // IncomingApiEventWrapper doesn't find the clientActivityId in the header for requests in this service, this is indicated by the value being a Guid.Empty.
            // So to work around this, set the property here if the value is specified as a method param.
            if (!string.IsNullOrWhiteSpace(clientActivityId) && string.Equals(Guid.Empty.ToString(), apiEvent.ClientActivityId))
            {
                apiEvent.ClientActivityId = clientActivityId;
            }

            apiEvent.Start(apiName);

            return apiEvent;
        }

        private static void EndApiEventCommon(IncomingApiEventWrapper apiEvent, HttpRequestMessage request)
        {
            HttpRequestContext requestContext = request.GetRequestContext();
            if (requestContext != null && requestContext.Principal != null && requestContext.Principal.Identity != null)
            {
                IIdentity identity = requestContext.Principal.Identity;
                apiEvent.Authentication = identity.AuthenticationType;
                if (identity.IsAuthenticated)
                {
                    MsaSelfIdentity selfIdentity;
                    MsaSiteIdentity siteIdentity;
                    AadIdentity aadIdentity;
                    VortexPrincipal vortexPrincipal;
                    if ((selfIdentity = identity as MsaSelfIdentity) != null)
                    {
                        apiEvent.SetUserId(selfIdentity.AuthorizingPuid);
                        apiEvent.CallerName = selfIdentity.CallerNameFormatted;
                    }
                    else if ((siteIdentity = identity as MsaSiteIdentity) != null)
                    {
                        apiEvent.CallerName = siteIdentity.CallerNameFormatted;
                    }
                    else if ((aadIdentity = identity as AadIdentity) != null)
                    {
                        apiEvent.SetAadUserId(aadIdentity.ObjectId.ToString());
                        apiEvent.CallerName = aadIdentity.CallerNameFormatted;
                    }
                    else if ((vortexPrincipal = requestContext.Principal as VortexPrincipal) != null)
                    {
                        apiEvent.CallerName = vortexPrincipal.CallerNameFormatted;
                    }
                    else
                    {
                        apiEvent.CallerName = "UnknownIdentityType";
                    }
                }
                else
                {
                    apiEvent.CallerName = "Unauthenticated";
                }
            }
        }

        private static void EndApiEventNormally(IncomingApiEventWrapper apiEvent, HttpResponseMessage response)
        {
            PopulateErrorDetails(apiEvent, response);
            EndApiEventCommon(apiEvent, response.RequestMessage);
            apiEvent.ProtocolStatusCode = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
            apiEvent.Success = response.IsSuccessStatusCode;
            apiEvent.Finish();
        }

        private static void EndApiEventWithException(IncomingApiEventWrapper apiEvent, HttpRequestMessage request, Exception exception)
        {
            EndApiEventCommon(apiEvent, request);
            apiEvent.ProtocolStatusCode = ((int)HttpStatusCode.InternalServerError).ToString(CultureInfo.InvariantCulture);

            apiEvent.ErrorMessage = exception.Message;

            apiEvent.Success = false;
            apiEvent.Finish();
        }

        /// <summary>
        ///     Retrieves the client Activity Id if present on the request.
        /// </summary>
        /// <param name="request">Http Request</param>
        /// <returns>Client activity id</returns>
        private static Guid? GetClientActivityId(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            IEnumerable<string> headerValues;
            request.Headers.TryGetValues(HeaderNames.ClientRequestId, out headerValues);

            if (headerValues == null)
            {
                return null;
            }

            string activityIdHeaderValue = headerValues.FirstOrDefault();

            Guid activityId;
            if (string.IsNullOrEmpty(activityIdHeaderValue) || !Guid.TryParse(activityIdHeaderValue, out activityId))
            {
                return null;
            }

            return activityId;
        }

        /// <summary>
        ///     Retrieves the service version (file version of the DLL). This version is set during a rolling build.
        /// </summary>
        /// <returns>File Version of the WebApiApplication</returns>
        private static string GetServiceVersion()
        {
            string serviceVersion = string.Empty;
            IfxTraceLogger logger = IfxTraceLogger.Instance;

            if (logger == null)
            {
                throw new MemberAccessException("logger has not yet been resolved");
            }

            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
                serviceVersion = fileVersionInfo.FileVersion;
            }
            catch (FileNotFoundException exception)
            {
                // This sort of error should not prevent the service from running
                logger.Error(ComponentName, exception, "Unable to obtain service version number.");
            }

            return serviceVersion;
        }

        /// <summary>
        ///     Initializes the SLL Context correlation vector for the current request by obtaining it from the request-header if present.
        ///     For some api's, the CV is optional.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        internal static void InitializeSllContextVector(HttpRequestMessage request)
        {
            if (request.Headers != null && request.Headers.Any())
            {
                if (request.Headers.TryGetValues(CorrelationVector.HeaderName, out IEnumerable<string> correlationVectors))
                {
                    string correlationVector = correlationVectors.FirstOrDefault();

                    // Creates a new correlation vector by extending an existing value, or creating a new one.
                    if (!string.IsNullOrWhiteSpace(correlationVector))
                    {
                        Sll.Context.Vector = CorrelationVector.Extend(correlationVector);
                        return;
                    }
                }
            }

            Sll.Context.Vector = new CorrelationVector();
        }

        internal static void InitializeSllCorrelationContext(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues(CorrelationContext.HeaderName, out IEnumerable<string> correlationContexts))
            {
                Sll.Context.CorrelationContext = new CorrelationContext(correlationContexts.FirstOrDefault());
            }
            else
            {
                Sll.Context.CorrelationContext = new CorrelationContext();
            }
        }

        private static void PopulateErrorDetails(IncomingApiEventWrapper apiEvent, HttpResponseMessage response)
        {
            if (apiEvent == null || response == null)
            {
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                Error error;
                if (response.TryGetContentValue(out error))
                {
                    if (error != null)
                    {
                        apiEvent.ErrorCode = error.Code;
                        apiEvent.ErrorMessage = error.Message;
                    }
                }
                else
                {
                    apiEvent.ErrorCode = "Unknown";
                }
            }
        }

        /// <summary>
        ///     Sets the Trace.CorrelationManager.ActivityId to the client activity guid (if it exists), or generates a new one.
        /// </summary>
        /// <param name="clientActivityId">Client activity id (if available)</param>
        private static void SetTraceActivityId(Guid? clientActivityId)
        {
            if (!Guid.Empty.Equals(Trace.CorrelationManager.ActivityId))
            {
                return;
            }

            Guid traceActivityId;

            // Try to use the clientActivityId, otherwise assign a new ID
            if (clientActivityId.HasValue && clientActivityId.Value != Guid.Empty)
            {
                traceActivityId = clientActivityId.Value;
            }
            else
            {
                traceActivityId = Guid.NewGuid();
            }

            Trace.CorrelationManager.ActivityId = traceActivityId;
        }
    }
}
