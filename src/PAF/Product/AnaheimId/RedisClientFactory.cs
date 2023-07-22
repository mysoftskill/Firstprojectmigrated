namespace Microsoft.PrivacyServices.AnaheimId
{
    using System;
    using System.Runtime.CompilerServices;

    using global::Azure.Identity;

    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.AnaheimId.Config;
    using Microsoft.PrivacyServices.AzureFunctions.Common;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// RedisClientFactory
    /// </summary>
    public class RedisClientFactory
    {
        /// <summary>
        /// Create
        /// </summary>
        /// <param name="aidFuncConfig">AIdFunctionConfiguration</param>
        /// <returns>IRedisClient</returns>
        public static IRedisClient Create(IAIdFunctionConfiguration aidFuncConfig)
        {
            try
            {
                if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
                {
                    return new InMemoryRedisClient();
                }
                else
                {
                    var redisPassword = GetRedisSecret(aidFuncConfig.AIdUamiId, aidFuncConfig.RedisPasswordKeyVaultEndpoint, aidFuncConfig.RedisCachePasswordName);

                    var redisConnection = new RedisConnection(
                        "AidFunction",
                        aidFuncConfig.RedisCacheEndpoint,
                        aidFuncConfig.RedisCachePort,
                        redisPassword,
                        DualLogger.Instance);
                    return new RedisClient(redisConnection, DualLogger.Instance);
                }
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(RedisClientFactory), ex, $"Unexpected exception occured while creating Redis client.");
                throw;
            }
        }

        /// <summary>
        /// Retrieve Redis password from Azure key vault.
        /// </summary>
        /// <returns>A secret.</returns>
        private static string GetRedisSecret(string msiClientId, string keyVaultUrl, string secretName)
        {
            // Create a keyvault client using msi
            var secretsReader = new SecretsReader(keyVaultUrl, new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = msiClientId }), "secrets/" + secretName);

            try
            {
                return secretsReader.GetSecretByNameAsync(secretName).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                DualLogger.Instance.Error(nameof(RedisClientFactory), ex, $"Failed to get redis cache secret by name.");
                throw;
            }
        }
    }
}
