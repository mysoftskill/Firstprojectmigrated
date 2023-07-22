// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.Configuration
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     Privacy ConfigurationHelper
    /// </summary>
    public class PrivacyConfigurationHelper
    {
        /// <summary>
        ///     Enum containing lease extension types
        /// </summary>
        public enum LeaseExtensionTimeType
        {
            Minutes,

            Hours
        }

        /// <summary>
        ///     Method to build the full lease extension set of time spans from collection based on lease extension type
        /// </summary>
        /// <param name="leaseExtensionItems">collection of lease extension items</param>
        /// <param name="timeType">Lease type</param>
        /// <returns>list of time spans</returns>
        public static IList<TimeSpan> BuildFullLeaseExtensionSet(ICollection<string> leaseExtensionItems, LeaseExtensionTimeType timeType)
        {
            List<TimeSpan> result = new List<TimeSpan>();

            if (leaseExtensionItems != null)
            {
                foreach (string entry in leaseExtensionItems)
                {
                    string[] parts = entry.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);
                    int value;

                    if (parts.Length > 0 && int.TryParse(parts[0], out value) && value > 0)
                    {
                        TimeSpan fullVal;
                        switch (timeType)
                        {
                            case LeaseExtensionTimeType.Minutes:
                                fullVal = TimeSpan.FromMinutes(value);
                                break;
                            case LeaseExtensionTimeType.Hours:
                                fullVal = TimeSpan.FromHours(value);
                                break;
                            default:
                                fullVal = TimeSpan.FromSeconds(value);
                                break;
                        }

                        int repeat;

                        if (parts.Length == 1 || int.TryParse(parts[1], out repeat) == false)
                        {
                            repeat = 1;
                        }

                        for (int i = 0; i < repeat; ++i)
                        {
                            result.Add(fullVal);
                        }
                    }
                }
            }

            return result;
        }
    }
}
