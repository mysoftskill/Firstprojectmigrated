// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Represents a product by id, as far as export is concerned. This is used for mapping exported data into folders for the
    /// end user's export zip file.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ExportProductId
    {
        /// <summary>
        /// The id of the export product id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// The path the data will show up in in the final zip file. Not the NGP services configure the final path, so trying
        /// to change a path in the agent's code will have no effect. Agents provide a product id and PCF will map that to a folder.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// All known product ids.
        /// </summary>
        public static IReadOnlyDictionary<int, ExportProductId> ProductIds => new ReadOnlyDictionary<int, ExportProductId>(ExportProductIdMapping);

        private static readonly ConcurrentDictionary<int, ExportProductId> ExportProductIdMapping = new ConcurrentDictionary<int, ExportProductId>();

#pragma warning disable SA1600 // Elements must be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static ExportProductId Unknown { get; } = new ExportProductId(0, "Miscellaneous");

        public static ExportProductId Account { get; } = new ExportProductId(100, "Account");

        public static ExportProductId AccountIdentity { get; } = new ExportProductId(105, "Account/Identity");

        public static ExportProductId AccountIdentityManager { get; } = new ExportProductId(110, "Account/IdentityManager");

        public static ExportProductId AccountPlatform { get; } = new ExportProductId(115, "Account/Platform");

        public static ExportProductId Azure { get; } = new ExportProductId(200, "Azure");

        public static ExportProductId AzureContainer { get; } = new ExportProductId(205, "Azure/Container");

        public static ExportProductId AzureDataCatalog { get; } = new ExportProductId(210, "Azure/Data Catalog");

        public static ExportProductId AzureDataFactory { get; } = new ExportProductId(215, "Azure/Data Factory");

        public static ExportProductId AzureEcosystem { get; } = new ExportProductId(217, "Azure/Ecosystem");

        public static ExportProductId AzureFinance { get; } = new ExportProductId(220, "Azure/Finance");

        public static ExportProductId AzureIdentity { get; } = new ExportProductId(225, "Azure/Identity");

        public static ExportProductId AzureMarketing { get; } = new ExportProductId(230, "Azure/Marketing");

        public static ExportProductId AzurePlatform { get; } = new ExportProductId(235, "Azure/Platform");

        public static ExportProductId AzurePortal { get; } = new ExportProductId(240, "Azure/Portal");

        public static ExportProductId AzureWebAnalytics { get; } = new ExportProductId(245, "Azure/Web Analytics");

        public static ExportProductId Browser { get; } = new ExportProductId(300, "Browser");

        public static ExportProductId BrowserInternetExplorer { get; } = new ExportProductId(305, "Browser/InternetExplorer");

        public static ExportProductId BrowserEdge { get; } = new ExportProductId(310, "Browser/Edge");

        public static ExportProductId CloudAppSecurity { get; } = new ExportProductId(380, "CloudAppSecurity");

        public static ExportProductId Development { get; } = new ExportProductId(400, "Development");

        public static ExportProductId DevelopmentMsdn { get; } = new ExportProductId(405, "Development/MSDN");

        public static ExportProductId DevelopmentNetFramework { get; } = new ExportProductId(410, "Development/NET Framework");

        public static ExportProductId DevelopmentPlatform { get; } = new ExportProductId(415, "Development/Platform");

        public static ExportProductId DevelopmentSyncFramework { get; } = new ExportProductId(420, "Development/Sync Framework");

        public static ExportProductId DevelopmentTeamFoundationServer { get; } = new ExportProductId(425, "Development/Team Foundation Server");

        public static ExportProductId DevelopmentTools { get; } = new ExportProductId(430, "Development/Tools");

        public static ExportProductId DevelopmentVisualStudio { get; } = new ExportProductId(435, "Development/VisualStudio");

        public static ExportProductId Dynamics { get; } = new ExportProductId(500, "Dynamics");

        public static ExportProductId DynamicsCrm { get; } = new ExportProductId(505, "Dynamics/CRM");

        public static ExportProductId DynamicsInfrastructure { get; } = new ExportProductId(510, "Dynamics/Infrastructure");

        public static ExportProductId DynamicsMarketing { get; } = new ExportProductId(515, "Dynamics/Marketing");

        public static ExportProductId DynamicsPlatform { get; } = new ExportProductId(520, "Dynamics/Platform");

        public static ExportProductId DynamicsRetail { get; } = new ExportProductId(525, "Dynamics/Retail");

        public static ExportProductId HumanResources { get; } = new ExportProductId(600, "Human Resources");

        public static ExportProductId Intune { get; } = new ExportProductId(605, "Intune");

        public static ExportProductId IntuneExtensions { get; } = new ExportProductId(615, "Intune/Extensions");

        public static ExportProductId IntunePlatform { get; } = new ExportProductId(610, "Intune/Platform");

        public static ExportProductId LinkedIn { get; } = new ExportProductId(700, "LinkedIn");

        public static ExportProductId LinkedInPlatform { get; } = new ExportProductId(705, "LinkedIn/Platform");

        public static ExportProductId Media { get; } = new ExportProductId(800, "Media");

        public static ExportProductId MediaBooks { get; } = new ExportProductId(805, "Media/Books");

        public static ExportProductId MediaGrooveMusic { get; } = new ExportProductId(810, "Media/Groove Music");

        public static ExportProductId MediaServices { get; } = new ExportProductId(815, "Media/Services");

        public static ExportProductId MediaVideo { get; } = new ExportProductId(820, "Media/Video");

        public static ExportProductId Mobile { get; } = new ExportProductId(850, "Mobile");

        public static ExportProductId MobileIOS { get; } = new ExportProductId(851, "Mobile/iOS");

        public static ExportProductId MobileAndroid { get; } = new ExportProductId(852, "Mobile/Android");

        public static ExportProductId Msit { get; } = new ExportProductId(900, "MSIT");

        public static ExportProductId Office { get; } = new ExportProductId(1000, "Office");

        public static ExportProductId OfficeO365 { get; } = new ExportProductId(1005, "Office/O365");

        public static ExportProductId OfficeO365Creator { get; } = new ExportProductId(1006, "Office/O365/Creator");

        public static ExportProductId OfficeO365Designer { get; } = new ExportProductId(1008, "Office/O365/Designer");

        public static ExportProductId OfficeO365Outlook { get; } = new ExportProductId(1010, "Office/O365/Outlook");

        public static ExportProductId OfficeOneDriveUsage { get; } = new ExportProductId(1015, "Office/OneDriveUsage");

        public static ExportProductId OfficeListUsage { get; } = new ExportProductId(1020, "Office/ListUsage");
        
        public static ExportProductId OfficeMicrosoftStream { get; } = new ExportProductId(1025, "Office/MicrosoftStream");

        public static ExportProductId Profile { get; } = new ExportProductId(1100, "Profile");

        public static ExportProductId Search { get; } = new ExportProductId(1500, "Search");

        public static ExportProductId SearchAds { get; } = new ExportProductId(1505, "Search/Ads");

        public static ExportProductId SearchBing { get; } = new ExportProductId(1510, "Search/Bing");

        public static ExportProductId SearchCortana { get; } = new ExportProductId(1515, "Search/Cortana");

        public static ExportProductId SearchExperiences { get; } = new ExportProductId(1520, "Search/Experiences");

        public static ExportProductId SearchOffers { get; } = new ExportProductId(1525, "Search/Offers");

        public static ExportProductId SearchPlatform { get; } = new ExportProductId(1530, "Search/Platform");

        public static ExportProductId SharedData { get; } = new ExportProductId(1550, "SharedData");

        public static ExportProductId Simplygon { get; } = new ExportProductId(1590, "Simplygon");

        public static ExportProductId SharedDataBing { get; } = new ExportProductId(1555, "SharedData/Bing");

        public static ExportProductId SharedDataCortana { get; } = new ExportProductId(1560, "SharedData/Cortana");

        public static ExportProductId SharedDataOffice365 { get; } = new ExportProductId(1565, "SharedData/Office365");

        public static ExportProductId Skype { get; } = new ExportProductId(1600, "Skype");

        public static ExportProductId SkypePlatform { get; } = new ExportProductId(1605, "Skype/Platform");

        public static ExportProductId SkypeSkypeForBusiness { get; } = new ExportProductId(1610, "Skype/SkypeForBusiness");

        public static ExportProductId SkypeSkypeForConsumer { get; } = new ExportProductId(1615, "Skype/SkypeForConsumer");

        public static ExportProductId Store { get; } = new ExportProductId(1700, "Store");

        public static ExportProductId StoreApplication { get; } = new ExportProductId(1705, "Store/Application");

        public static ExportProductId StorePlatform { get; } = new ExportProductId(1710, "Store/Platform");

        public static ExportProductId StoreServices { get; } = new ExportProductId(1715, "Store/Services");

        public static ExportProductId StoreWeb { get; } = new ExportProductId(1720, "Store/Web");

        public static ExportProductId Support { get; } = new ExportProductId(1750, "Support");

        public static ExportProductId Teams { get; } = new ExportProductId(4000, "Teams");

        public static ExportProductId TeamsForLife { get; } = new ExportProductId(4100, "Teams/Teams for life");

        public static ExportProductId WebServices { get; } = new ExportProductId(1800, "WebServices");

        public static ExportProductId WebServicesSites { get; } = new ExportProductId(1805, "WebServices/Sites");

        public static ExportProductId WebServicesPlatform { get; } = new ExportProductId(1810, "WebServices/Platform");

        public static ExportProductId MicrosoftSites { get; } = new ExportProductId(1900, "Microsoft Sites");

        public static ExportProductId Windows { get; } = new ExportProductId(2000, "Windows");

        public static ExportProductId WindowsAdministrativeTools { get; } = new ExportProductId(2005, "Windows/Administrative Tools");

        public static ExportProductId WindowsApplicationsUtilities { get; } = new ExportProductId(2010, "Windows/Applications and Utilities");

        public static ExportProductId WindowsBizTalk { get; } = new ExportProductId(2015, "Windows/BizTalk");

        public static ExportProductId WindowsCoreComponents { get; } = new ExportProductId(2020, "Windows/Core Components");

        public static ExportProductId WindowsEnterprise { get; } = new ExportProductId(2025, "Windows/Enterprise");

        public static ExportProductId WindowsExchange { get; } = new ExportProductId(2030, "Windows/Exchange");

        public static ExportProductId WindowsFileSystem { get; } = new ExportProductId(2035, "Windows/File System");

        public static ExportProductId WindowsForefront { get; } = new ExportProductId(2040, "Windows/Forefront");

        public static ExportProductId WindowsHyperV { get; } = new ExportProductId(2045, "Windows/Hyper-V");

        public static ExportProductId WindowsIdentityManager { get; } = new ExportProductId(2050, "Windows/IdentityManager");

        public static ExportProductId WindowsInternetInformationServices { get; } = new ExportProductId(2055, "Windows/Internet Information Services");

        public static ExportProductId WindowsMSN { get; } = new ExportProductId(2057, "Windows/MSN");

        public static ExportProductId WindowsPhotosAndCamera { get; } = new ExportProductId(2059, "Windows/Photos and Camera");

        public static ExportProductId WindowsPlatform { get; } = new ExportProductId(2060, "Windows/Platform");

        public static ExportProductId WindowsRemix3D { get; } = new ExportProductId(2063, "Windows/Remix3D");

        public static ExportProductId WindowsRemoteDesktop { get; } = new ExportProductId(2065, "Windows/Remote Desktop");

        public static ExportProductId WindowsSoftwareSetup { get; } = new ExportProductId(2075, "Windows/Software Setup");

        public static ExportProductId WindowsSqlServer { get; } = new ExportProductId(2070, "Windows/SQL Server");

        public static ExportProductId WindowsSystemsCenter { get; } = new ExportProductId(2080, "Windows/Systems Center");

        public static ExportProductId WindowsInsiderProgram { get; } = new ExportProductId(2081, "Windows/Windows Insider Program");

        public static ExportProductId WindowsWindowsServer { get; } = new ExportProductId(2085, "Windows/Windows Server");

        public static ExportProductId WindowsUpdate { get; } = new ExportProductId(2089, "Windows/Windows Update");

        public static ExportProductId WindowsUserInterface { get; } = new ExportProductId(2090, "Windows/User Interface");

        public static ExportProductId WindowsUwpApps { get; } = new ExportProductId(2095, "Windows/UWP Apps");

        public static ExportProductId WindowsVirtualization { get; } = new ExportProductId(2100, "Windows/Virtualization");

        public static ExportProductId Xbox { get; } = new ExportProductId(2500, "Xbox");

        public static ExportProductId XboxApplications { get; } = new ExportProductId(2505, "Xbox/Applications");

        public static ExportProductId XboxGames { get; } = new ExportProductId(2510, "Xbox/Games");

        public static ExportProductId XboxLive { get; } = new ExportProductId(2515, "Xbox/Live");

        public static ExportProductId XboxPlatform { get; } = new ExportProductId(2520, "Xbox/Platform");

        public static ExportProductId XboxServices { get; } = new ExportProductId(2525, "Xbox/Services");

        public static ExportProductId Other { get; } = new ExportProductId(3000, "Other");

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600 // Elements must be documented

        /// <inheritdoc />
        public override string ToString()
        {
            return $"ExportProductId({this.Id}, \"{this.Path}\")";
        }

        private ExportProductId(int id, string path)
        {
            this.Id = id;
            this.Path = path;

            if (!ExportProductIdMapping.TryAdd(id, this))
            {
                throw new ArgumentOutOfRangeException(nameof(id), $"ExportProductId {id} is already defined.");
            }
        }
    }
}
