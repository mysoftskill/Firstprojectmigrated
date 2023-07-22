namespace PCF.UnitTests
{
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor;
    using Microsoft.Windows.Services.AuthN.Server;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class AadAuthenticationConfigurationTests
    {
        [Fact]
        public void GetAuthenticationConfigurationGetsProductionConfig()
        {
            string issuer = "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/";
            var config = AadAuthenticationConfiguration.GetAuthenticationConfiguration(issuer, AadAuthenticationConfiguration.Public.Audiences);

            Assert.Equal(AadAuthenticationConfiguration.Public.Audiences, config.Audiences);
            Assert.Equal(AadAuthenticationConfiguration.Public.CloudInstance, config.CloudInstance);
            Assert.Equal(AadAuthenticationConfiguration.Public.Issuer, config.Issuer);
            Assert.Equal("https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/.well-known/openid-configuration", config.OpenIdConfigurationEndpoint);
            Assert.NotNull(config.ConfigurationManager);
        }

        [Fact]
        public void GetAuthenticationConfigurationGetsAmeConfig()
        {
            string issuer = "https://sts.windows.net/33e01921-4d64-4f8c-a055-5bdaffd5e33d/";
            var config = AadAuthenticationConfiguration.GetAuthenticationConfiguration(issuer, AadAuthenticationConfiguration.Public.Audiences);

            Assert.Equal(AadAuthenticationConfiguration.Public.Audiences, config.Audiences);
            Assert.Equal(AadAuthenticationConfiguration.Public.CloudInstance, config.CloudInstance);
            Assert.Equal(AadAuthenticationConfiguration.Public.Issuer, config.Issuer);
            Assert.Equal("https://sts.windows.net/33e01921-4d64-4f8c-a055-5bdaffd5e33d/.well-known/openid-configuration", config.OpenIdConfigurationEndpoint);
            Assert.NotNull(config.ConfigurationManager);
        }

        [Fact]
        public void GetAuthenticationConfigurationGetsMooncakeConfig()
        {
            string issuer = "https://sts.chinacloudapi.cn/a55a4d5b-9241-49b1-b4ff-befa8db00269/";
            var config = AadAuthenticationConfiguration.GetAuthenticationConfiguration(issuer, AadAuthenticationConfiguration.Mooncake.Audiences);

            Assert.Equal(AadAuthenticationConfiguration.Mooncake.Audiences, config.Audiences);
            Assert.Equal(AadAuthenticationConfiguration.Mooncake.CloudInstance, config.CloudInstance);
            Assert.Equal(AadAuthenticationConfiguration.Mooncake.Issuer, config.Issuer);
            Assert.Equal("https://sts.chinacloudapi.cn/a55a4d5b-9241-49b1-b4ff-befa8db00269/.well-known/openid-configuration", config.OpenIdConfigurationEndpoint);
            Assert.NotNull(config.ConfigurationManager);
        }

        [Fact]
        public void GetAuthenticationConfigurationGetsMooncakeDevopsConfig()
        {
            string issuer = "https://sts.chinacloudapi.cn/3d0a72e2-8b06-4528-98df-1391c6f12c11/";
            var config = AadAuthenticationConfiguration.GetAuthenticationConfiguration(issuer, AadAuthenticationConfiguration.Mooncake.Audiences);

            Assert.Equal(AadAuthenticationConfiguration.Mooncake.Audiences, config.Audiences);
            Assert.Equal(AadAuthenticationConfiguration.Mooncake.CloudInstance, config.CloudInstance);
            Assert.Equal(AadAuthenticationConfiguration.Mooncake.Issuer, config.Issuer);
            Assert.Equal("https://sts.chinacloudapi.cn/3d0a72e2-8b06-4528-98df-1391c6f12c11/.well-known/openid-configuration", config.OpenIdConfigurationEndpoint);
            Assert.NotNull(config.ConfigurationManager);
        }

        [Fact]
        public void GetAuthenticationConfigurationGetsFairfaxConfig()
        {
            string issuer = "https://sts.windows.net/cab8a31a-1906-4287-a0d8-4eef66b95f6e/";
            var config = AadAuthenticationConfiguration.GetAuthenticationConfiguration(issuer, AadAuthenticationConfiguration.Fairfax.Audiences);

            Assert.Equal(AadAuthenticationConfiguration.Fairfax.Audiences, config.Audiences);
            Assert.Equal(AadAuthenticationConfiguration.Fairfax.CloudInstance, config.CloudInstance);
            Assert.Equal(AadAuthenticationConfiguration.Fairfax.Issuer, config.Issuer);
            Assert.Equal("https://sts.windows.net/cab8a31a-1906-4287-a0d8-4eef66b95f6e/.well-known/openid-configuration", config.OpenIdConfigurationEndpoint);
            Assert.NotNull(config.ConfigurationManager);
        }

        [Fact]
        public void GetAuthenticationConfigurationGetsFirstPartyConfigCorrectly()
        {
            string firstPartyIssuer = "https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/";

            var publicConfiguration = AadAuthenticationConfiguration.GetAuthenticationConfiguration(firstPartyIssuer, AadAuthenticationConfiguration.Public.Audiences);
            var fairfaxConfiguration = AadAuthenticationConfiguration.GetAuthenticationConfiguration(firstPartyIssuer, AadAuthenticationConfiguration.Fairfax.Audiences);

            Assert.Equal(AadAuthenticationConfiguration.Public.Audiences, publicConfiguration.Audiences);
            Assert.Equal(AadAuthenticationConfiguration.Public.CloudInstance, publicConfiguration.CloudInstance);
            Assert.Equal(AadAuthenticationConfiguration.Public.Issuer, publicConfiguration.Issuer);
            Assert.Equal("https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/.well-known/openid-configuration", publicConfiguration.OpenIdConfigurationEndpoint);
            Assert.NotNull(publicConfiguration.ConfigurationManager);

            Assert.Equal(AadAuthenticationConfiguration.Fairfax.Audiences, fairfaxConfiguration.Audiences);
            Assert.Equal(AadAuthenticationConfiguration.Fairfax.CloudInstance, fairfaxConfiguration.CloudInstance);
            Assert.Equal(AadAuthenticationConfiguration.Fairfax.Issuer, fairfaxConfiguration.Issuer);
            Assert.Equal("https://sts.windows.net/f8cdef31-a31e-4b4a-93e4-5f571e91255a/.well-known/openid-configuration", fairfaxConfiguration.OpenIdConfigurationEndpoint);
            Assert.NotNull(fairfaxConfiguration.ConfigurationManager);
        }

        [Fact]
        public void GetAuthenticationConfigurationThrowsForUnknownIssuer()
        {
            string issuer = "https://sts.microsoftonline.us/abc8a31a-1906-4287-a0d8-4eef66b95f6e/";
            bool isException = false;
            try
            {
                AadAuthenticationConfiguration.GetAuthenticationConfiguration(issuer, AadAuthenticationConfiguration.Fairfax.Audiences);
            }
            catch (AuthNException)
            {
                isException = true;
            }

            Assert.True(isException);
        }
    }
}
