// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;

    public enum DayOfWeekExt
    {
        Invalid = 0,
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Weekday,
        Weekend,
    }

    /// <summary>
    ///     A allowed time range instance for use in a time applicability action
    /// </summary>
    public class TimeRange
    {
        /// <summary>
        ///     Gets or sets the inclusive start of the allowed span or null to start at the start of day
        /// </summary>
        public TimeSpan? Start { get; set; }

        /// <summary>
        ///     Gets or sets the exclusive end of the allowed span or null to end at the end of day
        /// </summary>
        public TimeSpan? End { get; set; }

        /// <summary>
        ///     Validates the argument object and logs any errors to the context
        /// </summary>
        /// <param name="context">execution context</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            if (this.Start.HasValue && 
                (this.Start.Value < TimeSpan.Zero || this.Start.Value.TotalHours >= 24))
            {
                context.LogError("start time must be a valid time of day");
                result = false;
            }

            // use of '>' instead of '>=' here is intentional because we want to allow a special value to indicate 'end of day'
            //  this isn't valid for the start of day because it doesn't make sense to start a time range at the end of day
            if (this.End.HasValue &&
                (this.End.Value < TimeSpan.Zero || this.End.Value.TotalHours > 24))
            {
                context.LogError("end time must be a valid time of day");
                result = false;
            }

            if (this.Start.HasValue && this.End.HasValue && this.Start.Value >= this.End.Value)
            {
                context.LogError("start time must be strictly before end time");
                result = false;
            }

            return result;
        }
    }

    /// <summary>
    ///     A time range override instance for use in a time applicability action
    /// </summary>
    public class TimeRangeOverride : TimeRange
    {
        /// <summary>
        ///     Gets or sets whether or not the time range should be excluded or not
        /// </summary>
        public bool Exclude { get; set; }
    }

    /// <summary>
    ///     definition of a time applicability action
    /// </summary>
    public class TimeApplicabilityDef : IValidatable
    {
        /// <summary>
        ///     Gets or sets allowed days and times
        /// </summary>
        /// <remarks>
        ///     if a day of week is not listed, it is excluded
        ///     if multiple ranges exist for a given day, overlapping ranges are ineffecient, but not forbidden
        /// </remarks>
        public IDictionary<DayOfWeekExt, ICollection<TimeRange>> AllowedDaysAndTimes { get; set; }

        /// <summary>
        ///     Gets or sets a set of overrides for specific dates to allow or exclude time ranges
        /// </summary>
        /// <remarks>
        ///     if a date is present in the override list, the AllowedDaysAndTimes list is ignored
        ///     if multiple ranges exist for a given day, overlapping ranges is not an error
        ///     if overlapping ranges exist for a given day and they have conflicting exclude values, exclusion wins
        /// </remarks>
        public IDictionary<DateTime, ICollection<TimeRangeOverride>> Overrides { get; set; }

        /// <summary>
        ///     Validates the argument object and logs any errors to the context
        /// </summary>
        /// <param name="context">execution context</param>
        /// <returns>true if the object validated successfully; false otherwise</returns>
        public bool ValidateAndNormalize(IContext context)
        {
            bool result = true;

            if (this.AllowedDaysAndTimes?.Count > 0)
            {
                foreach (KeyValuePair<DayOfWeekExt, ICollection<TimeRange>> item in this.AllowedDaysAndTimes)
                {
                    if (Enum.IsDefined(typeof(DayOfWeekExt), item.Key) == false || item.Key == DayOfWeekExt.Invalid)
                    {
                        context.LogError("[" + item.Key.ToStringInvariant() + "] is not a valid day of the week");
                        result = false;
                    }

                    context.PushErrorIntroMessage(() => "Errors found for " + item.Key + " allowed day");
                    result = TimeApplicabilityDef.ValidateRangeSet(context, item.Value) && result;
                    context.PopErrorIntroMessage();
                }
            }

            if (this.Overrides?.Count > 0)
            {
                foreach (KeyValuePair<DateTime, ICollection<TimeRangeOverride>> item in this.Overrides)
                {
                    if (item.Key.TimeOfDay != TimeSpan.Zero)
                    {
                        context.LogError(
                            "Override date [" + item.Key.ToString("yyyy-MM-dd HH:mm:ss") + "] must not specify a time of day");
                        result = false;
                    }

                    context.PushErrorIntroMessage(() => "Errors found for " + item.Key.ToString("yyyy-MM-dd") + " override date");
                    result = TimeApplicabilityDef.ValidateRangeSet(context, item.Value) && result;
                    context.PopErrorIntroMessage();
                }
            }

            return result;
        }

        /// <summary>
        ///     Validates the range set
        /// </summary>
        /// <typeparam name="T">type of TimeRange to validate</typeparam>
        /// <param name="context">execution context</param>
        /// <param name="set">time range set</param>
        /// <returns>resulting value</returns>
        private static bool ValidateRangeSet<T>(
            IContext context,
            ICollection<T> set)
            where T : TimeRange
        {
            bool result = true;

            if (set != null)
            {
                foreach (T t in set)
                {
                    result = t.ValidateAndNormalize(context) && result;
                }
            }

            return result;
        }
    }
}
