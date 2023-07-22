// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyMockService.DataSource
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.CustomerMasterAdapter.Models;
    using Microsoft.Membership.MemberServices.PrivacyMockService.Controllers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class ProfileSettingsStore
    {
        private static CustomerMasterException CreateExceptionProfileDoesNotExist()
        {
            return new CustomerMasterException(
                new CustomerMasterError
                {
                    ErrorCode = CustomerMasterErrorCode.UnsupportedAction.ToString(),
                    Message = "Profile does not exist, and cannot be updated.",
                    ObjectType = "Error"
                });
        }

        private static CustomerMasterException CreateExceptionConcurrencyConflict()
        {
            return new CustomerMasterException(
                new CustomerMasterError
                {
                    ErrorCode = CustomerMasterErrorCode.ConcurrencyFailure.ToString(),
                    Message =
                        "The ???If-Match??? header of the request contained an ETag that didn???t match the ETag of the resource specified in the URI.",
                    ObjectType = "Error"
                });
        }

        /// <summary>
        /// Singleton Instance
        /// </summary>
        public static ProfileSettingsStore Instance { get; } = new ProfileSettingsStore();

        public ConcurrentDictionary<long, Profiles> Users { get; } = new ConcurrentDictionary<long, Profiles>();

        /// <summary>
        /// Gets or generates data
        /// </summary>
        /// <param name="user">User Puid</param>
        /// <param name="profileType">Type of the profile.</param>
        /// <returns>Set of data for that user</returns>
        public Profiles Get(long user, string profileType)
        {
            // To Mock CM behavior, return a profile if it exists, otherwise return an empty collection of profiles
            if (this.Users.TryGetValue(user, out Profiles allProfiles))
            {
                var profilesResponse = new Profiles { Links = allProfiles.Links, Items = new List<JObject>() };

                if (allProfiles.Items == null || allProfiles.Items.Count == 0)
                {
                    return profilesResponse;
                }

                IList<JObject> responseItems = new List<JObject>();
                foreach (JObject userProfile in allProfiles.Items)
                {
                    var profile = JsonConvert.DeserializeObject<Profile>(userProfile.ToString());
                    if (string.Equals(profile.Type, profileType))
                    {
                        responseItems.Add(userProfile);
                    }
                }

                profilesResponse.Items = responseItems;
                return profilesResponse;
            }
            else
            {
                return new Profiles { Items = new List<JObject>() };
            }
        }

        /// <summary>
        /// Creates the specified profile for the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="profile">The profile.</param>
        public PrivacyProfile Create(long user, PrivacyProfile profile)
        {
            var profiles = new Profiles();
            profiles.Items = new List<JObject>();

            // mock generates the etag and id (even if user provided it, it wouldn't be used in a 'create' of a resource).
            profile.ETag = Guid.NewGuid().ToString();
            profile.Id = Guid.NewGuid().ToString();

            profiles.Items.Add(JObject.FromObject(profile));

            if (!this.Users.ContainsKey(user))
            {
                this.Users[user] = profiles;
                return profile;
            }

            throw new CustomerMasterException(
                new CustomerMasterError
                {
                    ErrorCode = CustomerMasterErrorCode.ResourceAlreadyExists.ToString(),
                    Message = "The request was an attempt to create a resource that already exists.",
                    ObjectType = "Error"
                });
        }

        /// <summary>
        /// Updates the profile for the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="updatedProfile">The updated profile.</param>
        /// <param name="profileId">The profile identifier to update.</param>
        /// <param name="etag">The etag.</param>
        public PrivacyProfile Update(long user, PrivacyProfile updatedProfile, string profileId, string etag)
        {
            if (this.Users.TryGetValue(user, out var _))
            {
                for (var i = 0; i < this.Users[user].Items.Count; i++)
                {
                    var profile = JsonConvert.DeserializeObject<Profile>(this.Users[user].Items[i].ToString());

                    // there should be only one profile with matching etag and profile id.
                    if (string.Equals(profile.Id, profileId))
                    {
                        if (!string.Equals(etag, profile.ETag, StringComparison.OrdinalIgnoreCase))
                        {
                            throw CreateExceptionConcurrencyConflict();
                        }

                        // create a new etag and then update the value in memory.
                        updatedProfile.ETag = Guid.NewGuid().ToString();
                        this.Users[user].Items[i] = JObject.FromObject(updatedProfile);
                        return updatedProfile;
                    }
                }
            }

            throw CreateExceptionProfileDoesNotExist();
        }

        /// <summary>
        /// delete all items for a user.  If you re-read this user, you will get new random data.
        /// </summary>
        /// <param name="user">User Puid</param>
        /// <returns>True if the user was found and deleted</returns>
        public bool DeleteUser(long user)
        {
            return this.Users.TryRemove(user, out Profiles _);
        }
    }
}