// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Web;

    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V1;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.PrivacyServices.Policy;

    using DateOption = Microsoft.Membership.MemberServices.PrivacyAdapters.DateOption;
    using OrderByType = Microsoft.Membership.MemberServices.PrivacyAdapters.OrderByType;

    /// <summary>
    ///     Request Validation Helper
    /// </summary>
    public static class RequestValidation
    {
        private const string InvalidFilterErrorMessage = "Invalid 'filter' specified.";

        public static DateTime SetUnspecifiedTimeZoneToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            return dateTime;
        }

        /// <summary>
        ///     Validates the input parameters are valid and returns an <see cref="Error" /> if they are not, otherwise returns null if they are valid.
        /// </summary>
        /// <param name="orderBy">The order by.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="orderByType">Type of the order by.</param>
        /// <param name="dateOption">The date option.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="timeZoneOffset">The time zone offset.</param>
        /// <param name="filterRequired">if set to <c>true</c>, filter is required.</param>
        /// <returns>
        ///     An <see cref="Error" /> if the input is invalid, otherwise returns null for valid.
        /// </returns>
        public static Error ValidateInputParams(
            string orderBy,
            string filter,
            out OrderByType? orderByType,
            out DateOption? dateOption,
            out DateTime? startDate,
            out DateTime? endDate,
            TimeSpan? timeZoneOffset = null,
            bool filterRequired = true)
        {
            dateOption = null;
            startDate = null;
            endDate = null;

            Error error = ValidateAndParseOrderBy(orderBy, out orderByType);

            if (error != null)
            {
                return error;
            }

            error = ValidateAndParseFilter(filter, out dateOption, out startDate, out endDate, filterRequired);

            if (error != null)
            {
                return error;
            }

            if (timeZoneOffset.HasValue &&
                (timeZoneOffset < MinTimeZoneOffset || timeZoneOffset > MaxTimeZoneOffset))
            {
                return new Error(ErrorCode.InvalidInput, $"Invalid TimeZoneOffset specified. Must be between {MinTimeZoneOffset} and {MaxTimeZoneOffset}");
            }

            return null;
        }

        /// <summary>
        ///     Validates the request.
        /// </summary>
        /// <param name="deleteRequest">The delete request of type <see cref="DeleteRequestV1" />.</param>
        /// <returns>An <see cref="Error" /> if validation errors occurred; else null for no errors.</returns>
        public static Error ValidateRequest(DeleteRequestV1 deleteRequest)
        {
            if (null == deleteRequest)
            {
                return new Error(ErrorCode.InvalidInput, "Parameter 'deleteRequest' must be specified.");
            }

            if (!string.IsNullOrWhiteSpace(deleteRequest.DateRangeStart) ||
                !string.IsNullOrWhiteSpace(deleteRequest.DateRangeEnd) ||
                deleteRequest.ResourceIds != null)
            {
                return new Error(ErrorCode.InvalidInput, "Delete requests currently only support 'deleteAll'.");
            }

            return null;
        }

        // Per MSDN, the min/max timezone offsets are between -14 to 14
        // https://msdn.microsoft.com/en-us/library/system.datetimeoffset.offset(v=vs.110).aspx
        private static TimeSpan MaxTimeZoneOffset => new TimeSpan(14, 0, 0);

        private static TimeSpan MinTimeZoneOffset => new TimeSpan(-14, 0, 0);

        /// <summary>
        ///     Validates the specified input values are valid and parses into the filter properties specified as out params.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="dateOption">The date option.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="filterRequired">if set to <c>true</c>, filter is required.</param>
        /// <returns><see cref="Error" /> if an error occurred during parsing, otherwise null for success.</returns>
        internal static Error ValidateAndParseFilter(string filter, out DateOption? dateOption, out DateTime? startDate, out DateTime? endDate, bool filterRequired)
        {
            dateOption = null;
            startDate = null;
            endDate = null;

            if (!filterRequired && string.IsNullOrWhiteSpace(filter))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(filter))
            {
                return new Error(ErrorCode.InvalidInput, InvalidFilterErrorMessage)
                {
                    ErrorDetails = "Filter was null or whitespace. Filter is required."
                };
            }

            filter = HttpUtility.UrlDecode(filter);
            if (string.IsNullOrWhiteSpace(filter))
            {
                return new Error(ErrorCode.InvalidInput, InvalidFilterErrorMessage)
                {
                    ErrorDetails = "Filter was null or whitespace after url decoding."
                };
            }

            try
            {
                DateFilterResult dateFilterResult = DateFilterResult.ParseFromFilter(filter);
                startDate = dateFilterResult.StartDate;

                switch (dateFilterResult.Comparison)
                {
                    case Comparison.Equal:
                        dateOption = DateOption.SingleDay;
                        break;
                    case Comparison.Between:
                        dateOption = DateOption.Between;

                        // endDate is only set on the 'between' filter.
                        endDate = dateFilterResult.EndDate;
                        break;
                    default:
                        return new Error(ErrorCode.InvalidInput, InvalidFilterErrorMessage)
                        {
                            ErrorDetails = "Invalid comparison option: " + dateFilterResult.Comparison
                        };
                }
            }
            catch (NotSupportedException ex)
            {
                return new Error(ErrorCode.InvalidInput, InvalidFilterErrorMessage)
                {
                    // inner error contains the stack trace
                    ErrorDetails = ex.Message,
                    InnerError = new Error(ErrorCode.InvalidInput, ex.ToString())
                };
            }

            return null;
        }

        /// <summary>
        ///     Validates the specified input value is valid and parses the output to the out param.
        /// </summary>
        /// <param name="orderBy">The order by value.</param>
        /// <param name="orderByTypeNullable">The order by type (nullable).</param>
        /// <returns>
        ///     <see cref="Error" /> if an error occurred during parsing, otherwise null for success.
        /// </returns>
        internal static Error ValidateAndParseOrderBy(string orderBy, out OrderByType? orderByTypeNullable)
        {
            orderByTypeNullable = null;

            // Since orderBy is optional, only try and parse if specified in the request.
            if (string.IsNullOrWhiteSpace(orderBy))
            {
                return null;
            }

            OrderByType orderByType;
            if (!Enum.TryParse(
                orderBy,
                ignoreCase: true,
                result: out orderByType))
            {
                string errorMessage = string.Format(
                    CultureInfo.InvariantCulture,
                    "Unable to parse 'orderBy' to a valid enumeration value. The specified input value was: {0}",
                    orderBy);
                return new Error(ErrorCode.InvalidInput, "Invalid 'orderBy' specified.") { ErrorDetails = errorMessage };
            }

            if (orderByType == OrderByType.SearchTerms)
            {
                return new Error(ErrorCode.InvalidInput, "Sorting by SearchTerms is not supported.");
            }

            orderByTypeNullable = orderByType;
            return null;
        }

        internal static Error ValidateIndividualDeleteRequest(List<TimelineCard> cards)
        {
            foreach (TimelineCard c in cards)
            {
                if (c is BrowseCard browseCard)
                {
                    foreach (BrowseCard.Navigation browseCardNavigation in browseCard.Navigations)
                    {
                        if (string.IsNullOrWhiteSpace(browseCardNavigation?.UriHash))
                        {
                            return new Error(
                                ErrorCode.InvalidInput,
                                $"The {browseCard.GetType().FullName} requires a valid UriHash for doing individual deletes. The value provided for card id {c.Id} was null or empty.");
                        }
                    }
                }
                else if (c is AppUsageCard appUsageCard)
                {
                    // Timestamp is midnight
                    // EndTimestamp is midnight +24 hours (or maybe +30 days)
                    if (appUsageCard.Timestamp.ToUniversalTime() > appUsageCard.EndTimestamp.ToUniversalTime())
                    {
                        // If the timestamp is greater than the endtime, then the time range is inverted
                        // This would be a problem or at the very least confusion for agents, so reject it.

                        return new Error(
                            ErrorCode.InvalidInput,
                            $"Usage start is {appUsageCard.Timestamp} which is greater than the usage end at {appUsageCard.EndTimestamp}");
                    }
                }

                // If any other data types required additional input validation, do that here.
            }

            return null;
        }

        internal static Error ValidatePrivacyDataTypes(IList<string> types, Policy policy)
        {
            var invalidDataTypes = new StringBuilder();

            foreach (string type in types)
            {
                DataTypeId dataTypeId;
                if (!policy.DataTypes.TryCreateId(type, out dataTypeId))
                {
                    if (invalidDataTypes.Length > 0)
                    {
                        invalidDataTypes.Append(", ");
                    }

                    invalidDataTypes.Append(type);
                }
            }

            if (invalidDataTypes.Length > 0)
            {
                return new Error(ErrorCode.InvalidInput, $"Invalid privacy-data-type(s) specified in the request: {invalidDataTypes}");
            }

            return null;
        }
    }
}
