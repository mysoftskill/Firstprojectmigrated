namespace Microsoft.PrivacyServices.DataManagement.Client.AAD
{
    using System.Collections.Generic;

    /// <summary>
    /// A collection of default constant values.
    /// </summary>
    public static class Defaults
    {
        /// <summary>
        /// Clients that live in MS Tenant and need to use old resourceIds,
        /// until they can transition to the 1st Party App Ids.
        /// NOTE: Currently only listing the NGP clients because they include the
        /// client SDK project directly. The assumption is that other clients will not
        /// use the new SDK until they are ready to use the new app ids.
        /// </summary>
        private static readonly IList<string> MsClients = new List<string>()
        {
            "88abfced-edc8-4ec5-bd7b-beecfd0378df", // pdms-client
            "364e9b3d-cb8c-45b7-9e78-8eeb5c9672f7", // pdms-int
            "05bff9ab-0118-4731-8890-468948eba2e8", // pdms
            "364193f7-a0fe-4868-a57a-3bdcf1e3af7f", // pdmsux-int
            "a3058380-1ceb-4aa9-a0ac-1beeee9f27bd"  // pdmsux
        };

        public static string GetResourceId(string clientId, bool targetProduction)
        {
            if (MsClients.Contains(clientId))
            {
                return targetProduction ? PdmsResourceId : PdmsIntResourceId;
            }

            return targetProduction ? PdmsResourceId_FP : PdmsIntResourceId_FP;
        }

        /// <summary>
        /// The default redirect uri.
        /// </summary>
        public const string RedirectUri = "https://management.privacy.microsoft.com";

        /// <summary>
        /// The resource id for PDMS authentication. This is the default resource id.
        /// </summary>
        public const string PdmsResourceId = "https://management.privacy.microsoft.com";

        // Task 1537476: [PDMS] Remove code that handles old app id
        // TODO: replace above constant with the value of this constant
        public const string PdmsResourceId_FP = "b1b98629-ed2f-450c-9095-b895d292ccf1";

        /// <summary>
        /// The resource id for PDMS-INT authentication.
        /// </summary>
        public const string PdmsIntResourceId = "https://management.privacy.microsoft-int.com";

        // Task 1537476: [PDMS] Remove code that handles old app id
        // TODO: replace above constant with the value of this constant
        public const string PdmsIntResourceId_FP = "ff3a41f1-6748-48fa-ba46-d19a123ae965";

        /// <summary>
        /// The resource id for service tree authentication.
        /// </summary>
        public const string ServiceTreeResourceId = "bee782c6-8654-4298-a692-90976578870d";

        /// <summary>
        /// The resource id for service tree PPE authentication.
        /// </summary>
        public const string ServiceTreePpeResourceId = "294223da-1062-4786-b12c-157b25c248fc";

        /// <summary>
        /// The url for service tree authentication.
        /// </summary>
        public const string ServiceTreeUrl = "https://servicetree.msftcloudes.com/";

        /// <summary>
        /// The url for service tree PPE authentication.
        /// </summary>
        public const string ServiceTreePpeUrl = "https://servicetreeppe.msftcloudes.com/";
    }
}