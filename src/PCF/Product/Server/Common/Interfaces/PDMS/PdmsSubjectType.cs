namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Enumerates the set of Subject Types that come in the PDMS stream. The main difference between this and PCF's internal
    /// notion of subject type is that PCF has a single "Device" subject, while the PDMS data set distinguishes between Win10 and DeviceOther.
    /// 
    /// In pratice, this simply has an affect on filtering.
    /// </summary>
    public enum PdmsSubjectType : int
    {
        /// <summary>
        /// An invalid default value.
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// The MSA (Consumer) subject.
        /// </summary>
        MSAUser = 1,

        /// <summary>
        /// The AAD (enterprise) subject.
        /// </summary>
        AADUser = 2,

        /// <summary>
        /// The demographic subject.
        /// </summary>
        DemographicUser = 3,

        /// <summary>
        /// The windows 10 device subject.
        /// </summary>
        Windows10Device = 4,
        
        /// <summary>
        /// Other devices that are not Windows 10.
        /// </summary>
        DeviceOther = 5,

        /// <summary>
        /// A value in the PDMS stream that we ignore.
        /// </summary>
        Other = 6,

        /// <summary>
        /// Non-Windows device subject.
        /// </summary>
        NonWindowsDevice = 7,

        /// <summary>
        /// Xbox subject.
        /// </summary>
        Xbox = 8,

        /// <summary>
        /// EdgeBrowser subject.
        /// </summary>
        EdgeBrowser = 9,

        /// <summary>
        /// The AAD subject supports Multi-tenant collaboration.
        /// </summary>
        AADUser2 = 10,


        /// <summary>
        /// The MicrosoftEmployee subject
        /// </summary>
        MicrosoftEmployee = 11,
    }
}
