# Azure Key Vault Certificate Installer

## Summary
This installer will find all of the certificates in a specified Azure Key Vault, install them on the local machine, and grant access to network service.

## Pre-requisites
1) A managed identity (when run in Pilotfish) needs access to the Key Vault. For developers, you must be in the correct security group (such as meepxseng).

2) The SSL certificate (found in Key Vault) must have a secret name of "ssl". This allows cert binding to take place without storing a cert thumbprint in any configuration/code.

## Instructions for Developer to run on Dev box

1) Make sure you are a member of the SG that can access Key Vault.
2) Launch VS as Admin
3) Start AzureKeyVaultCertificateInstaller as StartUp project.
4) F5 to run.
5) Look for successful output: *Certificate installation completed successfully!*

## What does this installer do?

1) Creates a secure connection to Azure Key Vault with either a [system-assigned managed identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview), or a [user-assigned managed identity](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) - depending on the context this is executed in. 
More on how that works at this [link](https://docs.microsoft.com/en-us/azure/key-vault/service-to-service-authentication)

2) List all certificates found in Key Vault.
See GlobalConfiguration.ini for the target Key Vault.

3) Iterate over each certificate and retrieves the private key from Key Vault.

4) Checks if the certificate is valid: enabled and not expired.

5) If cert is valid, it installs the certificate private key to the local machine certificate store.

6) Grant network service access to the certificate private key.

7) Add SSL certificate binding for the SSL certificate to port 443.

*Note: This is intended to operate the same on a local machine and in Pilotfish.*