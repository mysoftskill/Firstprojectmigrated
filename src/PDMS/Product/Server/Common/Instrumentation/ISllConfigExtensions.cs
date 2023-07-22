namespace Microsoft.PrivacyServices.DataManagement.Common.Instrumentation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;

    /// <summary>
    /// Extension methods to the <c>ISllConfig</c> Parallax objects.
    /// </summary>
    public static class ISllConfigExtensions
    {
        /// <summary>
        /// Parses the event levels in a safe manner. Filter any invalid values.
        /// </summary>
        /// <param name="config">The config object.</param>
        /// <returns>The parsed data.</returns>
        public static IEnumerable<EventLevel> ParsedEventLevels(this ISllConfig config)
        {
            if (config.EventLevels == null)
            {
                return new EventLevel[0];
            }
            else
            {
                return config.EventLevels.SelectMany(v =>
                {
                    EventLevel value;
                    if (Enum.TryParse(v, true, out value))
                    {
                        return new[] { value };
                    }
                    else
                    {
                        return new EventLevel[0];
                    }
                });
            }
        }

        /// <summary>
        /// Parses the path by expanding any environment variables.
        /// </summary>
        /// <param name="config">The config object.</param>
        /// <returns>The parsed data.</returns>
        public static string ParsedLocalLogDir(this ISllConfig config)
        {
            return Environment.ExpandEnvironmentVariables(config.LocalLogDir);
        }
    }
}