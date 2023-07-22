namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using System;
    using System.Linq;
    using Applicability = Microsoft.PrivacyServices.CosmosSignals.Contracts.V1;
    using PcfSubjects = Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;

    /// <summary>
    /// Defines the <see cref="PrivacySubjectExtension" />
    /// </summary>
    static public class PrivacySubjectExtension
    {
        /// <summary>
        /// Converts <see cref="PcfSubjects.DemographicSubject"/> to <see cref="Applicability.DemographicSubject"/>.
        /// </summary>
        /// <param name="subject">The subject <see cref="PcfSubjects.DemographicSubject"/></param>
        /// <returns>The <see cref="Applicability.DemographicSubject"/></returns>
        public static Applicability.DemographicSubject ToApplicabilityDemographicSubject(this PcfSubjects.DemographicSubject subject)
        {
            Applicability.DemographicSubject resultSubject = new Applicability.DemographicSubject()
            {
                Address = subject.Address == null ? null : new Applicability.AddressQueryParams()
                {
                    Cities = subject.Address.Cities?.ToList(),
                    PostalCodes = subject.Address.PostalCodes?.ToList(),
                    States = subject.Address.States?.ToList(),
                    StreetNumbers = subject.Address.StreetNumbers?.ToList(),
                    Streets = subject.Address.Streets?.ToList(),
                    UnitNumbers = subject.Address.UnitNumbers?.ToList(),
                },
                EmailAddresses = subject.EmailAddresses?.ToList(),
                Names = subject.Names?.ToList(),
                PhoneNumbers = subject.PhoneNumbers?.ToList(),
            };

            return resultSubject;
        }

        /// <summary>
        /// Converts <see cref="PcfSubjects.IPrivacySubject"/> to <see cref="Applicability.Subject"/>.
        /// </summary>
        /// <param name="privacySubject">The privacySubject <see cref="PcfSubjects.IPrivacySubject"/></param>
        /// <returns>The <see cref="Applicability.Subject"/></returns>
        public static Applicability.Subject ToApplicabilitySubject(this PcfSubjects.IPrivacySubject privacySubject)
        {
            Applicability.Subject subject = new Applicability.Subject();

            switch (privacySubject)
            {
                case PcfSubjects.AadSubject2 aadSubject2:
                    // If the Home Tenant Id type is the Tenant Id type then include subject for AadSubject instead for the applicability test
                    if (aadSubject2.HomeTenantId == default || aadSubject2.HomeTenantId == aadSubject2.TenantId || aadSubject2.TenantIdType == PcfSubjects.TenantIdType.Home)
                    {
                        subject.AadUser = new Applicability.AadUser()
                        {
                            AadObjectId = aadSubject2.ObjectId,
                            AadTenantId = aadSubject2.TenantId,
                            OrgIdPuid = aadSubject2.OrgIdPUID
                        };
                    }
                    else
                    {
                        subject.AadUser2 = new Applicability.AadUser2()
                        {
                            AadObjectId = aadSubject2.ObjectId,
                            AadTenantId = aadSubject2.TenantId,
                            OrgIdPuid = aadSubject2.OrgIdPUID,
                            AadHomeTenantId = aadSubject2.HomeTenantId,
                            AadTenantIdType = (aadSubject2.TenantIdType == PcfSubjects.TenantIdType.Home) ? Applicability.TenantIdType.Home : Applicability.TenantIdType.Resource
                        };
                    }
                    return subject;

                case PcfSubjects.AadSubject aadSubject:
                    subject.AadUser = new Applicability.AadUser()
                    {
                        AadObjectId = aadSubject.ObjectId,
                        AadTenantId = aadSubject.TenantId,
                        OrgIdPuid = aadSubject.OrgIdPUID
                    };
                    return subject;

                case PcfSubjects.MsaSubject msaSubject:
                    subject.User = new Applicability.User()
                    {
                        Anid = msaSubject.Anid,
                        Cid = msaSubject.Cid,
                        Opid = msaSubject.Opid,
                        Puid = msaSubject.Puid,
                        Xuid = msaSubject.Xuid
                    };
                    return subject;

                case PcfSubjects.DemographicSubject demographicSubject:
                    subject.DemographicSubject = demographicSubject.ToApplicabilityDemographicSubject();
                    return subject;

                case PcfSubjects.MicrosoftEmployee microsoftEmployeeSubject:
                    subject.MicrosoftEmployee = new Applicability.MicrosoftEmployee()
                    { 
                        Emails = microsoftEmployeeSubject.Emails?.ToList(),
                        EmployeeId = microsoftEmployeeSubject.EmployeeId,
                        StartDate = microsoftEmployeeSubject.StartDate,
                        StartDateString = microsoftEmployeeSubject.StartDate.ToString(),
                        EndDate = microsoftEmployeeSubject.EndDate,
                        EndDateString = microsoftEmployeeSubject.EndDate.ToString(),
                    };
                    return subject;

                case PcfSubjects.DeviceSubject deviceSubject:
                    subject.Device = new Applicability.Device()
                    {
                        GlobalDeviceId = (ulong)deviceSubject.GlobalDeviceId
                    };

                    return subject;

                case PcfSubjects.NonWindowsDeviceSubject nonWindowsDeviceSubject:
                    subject.NonWindowsDevice = new Applicability.NonWindowsDevice()
                    {
                        MacOsPlatformDeviceId = nonWindowsDeviceSubject.MacOsPlatformDeviceId,
                    };

                    return subject;

                case PcfSubjects.EdgeBrowserSubject edgeBrowserSubject:
                    subject.EdgeBrowser = new Applicability.EdgeBrowser()
                    {
                        EdgeBrowserId = edgeBrowserSubject.EdgeBrowserId,
                    };

                    return subject;

                default:
                    throw new ArgumentOutOfRangeException(nameof(privacySubject), $"Unable to extract an applicability subject from {privacySubject?.GetType().Name}");
            }
        }
    }
}
