namespace PCF.UnitTests.Pdms
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.PdmsCache;
    using Microsoft.PrivacyServices.Policy;
    using System;

    public static class PdmsSubjectTypeExtensions
    {
        public static IPrivacySubject GetPrivacySubject(this PdmsSubjectType pcfTestSubjectType)
        {
            IPrivacySubject privacySubject;

            switch (pcfTestSubjectType)
            {
                case PdmsSubjectType.AADUser:
                    privacySubject = new AadSubject();
                    break;
                case PdmsSubjectType.DemographicUser:
                    privacySubject = new DemographicSubject();
                    break;
                case PdmsSubjectType.MicrosoftEmployee:
                    privacySubject = new MicrosoftEmployee();
                    break;
                case PdmsSubjectType.DeviceOther:
                    privacySubject = new DeviceSubject();
                    break;
                case PdmsSubjectType.MSAUser:
                    privacySubject = new MsaSubject();
                    break;
                case PdmsSubjectType.Windows10Device:
                    privacySubject = new DeviceSubject()
                    {
                        // GlobalDeviceId != 0 means Windows10Device
                        GlobalDeviceId = 123 
                    };
                    break;
                case PdmsSubjectType.NonWindowsDevice:
                    privacySubject = new NonWindowsDeviceSubject()
                    { 
                        MacOsPlatformDeviceId = Guid.NewGuid()
                    };
                    break;
                case PdmsSubjectType.EdgeBrowser:
                    privacySubject = new EdgeBrowserSubject()
                    {
                        EdgeBrowserId = 567
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown pcf subject type: {pcfTestSubjectType}.");
            }

            return privacySubject;
        }
    }
}
