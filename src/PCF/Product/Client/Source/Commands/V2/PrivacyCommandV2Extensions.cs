namespace Microsoft.PrivacyServices.CommandFeed.Client.Commands.V2
{
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Microsoft.PrivacyServices.Policy;
    using System.Linq;

    public static class PrivacyCommandV2Extensions
    {
        public static ValidOperation GetV1Operation(this PrivacyCommandV2 commandV2)
        {
            // Possible V2 Operations are: 
            // https://msdata.visualstudio.com/Babylon/_git/Privacy?path=%2Fsrc%2FServices%2FAudit%2FDsar1P%2FCommandConfiguration%2FCommandConfiguration.json&_a=contents&version=GBdevelop
            switch (commandV2.Operation?.ToLowerInvariant())
            {
                case "accountclose":
                    return ((SubjectConverter.GetV1SubjectForValidation(commandV2.Subject) is AadSubject2 aadSubject2) && (aadSubject2.HomeTenantId != aadSubject2.TenantId)) ? ValidOperation.AccountCleanup : ValidOperation.AccountClose;
                case "delete-bulk": 
                    return ValidOperation.Delete;
                case "delete-single": 
                    return ValidOperation.Delete;
                case "ageout":
                    // AgeOut commands have Operation Type set to AccountClose in the verifier.
                    // This is the same workaround we did in V1.
                    return ValidOperation.AccountClose;
                case "export": 
                    return ValidOperation.Export;
                default: 
                    return ValidOperation.None;
            }
        }

        public static DataTypeId GetV1DataType(this PrivacyCommandV2 commandV2)
        {
            DataTypeId dataType = null;

            // Flatten all DataTypes
            var dataTypeStrings = commandV2.CommandProperties.Where(e => e.Property == "DataType").Select(e => e.Values).SelectMany(e => e);

            if (dataTypeStrings.Any())
            {
                Policies.Current?.DataTypes.TryCreateId(dataTypeStrings.First(), out dataType);
            }

            return dataType;
        }
    }
}
