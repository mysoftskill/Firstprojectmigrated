namespace PCF.UnitTests.Frontdoor
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor.ApiTrafficThrottling;

    using Xunit;

    [Trait("Category", "UnitTest")]
    public class ApiTrafficHandlerTests
    {
        private ApiTrafficHandler apiTrafficHandler;

        // Config Names. Corresponds to the names in local.settings.test.json
        private const string ShouldNotThrottleTrafficConfigName = "PCF.ApiTrafficPercantage_ShouldNotThrottleTraffic";
        private const string ShouldThrottleAllTrafficConfigName = "PCF.ApiTrafficPercantage_ShouldThrottleAllTraffic";
        private const string ShouldThrottleGivenTrafficConfigName = "PCF.ApiTrafficPercantage_ShouldThrottleGivenTraffic";
        private const string ShouldThrottleAtGivenPercentageConfigName = "PCF.ApiTrafficPercantage_ShouldThrottleAtGivenPercentage";


        public ApiTrafficHandlerTests()
        {
            FlightingUtilities.Initialize(new AppConfiguration("local.settings.test.json"));
            this.apiTrafficHandler = new ApiTrafficHandler();
        }

        [Fact]
        public void ShouldNotThrottleTraffic()
        {
            // act
            var shouldAllowAll = this.apiTrafficHandler.ShouldAllowTraffic(ShouldNotThrottleTrafficConfigName, "*", "*");
            var shouldAllowApi1AgentId1 = this.apiTrafficHandler.ShouldAllowTraffic(ShouldNotThrottleTrafficConfigName, "ApiName1", "AgentId1");

            // assert. No traffic percentage is set in app config. Should not apply throttling logic at all
            Assert.True(shouldAllowAll, "Should allow");
            Assert.True(shouldAllowApi1AgentId1, "Should allow");
        }

        [Fact]
        public void ShouldThrottleAllTraffic()
        {
            // act
            var shouldAllowApi1AgentId1 = this.apiTrafficHandler.ShouldAllowTraffic(ShouldThrottleAllTrafficConfigName,"ApiName1","AgentId1");
            var shouldAllowApi2AgentId2 = this.apiTrafficHandler.ShouldAllowTraffic(ShouldThrottleAllTrafficConfigName, "ApiName2", "AgentId2");

            // assert. Should throttle all because percentage value for "*.*.*" is set to 0
            Assert.False(shouldAllowApi1AgentId1, "Should throttle even if its own percentage value is set to 100");
            Assert.False(shouldAllowApi2AgentId2, "Should throttle even if its own percentage value is set to 100");
        }

        [Fact]
        public void ShouldThrottleGivenTraffic()
        {
            // act
            var shouldAllowAll = this.apiTrafficHandler.ShouldAllowTraffic(ShouldThrottleGivenTrafficConfigName, "*", "*");
            var shouldAllowApiAgentId = this.apiTrafficHandler.ShouldAllowTraffic(ShouldThrottleGivenTrafficConfigName, "ApiName", "AgentId");

            // assert. Should only throttle given traffic
            Assert.True(shouldAllowAll, "Should allow in general because its percentage is set to 100");
            Assert.False(shouldAllowApiAgentId, "Should throttle because its own percentage value is set to 0");
        }

        [Fact]
        public void ShouldThrottleAtGivenPercentage()
        {
            int retryTimes = 5;
            int allowedTimes = 0;

            // act
            for (int i = 0; i < retryTimes; i++)
            {
                allowedTimes = CountAllowedTimesByRunningInBatch();
                if (allowedTimes >= 30 && allowedTimes <= 70)
                {
                    break;
                }
            }

            // assert. The traffic gets allowed should be about 50%
            Assert.True(allowedTimes >= 30 && allowedTimes <= 70, 
                "The result can be flaky even it's given a wide range and retried 5 times since it totally depends on probabliity. Please rerun if asset failed.");
        }

        private int CountAllowedTimesByRunningInBatch()
        {
            int allowedCounter = 0;

            // A for-loop to call ShouldAllowTraffic 100 times
            for (int i = 0; i < 100; i++)
            {
                var shouldAllowApiAgentId = this.apiTrafficHandler.ShouldAllowTraffic(ShouldThrottleAtGivenPercentageConfigName, "ApiName", "AgentId");
                if (shouldAllowApiAgentId)
                {
                    allowedCounter++;
                }
            }

            return allowedCounter;
        }
    }
}
