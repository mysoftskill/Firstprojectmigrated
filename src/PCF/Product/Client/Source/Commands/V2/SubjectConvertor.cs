
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    public class SubjectConverter
    {
        public static IPrivacySubject GetV1SubjectForValidation(JToken subjectV2)
        {
            if (subjectV2["msaPuid"] != null)
            {
                var msaSubject = new MsaSubject
                {
                    Puid = long.Parse(subjectV2["msaPuid"].ToString()),
                    Anid = subjectV2["msaAnid"]?.ToString(),
                    Opid = subjectV2["msaOpid"]?.ToString(),
                    Xuid = subjectV2["msaXuid"]?.ToString()
                };
                    
                if (subjectV2["msaCid"] != null)
                {
                    msaSubject.Cid = long.Parse(subjectV2["msaCid"].ToString());
                }

                return msaSubject;
            }
            else if (subjectV2["aadResourceTenantId"] != null && subjectV2["aadObjectId"] != null)
            {
                var aadSubject2 = new AadSubject2
                {
                    ObjectId = (Guid)subjectV2["aadObjectId"],
                    HomeTenantId = subjectV2["aadHomeTenantId"] != null ? (Guid)subjectV2["aadHomeTenantId"] : (Guid)subjectV2["aadResourceTenantId"],
                    TenantId = (Guid)subjectV2["aadResourceTenantId"]
                };

                if (subjectV2["aadPuid"] != null)
                {
                    aadSubject2.OrgIdPUID = long.Parse(subjectV2["aadPuid"].ToString());
                }

                aadSubject2.TenantIdType = aadSubject2.TenantId == aadSubject2.HomeTenantId ? TenantIdType.Home : TenantIdType.Resource;
                return aadSubject2;
            }
            else if (subjectV2["aadObjectId"] != null)
            {
                var aadSubject = new AadSubject
                {
                    ObjectId = (Guid)subjectV2["aadObjectId"],
                    TenantId = (Guid)subjectV2["aadHomeTenantId"]
                };

                if (subjectV2["aadPuid"] != null)
                {
                    aadSubject.OrgIdPUID = long.Parse(subjectV2["aadPuid"].ToString());
                }

                return aadSubject;
            }
            else if (subjectV2["globalDeviceId"] != null)
            {
                var deviceSubject = new DeviceSubject
                {
                    GlobalDeviceId = long.Parse(subjectV2["globalDeviceId"].ToString())
                };

                return deviceSubject;
            }
            else if(subjectV2["demographicEmailAddresses"] != null)
            {
                var demographicSubject = new DemographicSubject
                {
                    EmailAddresses = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicEmailAddresses"].ToString()),
                    Address = new AddressQueryParams()
                };

                if (subjectV2["demographicNames"] != null)
                {
                    demographicSubject.Names = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicNames"].ToString());
                }
                
                if (subjectV2["demographicPhoneNumbers"] != null)
                {
                    demographicSubject.PhoneNumbers = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicPhoneNumbers"].ToString());
                }

                if (subjectV2["demographicAddressCities"] != null)
                {
                    demographicSubject.Address.Cities = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicAddressCities"].ToString());
                }

                if (subjectV2["demographicAddressPostalCodes"] != null)
                {
                    demographicSubject.Address.PostalCodes = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicAddressPostalCodes"].ToString());
                }

                if (subjectV2["demographicAddressStates"] != null)
                {
                    demographicSubject.Address.States = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicAddressStates"].ToString());
                }

                if (subjectV2["demographicAddressStreetNumbers"] != null)
                {
                    demographicSubject.Address.StreetNumbers = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicAddressStreetNumbers"].ToString());
                }

                if (subjectV2["demographicAddressStreets"] != null)
                {
                    demographicSubject.Address.Streets = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicAddressStreets"].ToString());
                }

                if (subjectV2["demographicAddressUnitNumbers"] != null)
                {
                    demographicSubject.Address.UnitNumbers = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicAddressUnitNumbers"].ToString());
                }

                return demographicSubject;
            }
            else if (subjectV2["demographicMsftEmployeeEmails"] != null)
            {
                
                var microsoftEmployeeSubject = new MicrosoftEmployee() 
                { 
                    Emails = JsonConvert.DeserializeObject<IEnumerable<string>>(subjectV2["demographicMsftEmployeeEmails"].ToString())
                };

                microsoftEmployeeSubject.EmployeeId = subjectV2["demographicMsftEmployeeId"]?.ToString();

                if (subjectV2["demographicMsftEmployeeStartDate"] != null)
                {
                    microsoftEmployeeSubject.StartDate = DateTime.Parse(subjectV2["demographicMsftEmployeeStartDate"].ToString());
                }

                if (subjectV2["demographicMsftEmployeeEndDate"] != null)
                {
                    microsoftEmployeeSubject.EndDate = DateTime.Parse(subjectV2["demographicMsftEmployeeEndDate"].ToString());
                }

                return microsoftEmployeeSubject;
            }
            else if (subjectV2["edgeBrowserId"] != null)
            {
                var edgeBrowserSubject = new EdgeBrowserSubject
                {
                    EdgeBrowserId = long.Parse(subjectV2["edgeBrowserId"].ToString())
                };

                return edgeBrowserSubject;
            }
            else if (subjectV2["macOsPlatformDeviceId"] != null)
            {
                var nonWindowsDeviceSubject = new NonWindowsDeviceSubject
                {
                    MacOsPlatformDeviceId = Guid.Parse(subjectV2["macOsPlatformDeviceId"].ToString())
                };

                return nonWindowsDeviceSubject;
            }

            return null;
        }
    }
}
