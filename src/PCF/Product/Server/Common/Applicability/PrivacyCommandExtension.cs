namespace Microsoft.PrivacyServices.CommandFeed.Service.Common.Applicability
{
    using Microsoft.PrivacyServices.SignalApplicability;

    using CloudInstanceId = Microsoft.PrivacyServices.Policy.CloudInstanceId;

    /// <summary>
    /// Defines the <see cref="PrivacyCommandExtension" />
    /// </summary>
    public static class PrivacyCommandExtension
    {
        /// <summary>
        /// Converts PCF <see cref="PrivacyCommand"/> to Applicability <see cref="SignalInfo"/>
        /// </summary>
        /// <param name="command">The command<see cref="PrivacyCommand"/></param>
        /// <returns>The <see cref="SignalInfo"/></returns>
        public static SignalInfo ToSignalInfo(this PrivacyCommand command)
        {
            var signal = new SignalInfo
            {
                Capability = command.CommandType.ToCapabilityId(),
                DataTypes = command.DataTypeIds,
                RequestTimeStamp = command.Timestamp.UtcDateTime,
                RequestType = RequestType.Single,
                Subject = command.Subject.ToApplicabilitySubject(),
                CloudInstance = ParseCloudInstance(command.CloudInstance)
            };

            if (command is AgeOutCommand ageOutCommand)
            {
                // Note: DateTimeOffset.DateTime doesn't have DateTime.Kind property set, but DateTimeOffset.UtcDateTime does. This allows for time zone information to be passed on.
                signal.LastActiveTimeStamp = ageOutCommand.LastActive?.UtcDateTime;
            }

            return signal;
        }

        private static CloudInstanceId ParseCloudInstance(string rawCloudInstance)
        {
            if (string.IsNullOrEmpty(rawCloudInstance))
            {
                return null;
            }

            if (Policy.Policies.Current.CloudInstances.TryCreateId(rawCloudInstance, out CloudInstanceId result))
            {
                return result;
            }

            return null;
        }
    }
}
