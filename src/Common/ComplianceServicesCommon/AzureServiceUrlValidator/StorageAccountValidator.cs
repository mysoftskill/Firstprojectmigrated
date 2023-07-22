// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Azure.ComplianceServices.Common.AzureServiceUrlValidator
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     Storage account validator.
    /// </summary>
    public static class StorageAccountValidator
    {
        private static readonly Regex AccountNameRegex = new Regex(@"^[a-z0-9]{3,24}$", RegexOptions.Compiled);

        /// <summary>
        ///     The length of storage account name needs to be between 3 to 24, only containing lowercase letters and numbers.
        ///     As specified at https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/resource-name-rules
        /// </summary>
        public static StorageAccountValidationResult IsValidStorageAccountName(string accountName)
        {
            if (accountName == null)
            {
                throw new ArgumentNullException(nameof(accountName));
            }

            if (!AccountNameRegex.IsMatch(accountName))
            {
                return new StorageAccountValidationResult(false, $"{accountName} does not satisfy storage account naming rules.");
            }
            
            return new StorageAccountValidationResult(true, $"{accountName} satisfy expected storage account naming rules.");
        }
    }

    /// <summary>
    ///     Storage account validation result.
    /// </summary>
    public class StorageAccountValidationResult
    {
        public StorageAccountValidationResult(bool isValid, string reason)
        {
            IsValid = isValid;
            Reason = reason;
        }

        public bool IsValid
        {
            get;
            private set;
        }

        public string Reason
        {
            get;
            private set;
        }
    }
}
