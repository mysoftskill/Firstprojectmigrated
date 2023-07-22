// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PXS.Command.Contracts.V1
{
    /// <summary>
    ///     Constants for the different portals known.
    /// </summary>
    public class Portals
    {
        /// <summary>
        ///     The feed for AAD account close events.
        /// </summary>
        public const string AadAccountCloseEventSource = "AADAccountCloseEventSource";

        /// <summary>
        ///     AKA account.microsoft.com
        /// </summary>
        public const string Amc = "AMC";

        /// <summary>
        ///     Bing
        /// </summary>
        public const string Bing = "Bing";

        /// <summary>
        ///     The feed for MSA account close events.
        /// </summary>
        public const string MsaAccountCloseEventSource = "MSAAccountCloseEventSource";

        /// <summary>
        ///     MSGraph
        /// </summary>
        public const string MsGraph = "MSGraph";

        /// <summary>
        ///     The partner test page on AMC
        /// </summary>
        public const string PartnerTestPage = "PartnerTestPage";

        /// <summary>
        ///     PCD (Privacy Compliance Dashboard)
        /// </summary>
        public const string Pcd = "PCD";

        /// <summary>
        ///     An unknown portal.
        /// </summary>
        public const string Unknown = "Unknown";

        /// <summary>
        ///     The feed for device delete signals
        /// </summary>
        public const string VortexDeviceDeleteSignal = "VortexDeviceDeleteSignal";

        /// <summary>
        ///     The feed for Edge Browser device delete signals
        /// </summary>
        public const string EdgeBrowserDeviceDeleteSignal = "EdgeBrowserDeviceDeleteSignal";

        /// <summary>
        ///     The feed for Recurring device delete signals
        /// </summary>
        public const string RecurringDeleteSignal = "RecurringDeleteSignal";
    }
}
