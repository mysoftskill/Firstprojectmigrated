namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    using System;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// Extension methods for privacy subjects.
    /// </summary>
    public static class SubjectExtensions
    {
        /// <summary>
        /// Maps the given IPrivacySubject to a subject type.
        /// </summary>
        public static SubjectType GetSubjectType(this IPrivacySubject subject)
        {
            if (subject == null)
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (subject is AadSubject2)
            {
                return SubjectType.Aad2;
            }

            if (subject is AadSubject)
            {
                return SubjectType.Aad;
            }

            if (subject is MsaSubject)
            {
                return SubjectType.Msa;
            }

            if (subject is DeviceSubject)
            {
                return SubjectType.Device;
            }

            if (subject is DemographicSubject)
            {
                return SubjectType.Demographic;
            }

            if (subject is NonWindowsDeviceSubject)
            {
                return SubjectType.NonWindowsDevice;
            }

            if (subject is EdgeBrowserSubject)
            {
                return SubjectType.EdgeBrowser;
            }

            if (subject is MicrosoftEmployee)
            {
                return SubjectType.MicrosoftEmployee;
            }

            DualLogger.Instance.Error(nameof(SubjectExtensions), "Unrecognized subject: " + subject.GetType().FullName);

            throw new ArgumentOutOfRangeException(nameof(subject), "Unrecognized subject: " + subject.GetType().FullName);
        }
    }
}