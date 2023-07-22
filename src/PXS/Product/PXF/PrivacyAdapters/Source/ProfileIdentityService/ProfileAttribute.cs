// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyAdapters.ProfileIdentityService
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     MSA Profile Attributes
    /// </summary>
    public enum ProfileAttribute
    {
        Unknown,

        All,

        FirstName,

        LastName,

        Birthdate,

        Gender,

        LanguagePreference,

        City,

        Street1,

        Street2,

        Street3,

        Country,

        Region,

        PostalCode,

        TimeZone,

        ProfileVersion,

        ProfileCreationInfo,

        HardwareIdentifier,

        Model,

        OSVersion,

        Icon,

        FriendlyName,

        DefaultPhoneNumber,

        DeviceType,

        Make,

        MsnTouVersion,

        ChildBit28,

        FamilyId,

        IsGroupMember,

        GroupVersion,

        AgeGroup
    }

    /// <summary>
    ///     Helpers for converting <see cref="ProfileAttribute" /> to use with MSA SAPIs.
    /// </summary>
    public static class ProfileAttributesExtension
    {
        private static readonly Lazy<IDictionary<ProfileAttribute, string>> lazyAttributesStringMapping = new Lazy<IDictionary<ProfileAttribute, string>>(
            () =>
            {
                var mapping = new Dictionary<ProfileAttribute, string>();
                foreach (KeyValuePair<string, IDictionary<string, ProfileAttribute>> pair in StringAttributesMapping)
                {
                    IDictionary<string, ProfileAttribute> attributesCollectionInOneNamespace = pair.Value;
                    string propNamespace = pair.Key;
                    foreach (KeyValuePair<string, ProfileAttribute> innerPair in attributesCollectionInOneNamespace)
                    {
                        mapping.Add(innerPair.Value, propNamespace + "." + innerPair.Key);
                    }
                }

                return mapping;
            });

        private static readonly Lazy<IDictionary<string, IDictionary<string, ProfileAttribute>>> lazyStringAttributesMapping =
            new Lazy<IDictionary<string, IDictionary<string, ProfileAttribute>>>(
                () => new Dictionary<string, IDictionary<string, ProfileAttribute>>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "Personal_CS", new Dictionary<string, ProfileAttribute>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "birthdate", ProfileAttribute.Birthdate },
                            { "gender", ProfileAttribute.Gender },
                            { "langpreference", ProfileAttribute.LanguagePreference },
                            { "agegroup", ProfileAttribute.AgeGroup }
                        }
                    },
                    {
                        "Personal2_CS", new Dictionary<string, ProfileAttribute>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "name.first", ProfileAttribute.FirstName },
                            { "name.last", ProfileAttribute.LastName }
                        }
                    },
                    {
                        "Family_CS", new Dictionary<string, ProfileAttribute>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "familyid", ProfileAttribute.FamilyId }
                        }
                    },
                    {
                        "Addresses_CS", new Dictionary<string, ProfileAttribute>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "home.city", ProfileAttribute.City },
                            { "home.street1", ProfileAttribute.Street1 },
                            { "home.street2", ProfileAttribute.Street2 },
                            { "home.street3", ProfileAttribute.Street3 },
                            { "home.country", ProfileAttribute.Country },
                            { "home.region", ProfileAttribute.Region },
                            { "home.postalcode", ProfileAttribute.PostalCode },
                            { "home.timezone", ProfileAttribute.TimeZone }
                        }
                    },
                    {
                        "Internal", new Dictionary<string, ProfileAttribute>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "profileversion", ProfileAttribute.ProfileVersion },
                            { "profilecreationinfo", ProfileAttribute.ProfileCreationInfo }
                        }
                    },
                    {
                        "Device", new Dictionary<string, ProfileAttribute>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "hardware_identifier", ProfileAttribute.HardwareIdentifier },
                            { "model", ProfileAttribute.Model },
                            { "os_version", ProfileAttribute.OSVersion },
                            { "icon", ProfileAttribute.Icon },
                            { "friendly_name", ProfileAttribute.FriendlyName },
                            { "default_phone_number", ProfileAttribute.DefaultPhoneNumber },
                            { "type", ProfileAttribute.DeviceType },
                            { "make", ProfileAttribute.Make }
                        }
                    },
                    {
                        "Authorization_CS", new Dictionary<string, ProfileAttribute>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "msntouversion", ProfileAttribute.MsnTouVersion },
                            { "F29_External", ProfileAttribute.ChildBit28 },
                            { "f34_isgroupmember", ProfileAttribute.IsGroupMember },
                            { "groupversion", ProfileAttribute.GroupVersion }
                        }
                    }
                });

        /// <summary>
        ///     Converts a property to it's enum
        /// </summary>
        /// <param name="propertyNameSpace">Property namespace</param>
        /// <param name="propertyName">Property name</param>
        /// <returns><see cref="ProfileAttribute" /> enum, or <see cref="ProfileAttribute.Unknown" /></returns>
        public static ProfileAttribute ToAttributeEnum(string propertyNameSpace, string propertyName)
        {
            if (StringAttributesMapping.TryGetValue(propertyNameSpace, out IDictionary<string, ProfileAttribute> profileNames) &&
                profileNames.TryGetValue(propertyName, out ProfileAttribute value))
            {
                return value;
            }

            return ProfileAttribute.Unknown;
        }

        /// <summary>
        ///     Converts a collection of <see cref="ProfileAttribute" /> into the string to send to MSA in requests for profile attribute values
        /// </summary>
        /// <param name="profileAttributes">Collection of attributes</param>
        /// <returns>A string that represents the collection of attributes</returns>
        public static string ToAttributeListString(this IEnumerable<ProfileAttribute> profileAttributes)
        {
            return string.Join(",", profileAttributes.Select(ToAttributeString));
        }

        /// <summary>
        ///     Converts a <see cref="ProfileAttribute" /> into it's string representation
        /// </summary>
        /// <param name="profileAttribute">The profile attribute to convert</param>
        /// <returns>The string representation</returns>
        public static string ToAttributeString(this ProfileAttribute profileAttribute)
        {
            if (AttributesStringMapping.ContainsKey(profileAttribute))
            {
                return AttributesStringMapping[profileAttribute];
            }

            throw new InvalidOperationException("Unknown attribute");
        }

        private static IDictionary<ProfileAttribute, string> AttributesStringMapping => lazyAttributesStringMapping.Value;

        private static IDictionary<string, IDictionary<string, ProfileAttribute>> StringAttributesMapping => lazyStringAttributesMapping.Value;
    }
}
