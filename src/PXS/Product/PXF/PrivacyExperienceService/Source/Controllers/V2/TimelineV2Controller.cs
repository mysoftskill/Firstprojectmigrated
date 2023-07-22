// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Results;
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.CommonSchema.Services.Logging;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Privacy.Core.Timeline;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Controllers.Helpers;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Extensions;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Service.Security;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     TimelineV2 Controller
    /// </summary>
    [Authorize]
    [CorrelationVectorRequired]
    public class TimelineV2Controller : MsaOnlyPrivacyController
    {
        private static readonly char[] commaSeparator = { ',' };

        private readonly IPrivacyConfigurationManager configurationManager;

        private readonly MediaTypeFormatter[] formatters;

        private readonly ILogger logger;

        private readonly IAppConfiguration appConfiguration;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TimelineV2Controller" /> class.
        /// </summary>
        public TimelineV2Controller(
            ITimelineService timelineService, 
            ILogger logger, 
            IPrivacyConfigurationManager configurationManager,
            IAppConfiguration appConfiguration)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            this.TimelineService = timelineService ?? throw new ArgumentNullException(nameof(timelineService)); ;
            this.configurationManager = configurationManager ?? throw new ArgumentNullException(nameof(configurationManager)); ;
            this.appConfiguration = appConfiguration ?? throw new ArgumentNullException(nameof(appConfiguration));
            this.formatters = new MediaTypeFormatter[]
            {
                new JsonMediaTypeFormatter
                {
                    SerializerSettings =
                    {
                        SerializationBinder = new TimelineCardBinder()
                    }
                }
            };
        }

        /// <summary>
        ///     Deletes a list of cards by the card's Id field.
        /// </summary>
        /// <group>Timeline V2</group>
        /// <verb>post</verb>
        /// <url>https://pxs.api.account.microsoft.com/v2/timeline</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <param in="body" name="ids">
        /// <see cref="List{T}"/>
        /// where T is <see cref="string"/>
        /// A list of card IDs.
        /// </param>
        /// <response code="200"></response>
        [HttpPost, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.Delete)]
        [Route(RouteNames.DeleteTimelineV2)]
        public async Task<IHttpActionResult> DeleteAsync([FromBody] List<string> ids)
        {
            // Give the API a name suffix to differentiate it.
            Sll.Context.Incoming.baseData.operationName = $"{Sll.Context.Incoming.baseData.operationName}ById";

            ServiceResponse response =
                await this.TimelineService.DeleteAsync(this.CurrentRequestContext, ids).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     Deletes a list of types by their Policy type Id for a given period of time.
        /// </summary>
        /// <group>Timeline V2</group>
        /// <verb>delete</verb>
        /// <url>https://pxs.api.account.microsoft.com/v2/timeline</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <param in="query" name="types" cref="string">A comma separated list of data types.</param>
        /// <param in="query" name="period"><see cref="TimeSpan"/>The timespan to for the delete to be performed.</param>
        /// <response code="200"></response>
        [HttpDelete, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.Delete)]
        [Route(RouteNames.DeleteTimelineV2)]
        public async Task<IHttpActionResult> DeleteAsync(string types, TimeSpan period)
        {
            ServiceResponse response =
                await this.TimelineService.DeleteAsync(
                        this.CurrentRequestContext,
                        types.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries).ToList(),
                        period,
                        PortalHelper.DeducePortal(this, this.User.Identity, this.configurationManager.PrivacyExperienceServiceConfiguration.SiteIdToCallerName))
                    .ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     Gets the timeline cards.
        /// </summary>
        /// <group>Timeline V2</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v3/timeline</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <param in="query" required="true" name="cardTypes" cref="string">A comma separated list of the card types to get.</param>
        /// <param in="query" required="true" name="timeZoneOffset"><see cref="TimeSpan"/>The time span to get cards for.</param>
        /// <param in="query" required="false" name="startingAt"><see cref="DateTimeOffset"/>The starting time.</param>
        /// <param in="query" required="false" name="count" cref="int">The count of cards to get.</param>
        /// <param in="query" required="false" name="deviceIds" cref="string">A comma separated list of device IDs.</param>
        /// <param in="query" required="false" name="sources" cref="string">A comma separated list of sources.</param>
        /// <param in="query" required="false" name="search" cref="string">Search parameter.</param>
        /// <param in="query" required="false" name="nextToken" cref="string">Token to get the next set of cards.</param>
        /// <response code="200">
        /// <see cref="PagedResponse{T}"/>
        /// where T is <see cref="TimelineCard"/>
        /// Timeline cards.
        /// </response>
        [HttpGet, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.GetTimelineV3)]
        public async Task<IHttpActionResult> GetV3Async(
            string cardTypes,
            TimeSpan timeZoneOffset,
            DateTimeOffset? startingAt = null,
            int? count = null,
            string deviceIds = null,
            string sources = null,
            string search = null,
            string nextToken = null)
        {
            return this.CreateV3HttpActionResult(
                await this.GetResponseAsync(cardTypes, timeZoneOffset, startingAt, count, deviceIds, sources, search, nextToken).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets the aggregate count of timeline cards
        /// </summary>
        /// <param in="query" required="true" name="cardTypes" cref="string">A comma separated list of case-sensitive card types <see cref="TimelineCard.CardTypes"/> to get.</param>
        /// <response code="200">
        /// <see cref="ServiceResponse{T}"/>
        /// where T is <see cref="AggregateCountResponse"/>
        /// count of items for each card type.
        /// </response>
        [HttpGet, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.GetTimelineAggregateCount)]
        public async Task<IHttpActionResult> GetAggregateCountAsync(
            string cardTypes
            )
        {
            var flightEnabled = this.appConfiguration.GetConfigValue<bool>(ConfigNames.PXS.TimelineAggregateCountAPIEnabled);
            this.logger.Information(nameof(TimelineV2Controller), $"Status of flight PXS.TimelineAggregateCountAPIEnabled: {flightEnabled}");

            if (!flightEnabled)
            {
                var notImplementedError = new Error()
                { 
                   Code = "NotImplemented", 
                   Message = "The flight for this API is currently not enabled",
                };

                var serviceResponseError = new ServiceResponse()
                {
                    Error = notImplementedError
                };

                return this.CreateHttpActionResult(serviceResponseError);
            }
            return this.CreateV3HttpActionResult(await this.GetAggregateCountResponseAsync(cardTypes).ConfigureAwait(false));
        }

        /// <summary>
        ///     Gets the timeline voice card audio
        /// </summary>
        /// <group>Timeline V2</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v2/voicecardaudio</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <param in="query" name="id" cref="string">The card ID.</param>
        /// <response code="200"><see cref="VoiceCardAudio"/></response>
        [HttpGet, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.GetVoiceCardAudioV2)]
        public async Task<IHttpActionResult> GetVoiceCardAudioAsync(string id)
        {
            ServiceResponse<VoiceCardAudio> response = await this.TimelineService.GetVoiceCardAudioAsync(this.CurrentRequestContext, id).ConfigureAwait(false);

            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        ///     Warms up the timeline cards. (They sometimes are too cold)
        /// </summary>
        /// <group>Timeline V2</group>
        /// <verb>get</verb>
        /// <url>https://pxs.api.account.microsoft.com/v1/warmup-timeline</url>        
        /// <header name="X-Family-Json-Web-Token" cref="string" required="false">The family JWT with information required for on behalf of.</header>
        /// <header name="client-request-id" cref="string" required="false">The client activity ID.</header>
        /// <header name="MS-CV" cref="string" required="false">The correlation vector for the request.</header>
        /// <header name="Correlation-Context" cref="string" required="false">The correlation context for the request.</header>
        /// <header name="X-Flights" cref="string" required="false">The flights to apply to the request applied.</header>
        /// <header name="server-machine-id" cref="string" required="false">The server machine ID.</header>
        /// <header name="X-S2S-Proxy-Ticket" cref="string" required="true">The MSA User Proxy Ticket.</header>
        /// <header name="X-S2S-Access-Token" cref="string" required="true">The MSA S2S Access Token.</header>
        /// <response code="200"></response>
        [HttpGet, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.WarmupTimelineV1)]
        public IHttpActionResult WarmupV1()
        {
            if (IsCallerBlocked())
            {
                return this.CreateHttpActionResult(new ServiceResponse()
                    { Error = new Error(ErrorCode.Unauthorized, ErrorMessages.TooManyRequests) }
                );
            }

            // Disable the warning about not awaiting the task.
#pragma warning disable 4014
            this.TimelineService.WarmupAsync(this.CurrentRequestContext)
                .ContinueWith(
#pragma warning restore 4014
                    t =>
                    {
                        try
                        {
                            ServiceResponse _ = t.Result;
                        }
                        catch (Exception ex)
                        {
                            // Swallow this safely. This is a best effort API.
                            this.logger.Information(nameof(TimelineV2Controller), ex, "Exception during warmup");
                        }
                    });

            // Return unconditional success
            return this.CreateHttpActionResult(new ServiceResponse());
        }

        /// <summary>
        /// Gets ALL recurring deletes records for given PUID.
        /// </summary>
        /// <response code="200">
        /// <see cref="IList{T}"/>
        /// where T is <see cref="GetRecurringDeleteResponse"/>
        /// Recurring delete records.
        /// </response>
        [HttpGet, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.RecurringDeletesV1)]
        public async Task<IHttpActionResult> GetRecurringDeletesAsync()
        {
            var featureFlag = FeatureNames.PXS.RecurringDeleteAPIEnabled;
            if (!(await this.IsApiFlightEnabledAsync(featureFlag)))
            {
                ServiceResponse errorResponse = this.GetServiceResponseErrorApiIsNotEnabled(featureFlag, nameof(GetRecurringDeletesAsync));
                return this.CreateHttpActionResult(errorResponse);
            }

            int maxNumberOfRetries = this.configurationManager.RecurringDeleteWorkerConfiguration.ScheduleDbConfig.MaxNumberOfRetries;

            var response = 
                await this.TimelineService.GetRecurringDeletesAsync(this.CurrentRequestContext, maxNumberOfRetries).ConfigureAwait(false);


            return this.CreateV3HttpActionResult(response);
        }

        /// <summary>
        /// Delete recurring deletes record for given puid and datatype.
        /// </summary>
        /// <param in="query" name="dataType" cref="string">The data type name.</param>
        /// <response code="200">
        /// Recurring delete records.
        /// </response>
        [HttpDelete, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.RecurringDeletesV1)]
        public async Task<IHttpActionResult> DeleteRecurringDeletesAsync(string dataType)
        {
            var featureFlag = FeatureNames.PXS.RecurringDeleteAPIEnabled;
            if (!(await this.IsApiFlightEnabledAsync(featureFlag)))
            {
                ServiceResponse errorResponse = this.GetServiceResponseErrorApiIsNotEnabled(featureFlag, nameof(DeleteRecurringDeletesAsync));
                return this.CreateHttpActionResult(errorResponse);
            }

            var response =
                await this.TimelineService.DeleteRecurringDeletesAsync(this.CurrentRequestContext, dataType).ConfigureAwait(false);


            return this.CreateHttpActionResult(response);
        }

        /// <summary>
        /// Create a new recurring delete record or update existing one.
        /// </summary>
        /// <param in="query" name="dataType" cref="string">The data type name.</param>
        /// <param in="query" name="nextDeleteDate" cref="DateTimeOffset">When to run it next time.</param>
        /// <param in="query" name="recurringIntervalDays" cref="RecurringIntervalDays">Recurring delete interval.</param>
        /// <param in="query" name="status" cref="RecurrentDeleteStatus">Status of recurring deletes.</param>
        /// <response code="200">
        /// <see cref="ServiceResponse{T}"/>
        /// where T is <see cref="GetRecurringDeleteResponse"/>
        /// Recurring delete records.
        /// </response>
        [HttpPost, PrivacyExperienceAgeAuthZAuthorization(typeof(AgeAuthZLegalAgeGroup), PrivacyAction.View)]
        [Route(RouteNames.RecurringDeletesV1)]
        public async Task<IHttpActionResult> CreateOrUpdateRecurringDeletesAsync(
            string dataType, 
            DateTimeOffset nextDeleteDate, 
            RecurringIntervalDays recurringIntervalDays, 
            RecurrentDeleteStatus status)
        {
            var featureFlag = FeatureNames.PXS.RecurringDeleteAPIEnabled;
            if (!(await this.IsApiFlightEnabledAsync(featureFlag)))
            {
                ServiceResponse errorResponse = this.GetServiceResponseErrorApiIsNotEnabled(featureFlag, nameof(CreateOrUpdateRecurringDeletesAsync));
                return this.CreateHttpActionResult(errorResponse);
            }

            var response =
                await this.TimelineService.CreateOrUpdateRecurringDeletesAsync(
                    this.CurrentRequestContext, 
                    dataType,
                    nextDeleteDate,
                    recurringIntervalDays,
                    status,
                    this.configurationManager.RecurringDeleteWorkerConfiguration).ConfigureAwait(false);


            return this.CreateHttpActionResult(response);
        }

        private ServiceResponse GetServiceResponseErrorApiIsNotEnabled(string featureFlight, string apiName)
        {
            var notImplementedError = new Error()
            {
                Code = "NotImplemented",
                Message = $"{apiName} is currently not enabled. Featuare flight: {featureFlight}",
            };

            var serviceResponseError = new ServiceResponse()
            {
                Error = notImplementedError
            };

            return serviceResponseError;
        }

        private async Task<bool> IsApiFlightEnabledAsync(string featureFlag)
        {
            var flightEnabled = await  this.appConfiguration.IsFeatureFlagEnabledAsync(featureFlag).ConfigureAwait(false);
            this.logger.Information(nameof(TimelineV2Controller), $"{featureFlag} is enabled: {flightEnabled}");

            return flightEnabled;
        }

        private ITimelineService TimelineService { get; }

        private IHttpActionResult CreateV3HttpActionResult<T>(ServiceResponse<T> serviceResponse)
        {
            if (serviceResponse == null)
            {
                return this.InternalServerError();
            }

            if (serviceResponse.IsSuccess)
            {
                return new OkNegotiatedContentResult<T>(
                    serviceResponse.Result,
                    this.Configuration.Services.GetContentNegotiator(),
                    this.Request,
                    this.formatters);
            }

            return this.ResponseMessage(this.Request.CreateErrorResponse(serviceResponse.Error));
        }

        private async Task<ServiceResponse<AggregateCountResponse>> GetAggregateCountResponseAsync(
            string cardTypes)
        {
            if (cardTypes == null)
            {
                throw new ArgumentNullException(nameof(cardTypes));
            }
            this.logger.Information(nameof(TimelineV2Controller), $"Card types are: {cardTypes}");

            ServiceResponse<AggregateCountResponse> response = await this.TimelineService.GetAggregateCountAsync(
                    this.CurrentRequestContext, 
                    cardTypes.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries).ToList())
                .ConfigureAwait(false);
            return response;
        }

        private async Task<ServiceResponse<PagedResponse<TimelineCard>>> GetResponseAsync(
            string cardTypes,
            TimeSpan timeZoneOffset,
            DateTimeOffset? startingAt,
            int? count,
            string deviceIds,
            string sources,
            string search,
            string nextToken)
        {
            if (cardTypes == null)
            {
                throw new ArgumentNullException(nameof(cardTypes));
            }

            if (startingAt == null)
            {
                startingAt = DateTimeOffset.UtcNow;
            }

            ServiceResponse<PagedResponse<TimelineCard>> response =
                await this.TimelineService.GetAsync(
                        this.CurrentRequestContext,
                        cardTypes.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries).ToList(),
                        count,
                        (deviceIds ?? string.Empty).Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries).ToList(),
                        sources?.Split(commaSeparator, StringSplitOptions.RemoveEmptyEntries).ToList(),
                        search,
                        timeZoneOffset,
                        startingAt.Value,
                        nextToken)
                    .ConfigureAwait(false);

            return response;
        }

        private bool IsCallerBlocked()
        {
            string id = this.User?.Identity?.Name;
            return !string.IsNullOrWhiteSpace(id) &&
                appConfiguration.IsFeatureFlagEnabledAsync<ICustomOperatorContext>(
                    FeatureNames.TimelineApiBlocked,
                    CustomOperatorContextFactory.CreateDefaultStringComparisonContext(id)).GetAwaiter().GetResult();
        }
    }
}
