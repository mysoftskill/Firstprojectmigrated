namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using System;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Defines the <see cref="SubjectTypeExtension" />
    /// </summary>
    public static class SubjectTypeExtension
    {
        /// <summary>
        /// Converts PCF <see cref="Common.SubjectType"/> to the <see cref="SubjectTypeId"/>.
        /// </summary>
        /// <param name="subjectType">The PCF <see cref="Common.SubjectType"/></param>
        /// <returns>The <see cref="SubjectTypeId"/></returns>
        public static SubjectTypeId ToSubjectTypeId(this Common.SubjectType subjectType)
        {
            SubjectTypeId subjectTypeId = Policies.Current.SubjectTypes.Ids.Other;

            switch (subjectType)
            {
                default:
                    {
                        throw new InvalidCastException($"Cannot convert PCF SubjectType={subjectType.ToString()} to applicability library SubjectTypeId.");
                    }

                case Common.SubjectType.Aad2:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.AADUser2;
                        break;
                    }

                case Common.SubjectType.Aad:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.AADUser;
                        break;
                    }

                case Common.SubjectType.Demographic:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.DemographicUser;
                        break;
                    }

                case Common.SubjectType.Device:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.Windows10Device;
                        break;
                    }

                case Common.SubjectType.Msa:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.MSAUser;
                        break;
                    }

                case Common.SubjectType.NonWindowsDevice:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.NonWindowsDevice;
                        break;
                    }

                case Common.SubjectType.EdgeBrowser:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.EdgeBrowser;
                        break;
                    }

                case Common.SubjectType.MicrosoftEmployee:
                    {
                        subjectTypeId = Policies.Current.SubjectTypes.Ids.MicrosoftEmployee;
                        break;
                    }
            }

            return subjectTypeId;
        }
    }
}
