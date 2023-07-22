namespace PCF.UnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    using Microsoft.Azure.ComplianceServices.Common.SecretClient;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common.ConfigGen;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ConfigurationTests
    {
        /// <summary>
        /// Ensures that we can read all configuration files.
        /// </summary>
        [Fact]
        public void Configuration_CanReadConfig()
        {
            int i = 0;
            
            DirectoryInfo current = new DirectoryInfo(Environment.CurrentDirectory);
            foreach (var fileName in current.GetFiles("Config.*.config"))
            {
                XDocument doc = XDocument.Load(fileName.FullName);
                Configuration c = new Configuration(doc.Root, ConfigurationValueParsers.GetParsers(new NoOpKeyVaultClient()).ToArray());

                // Make sure all configs indicate that they are exactly one of [INT,PPE, or PROD].
                int environmentTypeCount = 0;
                if (c.Common.IsPreProdEnvironment)
                {
                    environmentTypeCount++;
                }

                if (c.Common.IsProductionEnvironment)
                {
                    environmentTypeCount++;
                }

                if (c.Common.IsTestEnvironment)
                {
                    environmentTypeCount++;
                }

                if (c.Common.IsStressEnvironment)
                {
                    environmentTypeCount++;
                }

                Assert.Equal(1, environmentTypeCount);
                
                Assert.True(c.CosmosDBQueues.Instances.Count() > 0);
                i++;
            }

            Assert.True(Config.Instance.CosmosDBQueues.Instances.Count() > 0);
            Assert.True(i > 0);
        }
    }
}
