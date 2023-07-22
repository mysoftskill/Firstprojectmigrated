// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using System;

    using Microsoft.Membership.MemberServices.Common.Utilities;
    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     provides read only access to environment models
    /// </summary>
    public class EnvironmentModelReader : IModelReader
    {
        private readonly IClock clock;

        /// <summary>
        ///     Initializes a new instance of the EnvironmentModelReader class
        /// </summary>
        /// <param name="clock">clock</param>
        public EnvironmentModelReader(IClock clock)
        {
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>
        ///     extracts a value from the model
        /// </summary>
        /// <typeparam name="T">type of result value</typeparam>
        /// <param name="context">model context</param>
        /// <param name="source">model to extract a value from</param>
        /// <param name="selector">path to value</param>
        /// <param name="defaultValue">default value to assign if the value could not be found</param>
        /// <param name="result">receives the resulting value if found</param>
        /// <returns>true if the requested object could be found, false otherwise</returns>
        public bool TryExtractValue<T>(
            IContext context,
            object source, 
            string selector, 
            T defaultValue, 
            out T result)
        {
            const string NowLocalStart = "#.Time.Now.Local(\"";
            const string NowLocalEnd = "\")";
            const string NowUtc = "#.Time.Now.Utc";

            result = defaultValue;

            if (selector != null)
            {
                selector = selector.Trim();

                if (NowUtc.Equals(selector, StringComparison.Ordinal))
                {
                    result = (T)(object)this.clock.UtcNow;
                    return true;
                }

                if (selector.StartsWith(NowLocalStart, StringComparison.Ordinal) &&
                    selector.EndsWith(NowLocalEnd, StringComparison.Ordinal))
                {
                    TimeZoneInfo tzi;
                    string timeZoneId;
                    int startIndex = NowLocalStart.Length;
                    int length = selector.Length - (startIndex + NowLocalEnd.Length);

                    timeZoneId = selector.Substring(startIndex, length);

                    tzi = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    result = (T)(object)TimeZoneInfo.ConvertTime(this.clock.UtcNow, tzi);
                    return true;
                }
            }

            return false;
        }
    }
}
