using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.PrivacyServices.UX.Configuration;

namespace Microsoft.PrivacyServices.UX.Models.AppBootstrap
{
    /// <summary>
    /// NGP lockdown configuration.
    /// </summary>
    public sealed class NgpLockdownModel
    {
        /// <summary>
        /// Gets or sets value indicating whether the lockdown is in effect.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets value indicating when lockdown is started (UTC timestamp in ISO format).
        /// </summary>
        public string StartedUtc { get; set; }

        /// <summary>
        /// Gets or sets value indicating when lockdown is ended (UTC timestamp in ISO format).
        /// </summary>
        public string EndedUtc { get; set; }

        /// <summary>
        /// Creates and configures an instance of the <see cref="NgpLockdownModel"/> class using an instance of <see cref="INgpLockdownConfig"/>.
        /// </summary>
        /// <param name="ngpLockdownConfig">Config to create a new model from.</param>
        public static NgpLockdownModel CreateFromConfig(INgpLockdownConfig ngpLockdownConfig)
        {
            if (NgpLockdownKind.Forced == ngpLockdownConfig.Kind)
            {
                return new NgpLockdownModel
                {
                    IsActive = true,
                    StartedUtc = FormatTimestampAsIsoDateString(DateTimeOffset.UtcNow),
                    EndedUtc = FormatTimestampAsIsoDateString(DateTimeOffset.UtcNow.AddYears(1))
                };
            }

            if (DateTimeOffset.TryParse(ngpLockdownConfig.StartedUtc, out var startedUtc) && DateTimeOffset.TryParse(ngpLockdownConfig.EndedUtc, out var endedUtc))
            {
                var now = DateTimeOffset.UtcNow;

                if (now >= startedUtc && now <= endedUtc)
                {
                    return new NgpLockdownModel
                    {
                        IsActive = true,
                        StartedUtc = FormatTimestampAsIsoDateString(startedUtc),
                        EndedUtc = FormatTimestampAsIsoDateString(endedUtc)
                    };
                }
            }

            return new NgpLockdownModel();

            string FormatTimestampAsIsoDateString(DateTimeOffset timestamp) => timestamp.Date.ToString("o");
        }
    }
}
