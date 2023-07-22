// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Utility;

    using Newtonsoft.Json;

    /// <summary>
    ///     an action that determins if the current day and time matches an input set of day of week,
    ///      time of day, and date rules      
    /// </summary>
    public sealed class TimeApplicabilityAction : ActionOp<TimeApplicabilityDef>
    {
        private const string DefaultTimeZoneId = "Pacific Standard Time";

        public const string ActionType = "APPLICABILITY-TIME";

        private TimeApplicabilityDef def;

        /// <summary>
        ///     Initializes a new instance of the ActionTimeApplicability class
        /// </summary>
        /// <param name="modelManipulator">model manipulator</param>
        public TimeApplicabilityAction(
            IModelManipulator modelManipulator) :
            base(modelManipulator)
        {
        }

        /// <summary>
        ///     Gets the action type
        /// </summary>
        public override string Type => TimeApplicabilityAction.ActionType;

        /// <summary>
        ///     Allows a derived type to perform validation on the definition object created during parsing
        /// </summary>
        /// <param name="context">parse context</param>
        /// <param name="factory">action factory</param>
        /// <param name="definition">definition object</param>
        /// <returns>true if the parse was successful, false if at least one error was found</returns>
        protected override bool ProcessAndStoreDefinition(
            IParseContext context,
            IActionFactory factory,
            TimeApplicabilityDef definition)
        {
            bool result = base.ProcessAndStoreDefinition(context, factory, definition);
            this.def = definition;
            return result;
        }

        /// <summary>
        ///     Executes the action using the specified input
        /// </summary>
        /// <param name="context">execution context</param>
        /// <param name="actionRef">action reference</param>
        /// <param name="model">model from previous actions in the containing action set</param>
        /// <returns>execution result</returns>
        protected override Task<(bool Continue, object Result)> ExecuteInternalAsync(
            IExecuteContext context,
            ActionRefCore actionRef,
            object model)
        {
            const string ResultMsg =
                "Time applicability check evaluated at {0:yyyy-MM-dd HH:mm:ss} ({1}, {2}) in time zone [{3}] indicates " + 
                "processing should {4}continue";

            object args = this.ModelManipulator.MergeModels(context, model, null, actionRef.ArgTransform);
            Args argsActual = Utility.ExtractObject<Args>(context, this.ModelManipulator, args);

            DateTimeOffset nowTimeZone;
            DateTimeOffset nowRaw = argsActual.Now ?? context.NowUtc;
            DayOfWeekExt dow;
            DayOfWeekExt dayOrEnd;

            bool? result = null;

            nowTimeZone = TimeZoneInfo.ConvertTime(nowRaw, argsActual.TimeZone);

            (dayOrEnd, dow) = TimeApplicabilityAction.GetDayOfWeek(nowTimeZone.DayOfWeek);

            // do the overrides first because that can both allow and deny specific dates. If we get an explicit allow or deny
            //  from the overrides, no need to process the allowed date and time
            if (this.def.Overrides != null &&
                this.def.Overrides.TryGetValue(nowTimeZone.Date, out ICollection<TimeRangeOverride> overrides))
            {
                TimeSpan tod = nowTimeZone.TimeOfDay;

                bool? IsAllowed(bool excludeValueToFilter)
                {
                    foreach (TimeRangeOverride item in overrides.Where(o => o.Exclude == excludeValueToFilter))
                    {
                        if ((item.Start.HasValue == false || item.Start.Value < tod) &&
                            (item.End.HasValue == false || item.End.Value >= tod))
                        {
                            const string Msg =
                                "Override range {0:yyyy-MM-dd} [{1:hh\\:mm} to {2:hh\\:mm}] (evaluated at " +
                                "{3:HH:mm:ss} in time zone [{4}]) explicitly {5} continuning to process the " +
                                "containing action set.";

                            context.Log(
                                Msg.FormatInvariant(
                                    nowTimeZone.Date,
                                    item.Start ?? TimeSpan.Zero,
                                    item.End ?? TimeSpan.FromHours(24), // force EoD to look different than beginning of day
                                    nowTimeZone,
                                    argsActual.TimeZone.DisplayName,
                                    item.Exclude ? "forbids" : "permits"));

                            return item.Exclude == false;
                        }
                    }

                    return null;
                }

                // process the excluded ranges first and only process the include ones if we don't find a match in the exclude
                //  ones- this means that exclude always wins over include, which is the documented behavior
                if (overrides?.Count > 0)
                {
                    result = IsAllowed(true) ?? IsAllowed(false);
                }
            }

            if (result.HasValue == false)
            {
                if (this.def.AllowedDaysAndTimes != null)
                {
                    bool IsAllowed(DayOfWeekExt dowLocal)
                    {
                        if (this.def.AllowedDaysAndTimes.TryGetValue(dowLocal, out ICollection<TimeRange> ranges))
                        {
                            TimeSpan tod = nowTimeZone.TimeOfDay;

                            foreach (TimeRange item in ranges)
                            {
                                if ((item.Start.HasValue == false || item.Start.Value < tod) &&
                                    (item.End.HasValue == false || item.End.Value >= tod))
                                {
                                    const string Msg =
                                        "Time range {0} [{1:hh\\:mm} to {2:hh\\:mm}] (evaluated at {3:HH:mm:ss} in time zone " +
                                        "[{4}]) permits continuning to process the containing action set.";

                                    context.Log(
                                        Msg.FormatInvariant(
                                            dowLocal,
                                            item.Start ?? TimeSpan.Zero,
                                            item.End ?? TimeSpan.FromHours(24), // force EoD to look different than beginning of day
                                            nowTimeZone,
                                            argsActual.TimeZone.DisplayName));

                                    return true;
                                }
                            }
                        }

                        return false;
                    }

                    result = IsAllowed(dow) || IsAllowed(dayOrEnd);
                }

                result = result ?? false;
            }
            
            context.Log(
                ResultMsg.FormatInvariant(
                    nowTimeZone,
                    dow,
                    dayOrEnd,
                    argsActual.TimeZone.DisplayName,
                    result.Value ? string.Empty : "NOT "));

            // this has no result object.  It's only purpose is to return a continue / stop processing
            return Task.FromResult((result.Value, (object)null));
        }

        /// <summary>
        ///     Gets the day of week
        /// </summary>
        /// <param name="dow">day of week</param>
        /// <returns>resulting value</returns>
        private static (DayOfWeekExt DayOrEnd, DayOfWeekExt DoW) GetDayOfWeek(DayOfWeek dow)
        {
            switch (dow)
            {
                case DayOfWeek.Sunday: return (DayOfWeekExt.Weekend, DayOfWeekExt.Sunday);
                case DayOfWeek.Monday: return (DayOfWeekExt.Weekday, DayOfWeekExt.Monday);
                case DayOfWeek.Tuesday: return (DayOfWeekExt.Weekday, DayOfWeekExt.Tuesday);
                case DayOfWeek.Wednesday: return (DayOfWeekExt.Weekday, DayOfWeekExt.Wednesday);
                case DayOfWeek.Thursday: return (DayOfWeekExt.Weekday, DayOfWeekExt.Thursday);
                case DayOfWeek.Friday: return (DayOfWeekExt.Weekday, DayOfWeekExt.Friday);
                case DayOfWeek.Saturday: return (DayOfWeekExt.Weekend, DayOfWeekExt.Saturday);
            }

            return (DayOfWeekExt.Invalid, DayOfWeekExt.Invalid);
        }

        /// <summary>
        ///     Action arguments
        /// </summary>
        internal class Args : IValidatable
        {
            public DateTimeOffset? Now { get; set; }
            public string TimeZoneId { get; set; }

            [JsonIgnore]
            public TimeZoneInfo TimeZone { get; set; }

            /// <summary>
            ///     Validates and normalizes the argument object and logs any errors to the context
            /// </summary>
            /// <param name="context">execution context</param>
            /// <returns>true if the object validated successfully; false otherwise</returns>
            public bool ValidateAndNormalize(IContext context)
            {
                bool result = true;

                if (string.IsNullOrWhiteSpace(this.TimeZoneId))
                {
                    this.TimeZoneId = TimeApplicabilityAction.DefaultTimeZoneId;
                }

                try
                {
                    this.TimeZone = TimeZoneInfo.FindSystemTimeZoneById(this.TimeZoneId);
                }
                catch (Exception e) when (e is TimeZoneNotFoundException || e is InvalidTimeZoneException)
                {
                    context.LogError(this.TimeZoneId + " is not a known time zone");
                    result = false;
                }

                return result;
            }
        }
    }
}
