namespace Microsoft.Azure.ComplianceServices.Common.UnitTests
{
    using Microsoft.Azure.ComplianceServices.Common.AppConfig.Cache;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Xunit;

    [Trait("Category", "UnitTest")]
    public class AppConfigurationTest
    {
        public static string localSettingFile = "local.settings.test.json";

        [Theory]
        [InlineData("Arg1")]
        [InlineData("ARG2")]
        public async Task CustomOperatorFilterTestForStringOperatorIncludes(string arg)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeStringInclude", 
                CustomOperatorContextFactory.CreateDefaultStringComparisonContext(arg)).ConfigureAwait(false);

            Assert.True(isEnabled);
        }

        [Theory]
        [InlineData("ARG3")]
        [InlineData("arg4")]
        public async Task CustomOperatorFilterTestForStringOperatorNotInIncludes(string arg)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeStringInclude", 
                CustomOperatorContextFactory.CreateDefaultStringComparisonContext(arg)).ConfigureAwait(false);

            Assert.False(isEnabled);
        }

        [Theory]
        [InlineData("ARG1")]
        [InlineData("Arg2")]
        public async Task CustomOperatorFilterTestForStringOperatorExlcudes(string arg)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeStringExclude", 
                CustomOperatorContextFactory.CreateDefaultStringComparisonContext(arg)).ConfigureAwait(false);

            Assert.False(isEnabled);
        }

        [Theory]
        [InlineData("arg3")]
        [InlineData("arg4")]
        public async Task CustomOperatorFilterTestForStringOperatorNotInExlcudes(string arg)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeStringExclude", 
                CustomOperatorContextFactory.CreateDefaultStringComparisonContext(arg)).ConfigureAwait(false);

            Assert.True(isEnabled);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        public async Task CustomOperatorFilterTestForIntOperatorIncludes(int num)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeIntInclude", 
                CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(num)).ConfigureAwait(false);

            Assert.True(isEnabled);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(200)]
        public async Task CustomOperatorFilterTestForIntOperatorExcludes(int num)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeIntExclude", 
                CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(num)).ConfigureAwait(false);

            Assert.False(isEnabled);
        }

        [Theory]
        [InlineData(25)]
        [InlineData(10)]
        public async Task CustomOperatorFilterTestForLessThanOperator(int num)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeLessThan", 
                CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(num)).ConfigureAwait(false);

            Assert.True(isEnabled);
        }

        [Theory]
        [InlineData(125)]
        [InlineData(110)]
        public async Task CustomOperatorFilterTestForLessThanOperatorDisabled(int num)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeLessThan", 
                CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(num)).ConfigureAwait(false);

            Assert.False(isEnabled);
        }

        [Theory]
        [InlineData(150)]
        [InlineData(200)]
        public async Task CustomOperatorFilterTestForGreaterThanOperator(int num)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeGreaterThan", 
                CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(num)).ConfigureAwait(false);

            Assert.True(isEnabled);
        }

        [Theory]
        [InlineData(50)]
        [InlineData(20)]
        public async Task CustomOperatorFilterTestForGreaterThanOperatorDisabled(int num)
        {
            AppConfiguration config = new AppConfiguration(localSettingFile);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync<ICustomOperatorContext>("TestFeatureTypeGreaterThan", 
                CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(num)).ConfigureAwait(false);

            Assert.False(isEnabled);
        }

        [Fact]
        public async Task VerifyCacheDisablingInFeatureEvaluation()
        {
            var cache = new LruCache<bool>();
            AppConfiguration config = new AppConfiguration(localSettingFile, null, cache);
            var featureName = "UndefinedFeature";

            ICustomOperatorContext context = CustomOperatorContextFactory.CreateDefaultIntValueComparisonContext(10);

            bool isEnabled = await config.IsFeatureFlagEnabledAsync(featureName, false).ConfigureAwait(false);
            // must be false.
            Assert.False(isEnabled);

            // Change it in cache.
            cache.AddItem(featureName, true);
            cache.AddItem(featureName + context.ToString(), true);

            // confirm that cache is read.
            Assert.True(await config.IsFeatureFlagEnabledAsync(featureName).ConfigureAwait(false));
            Assert.True(await config.IsFeatureFlagEnabledAsync(featureName, context).ConfigureAwait(false));

            // try without cache.
            Assert.False(await config.IsFeatureFlagEnabledAsync(featureName, false).ConfigureAwait(false));
            Assert.False(await config.IsFeatureFlagEnabledAsync(featureName, context, false).ConfigureAwait(false));
        }

        [Fact]
        public async Task TestFeatureWithMultiplePropertyFilters()
        {
            const string featureName = "TestFeatureTypeProperty";
            // Filter is set to 50% so every other call should resolve to true
            AppConfiguration config = new AppConfiguration(localSettingFile);

            List<ICustomOperatorContext> contextList = new List<ICustomOperatorContext>
            {
                // feature is Enabled for the following context.
                CustomOperatorContextFactory.CreateDefaultIntComparisonContextWithKeyValue("OID", 90),
                // feature is disabled for the following context.
                CustomOperatorContextFactory.CreateDefaultIntComparisonContextWithKeyValue("OID", 10)
            };

            bool isEnabled = await config.IsFeatureFlagEnabledAnyAsync(featureName, contextList).ConfigureAwait(false);

            Assert.True(isEnabled);

            isEnabled = await config.IsFeatureFlagEnabledAllAsync(featureName, contextList).ConfigureAwait(false);
            Assert.False(isEnabled);

            isEnabled = await config.IsFeatureFlagEnabledAsync(featureName, CustomOperatorContextFactory.CreateDefaultIntComparisonContextWithKeyValue("OID",90)).ConfigureAwait(false);

            Assert.True(isEnabled);

            isEnabled = await config.IsFeatureFlagEnabledAsync(featureName, CustomOperatorContextFactory.CreateDefaultIntComparisonContextWithKeyValue("OID", 10)).ConfigureAwait(false);

            Assert.False(isEnabled);

            isEnabled = await config.IsFeatureFlagEnabledAsync(featureName, CustomOperatorContextFactory.CreateDefaultIntComparisonContextWithKeyValue("PID", 20)).ConfigureAwait(false);

            Assert.True(isEnabled);

            isEnabled = await config.IsFeatureFlagEnabledAsync(featureName, CustomOperatorContextFactory.CreateDefaultIntComparisonContextWithKeyValue("PID", 90)).ConfigureAwait(false);

            Assert.False(isEnabled);
        }
    }
}