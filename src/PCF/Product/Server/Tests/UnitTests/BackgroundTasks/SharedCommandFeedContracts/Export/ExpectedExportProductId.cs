// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace PCF.UnitTests.BackgroundTasks.SharedCommandFeedContracts.Export
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    public class ExpectedExportProductId
    {
        private static readonly ConcurrentDictionary<int, ExpectedExportProductId> ExportProductIdMapping = new ConcurrentDictionary<int, ExpectedExportProductId>();

        /// <summary>
        ///     All known product ids.
        /// </summary>
        public static IReadOnlyDictionary<int, ExpectedExportProductId> ProductIds => new ReadOnlyDictionary<int, ExpectedExportProductId>(ExportProductIdMapping);

        /// <summary>
        ///     The id of the export product id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        ///     The path the data will show up in in the final zip file. Not the NGP services configure the final path, so trying
        ///     to change a path in the agent's code will have no effect. Agents provide a product id and PCF will map that to a folder.
        /// </summary>
        public string Path { get; }

        public override string ToString()
        {
            return $"ExpectedExportProductId({this.Id}, \"{this.Path}\")";
        }

        private ExpectedExportProductId(int id, string path)
        {
            this.Id = id;
            this.Path = path;

            if (!ExportProductIdMapping.TryAdd(id, this))
            {
                throw new ArgumentOutOfRangeException(nameof(id), $"ExpectedExportProductId {id} is already defined.");
            }
        }

#pragma warning disable SA1600 // Elements must be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static ExpectedExportProductId Unknown { get; } = new ExpectedExportProductId(0, "Miscellaneous");

        public static ExpectedExportProductId Account { get; } = new ExpectedExportProductId(100, "Account");

        public static ExpectedExportProductId AccountIdentity { get; } = new ExpectedExportProductId(105, "Account/Identity");

        public static ExpectedExportProductId AccountIdentityManager { get; } = new ExpectedExportProductId(110, "Account/IdentityManager");

        public static ExpectedExportProductId AccountPlatform { get; } = new ExpectedExportProductId(115, "Account/Platform");

        public static ExpectedExportProductId Azure { get; } = new ExpectedExportProductId(200, "Azure");

        public static ExpectedExportProductId AzureContainer { get; } = new ExpectedExportProductId(205, "Azure/Container");

        public static ExpectedExportProductId AzureDataCatalog { get; } = new ExpectedExportProductId(210, "Azure/Data Catalog");

        public static ExpectedExportProductId AzureDataFactory { get; } = new ExpectedExportProductId(215, "Azure/Data Factory");

        public static ExpectedExportProductId AzureEcosystem { get; } = new ExpectedExportProductId(217, "Azure/Ecosystem");

        public static ExpectedExportProductId AzureFinance { get; } = new ExpectedExportProductId(220, "Azure/Finance");

        public static ExpectedExportProductId AzureIdentity { get; } = new ExpectedExportProductId(225, "Azure/Identity");

        public static ExpectedExportProductId AzureMarketing { get; } = new ExpectedExportProductId(230, "Azure/Marketing");

        public static ExpectedExportProductId AzurePlatform { get; } = new ExpectedExportProductId(235, "Azure/Platform");

        public static ExpectedExportProductId AzurePortal { get; } = new ExpectedExportProductId(240, "Azure/Portal");

        public static ExpectedExportProductId AzureWebAnalytics { get; } = new ExpectedExportProductId(245, "Azure/Web Analytics");

        public static ExpectedExportProductId Browser { get; } = new ExpectedExportProductId(300, "Browser");

        public static ExpectedExportProductId BrowserInternetExplorer { get; } = new ExpectedExportProductId(305, "Browser/InternetExplorer");

        public static ExpectedExportProductId BrowserEdge { get; } = new ExpectedExportProductId(310, "Browser/Edge");

        public static ExpectedExportProductId CloudAppSecurity { get; } = new ExpectedExportProductId(380, "CloudAppSecurity");

        public static ExpectedExportProductId Development { get; } = new ExpectedExportProductId(400, "Development");

        public static ExpectedExportProductId DevelopmentMsdn { get; } = new ExpectedExportProductId(405, "Development/MSDN");

        public static ExpectedExportProductId DevelopmentNetFramework { get; } = new ExpectedExportProductId(410, "Development/NET Framework");

        public static ExpectedExportProductId DevelopmentPlatform { get; } = new ExpectedExportProductId(415, "Development/Platform");

        public static ExpectedExportProductId DevelopmentSyncFramework { get; } = new ExpectedExportProductId(420, "Development/Sync Framework");

        public static ExpectedExportProductId DevelopmentTeamFoundationServer { get; } = new ExpectedExportProductId(425, "Development/Team Foundation Server");

        public static ExpectedExportProductId DevelopmentTools { get; } = new ExpectedExportProductId(430, "Development/Tools");

        public static ExpectedExportProductId DevelopmentVisualStudio { get; } = new ExpectedExportProductId(435, "Development/VisualStudio");

        public static ExpectedExportProductId Dynamics { get; } = new ExpectedExportProductId(500, "Dynamics");

        public static ExpectedExportProductId DynamicsCrm { get; } = new ExpectedExportProductId(505, "Dynamics/CRM");

        public static ExpectedExportProductId DynamicsInfrastructure { get; } = new ExpectedExportProductId(510, "Dynamics/Infrastructure");

        public static ExpectedExportProductId DynamicsMarketing { get; } = new ExpectedExportProductId(515, "Dynamics/Marketing");

        public static ExpectedExportProductId DynamicsPlatform { get; } = new ExpectedExportProductId(520, "Dynamics/Platform");

        public static ExpectedExportProductId DynamicsRetail { get; } = new ExpectedExportProductId(525, "Dynamics/Retail");

        public static ExpectedExportProductId HumanResources { get; } = new ExpectedExportProductId(600, "Human Resources");

        public static ExpectedExportProductId Intune { get; } = new ExpectedExportProductId(605, "Intune");

        public static ExpectedExportProductId IntuneExtensions { get; } = new ExpectedExportProductId(615, "Intune/Extensions");

        public static ExpectedExportProductId IntunePlatform { get; } = new ExpectedExportProductId(610, "Intune/Platform");

        public static ExpectedExportProductId LinkedIn { get; } = new ExpectedExportProductId(700, "LinkedIn");

        public static ExpectedExportProductId LinkedInPlatform { get; } = new ExpectedExportProductId(705, "LinkedIn/Platform");

        public static ExpectedExportProductId Media { get; } = new ExpectedExportProductId(800, "Media");

        public static ExpectedExportProductId MediaBooks { get; } = new ExpectedExportProductId(805, "Media/Books");

        public static ExpectedExportProductId MediaGrooveMusic { get; } = new ExpectedExportProductId(810, "Media/Groove Music");

        public static ExpectedExportProductId MediaServices { get; } = new ExpectedExportProductId(815, "Media/Services");

        public static ExpectedExportProductId MediaVideo { get; } = new ExpectedExportProductId(820, "Media/Video");

        public static ExpectedExportProductId Mobile { get; } = new ExpectedExportProductId(850, "Mobile");

        public static ExpectedExportProductId MobileIOS { get; } = new ExpectedExportProductId(851, "Mobile/iOS");

        public static ExpectedExportProductId MobileAndroid { get; } = new ExpectedExportProductId(852, "Mobile/Android");

        public static ExpectedExportProductId Msit { get; } = new ExpectedExportProductId(900, "MSIT");

        public static ExpectedExportProductId Office { get; } = new ExpectedExportProductId(1000, "Office");

        public static ExpectedExportProductId OfficeO365 { get; } = new ExpectedExportProductId(1005, "Office/O365");

        public static ExpectedExportProductId OfficeO365Creator { get; } = new ExpectedExportProductId(1006, "Office/O365/Creator");

        public static ExpectedExportProductId OfficeO365Designer { get; } = new ExpectedExportProductId(1008, "Office/O365/Designer");

        public static ExpectedExportProductId OfficeO365Outlook { get; } = new ExpectedExportProductId(1010, "Office/O365/Outlook");

        public static ExpectedExportProductId OfficeOneDriveUsage { get; } = new ExpectedExportProductId(1015, "Office/OneDriveUsage");

        public static ExpectedExportProductId OfficeListUsage { get; } = new ExpectedExportProductId(1020, "Office/ListUsage");

        public static ExpectedExportProductId OfficeMicrosoftStream { get; } = new ExpectedExportProductId(1025, "Office/MicrosoftStream");

        public static ExpectedExportProductId Profile { get; } = new ExpectedExportProductId(1100, "Profile");

        public static ExpectedExportProductId Search { get; } = new ExpectedExportProductId(1500, "Search");

        public static ExpectedExportProductId SearchAds { get; } = new ExpectedExportProductId(1505, "Search/Ads");

        public static ExpectedExportProductId SearchBing { get; } = new ExpectedExportProductId(1510, "Search/Bing");

        public static ExpectedExportProductId SearchCortana { get; } = new ExpectedExportProductId(1515, "Search/Cortana");

        public static ExpectedExportProductId SearchExperiences { get; } = new ExpectedExportProductId(1520, "Search/Experiences");

        public static ExpectedExportProductId SearchOffers { get; } = new ExpectedExportProductId(1525, "Search/Offers");

        public static ExpectedExportProductId SearchPlatform { get; } = new ExpectedExportProductId(1530, "Search/Platform");

        public static ExpectedExportProductId SharedData { get; } = new ExpectedExportProductId(1550, "SharedData");

        public static ExpectedExportProductId Simplygon { get; } = new ExpectedExportProductId(1590, "Simplygon");

        public static ExpectedExportProductId SharedDataBing { get; } = new ExpectedExportProductId(1555, "SharedData/Bing");

        public static ExpectedExportProductId SharedDataCortana { get; } = new ExpectedExportProductId(1560, "SharedData/Cortana");

        public static ExpectedExportProductId SharedDataOffice365 { get; } = new ExpectedExportProductId(1565, "SharedData/Office365");

        public static ExpectedExportProductId Skype { get; } = new ExpectedExportProductId(1600, "Skype");

        public static ExpectedExportProductId SkypePlatform { get; } = new ExpectedExportProductId(1605, "Skype/Platform");

        public static ExpectedExportProductId SkypeSkypeForBusiness { get; } = new ExpectedExportProductId(1610, "Skype/SkypeForBusiness");

        public static ExpectedExportProductId SkypeSkypeForConsumer { get; } = new ExpectedExportProductId(1615, "Skype/SkypeForConsumer");

        public static ExpectedExportProductId Store { get; } = new ExpectedExportProductId(1700, "Store");

        public static ExpectedExportProductId StoreApplication { get; } = new ExpectedExportProductId(1705, "Store/Application");

        public static ExpectedExportProductId StorePlatform { get; } = new ExpectedExportProductId(1710, "Store/Platform");

        public static ExpectedExportProductId StoreServices { get; } = new ExpectedExportProductId(1715, "Store/Services");

        public static ExpectedExportProductId StoreWeb { get; } = new ExpectedExportProductId(1720, "Store/Web");

        public static ExpectedExportProductId Support { get; } = new ExpectedExportProductId(1750, "Support");

        public static ExpectedExportProductId Teams { get; } = new ExpectedExportProductId(4000, "Teams");

        public static ExpectedExportProductId TeamsForLife { get; } = new ExpectedExportProductId(4100, "Teams/Teams for life");

        public static ExpectedExportProductId WebServices { get; } = new ExpectedExportProductId(1800, "WebServices");

        public static ExpectedExportProductId WebServicesSites { get; } = new ExpectedExportProductId(1805, "WebServices/Sites");

        public static ExpectedExportProductId WebServicesPlatform { get; } = new ExpectedExportProductId(1810, "WebServices/Platform");

        public static ExpectedExportProductId MicrosoftSites { get; } = new ExpectedExportProductId(1900, "Microsoft Sites");

        public static ExpectedExportProductId Windows { get; } = new ExpectedExportProductId(2000, "Windows");

        public static ExpectedExportProductId WindowsAdministrativeTools { get; } = new ExpectedExportProductId(2005, "Windows/Administrative Tools");

        public static ExpectedExportProductId WindowsApplicationsUtilities { get; } = new ExpectedExportProductId(2010, "Windows/Applications and Utilities");

        public static ExpectedExportProductId WindowsBizTalk { get; } = new ExpectedExportProductId(2015, "Windows/BizTalk");

        public static ExpectedExportProductId WindowsCoreComponents { get; } = new ExpectedExportProductId(2020, "Windows/Core Components");

        public static ExpectedExportProductId WindowsEnterprise { get; } = new ExpectedExportProductId(2025, "Windows/Enterprise");

        public static ExpectedExportProductId WindowsExchange { get; } = new ExpectedExportProductId(2030, "Windows/Exchange");

        public static ExpectedExportProductId WindowsFileSystem { get; } = new ExpectedExportProductId(2035, "Windows/File System");

        public static ExpectedExportProductId WindowsForefront { get; } = new ExpectedExportProductId(2040, "Windows/Forefront");

        public static ExpectedExportProductId WindowsHyperV { get; } = new ExpectedExportProductId(2045, "Windows/Hyper-V");

        public static ExpectedExportProductId WindowsIdentityManager { get; } = new ExpectedExportProductId(2050, "Windows/IdentityManager");

        public static ExpectedExportProductId WindowsInternetInformationServices { get; } = new ExpectedExportProductId(2055, "Windows/Internet Information Services");

        public static ExpectedExportProductId WindowsMSN { get; } = new ExpectedExportProductId(2057, "Windows/MSN");

        public static ExpectedExportProductId WindowsPhotosAndCamera { get; } = new ExpectedExportProductId(2059, "Windows/Photos and Camera");

        public static ExpectedExportProductId WindowsPlatform { get; } = new ExpectedExportProductId(2060, "Windows/Platform");

        public static ExpectedExportProductId WindowsRemix3D { get; } = new ExpectedExportProductId(2063, "Windows/Remix3D");

        public static ExpectedExportProductId WindowsRemoteDesktop { get; } = new ExpectedExportProductId(2065, "Windows/Remote Desktop");

        public static ExpectedExportProductId WindowsSoftwareSetup { get; } = new ExpectedExportProductId(2075, "Windows/Software Setup");

        public static ExpectedExportProductId WindowsSqlServer { get; } = new ExpectedExportProductId(2070, "Windows/SQL Server");

        public static ExpectedExportProductId WindowsSystemsCenter { get; } = new ExpectedExportProductId(2080, "Windows/Systems Center");

        public static ExpectedExportProductId WindowsInsiderProgram { get; } = new ExpectedExportProductId(2081, "Windows/Windows Insider Program");

        public static ExpectedExportProductId WindowsWindowsServer { get; } = new ExpectedExportProductId(2085, "Windows/Windows Server");

        public static ExpectedExportProductId WindowsUpdate { get; } = new ExpectedExportProductId(2089, "Windows/Windows Update");

        public static ExpectedExportProductId WindowsUserInterface { get; } = new ExpectedExportProductId(2090, "Windows/User Interface");

        public static ExpectedExportProductId WindowsUwpApps { get; } = new ExpectedExportProductId(2095, "Windows/UWP Apps");

        public static ExpectedExportProductId WindowsVirtualization { get; } = new ExpectedExportProductId(2100, "Windows/Virtualization");

        public static ExpectedExportProductId Xbox { get; } = new ExpectedExportProductId(2500, "Xbox");

        public static ExpectedExportProductId XboxApplications { get; } = new ExpectedExportProductId(2505, "Xbox/Applications");

        public static ExpectedExportProductId XboxGames { get; } = new ExpectedExportProductId(2510, "Xbox/Games");

        public static ExpectedExportProductId XboxLive { get; } = new ExpectedExportProductId(2515, "Xbox/Live");

        public static ExpectedExportProductId XboxPlatform { get; } = new ExpectedExportProductId(2520, "Xbox/Platform");

        public static ExpectedExportProductId XboxServices { get; } = new ExpectedExportProductId(2525, "Xbox/Services");

        public static ExpectedExportProductId Other { get; } = new ExpectedExportProductId(3000, "Other");

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600 // Elements must be documented
    }
}
