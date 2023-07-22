// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.V2;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Equality Helper
    /// </summary>
    public static class EqualityHelper
    {
        /// <summary>
        ///     Asserts the <see cref="Error" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        /// <param name="validateInnerError">if set to <c>true</c>, validate the inner error.</param>
        public static void AreEqual(Error expected, Error actual, bool validateInnerError = true)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Code, actual.Code);
            Assert.AreEqual(expected.Message, actual.Message);

            if (!string.IsNullOrWhiteSpace(expected.ErrorDetails) || !string.IsNullOrWhiteSpace(actual.ErrorDetails))
            {
                // verify the first string begins with the second string, due to stack trace being included
                StringAssert.StartsWith(actual.ErrorDetails, expected.ErrorDetails);
            }

            Assert.AreEqual(expected.Target, actual.Target);
            Assert.AreEqual(expected.TrackingId, actual.TrackingId);

            if (validateInnerError)
            {
                AreEqual(expected.InnerError, actual.InnerError);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="BrowseHistoryV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(BrowseHistoryV1 expected, BrowseHistoryV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.AreEqual(expected.NavigatedToUrl, actual.NavigatedToUrl);
            Assert.AreEqual(expected.PageTitle, actual.PageTitle);

            Assert.IsNotNull(actual);
            AreEqual((ResourceV1)expected, actual);
            AreEqual((WebActivityV1)expected, actual);
        }

        /// <summary>
        ///     Asserts the <see cref="AppUsageV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(AppUsageV1 expected, AppUsageV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.AreEqual(expected.AppIconBackground, actual.AppIconBackground);
            Assert.AreEqual(expected.AppIconUrl, actual.AppIconUrl);
            Assert.AreEqual(expected.AppId, actual.AppId);
            Assert.AreEqual(expected.AppName, actual.AppName);
            Assert.AreEqual(expected.AppPublisher, actual.AppPublisher);

            Assert.IsNotNull(actual);
            AreEqual((ResourceV1)expected, actual);
        }

        /// <summary>
        ///     Asserts the <see cref="VoiceHistoryV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(VoiceHistoryV1 expected, VoiceHistoryV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.AreEqual(expected.Application, actual.Application);
            Assert.AreEqual(expected.DeviceType, actual.DeviceType);
            Assert.AreEqual(expected.DisplayText, actual.DisplayText);

            Assert.IsNotNull(actual);
            AreEqual((ResourceV1)expected, actual);
        }

        /// <summary>
        ///     Asserts the <see cref="VoiceHistoryAudioV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(VoiceHistoryAudioV1 expected, VoiceHistoryAudioV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            if (expected.Audio != actual.Audio)
            {
                Assert.IsNotNull(expected.Audio);
                Assert.IsNotNull(actual.Audio);
                Assert.AreEqual(expected.Audio.LongLength, expected.Audio.LongLength);
                for (long i = 0; i < expected.Audio.LongLength; i++)
                {
                    Assert.AreEqual(expected.Audio[i], actual.Audio[i]);
                }
            }

            AreEqual((ResourceV1)expected, actual);
        }

        /// <summary>
        ///     Asserts the <see cref="SearchHistoryV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(SearchHistoryV1 expected, SearchHistoryV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            AreEqual(expected.NavigatedToUrls, actual.NavigatedToUrls);

            Assert.IsNotNull(actual);
            AreEqual((ResourceV1)expected, actual);
            AreEqual((WebActivityV1)expected, actual);
        }

        /// <summary>
        ///     Asserts the <see cref="List{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(IList<AppUsageResource> expected, IList<AppUsageResource> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < actual.Count; ++i)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Models.AppUsageResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(AppUsageResource expected, AppUsageResource actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.AreEqual(expected.AppId, actual.AppId);
            Assert.AreEqual(expected.AppName, actual.AppName);
            Assert.AreEqual(expected.Aggregation, actual.Aggregation);
            Assert.AreEqual(expected.AppIconBackground, actual.AppIconBackground);
            Assert.AreEqual(expected.EndDateTime, actual.EndDateTime);
            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreEqual(expected.AppId, actual.AppId);
            Assert.AreEqual(expected.AppId, actual.AppId);
        }

        /// <summary>
        ///     Asserts the <see cref="List{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(IList<VoiceResource> expected, IList<VoiceResource> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < actual.Count; ++i)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Models.VoiceResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(VoiceResource expected, VoiceResource actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreEqual(expected.DeviceType, actual.DeviceType);
            Assert.AreEqual(expected.DateTime, actual.DateTime);
            Assert.AreEqual(expected.DisplayText, actual.DisplayText);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Application, actual.Application);
        }

        /// <summary>
        ///     Asserts the <see cref="List{LocationHistoryV1}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(List<LocationHistoryV1> expected, List<LocationHistoryV1> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            for (int i = 0; i < actual.Count; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="LocationHistoryV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(LocationHistoryV1 expected, LocationHistoryV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            AreEqual(expected, (LocationV1)actual);
            AreEqual(expected.AggregateHistory, actual.AggregateHistory, AreEqual);
        }

        /// <summary>
        ///     Asserts the <see cref="LocationV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(LocationV1 expected, LocationV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            AreEqual(expected, (ResourceV1)actual);

            Assert.AreEqual(expected.Name, actual.Name);
            AreEqual(expected.Address, actual.Address);
            Assert.AreEqual(expected.Latitude, actual.Latitude);
            Assert.AreEqual(expected.Longitude, actual.Longitude);
            Assert.AreEqual(expected.AccuracyRadius, actual.AccuracyRadius);
            Assert.AreEqual(expected.Category, actual.Category);
            Assert.AreEqual(expected.LocationType, actual.LocationType);
            Assert.AreEqual(expected.DeviceType, actual.DeviceType);
            Assert.AreEqual(expected.ActivityType, actual.ActivityType);
            Assert.AreEqual(expected.EndDateTime, actual.EndDateTime);
            Assert.AreEqual(expected.Url, actual.Url);
            Assert.AreEqual(expected.Distance, actual.Distance);
        }

        /// <summary>
        ///     Asserts the <see cref="WebActivityV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(WebActivityV1 expected, WebActivityV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.SearchTerms, actual.SearchTerms);
            AreEqual(expected.Location, actual.Location);
        }

        /// <summary>
        ///     Asserts the <see cref="DeleteResponseV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(DeleteResponseV1 expected, DeleteResponseV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Status, actual.Status);
        }

        /// <summary>
        ///     Asserts the <see cref="WebLocationV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(WebLocationV1 expected, WebLocationV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.AccuracyRadius, actual.AccuracyRadius);
            Assert.AreEqual(expected.Latitude, actual.Latitude);
            Assert.AreEqual(expected.Longitude, actual.Longitude);
        }

        /// <summary>
        ///     Asserts the <see cref="TimelineCard" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(TimelineCard expected, TimelineCard actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Timestamp, actual.Timestamp);
            Assert.AreEqual(expected.DeviceIds?.Count, actual.DeviceIds?.Count);
            for (int i = 0; i < expected.DeviceIds?.Count; i++)
            {
                Assert.AreEqual(expected.DeviceIds[i], actual.DeviceIds[i]);
            }

            AreEqual(expected as AppUsageCard, actual as AppUsageCard);
            AreEqual(expected as BrowseCard, actual as BrowseCard);
            AreEqual(expected as SearchCard, actual as SearchCard);
            AreEqual(expected as VoiceCard, actual as VoiceCard);
        }

        /// <summary>
        ///     Asserts the <see cref="AppUsageCard" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(AppUsageCard expected, AppUsageCard actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.AppIconBackground, actual.AppIconBackground);
            Assert.AreEqual(expected.AppIconUri, actual.AppIconUri);
            Assert.AreEqual(expected.AppId, actual.AppId);
            Assert.AreEqual(expected.AppName, actual.AppName);
            Assert.AreEqual(expected.AppPublisher, actual.AppPublisher);
            Assert.AreEqual(expected.EndTimestamp, actual.EndTimestamp);
        }

        /// <summary>
        ///     Asserts the <see cref="BrowseCard" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(BrowseCard expected, BrowseCard actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Domain, actual.Domain);
            Assert.AreEqual(expected.Navigations?.Count, actual.Navigations?.Count);
            for (int i = 0; i < expected.Navigations?.Count; i++)
            {
                AreEqual(expected.Navigations[i], actual.Navigations[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="SearchCard" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(SearchCard expected, SearchCard actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Search, actual.Search);
            Assert.AreEqual(expected.Navigations?.Count, actual.Navigations?.Count);
            for (int i = 0; i < expected.Navigations?.Count; i++)
            {
                AreEqual(expected.Navigations[i], actual.Navigations[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="SearchCard.Navigation" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(SearchCard.Navigation expected, SearchCard.Navigation actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Uri, actual.Uri);
            Assert.AreEqual(expected.Title, actual.Title);
        }

        /// <summary>
        ///     Asserts the <see cref="BrowseCard.Navigation" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(BrowseCard.Navigation expected, BrowseCard.Navigation actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Uri, actual.Uri);
            Assert.AreEqual(expected.Title, actual.Title);
            Assert.AreEqual(expected.UriHash, actual.UriHash);
            Assert.AreEqual(expected.Timestamps?.Count, actual.Timestamps?.Count);
            for (int i = 0; i < expected.Timestamps?.Count; i++)
                Assert.AreEqual(expected.Timestamps[i], actual.Timestamps[i]);
        }

        /// <summary>
        ///     Asserts the <see cref="VoiceCard" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(VoiceCard expected, VoiceCard actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.Application, actual.Application);
            Assert.AreEqual(expected.DeviceType, actual.DeviceType);
            Assert.AreEqual(expected.Text, actual.Text);
            Assert.AreEqual(expected.VoiceId, actual.VoiceId);
        }

        /// <summary>
        ///     Asserts the <see cref="Privacy.ExperienceContracts.V1.PagedResponse{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(Privacy.ExperienceContracts.V1.PagedResponse<BrowseHistoryV1> expected, Privacy.ExperienceContracts.V1.PagedResponse<BrowseHistoryV1> actual)
        {
            AreEqual(expected.Items, actual.Items, AreEqual);
            Assert.AreEqual(expected.NextLink, actual.NextLink);
        }

        /// <summary>
        ///     Asserts the <see cref="Privacy.ExperienceContracts.V1.PagedResponse{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(Privacy.ExperienceContracts.V2.PagedResponse<TimelineCard> expected, Privacy.ExperienceContracts.V2.PagedResponse<TimelineCard> actual)
        {
            AreEqual(expected.Items, actual.Items, AreEqual);
            Assert.AreEqual(expected.NextLink, actual.NextLink);
        }

        /// <summary>
        ///     Asserts the <see cref="Privacy.ExperienceContracts.V1.PagedResponse{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(Privacy.ExperienceContracts.V1.PagedResponse<AppUsageV1> expected, Privacy.ExperienceContracts.V1.PagedResponse<AppUsageV1> actual)
        {
            AreEqual(expected.Items, actual.Items, AreEqual);
            Assert.AreEqual(expected.NextLink, actual.NextLink);
        }

        /// <summary>
        ///     Asserts the <see cref="Privacy.ExperienceContracts.V1.PagedResponse{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(Privacy.ExperienceContracts.V1.PagedResponse<VoiceHistoryV1> expected, Privacy.ExperienceContracts.V1.PagedResponse<VoiceHistoryV1> actual)
        {
            AreEqual(expected.Items, actual.Items, AreEqual);
            Assert.AreEqual(expected.NextLink, actual.NextLink);
        }

        /// <summary>
        ///     Asserts the <see cref="Privacy.ExperienceContracts.V1.PagedResponse{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(Privacy.ExperienceContracts.V1.PagedResponse<SearchHistoryV1> expected, Privacy.ExperienceContracts.V1.PagedResponse<SearchHistoryV1> actual)
        {
            AreEqual(expected.Items, actual.Items, AreEqual);
            Assert.AreEqual(expected.NextLink, actual.NextLink);
        }

        /// <summary>
        ///     Asserts the <see cref="Privacy.ExperienceContracts.V1.PagedResponse{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(
            Privacy.ExperienceContracts.V1.PagedResponse<LocationHistoryV1> expected,
            Privacy.ExperienceContracts.V1.PagedResponse<LocationHistoryV1> actual)
        {
            AreEqual(expected.Items, actual.Items, AreEqual);
            Assert.AreEqual(expected.NextLink, actual.NextLink);
        }

        /// <summary>
        ///     Asserts the <see cref="AggregateCountResponse" /> property values are equal
        /// </summary>
        public static void AreEqual(AggregateCountResponse expected, AggregateCountResponse actual)
        {
            AreEqual(expected.AggregateCounts, actual.AggregateCounts);
        }

        /// <summary>
        ///     Asserts two dictionaries (string, int) are equal
        /// </summary>
        public static void AreEqual(Dictionary<string, int> expected, Dictionary<string, int> actual)
        {
            int actualValue;
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var pair in expected)
            {
                var keyExists = actual.TryGetValue(pair.Key, out actualValue);
                Assert.IsTrue(keyExists);
                if(keyExists)
                {
                    //Check if value matches for the key
                    Assert.AreEqual(pair.Value, actualValue);
                }
            }
        }

        /// <summary>
        ///     Asserts the <see cref="IEnumerable{TItem}" /> property values are equal.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        /// <param name="equalityAssertFunc">The equality assert function.</param>
        public static void AreEqual<TItem>(IEnumerable<TItem> expected, IEnumerable<TItem> actual, Action<TItem, TItem> equalityAssertFunc)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", EnumerableUtilities.ToString(actual));
                return;
            }

            Assert.IsNotNull(actual);

            List<TItem> expectedList = expected.ToList();
            List<TItem> actualList = actual.ToList();

            Assert.AreEqual(expectedList.Count, actualList.Count);
            for (int i = 0; i < expectedList.Count; i++)
            {
                equalityAssertFunc(expectedList[i], actualList[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="ServiceResponse{PagedResonse}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<BrowseHistoryV1>> expected,
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<BrowseHistoryV1>> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            if (expected.IsSuccess)
            {
                Assert.IsTrue(actual.IsSuccess);
                AreEqual(expected.Result, actual.Result);
            }
            else
            {
                AreEqual(expected.Error, actual.Error);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Models.LocationResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(IList<LocationResource> expected, IList<LocationResource> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            for (int i = 0; i < actual.Count; i++)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Models.LocationResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(LocationResource expected, LocationResource actual)
        {
            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreEqual(expected.DeviceType, actual.DeviceType);
            Assert.AreEqual(expected.Latitude, actual.Latitude);
            Assert.AreEqual(expected.Longitude, actual.Longitude);
            Assert.AreEqual(expected.LocationType, actual.LocationType);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Distance, actual.Distance);
        }

        /// <summary>
        ///     Asserts the <see cref="ServiceResponse{PagedResonse}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<AppUsageV1>> expected,
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<AppUsageV1>> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            if (expected.IsSuccess)
            {
                Assert.IsTrue(actual.IsSuccess);
                AreEqual(expected.Result, actual.Result);
            }
            else
            {
                AreEqual(expected.Error, actual.Error);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="ServiceResponse{PagedResonse}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<VoiceHistoryV1>> expected,
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<VoiceHistoryV1>> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            if (expected.IsSuccess)
            {
                Assert.IsTrue(actual.IsSuccess);
                AreEqual(expected.Result, actual.Result);
            }
            else
            {
                AreEqual(expected.Error, actual.Error);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="ServiceResponse{PagedResonse}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<SearchHistoryV1>> expected,
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<SearchHistoryV1>> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            if (expected.IsSuccess)
            {
                Assert.IsTrue(actual.IsSuccess);
                AreEqual(expected.Result, actual.Result);
            }
            else
            {
                AreEqual(expected.Error, actual.Error);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="ServiceResponse{PagedResonse}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<LocationHistoryV1>> expected,
            ServiceResponse<Privacy.ExperienceContracts.V1.PagedResponse<LocationHistoryV1>> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);

            if (expected.IsSuccess)
            {
                Assert.IsTrue(actual.IsSuccess);
                AreEqual(expected.Result, actual.Result);
            }
            else
            {
                AreEqual(expected.Error, actual.Error);
            }
        }

        public static void AreEqual(IList<SearchResourceV2> expected, IList<SearchResourceV2> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            for (var i = 0; i < expected.Count(); ++i)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        public static void AreEqual(SearchResourceV2 expected, SearchResourceV2 actual)
        {
            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreEqual(expected.DateTime, actual.DateTime);
            for (var i = 0; i < expected.Navigations.Count; ++i)
            {
                Assert.AreEqual(expected.Navigations[i].PageTitle, actual.Navigations[i].PageTitle);
                Assert.AreEqual(expected.Navigations[i].Url, actual.Navigations[i].Url);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Models.ContentConsumptionResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(IList<ContentConsumptionResource> expected, IList<ContentConsumptionResource> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            for (var i = 0; i < expected.Count(); ++i)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="List{SearchResource}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(IList<SearchResource> expected, IList<SearchResource> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            for (var i = 0; i < expected.Count(); ++i)
            {
                AreEqual(expected[i], actual[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="SearchResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(SearchResource expected, SearchResource actual)
        {
            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.DateTime, actual.DateTime);
            Assert.AreEqual(expected.Location?.Latitude, actual.Location?.Latitude);
            Assert.AreEqual(expected.Location?.Longitude, actual.Location?.Longitude);
            Assert.AreEqual(expected.Location?.AccuracyRadius, actual.Location?.AccuracyRadius);
            if (expected.NavigatedToUrls?.Count > 0)
            {
                for (var i = 0; i < expected.NavigatedToUrls.Count; ++i)
                {
                    Assert.AreEqual(expected.NavigatedToUrls[i].Url, actual.NavigatedToUrls[i].Url);
                    Assert.AreEqual(expected.NavigatedToUrls[i].Time, actual.NavigatedToUrls[i].Time);
                }
            }
        }

        /// <summary>
        ///     Asserts the <see cref="ContentConsumptionResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(ContentConsumptionResource expected, ContentConsumptionResource actual)
        {
            Assert.AreEqual(expected.AppName, actual.AppName);
            Assert.AreEqual(expected.Artist, actual.Artist);
            Assert.AreEqual(expected.ConsumptionTime, actual.ConsumptionTime);
            Assert.AreEqual(expected.ContainerName, actual.ContainerName);
            Assert.AreEqual(expected.ContentUrl, actual.ContentUrl);
            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.MediaType, actual.MediaType);
        }

        /// <summary>
        ///     Asserts the <see cref="CortanaNotebookUserFeaturesV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(CortanaNotebookUserFeaturesV1 expected, CortanaNotebookUserFeaturesV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.UserFeatures[0].Id, actual.UserFeatures[0].Id);
            Assert.AreEqual(expected.UserFeatures[0].ExplicitDisplayName, actual.UserFeatures[0].ExplicitDisplayName);
            Assert.AreEqual(expected.UserFeatures[0].ImplicitDisplayName, actual.UserFeatures[0].ImplicitDisplayName);
        }

        /// <summary>
        ///     Asserts the <see cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Models.PagedResponse{T}" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(PrivacyAdapters.Models.PagedResponse<BrowseResource> expected, PrivacyAdapters.Models.PagedResponse<BrowseResource> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            for (var i = 0; i < expected.Items.Count; ++i)
            {
                AreEqual(expected.Items[i], actual.Items[i]);
            }
        }

        /// <summary>
        ///     Asserts the <see cref="Microsoft.Membership.MemberServices.PrivacyAdapters.Models.BrowseResource" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        public static void AreEqual(BrowseResource expected, BrowseResource actual)
        {
            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreNotEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.DateTime, actual.DateTime);
            Assert.AreEqual(expected.PageTitle, actual.PageTitle);
        }

        public static void AreEqual(ExportStatus expected, ExportStatus actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.AreEqual(expected.ExportId, actual.ExportId);
            Assert.AreEqual(expected.IsComplete, actual.IsComplete);
            Assert.AreEqual(expected.LastError, actual.LastError);
            Assert.AreEqual(expected.ZipFileUri, actual.ZipFileUri);
            Assert.AreEqual(expected.ExpiresAt, actual.ExpiresAt);
            Assert.AreEqual(expected.RequestedAt, actual.RequestedAt);
            Assert.AreEqual(expected.ZipFileSize, actual.ZipFileSize);
            Assert.AreEqual(expected.DataTypes?.Count, actual.DataTypes?.Count);
            if (expected.DataTypes?.Count > 0)
            {
                for (int i = 0; i < expected.DataTypes.Count; i++)
                {
                    Assert.AreEqual(expected.DataTypes[i], actual.DataTypes[i]);
                }
            }
        }

        private static void AreEqual(IEnumerable<NavigatedToUrlV1> expected, IEnumerable<NavigatedToUrlV1> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            List<NavigatedToUrlV1> actualList = actual.ToList();
            List<NavigatedToUrlV1> expectedList = expected.ToList();

            for (int i = 0; i < actualList.Count; i++)
            {
                AreEqual(expectedList[i], actualList[i]);
            }
        }

        private static void AreEqual(NavigatedToUrlV1 expected, NavigatedToUrlV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.AreEqual(expected.Url, actual.Url);
            Assert.AreEqual(expected.PageTitle, actual.PageTitle);
        }

        /// <summary>
        ///     Asserts the <see cref="ResourceV1" /> property values are equal.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        private static void AreEqual(ResourceV1 expected, ResourceV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            AreEqual(expected.Ids, actual.Ids, Assert.AreEqual);
            Assert.AreEqual(expected.DateTime, actual.DateTime);
            Assert.AreEqual(expected.DeviceId, actual.DeviceId);
            Assert.AreEqual(expected.Source, actual.Source);
            Assert.AreEqual(expected.PartnerId, actual.PartnerId);
            Assert.AreEqual(expected.IsAggregate, actual.IsAggregate);
        }

        private static void AreEqual(AddressV1 expected, AddressV1 actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual, "Actual: {0}", actual);
                return;
            }

            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.AddressLine1, actual.AddressLine1);
            Assert.AreEqual(expected.AddressLine2, actual.AddressLine2);
            Assert.AreEqual(expected.AddressLine3, actual.AddressLine3);
            Assert.AreEqual(expected.CountryRegion, actual.CountryRegion);
            Assert.AreEqual(expected.CountryRegionIso2, actual.CountryRegionIso2);
            Assert.AreEqual(expected.FormattedAddress, actual.FormattedAddress);
            Assert.AreEqual(expected.Locality, actual.Locality);
            Assert.AreEqual(expected.PostalCode, actual.PostalCode);
        }

        /// <summary>
        ///     Asserts the <see cref="GetRecurringDeleteResponse" /> property values are equal
        /// </summary>
        public static void AreEqual(GetRecurringDeleteResponse expected, GetRecurringDeleteResponse actual)
        {
            Assert.AreNotEqual(expected, actual);

            Assert.AreEqual(expected.PuidValue, actual.PuidValue);
            Assert.AreEqual(expected.DataType, actual.DataType);
            Assert.AreEqual(expected.CreateDate, actual.CreateDate);
            Assert.AreEqual(expected.UpdateDate, actual.UpdateDate);
            Assert.AreEqual(expected.LastDeleteOccurrence, actual.LastDeleteOccurrence);
            Assert.AreEqual(expected.NextDeleteOccurrence, actual.NextDeleteOccurrence);
            Assert.AreEqual(expected.LastSucceededDeleteOccurrence, actual.LastSucceededDeleteOccurrence);
            Assert.AreEqual(expected.NumberOfRetries, actual.NumberOfRetries);
            Assert.AreEqual(expected.MaxNumberOfRetries, actual.MaxNumberOfRetries);
            Assert.AreEqual(expected.Status, actual.Status);
            Assert.AreEqual(expected.RecurringIntervalDays, actual.RecurringIntervalDays);
        }
    }
}
