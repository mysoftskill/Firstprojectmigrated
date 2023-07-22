namespace Microsoft.PrivacyServices.DataManagement.AzureKeyVaultCertificateInstaller
{
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// A wrapper around certutil.exe
    /// https://technet.microsoft.com/en-us/library/cc732443.aspx
    /// And other certificate needs
    /// </summary>
    public static class CertificateUtility
    {
        /// <summary>
        /// Wrapper around certutil.exe -repairstore
        /// Repairs key association.
        /// https://technet.microsoft.com/en-us/library/cc732443.aspx#BKMK_repairstore
        /// </summary>
        /// <param name="storeName">Certificate store name</param>
        /// <param name="certHash">The hash of the certificate</param>
        public static void RepairCertificate(StoreName storeName, string certHash)
        {
            try
            {
                CommandPrompt.Execute($"certutil -repairstore {storeName} {certHash}");
            }
            catch (TimeoutException)
            {
                Trace.TraceError("Repair command timed out, likely due to not responding to SmartCard prompt.");
                throw;
            }
        }
    }
}
