
namespace Microsoft.Membership.MemberServices.Common
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Common.Configuration;
    using Microsoft.Membership.MemberServices.Common.SecretStore;
    using Microsoft.PrivacyServices.Common.Azure;

    public class RedisClientFactory
    {
        public static IRedisClient Create(IPrivacyConfigurationManager config, ISecretStoreReader secretReader, string clientName, ILogger logger)
        {
            if (config.EnvironmentConfiguration.EnvironmentType == MemberServices.Configuration.EnvironmentType.OneBox)
            {
                return new InMemoryRedisClient();
            }
            else
            {
                var redisConnection = new RedisConnection(
                    clientName,
                    config.AzureRedisCacheConfiguration.Endpoint,
                    config.AzureRedisCacheConfiguration.Port,
                    secretReader.ReadSecretByNameAsync(config.AzureRedisCacheConfiguration.PasswordSecretName).GetAwaiter().GetResult(),
                    logger);

                return new RedisClient(redisConnection, logger);
            }
        }
    }
}
