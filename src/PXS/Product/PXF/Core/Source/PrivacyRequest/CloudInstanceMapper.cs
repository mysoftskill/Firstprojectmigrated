// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.PrivacyCommand
{
    using Microsoft.Membership.MemberServices.Configuration;

    /// <summary>
    ///     CloudInstanceMapper
    /// </summary>
    public static class CloudInstanceMapper
    {
        /// <summary>
        ///     Maps the cloud instance type to the PCF equivalent
        /// </summary>
        /// <remarks>
        ///     PCF maps their clouds here:
        ///     https://microsoft.visualstudio.com/Universal%20Store/MEE.Privacy.CommandFeed/_git/MEE.Privacy.CommandFeed.Service?
        ///     path=%2FProduct%2FLibraries%2FPrivacyCommandValidator%2FConfiguration%2FKeyDiscoveryConfigurationCollection.cs
        /// </remarks>
        /// <param name="cloudInstanceType"></param>
        /// <returns>The cloud instance type that PCF understands</returns>
        public static string ToPcfCloudInstance(this CloudInstanceType? cloudInstanceType)
        {
            switch (cloudInstanceType)
            {
                // AAD RVS TIP uses this
                case CloudInstanceType.AzureTestInProd:
                case CloudInstanceType.PublicProd:
                    return "Public";
                case CloudInstanceType.CNAzureMooncake:
                    return "CN.Azure.Mooncake";
                case CloudInstanceType.CNO365Gallatin:
                    return "CN.O365.Gallatin";
                case CloudInstanceType.USAzureFairfax:
                    return "US.Azure.Fairfax";
            }

            return null;
        }

        public static string ToPcfCloudInstance(this CloudInstanceType cloudInstanceType)
        {
            return ToPcfCloudInstance((CloudInstanceType?)cloudInstanceType);
        }
    }
}
