namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using AutoMapper;

    using Xunit;

    public class MappingProfileTest
    {
        [Fact(DisplayName = "Verify AutoMapper configuration (V2)")]
        public void VerifyAutoMapper()
        {
            var mapConfig = new MapperConfiguration(cfg => cfg.AddProfile(new MappingProfile()));
            mapConfig.AssertConfigurationIsValid<MappingProfile>();
        }
    }
}