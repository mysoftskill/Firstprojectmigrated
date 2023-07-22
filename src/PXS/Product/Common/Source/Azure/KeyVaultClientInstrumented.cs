// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Azure
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Models;
    using Microsoft.Azure.KeyVault.WebKey;
    using Microsoft.Rest;
    using Microsoft.Rest.Azure;
    using Newtonsoft.Json;

    public class KeyVaultClientInstrumented : AzureOperationInstrumented, IKeyVaultClient
    {
        private IKeyVaultClient client;

        public KeyVaultClientInstrumented(IKeyVaultClient client)
        {
            this.client = client;
            this.DependencyName = nameof(KeyVaultClient);
            this.DependencyOperationVersion = client.ApiVersion;
            this.PartnerId = "AzureKeyVault";
        }

        public JsonSerializerSettings SerializationSettings => this.client.SerializationSettings;

        public JsonSerializerSettings DeserializationSettings => this.client.DeserializationSettings;

        public ServiceClientCredentials Credentials => this.client.Credentials;

        public string ApiVersion => this.client.ApiVersion;

        public string AcceptLanguage { get => this.client.AcceptLanguage; set => this.client.AcceptLanguage = value; }

        public int? LongRunningOperationRetryTimeout { get => this.client.LongRunningOperationRetryTimeout; set => this.client.LongRunningOperationRetryTimeout = value; }

        public bool? GenerateClientRequestId { get => this.client.GenerateClientRequestId; set => this.client.GenerateClientRequestId = value; }

        public Task<AzureOperationResponse<BackupKeyResult>> BackupKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(BackupKeyWithHttpMessagesAsync), () => this.client.BackupKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<BackupSecretResult>> BackupSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(BackupSecretWithHttpMessagesAsync), () => this.client.BackupSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateOperation>> CreateCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, CertificatePolicy certificatePolicy = null, CertificateAttributes certificateAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(CreateCertificateWithHttpMessagesAsync), () => this.client.CreateCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, certificatePolicy, certificateAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyOperationResult>> DecryptWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, string algorithm, byte[] value, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(DecryptWithHttpMessagesAsync), () => this.client.DecryptWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, algorithm, value, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<Contacts>> DeleteCertificateContactsWithHttpMessagesAsync(string vaultBaseUrl, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteCertificateContactsWithHttpMessagesAsync), () => this.client.DeleteCertificateContactsWithHttpMessagesAsync(vaultBaseUrl, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IssuerBundle>> DeleteCertificateIssuerWithHttpMessagesAsync(string vaultBaseUrl, string issuerName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteCertificateIssuerWithHttpMessagesAsync), () => this.client.DeleteCertificateIssuerWithHttpMessagesAsync(vaultBaseUrl, issuerName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateOperation>> DeleteCertificateOperationWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteCertificateOperationWithHttpMessagesAsync), () => this.client.DeleteCertificateOperationWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<IPage<CertificateItem>>> GetCertificatesWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, bool? includePending = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificatesWithHttpMessagesAsync), () => this.client.GetCertificatesWithHttpMessagesAsync(vaultBaseUrl, maxresults, includePending, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<DeletedCertificateBundle>> DeleteCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteCertificateWithHttpMessagesAsync), () => this.client.DeleteCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<IPage<DeletedSasDefinitionItem>>> GetDeletedSasDefinitionsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedSasDefinitionsNextWithHttpMessagesAsync), () => this.client.GetDeletedSasDefinitionsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<DeletedKeyBundle>> DeleteKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteKeyWithHttpMessagesAsync), () => this.client.DeleteKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<SasDefinitionBundle>> RecoverDeletedSasDefinitionWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string sasDefinitionName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(RecoverDeletedSasDefinitionWithHttpMessagesAsync), () => this.client.RecoverDeletedSasDefinitionWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, sasDefinitionName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<DeletedSasDefinitionBundle>> DeleteSasDefinitionWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string sasDefinitionName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteSasDefinitionWithHttpMessagesAsync), () => this.client.DeleteSasDefinitionWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, sasDefinitionName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<DeletedSecretBundle>> DeleteSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteSecretWithHttpMessagesAsync), () => this.client.DeleteSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<StorageBundle>> RestoreStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, byte[] storageBundleBackup, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(RestoreStorageAccountWithHttpMessagesAsync), () => this.client.RestoreStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageBundleBackup, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<DeletedStorageBundle>> DeleteStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(DeleteStorageAccountWithHttpMessagesAsync), () => this.client.DeleteStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, customHeaders, cancellationToken));
        }

        public void Dispose()
        {
            ((IKeyVaultClient)this.client).Dispose();
        }

        public Task<AzureOperationResponse<KeyOperationResult>> EncryptWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, string algorithm, byte[] value, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(EncryptWithHttpMessagesAsync), () => this.client.EncryptWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, algorithm, value, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<Contacts>> GetCertificateContactsWithHttpMessagesAsync(string vaultBaseUrl, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateContactsWithHttpMessagesAsync), () => this.client.GetCertificateContactsWithHttpMessagesAsync(vaultBaseUrl, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<CertificateIssuerItem>>> GetCertificateIssuersNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateIssuersNextWithHttpMessagesAsync), () => this.client.GetCertificateIssuersNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<CertificateIssuerItem>>> GetCertificateIssuersWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateIssuersWithHttpMessagesAsync), () => this.client.GetCertificateIssuersWithHttpMessagesAsync(vaultBaseUrl, maxresults, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IssuerBundle>> GetCertificateIssuerWithHttpMessagesAsync(string vaultBaseUrl, string issuerName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateIssuerWithHttpMessagesAsync), () => this.client.GetCertificateIssuerWithHttpMessagesAsync(vaultBaseUrl, issuerName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateOperation>> GetCertificateOperationWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateOperationWithHttpMessagesAsync), () => this.client.GetCertificateOperationWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificatePolicy>> GetCertificatePolicyWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificatePolicyWithHttpMessagesAsync), () => this.client.GetCertificatePolicyWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<CertificateItem>>> GetCertificatesNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificatesNextWithHttpMessagesAsync), () => this.client.GetCertificatesNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<CertificateItem>>> GetCertificateVersionsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateVersionsNextWithHttpMessagesAsync), () => this.client.GetCertificateVersionsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<CertificateItem>>> GetCertificateVersionsWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateVersionsWithHttpMessagesAsync), () => this.client.GetCertificateVersionsWithHttpMessagesAsync(vaultBaseUrl, certificateName, maxresults, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateBundle>> GetCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, string certificateVersion, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetCertificateWithHttpMessagesAsync), () => this.client.GetCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, certificateVersion, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<DeletedCertificateItem>>> GetDeletedCertificatesNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedCertificatesNextWithHttpMessagesAsync), () => this.client.GetDeletedCertificatesNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<IPage<DeletedCertificateItem>>> GetDeletedCertificatesWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, bool? includePending = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedCertificatesWithHttpMessagesAsync), () => this.client.GetDeletedCertificatesWithHttpMessagesAsync(vaultBaseUrl, maxresults, includePending, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<DeletedCertificateBundle>> GetDeletedCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedCertificateWithHttpMessagesAsync), () => this.client.GetDeletedCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<DeletedKeyItem>>> GetDeletedKeysNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedKeysNextWithHttpMessagesAsync), () => this.client.GetDeletedKeysNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<DeletedKeyItem>>> GetDeletedKeysWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedKeysWithHttpMessagesAsync), () => this.client.GetDeletedKeysWithHttpMessagesAsync(vaultBaseUrl, maxresults, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<DeletedKeyBundle>> GetDeletedKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedKeyWithHttpMessagesAsync), () => this.client.GetDeletedKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<DeletedSecretItem>>> GetDeletedSecretsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedSecretsNextWithHttpMessagesAsync), () => this.client.GetDeletedSecretsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<DeletedSecretItem>>> GetDeletedSecretsWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedSecretsWithHttpMessagesAsync), () => this.client.GetDeletedSecretsWithHttpMessagesAsync(vaultBaseUrl, maxresults, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<DeletedSecretBundle>> GetDeletedSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedSecretWithHttpMessagesAsync), () => this.client.GetDeletedSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<KeyItem>>> GetKeysNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetKeysNextWithHttpMessagesAsync), () => this.client.GetKeysNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<KeyItem>>> GetKeysWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetKeysWithHttpMessagesAsync), () => this.client.GetKeysWithHttpMessagesAsync(vaultBaseUrl, maxresults, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<SasDefinitionBundle>> UpdateSasDefinitionWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string sasDefinitionName, string templateUri = null, string sasType = null, string validityPeriod = null, SasDefinitionAttributes sasDefinitionAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateSasDefinitionWithHttpMessagesAsync), () => this.client.UpdateSasDefinitionWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, sasDefinitionName, templateUri, sasType, validityPeriod, sasDefinitionAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<KeyItem>>> GetKeyVersionsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetKeyVersionsNextWithHttpMessagesAsync), () => this.client.GetKeyVersionsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<KeyItem>>> GetKeyVersionsWithHttpMessagesAsync(string vaultBaseUrl, string keyName, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetKeyVersionsWithHttpMessagesAsync), () => this.client.GetKeyVersionsWithHttpMessagesAsync(vaultBaseUrl, keyName, maxresults, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyBundle>> GetKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetKeyWithHttpMessagesAsync), () => this.client.GetKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<string>> GetPendingCertificateSigningRequestWithHttpMessagesAsync(string vault, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetPendingCertificateSigningRequestWithHttpMessagesAsync), () => this.client.GetPendingCertificateSigningRequestWithHttpMessagesAsync(vault, certificateName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<KeyBundle>> CreateKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string kty, int? keySize = null, IList<string> keyOps = null, KeyAttributes keyAttributes = null, IDictionary<string, string> tags = null, string curve = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(CreateKeyWithHttpMessagesAsync), () => this.client.CreateKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, kty, keySize, keyOps, keyAttributes, tags, curve, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<IPage<DeletedStorageAccountItem>>> GetDeletedStorageAccountsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedStorageAccountsNextWithHttpMessagesAsync), () => this.client.GetDeletedStorageAccountsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<SasDefinitionItem>>> GetSasDefinitionsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSasDefinitionsNextWithHttpMessagesAsync), () => this.client.GetSasDefinitionsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<SasDefinitionItem>>> GetSasDefinitionsWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSasDefinitionsWithHttpMessagesAsync), () => this.client.GetSasDefinitionsWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, maxresults, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<IPage<DeletedSasDefinitionItem>>> GetDeletedSasDefinitionsWithHttpMessagesAsync(
            string vaultBaseUrl,
            string storageAccountName,
            int? maxresults = null,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedSasDefinitionsWithHttpMessagesAsync), () => this.client.GetDeletedSasDefinitionsWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, maxresults, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<DeletedSasDefinitionBundle>> GetDeletedSasDefinitionWithHttpMessagesAsync(
            string vaultBaseUrl,
            string storageAccountName,
            string sasDefinitionName,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedSasDefinitionWithHttpMessagesAsync), () => this.client.GetDeletedSasDefinitionWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, sasDefinitionName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<SasDefinitionBundle>> GetSasDefinitionWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string sasDefinitionName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSasDefinitionWithHttpMessagesAsync), () => this.client.GetSasDefinitionWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, sasDefinitionName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<SasDefinitionBundle>> SetSasDefinitionWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string sasDefinitionName, string templateUri, string sasType, string validityPeriod, SasDefinitionAttributes sasDefinitionAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(SetSasDefinitionWithHttpMessagesAsync), () => this.client.SetSasDefinitionWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, sasDefinitionName, templateUri, sasType, validityPeriod, sasDefinitionAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<SecretItem>>> GetSecretsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSecretsNextWithHttpMessagesAsync), () => this.client.GetSecretsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<SecretItem>>> GetSecretsWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSecretsWithHttpMessagesAsync), () => this.client.GetSecretsWithHttpMessagesAsync(vaultBaseUrl, maxresults, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<SecretItem>>> GetSecretVersionsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSecretVersionsNextWithHttpMessagesAsync), () => this.client.GetSecretVersionsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<SecretItem>>> GetSecretVersionsWithHttpMessagesAsync(string vaultBaseUrl, string secretName, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSecretVersionsWithHttpMessagesAsync), () => this.client.GetSecretVersionsWithHttpMessagesAsync(vaultBaseUrl, secretName, maxresults, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<SecretBundle>> GetSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, string secretVersion, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetSecretWithHttpMessagesAsync), () => this.client.GetSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, secretVersion, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<StorageAccountItem>>> GetStorageAccountsNextWithHttpMessagesAsync(string nextPageLink, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetStorageAccountsNextWithHttpMessagesAsync), () => this.client.GetStorageAccountsNextWithHttpMessagesAsync(nextPageLink, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IPage<StorageAccountItem>>> GetStorageAccountsWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetStorageAccountsWithHttpMessagesAsync), () => this.client.GetStorageAccountsWithHttpMessagesAsync(vaultBaseUrl, maxresults, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<IPage<DeletedStorageAccountItem>>> GetDeletedStorageAccountsWithHttpMessagesAsync(string vaultBaseUrl, int? maxresults = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedStorageAccountsWithHttpMessagesAsync), () => this.client.GetDeletedStorageAccountsWithHttpMessagesAsync(vaultBaseUrl, maxresults, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<DeletedStorageBundle>> GetDeletedStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetDeletedStorageAccountWithHttpMessagesAsync), () => this.GetDeletedStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse> PurgeDeletedStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(PurgeDeletedStorageAccountWithHttpMessagesAsync), () => this.PurgeDeletedStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<StorageBundle>> RecoverDeletedStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(RecoverDeletedStorageAccountWithHttpMessagesAsync), () => this.RecoverDeletedStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<BackupStorageResult>> BackupStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(BackupStorageAccountWithHttpMessagesAsync), () => this.client.BackupStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<StorageBundle>> GetStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(GetStorageAccountWithHttpMessagesAsync), () => this.client.GetStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateBundle>> ImportCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, string base64EncodedCertificate, string password = null, CertificatePolicy certificatePolicy = null, CertificateAttributes certificateAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(ImportCertificateWithHttpMessagesAsync), () => this.client.ImportCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, base64EncodedCertificate, password, certificatePolicy, certificateAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyBundle>> ImportKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, JsonWebKey key, bool? hsm = null, KeyAttributes keyAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(ImportKeyWithHttpMessagesAsync), () => this.client.ImportKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, key, hsm, keyAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateBundle>> MergeCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, IList<byte[]> x509Certificates, CertificateAttributes certificateAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(MergeCertificateWithHttpMessagesAsync), () => this.client.MergeCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, x509Certificates, certificateAttributes, tags, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<BackupCertificateResult>> BackupCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(BackupCertificateWithHttpMessagesAsync), () => this.client.BackupCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        /// <inheritdoc />
        public Task<AzureOperationResponse<CertificateBundle>> RestoreCertificateWithHttpMessagesAsync(string vaultBaseUrl, byte[] certificateBundleBackup, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = new CancellationToken())
        {
            return this.InstrumentOutgoingCallAsync(nameof(RestoreCertificateWithHttpMessagesAsync), () => this.client.RestoreCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateBundleBackup, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse> PurgeDeletedCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(PurgeDeletedCertificateWithHttpMessagesAsync), () => this.client.PurgeDeletedCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse> PurgeDeletedKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(PurgeDeletedKeyWithHttpMessagesAsync), () => this.client.PurgeDeletedKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse> PurgeDeletedSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(PurgeDeletedSecretWithHttpMessagesAsync), () => this.client.PurgeDeletedSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateBundle>> RecoverDeletedCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(RecoverDeletedCertificateWithHttpMessagesAsync), () => this.client.RecoverDeletedCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyBundle>> RecoverDeletedKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(RecoverDeletedKeyWithHttpMessagesAsync), () => this.client.RecoverDeletedKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<SecretBundle>> RecoverDeletedSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(RecoverDeletedSecretWithHttpMessagesAsync), () => this.client.RecoverDeletedSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<StorageBundle>> RegenerateStorageAccountKeyWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string keyName, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(RegenerateStorageAccountKeyWithHttpMessagesAsync), () => this.client.RegenerateStorageAccountKeyWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, keyName, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyBundle>> RestoreKeyWithHttpMessagesAsync(string vaultBaseUrl, byte[] keyBundleBackup, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(RestoreKeyWithHttpMessagesAsync), () => this.client.RestoreKeyWithHttpMessagesAsync(vaultBaseUrl, keyBundleBackup, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<SecretBundle>> RestoreSecretWithHttpMessagesAsync(string vaultBaseUrl, byte[] secretBundleBackup, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(RestoreSecretWithHttpMessagesAsync), () => this.client.RestoreSecretWithHttpMessagesAsync(vaultBaseUrl, secretBundleBackup, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<Contacts>> SetCertificateContactsWithHttpMessagesAsync(string vaultBaseUrl, Contacts contacts, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(SetCertificateContactsWithHttpMessagesAsync), () => this.client.SetCertificateContactsWithHttpMessagesAsync(vaultBaseUrl, contacts, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IssuerBundle>> SetCertificateIssuerWithHttpMessagesAsync(string vaultBaseUrl, string issuerName, string provider, IssuerCredentials credentials = null, OrganizationDetails organizationDetails = null, IssuerAttributes attributes = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(SetCertificateIssuerWithHttpMessagesAsync), () => this.client.SetCertificateIssuerWithHttpMessagesAsync(vaultBaseUrl, issuerName, provider, credentials, organizationDetails, attributes, customHeaders, cancellationToken));
        }
        
        public Task<AzureOperationResponse<SecretBundle>> SetSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, string value, IDictionary<string, string> tags = null, string contentType = null, SecretAttributes secretAttributes = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(SetSecretWithHttpMessagesAsync), () => this.client.SetSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, value, tags, contentType, secretAttributes, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<StorageBundle>> SetStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string resourceId, string activeKeyName, bool autoRegenerateKey, string regenerationPeriod = null, StorageAccountAttributes storageAccountAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(SetStorageAccountWithHttpMessagesAsync), () => this.client.SetStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, resourceId, activeKeyName, autoRegenerateKey, regenerationPeriod, storageAccountAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyOperationResult>> SignWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, string algorithm, byte[] value, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(SignWithHttpMessagesAsync), () => this.client.SignWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, algorithm, value, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyOperationResult>> UnwrapKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, string algorithm, byte[] value, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UnwrapKeyWithHttpMessagesAsync), () => this.client.UnwrapKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, algorithm, value, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<IssuerBundle>> UpdateCertificateIssuerWithHttpMessagesAsync(string vaultBaseUrl, string issuerName, string provider = null, IssuerCredentials credentials = null, OrganizationDetails organizationDetails = null, IssuerAttributes attributes = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateCertificateIssuerWithHttpMessagesAsync), () => this.client.UpdateCertificateIssuerWithHttpMessagesAsync(vaultBaseUrl, issuerName, provider, credentials, organizationDetails, attributes, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateOperation>> UpdateCertificateOperationWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, bool cancellationRequested, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateCertificateOperationWithHttpMessagesAsync), () => this.client.UpdateCertificateOperationWithHttpMessagesAsync(vaultBaseUrl, certificateName, cancellationRequested, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificatePolicy>> UpdateCertificatePolicyWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, CertificatePolicy certificatePolicy, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateCertificatePolicyWithHttpMessagesAsync), () => this.client.UpdateCertificatePolicyWithHttpMessagesAsync(vaultBaseUrl, certificateName, certificatePolicy, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<CertificateBundle>> UpdateCertificateWithHttpMessagesAsync(string vaultBaseUrl, string certificateName, string certificateVersion, CertificatePolicy certificatePolicy = null, CertificateAttributes certificateAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateCertificateWithHttpMessagesAsync), () => this.client.UpdateCertificateWithHttpMessagesAsync(vaultBaseUrl, certificateName, certificateVersion, certificatePolicy, certificateAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyBundle>> UpdateKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, IList<string> keyOps = null, KeyAttributes keyAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateKeyWithHttpMessagesAsync), () => this.client.UpdateKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, keyOps, keyAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<SecretBundle>> UpdateSecretWithHttpMessagesAsync(string vaultBaseUrl, string secretName, string secretVersion, string contentType = null, SecretAttributes secretAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateSecretWithHttpMessagesAsync), () => this.client.UpdateSecretWithHttpMessagesAsync(vaultBaseUrl, secretName, secretVersion, contentType, secretAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<StorageBundle>> UpdateStorageAccountWithHttpMessagesAsync(string vaultBaseUrl, string storageAccountName, string activeKeyName = null, bool? autoRegenerateKey = null, string regenerationPeriod = null, StorageAccountAttributes storageAccountAttributes = null, IDictionary<string, string> tags = null, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(UpdateStorageAccountWithHttpMessagesAsync), () => this.client.UpdateStorageAccountWithHttpMessagesAsync(vaultBaseUrl, storageAccountName, activeKeyName, autoRegenerateKey, regenerationPeriod, storageAccountAttributes, tags, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyVerifyResult>> VerifyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, string algorithm, byte[] digest, byte[] signature, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(VerifyWithHttpMessagesAsync), () => this.client.VerifyWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, algorithm, digest, signature, customHeaders, cancellationToken));
        }

        public Task<AzureOperationResponse<KeyOperationResult>> WrapKeyWithHttpMessagesAsync(string vaultBaseUrl, string keyName, string keyVersion, string algorithm, byte[] value, Dictionary<string, List<string>> customHeaders = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.InstrumentOutgoingCallAsync(nameof(WrapKeyWithHttpMessagesAsync), () => client.WrapKeyWithHttpMessagesAsync(vaultBaseUrl, keyName, keyVersion, algorithm, value, customHeaders, cancellationToken));
        }
    }
}