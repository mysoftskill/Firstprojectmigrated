// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.PrivacyExperience.Test.Common;
    using Microsoft.PrivacyServices.Policy;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using DateOption = Microsoft.Membership.MemberServices.PrivacyAdapters.DateOption;
    using OrderByType = Microsoft.Membership.MemberServices.PrivacyAdapters.OrderByType;

    /// <summary>
    ///     RequestValidationHelper Test
    /// </summary>
    [TestClass]
    public class RequestValidationTest
    {
        private const string InvalidFilterSpecified = "Invalid 'filter' specified.";

        private readonly Policy policy = Policies.Current;

        [TestMethod]
        public void ValidateAndParseFilterBetween()
        {
            // test ge and le translates to between
            string filter = "date ge datetime'2016-04-01' and date le datetime'2016-04-02'";
            Error expectedError = null;

            DateOption? dateOption;
            DateTime? startDate, endDate;
            Error actualError = RequestValidation.ValidateAndParseFilter(filter, out dateOption, out startDate, out endDate, true);

            EqualityHelper.AreEqual(expectedError, actualError);
            Assert.AreEqual(DateOption.Between, dateOption);
            Assert.AreEqual(new DateTime(2016, 4, 1, 0, 0, 0, DateTimeKind.Utc), startDate);
            Assert.AreEqual(new DateTime(2016, 4, 2, 0, 0, 0, DateTimeKind.Utc), endDate);
        }

        [TestMethod]
        public void ValidateAndParseFilterInvalidGreaterThanBetween()
        {
            // test gt returns error
            string filter = "date gt datetime'2016-04-01' and date lt datetime'2016-04-02'";
            var expectedError = new Error(ErrorCode.InvalidInput, InvalidFilterSpecified)
            {
                ErrorDetails = "Unsupported comparison type 'GreaterThan' in filter expression."
            };

            DateOption? dateOption;
            DateTime? startDate, endDate;
            Error actualError = RequestValidation.ValidateAndParseFilter(filter, out dateOption, out startDate, out endDate, true);

            EqualityHelper.AreEqual(expectedError, actualError, validateInnerError: false);
        }

        [TestMethod]
        public void ValidateAndParseFilterInvalidLessThanBetween()
        {
            // test lt returns error
            string filter = "date ge datetime'2016-04-01' and date lt datetime'2016-04-02'";
            var expectedError = new Error(ErrorCode.InvalidInput, InvalidFilterSpecified)
            {
                ErrorDetails = "Unsupported comparison type 'LessThan' in filter expression."
            };

            DateOption? dateOption;
            DateTime? startDate, endDate;
            Error actualError = RequestValidation.ValidateAndParseFilter(filter, out dateOption, out startDate, out endDate, true);

            EqualityHelper.AreEqual(expectedError, actualError, validateInnerError: false);
        }

        [TestMethod]
        public void ValidateAndParseFilterInvalidUrlDecode()
        {
            string filter = "+";
            var expectedError = new Error(ErrorCode.InvalidInput, InvalidFilterSpecified)
            {
                ErrorDetails = "Filter was null or whitespace after url decoding."
            };

            DateOption? dateOption;
            DateTime? startDate, endDate;
            Error actualError = RequestValidation.ValidateAndParseFilter(filter, out dateOption, out startDate, out endDate, true);

            EqualityHelper.AreEqual(expectedError, actualError);
        }

        [TestMethod]
        public void ValidateAndParseFilterSingleDay()
        {
            // test equals translates to single day
            string filter = "date eq datetime'2016-04-01'";
            Error expectedError = null;

            DateOption? dateOption;
            DateTime? startDate, endDate;
            Error actualError = RequestValidation.ValidateAndParseFilter(filter, out dateOption, out startDate, out endDate, true);

            EqualityHelper.AreEqual(expectedError, actualError);
            Assert.AreEqual(DateOption.SingleDay, dateOption);
            Assert.AreEqual(new DateTime(2016, 4, 1, 0, 0, 0, DateTimeKind.Utc), startDate);
            Assert.AreEqual(null, endDate);
        }

        [TestMethod]
        public void ValidateAndParseFilterSingleDayUrlEncoded()
        {
            // test url encoded filter value
            string filter = "date+eq+datetime'2016-04-01'";
            Error expectedError = null;

            DateOption? dateOption;
            DateTime? startDate, endDate;
            Error actualError = RequestValidation.ValidateAndParseFilter(filter, out dateOption, out startDate, out endDate, true);

            EqualityHelper.AreEqual(expectedError, actualError);
            Assert.AreEqual(DateOption.SingleDay, dateOption);
            Assert.AreEqual(new DateTime(2016, 4, 1, 0, 0, 0, DateTimeKind.Utc), startDate);
            Assert.AreEqual(null, endDate);
        }

        [TestMethod]
        public void ValidateAndParseOrderByError()
        {
            OrderByType? orderBy;

            // test non-allowed values
            Error actualError = RequestValidation.ValidateAndParseOrderBy("SearchTerms", out orderBy);
            var invalidSearchTermsError = new Error(ErrorCode.InvalidInput, "Sorting by SearchTerms is not supported.");
            EqualityHelper.AreEqual(invalidSearchTermsError, actualError);

            var expectedError = new Error(ErrorCode.InvalidInput, "Invalid 'orderBy' specified.");
            const string ErrorMessageFormat = "Unable to parse 'orderBy' to a valid enumeration value. The specified input value was: {0}";
            string testValue = "Date Time";
            actualError = RequestValidation.ValidateAndParseOrderBy(testValue, out orderBy);
            expectedError.ErrorDetails = string.Format(CultureInfo.InvariantCulture, ErrorMessageFormat, testValue);
            EqualityHelper.AreEqual(expectedError, actualError);

            testValue = "abc";
            actualError = RequestValidation.ValidateAndParseOrderBy(testValue, out orderBy);
            expectedError.ErrorDetails = string.Format(CultureInfo.InvariantCulture, ErrorMessageFormat, testValue);
            EqualityHelper.AreEqual(expectedError, actualError);
        }

        [TestMethod]
        public void ValidateAndParseOrderByNull()
        {
            OrderByType? orderBy;
            Error actualError = RequestValidation.ValidateAndParseOrderBy(null, out orderBy);
            EqualityHelper.AreEqual(null, actualError);
            Assert.AreEqual(null, orderBy);

            actualError = RequestValidation.ValidateAndParseOrderBy(string.Empty, out orderBy);
            EqualityHelper.AreEqual(null, actualError);
            Assert.AreEqual(null, orderBy);

            actualError = RequestValidation.ValidateAndParseOrderBy("          ", out orderBy);
            EqualityHelper.AreEqual(null, actualError);
            Assert.AreEqual(null, orderBy);
        }

        [TestMethod]
        public void ValidateAndParseOrderBySuccess()
        {
            OrderByType? orderBy;

            // test allowed values
            Assert.IsNull(RequestValidation.ValidateAndParseOrderBy("DateTime", out orderBy));

            // test uppercase
            Assert.IsNull(RequestValidation.ValidateAndParseOrderBy("DATETIME", out orderBy));
        }

        [TestMethod]
        public void ValidateAppUsageTimeRange()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var greaterThanCard = new AppUsageCard(null, null, null, null, null, null, now, now.AddDays(1).AddTicks(-1), null, null, null);
            var sameCard = new AppUsageCard(null, null, null, null, null, null, now, now, null, null, null);
            var lessThanCard = new AppUsageCard(null, null, null, null, null, null, now, now.AddDays(-1).AddTicks(1), null, null, null);

            Error greaterThanError = RequestValidation.ValidateIndividualDeleteRequest(new List<TimelineCard> { greaterThanCard });
            Error sameError = RequestValidation.ValidateIndividualDeleteRequest(new List<TimelineCard> { sameCard });
            Error lessThanError = RequestValidation.ValidateIndividualDeleteRequest(new List<TimelineCard> { lessThanCard });

            Assert.IsNull(greaterThanError);
            Assert.IsNull(sameError);
            Assert.IsNotNull(lessThanError);
            Assert.AreEqual(ErrorCode.InvalidInput.ToString(), lessThanError.Code);
        }

        [TestMethod]
        public void ValidatePrivacyDataTypes_Error()
        {
            // Arrange
            var expectedError = new Error(ErrorCode.InvalidInput, "Invalid privacy-data-type(s) specified in the request: randomString, andAnother");
            var types = new List<string>
            {
                "randomString",
                "andAnother"
            };

            // Act
            Error errorResponse = RequestValidation.ValidatePrivacyDataTypes(types, this.policy);

            // Assert
            Assert.IsNotNull(errorResponse);
            EqualityHelper.AreEqual(expectedError, errorResponse);
        }

        [TestMethod]
        public void ValidatePrivacyDataTypes_Valid()
        {
            var types = new List<string>
            {
                this.policy.DataTypes.Ids.PreciseUserLocation.Value,
                this.policy.DataTypes.Ids.BrowsingHistory.Value
            };

            Error errorResponse = RequestValidation.ValidatePrivacyDataTypes(types, this.policy);
            Assert.IsNull(errorResponse);
        }

        [TestMethod]
        public void ValidateRequestDeleteRequestV1NullTest()
        {
            var expectedError = new Error(ErrorCode.InvalidInput, "Parameter 'deleteRequest' must be specified.");
            Error errorResponse = RequestValidation.ValidateRequest(null);
            EqualityHelper.AreEqual(expectedError, errorResponse);
        }

        [TestMethod]
        public void ValidateRequestDeleteRequestV1TooManyParamsTest()
        {
            var expectedError = new Error(ErrorCode.InvalidInput, "Delete requests currently only support 'deleteAll'.");

            var deleteRequest = new DeleteRequestV1 { DeleteAll = true, DateRangeStart = "2016-01-01" };
            Error errorResponse = RequestValidation.ValidateRequest(deleteRequest);
            EqualityHelper.AreEqual(expectedError, errorResponse);

            deleteRequest = new DeleteRequestV1 { DeleteAll = true, DateRangeEnd = "2016-01-01" };
            errorResponse = RequestValidation.ValidateRequest(deleteRequest);
            EqualityHelper.AreEqual(expectedError, errorResponse);

            deleteRequest = new DeleteRequestV1 { DeleteAll = true, ResourceIds = new[] { "123" } };
            errorResponse = RequestValidation.ValidateRequest(deleteRequest);
            EqualityHelper.AreEqual(expectedError, errorResponse);

            deleteRequest = new DeleteRequestV1 { DateRangeStart = "2016-01-01", ResourceIds = new[] { "123" } };
            errorResponse = RequestValidation.ValidateRequest(deleteRequest);
            EqualityHelper.AreEqual(expectedError, errorResponse);

            deleteRequest = new DeleteRequestV1 { DeleteAll = true, DateRangeStart = "2016-01-01", ResourceIds = new[] { "123" } };
            errorResponse = RequestValidation.ValidateRequest(deleteRequest);
            EqualityHelper.AreEqual(expectedError, errorResponse);
        }

        [TestMethod]
        public void ValidateRequestDeleteRequestV1ValidTest()
        {
            var deleteRequest = new DeleteRequestV1 { DeleteAll = true };
            Error errorResponse = RequestValidation.ValidateRequest(deleteRequest);
            EqualityHelper.AreEqual(null, errorResponse);
        }
    }
}
