// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.Helpers
{
    /// <summary>
    /// Privacy-SourceId-Constants
    /// </summary>
    public static class PrivacySourceId
    {
        /// <summary>
        /// Personal Data Platform (PDP): Bing Location Inference Service (BLIS): Bing First Page Results (BFPR)
        /// </summary>
        public const string PdpBlisBfpr = "PdpBlisBfpr";

        /// <summary>
        /// Personal Data Platform (PDP): Cortana Locations
        /// </summary>
        public const string PdpCortanaLocations = "PdpCortanaLocations";

        /// <summary>
        /// Personal Data Platform (PDP): Bing Location Inference Service (BLIS): Last Known Good (LKG)
        /// </summary>
        public const string PdpBlisLkg = "PdpBlisLkg";

        /// <summary>
        /// Personal Data Platform (PDP): Bing Search History
        /// </summary>
        public const string PdpSearchHistory = "PdpSearchHistory";

        /// <summary>
        /// Personal Data Platform (PDP): Edge Browse History
        /// </summary>
        public const string PdpBrowseHistory = "PdpBrowseHistory";

        /// <summary>
        /// Membership Devices: Device Directory Service (DDS)
        /// </summary>
        public const string DDS = "DDS";

        /// <summary>
        /// Microsoft Health
        /// TODO: Remove this once PXF config is updated to the correct name (MicrosoftHealth), PXF config is updated in PROD, and client is using the correct name (MicrosoftHealth)
        /// </summary>
        public const string MicrosoftFitness = "MicrosoftFitness";

        /// <summary>
        /// MicrosoftHealth
        /// </summary>
        public const string MicrosoftHealth = "MicrosoftHealth";

        /// <summary>
        /// Cortana Notebook
        /// </summary>
        public const string CortanaNotebook = "CortanaNotebook";
    }
}