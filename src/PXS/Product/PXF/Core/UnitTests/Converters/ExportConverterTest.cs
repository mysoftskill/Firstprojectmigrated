// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Privacy.Core.Converters;
    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.ExportStorageHelper;
    using Microsoft.Membership.MemberServices.Privacy.DataContracts.ExportTypes;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Export Converter Unit Test
    /// </summary>
    [TestClass]
    public class ExportConverterTest
    {
        public const string Puid1 = "985153929959999";

        public const string RequestId1 = "170602005109836325399865c837";

        public const string Ticket1 = "AAAXXXAASS!@#21235aasdfjKKLKJJaaassdfafvqerwqerqUIOUI";

        [TestMethod]
        public void TestCreateExportHistoryFromStatus()
        {
            const string error = "it was bad";
            var statusRecord = new ExportStatusRecord(RequestId1)
            {
                DataTypes = new[] {
                    Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
                    Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value
                },
                UserId = Puid1,
                Ticket = Ticket1,
                IsComplete = false,
                LastError = error,
                LastSessionEnd = DateTimeOffset.MinValue,
                LastSessionStart = DateTimeOffset.MinValue,
            };
            var submitDate = DateTimeOffset.UtcNow;
            var expected = new ExportStatusHistoryRecord()
            {
                Completed = null,
                Error = error,
                DataTypes = new[] {
                    Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
                    Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value
                },
                ExportId = RequestId1,
                RequestedAt = submitDate
            };

            ExportStatusHistoryRecord actual = ExportStatusConverter.CreateHistoryRecordFromStatus(statusRecord, submitDate);
            EqualityWithDataContractsHelper.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestFromExportRequestWithFromAndToDates()
        {
            var fromDate = new DateTimeOffset(2017, 05, 29, 0, 0, 0, new TimeSpan());
            var toDate = new DateTimeOffset(2017, 05, 29, 0, 0, 0, new TimeSpan());
            var expected = new ExportStatusRecord(RequestId1)
            {
                DataTypes = new[] {
                    Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
                    Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value
                },
                StartTime = fromDate,
                EndTime = toDate,
                UserId = Puid1,
                Ticket = Ticket1,
                IsComplete = false,
                LastError = null,
                LastSessionEnd = DateTimeOffset.MinValue,
                LastSessionStart = DateTimeOffset.MinValue,
                Flights = new[] { "foo" }
            };

            ExportStatusRecord actual = ExportStatusConverter.FromExportRequest(Puid1, RequestId1, Ticket1, new[] {
                Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
                Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value
            }, fromDate, toDate, new[] { "foo" });

            EqualityWithDataContractsHelper.AreEqual(expected, actual);
        }


        [TestMethod]
        public void TestFromExportRequestWithDatesOutsideAcceptedRange()
        {
            var fromDate = new DateTimeOffset(1242, 05, 29, 0, 0, 0, new TimeSpan());
            var toDate = new DateTimeOffset(7777, 05, 29, 0, 0, 0, new TimeSpan());
            var expected = new ExportStatusRecord(RequestId1)
            {
                DataTypes = new[] {
                    Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
                    Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value
                },
                StartTime = fromDate,
                EndTime = toDate,
                UserId = Puid1,
                Ticket = Ticket1,
                IsComplete = false,
                LastError = null,
                LastSessionStart = DateTimeOffset.MinValue,
                LastSessionEnd = DateTimeOffset.MinValue,
                Flights = new[] { "foo" }
            };

            ExportStatusRecord actual = ExportStatusConverter.FromExportRequest(Puid1, RequestId1, Ticket1, new[] {
                Policies.Current.DataTypes.Ids.PreciseUserLocation.Value,
                Policies.Current.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value
            }, fromDate, toDate, new[] { "foo" });

            EqualityWithDataContractsHelper.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TestRequestIdConversion()
        {
            DateTime now = DateTime.UtcNow;
            string requestId = ExportStorageProvider.GenerateExportId(now);
            DateTime reqDate = ExportStorageProvider.ConvertRequestIdToDateTime(requestId);
            Assert.AreEqual(now.ToString("yyMMddHHmmssffffff"), reqDate.ToString("yyMMddHHmmssffffff"));
        }
    }
}
