// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.UserSettings
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Common.Logging;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;

    using Newtonsoft.Json.Linq;

    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    ///     UserSettingsService
    /// </summary>
    public class UserSettingsService : IUserSettingsService
    {
        private const string ComponentName = nameof(UserSettingsService);

        private const string ErrorMessageFailedToCreateProfile = "Failed to create profile";

        private readonly ICustomerMasterAdapter customerMasterAdapter;

        private readonly ILogger logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserSettingsService" /> class.
        /// </summary>
        /// <param name="customerMasterAdapter">The customer master adapter.</param>
        /// <param name="logger">The logger.</param>
        public UserSettingsService(ICustomerMasterAdapter customerMasterAdapter, ILogger logger)
        {
            this.customerMasterAdapter = customerMasterAdapter;
            this.logger = logger;
        }

        /// <inheritdoc />
        public async Task<ServiceResponse<ResourceSettingV1>> GetAsync(IRequestContext requestContext)
        {
            var serviceResponse = new ServiceResponse<ResourceSettingV1>();
            IPxfRequestContext adapterRequestContext = requestContext.ToAdapterRequestContext();

            // Get
            AdapterResponse<PrivacyProfile> getProfileResponse = await this.customerMasterAdapter.GetPrivacyProfileAsync(adapterRequestContext).ConfigureAwait(false);

            if (getProfileResponse.IsSuccess)
            {
                if (this.DoesPrivacyProfileExist(getProfileResponse.Result))
                {
                    PrivacyProfile updatedProfile = getProfileResponse.Result;
                    if (DoesPrivacyProfileNeedUpdates(requestContext, getProfileResponse.Result, out PrivacyProfile tempProfile))
                    {
                        ServiceResponse<PrivacyProfile> cmProfile = await this.PerformPrivacyProfileUpdateAsync(requestContext, tempProfile).ConfigureAwait(false);
                        if (!cmProfile.IsSuccess)
                        {
                            serviceResponse.Error = cmProfile.Error;
                            return serviceResponse;
                        }

                        updatedProfile = cmProfile.Result;
                    }

                    serviceResponse.Result = updatedProfile?.ToPrivacyUserSettings();
                    return serviceResponse;
                }

                string message = "Privacy profile not found for user, it must be created to get the profile.";
                this.logger.Information(ComponentName, message);
                serviceResponse.Error = new Error(ErrorCode.ResourceNotFound, message);
                return serviceResponse;
            }
            else
            {
                serviceResponse.Error = new Error(ErrorCode.PartnerError, getProfileResponse.Error.ToString());
                return serviceResponse;
            }
        }

        /// <summary>
        ///     Gets the user settings asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <returns>Task response contains a <see cref="ResourceSettingV1" />, or <see cref="Error" /></returns>
        public async Task<ServiceResponse<ResourceSettingV1>> GetOrCreateAsync(IRequestContext requestContext)
        {
            IPxfRequestContext adapterRequestContext = requestContext.ToAdapterRequestContext();

            // Get
            ServiceResponse<ResourceSettingV1> serviceResponse = await this.GetAsync(requestContext).ConfigureAwait(false);

            if (serviceResponse?.IsSuccess == true || serviceResponse?.Error.Code?.Equals(ErrorCode.ResourceNotFound.ToString()) == false)
            {
                return serviceResponse;
            }

            // Create
            // If the profile does not exist, create it.
            AdapterResponse<PrivacyProfile> creationResponse = await this.customerMasterAdapter.CreatePrivacyProfileAsync(
                adapterRequestContext,
                DefaultProfileSettingsFactory.CreateDefaultPrivacyProfile()).ConfigureAwait(false);

            if (serviceResponse == null || serviceResponse.Error != null)
            {
                // reset service response from above so the wrong error is not returned
                serviceResponse = new ServiceResponse<ResourceSettingV1>();
            }

            if (!creationResponse.IsSuccess)
            {
                if (AdapterErrorCode.ResourceAlreadyExists == creationResponse.Error.Code)
                {
                    // If we get two requests for the same user very close to each other, such as when the UX is doing traffic shadowing,
                    // this condition is very likely to be triggered where we get a conflict. As a GET api, generally we should not expose
                    // conflicts with our implementation detail of how provisioning is done. 
                    serviceResponse.Error = new Error(ErrorCode.CreateConflict, creationResponse.Error.ToString());
                }
                else
                {
                    serviceResponse.Error = new Error(ErrorCode.PartnerError, ErrorMessageFailedToCreateProfile)
                    {
                        ErrorDetails = creationResponse.Error.ToString()
                    };
                }

                return serviceResponse;
            }

            serviceResponse.Result = creationResponse.Result?.ToPrivacyUserSettings();
            return serviceResponse;
        }

        /// <summary>
        ///     Update the user settings asynchronously.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="settings">The requested user settings to change.</param>
        /// <returns>
        ///     <see cref="ServiceResponse" />
        /// </returns>
        public async Task<ServiceResponse<ResourceSettingV1>> UpdateAsync(IRequestContext requestContext, ResourceSettingV1 settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var serviceResponse = new ServiceResponse<ResourceSettingV1>();

            // create the adapter request
            PrivacyProfile adapterRequest = CreateAdapterRequest(settings);

            // handle the case when nothing changes
            if (adapterRequest.Advertising == null &&
                adapterRequest.TailoredExperiencesOffers == null &&
                adapterRequest.OnBehalfOfPrivacy == null &&
                adapterRequest.LocationPrivacy == null &&
                adapterRequest.SharingState == null)
            {
                this.logger.Information(ComponentName, "No settings requested to change. Bypassing call to Jarvis CM.");
                serviceResponse.Error = new Error(ErrorCode.ResourceNotModified, "Resource not modified.");
                return serviceResponse;
            }

            ServiceResponse<PrivacyProfile> updateProfileResponse = await this.PerformPrivacyProfileUpdateAsync(requestContext, adapterRequest).ConfigureAwait(false);
            if (!updateProfileResponse.IsSuccess)
            {
                serviceResponse.Error = updateProfileResponse.Error;
                return serviceResponse;
            }

            serviceResponse.Result = updateProfileResponse.Result?.ToPrivacyUserSettings();
            return serviceResponse;
        }

        private bool DoesPrivacyProfileExist(PrivacyProfile getProfileResponse)
        {
            if (getProfileResponse == null)
            {
                return false;
            }

            if (string.Equals(getProfileResponse.Type, CustomerMasterAdapter.PrivacyProfileGetType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            this.logger.Information(ComponentName, "Privacy profile not found for user, it must be created first to GET the profile.");
            return false;
        }

        /// <summary>
        ///     Performs the privacy profile update.
        /// </summary>
        /// <param name="requestContext">The request context.</param>
        /// <param name="updatesProfile">The profile with the desired updated values.</param>
        /// <returns>The updated profile from customer master.</returns>
        private async Task<ServiceResponse<PrivacyProfile>> PerformPrivacyProfileUpdateAsync(IRequestContext requestContext, PrivacyProfile updatesProfile)
        {
            IPxfRequestContext adapterRequestContext = requestContext.ToAdapterRequestContext();

            var serviceResponse = new ServiceResponse<PrivacyProfile>();

            // Get the existing profile as JObject
            AdapterResponse<JObject> existingProfile = await this.customerMasterAdapter.GetPrivacyProfileJObjectAsync(adapterRequestContext).ConfigureAwait(false);
            if (!existingProfile.IsSuccess)
            {
                serviceResponse.Error = new Error(ErrorCode.Unknown, existingProfile.Error.ToString());
            }

            // Update the privacy profile. Since the request to Jarvis CM is a PUT and not a PATCH, it will overwrite all profile values as long as the ETag matches.
            AdapterResponse<PrivacyProfile> updateProfileResponse = await this.customerMasterAdapter.UpdatePrivacyProfileAsync(
                adapterRequestContext,
                updatesProfile,
                existingProfile.Result).ConfigureAwait(false);

            if (!updateProfileResponse.IsSuccess)
            {
                switch (updateProfileResponse.Error?.Code)
                {
                    case AdapterErrorCode.ResourceNotModified:

                        // This isn't really an error, but it still allows us to return a different Http Status code when the resource doesn't change
                        serviceResponse.Error = new Error(ErrorCode.ResourceNotModified, updateProfileResponse.Error?.ToString());
                        break;

                    default:

                        // CM returns either of these status codes, so need to handle both.
                        if (updateProfileResponse.Error?.StatusCode == (int)HttpStatusCode.Conflict)
                        {
                            serviceResponse.Error = new Error(ErrorCode.UpdateConflict, updateProfileResponse.Error?.ToString());
                        }
                        else if (updateProfileResponse.Error?.StatusCode == (int)HttpStatusCode.PreconditionFailed)
                        {
                            serviceResponse.Error = new Error(ErrorCode.PreconditionFailed, updateProfileResponse.Error?.ToString());
                        }
                        else
                        {
                            serviceResponse.Error = new Error(ErrorCode.Unknown, updateProfileResponse.Error?.ToString());
                        }

                        break;
                }

                return serviceResponse;
            }

            serviceResponse.Result = updateProfileResponse.Result;
            return serviceResponse;
        }

        internal static PrivacyProfile CreateAdapterRequest(ResourceSettingV1 request)
        {
            var adapterRequest = new PrivacyProfile
            {
                Type = CustomerMasterAdapter.PrivacyProfileGetType
            };

            adapterRequest.ETag = request.ETag;
            adapterRequest.Advertising = request.Advertising;
            adapterRequest.TailoredExperiencesOffers = request.TailoredExperiencesOffers;
            adapterRequest.OnBehalfOfPrivacy = request.OnBehalfOfPrivacy;
            adapterRequest.LocationPrivacy = request.LocationPrivacy;
            adapterRequest.SharingState = request.SharingState;
            return adapterRequest;
        }

        /// <summary>
        ///     Checks to see if the existing privacy profile needs to be updated
        /// </summary>
        /// <param name="requestContext">The incoming request context.</param>
        /// <param name="existingProfile">The existing profile.</param>
        /// <param name="updatedProfile">The updated profile.</param>
        /// <returns><c>true</c> if the profile needs to be updated, otherwise <c>false</c></returns>
        private static bool DoesPrivacyProfileNeedUpdates(IRequestContext requestContext, PrivacyProfile existingProfile, out PrivacyProfile updatedProfile)
        {
            updatedProfile = existingProfile;

            // Flag that indicates if the profile needs to be updated with CM before returning to the UX
            bool updateRequired = false;
            PrivacyProfile defaultValues = DefaultProfileSettingsFactory.CreateDefaultPrivacyProfile();

            if (!updatedProfile.Advertising.HasValue)
            {
                updatedProfile.Advertising = defaultValues.Advertising;
                updateRequired = updatedProfile.Advertising.HasValue;
            }

            if (!updatedProfile.SharingState.HasValue)
            {
                updatedProfile.SharingState = defaultValues.SharingState;
                updateRequired = updatedProfile.SharingState.HasValue;
            }

            return updateRequired;
        }
    }
}
