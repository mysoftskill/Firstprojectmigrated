// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Common.PerfCounters
{
    /// <summary>
    ///     Category Names for the PerfCounters.
    /// </summary>
    public static class CounterCategoryNames
    {
        /// <summary>
        ///     Aad Account-Close
        /// </summary>
        public const string AadAccountClose = "AadAccountClose";

        /// <summary>
        ///     The AQS counter-category name.
        /// </summary>
        public const string Aqs = "aqs";

        /// <summary>
        ///     The Azure Queue counter-category name.
        /// </summary>
        public const string AzureQueue = "azure.queue";

        /// <summary>
        ///     The MSA Account-Close counter-category name.
        /// </summary>
        public const string MsaAccountClose = "msa.accountclose";

        /// <summary>
        ///     The MSA Account-Create counter-category name.
        /// </summary>
        public const string MsaAccountCreate = "msa.accountcreate";

        /// <summary>
        ///     The MSA Age Out counter-category name.
        /// </summary>
        public const string MsaAgeOut = "msa.ageout";

        /// <summary>
        ///     PcfAdapter
        /// </summary>
        public const string PcfAdapter = "PcfAdapter";

        /// <summary>
        ///     Counter category for counters related to Connections from PXS.
        /// </summary>
        public const string PrivacyExperienceServiceConnections = "PrivacyExperienceServiceConnections";

        /// <summary>
        ///     The PXS watchdog counter-category name.
        /// </summary>
        public const string PrivacyExperienceServiceWatchdog = "PrivacyExperienceServiceWatchdog";

        /// <summary>
        ///     The Vortex Device Delete counter-category name.
        /// </summary>
        public const string VortexDeviceDelete = "VortexDeviceDelete";

        /// <summary>
        ///     The Azure Event Hub counter-category name.
        /// </summary>
        public static string AzureEventHub = "azure.eventhub";
    }
}
