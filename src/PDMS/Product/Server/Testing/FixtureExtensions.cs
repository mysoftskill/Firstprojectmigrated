namespace Microsoft.PrivacyServices.Testing
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Identity.Metadata;
    using Microsoft.PrivacyServices.Policy;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;

    /// <summary>
    /// Provides a set of extensions to the <see cref="Fixture" /> class.
    /// </summary>
    public static class FixtureExtensions
    {
        /// <summary>
        /// By default AutoFixture will not create an object that has a recursive definition.
        /// This disables that check. It should only be disabled when there is a legitimate reason.
        /// Otherwise, it most likely indicates a design flaw.
        /// </summary>
        /// <param name="fixture">The AutoFixture object.</param>
        /// <returns>The updated fixture.</returns>
        public static IFixture DisableRecursionCheck(this IFixture fixture)
        {
            // Disables the recursion check.
            fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());
            return fixture;
        }

        /// <summary>
        /// Updates the fixture so that auto generated data has fully mocked properties and methods.
        /// </summary>
        /// <param name="fixture">The fixture to update.</param>
        /// <returns>The updated fixture.</returns>
        public static IFixture EnableAutoMoq(this IFixture fixture)
        {
            fixture.Customize(new AutoConfiguredMoqCustomization());
            return fixture;
        }

        /// <summary>
        /// Updates the fixture so that Microsoft.PrivacyServices.Policy data can be mocked.
        /// </summary>
        /// <param name="fixture">The fixture to update.</param>
        /// <returns>The updated fixture.</returns>
        public static IFixture EnablePolicy(this IFixture fixture)
        {
            fixture.Inject<Policy>(Policies.Current);
            fixture.Inject<DataTypeId>(Policies.Current.DataTypes.Set.First().Id);
            fixture.Inject<ProtocolId>(Policies.Current.Protocols.Set.First().Id);
            fixture.Inject<CapabilityId>(Policies.Current.Capabilities.Ids.Delete);
            fixture.Inject<OptionalFeatureId>(Policies.Current.OptionalFeatures.Ids.MsaAgeOutOptIn);
            fixture.Inject<CloudInstanceId>(Policies.Current.CloudInstances.Set.First().Id);
            fixture.Inject<DataResidencyInstanceId>(Policies.Current.DataResidencyInstances.Set.First().Id);
            fixture.Inject<IEnumerable<CloudInstanceId>>(new[] { Policies.Current.CloudInstances.Set.First().Id });
            fixture.Inject<IEnumerable<DataResidencyInstanceId>>(new[] { Policies.Current.DataResidencyInstances.Set.First().Id });
            fixture.Inject<IEnumerable<CapabilityId>>(new[] { Policies.Current.Capabilities.Ids.Delete });
            fixture.Inject<IEnumerable<OptionalFeatureId>>(new[] { Policies.Current.OptionalFeatures.Ids.MsaAgeOutOptIn });
            fixture.Inject<IEnumerable<DataTypeId>>(new[] { Policies.Current.DataTypes.Set.First().Id });
            fixture.Inject<IEnumerable<SubjectTypeId>>(new[] { Policies.Current.SubjectTypes.Set.First().Id });
            return fixture;
        }

        /// <summary>
        /// Updates the fixture so that Microsoft.PrivacyServices.Identity data can be mocked.
        /// </summary>
        /// <param name="fixture">The fixture to update.</param>
        /// <returns>The updated fixture.</returns>
        public static IFixture EnableIdentity(this IFixture fixture)
        {
            fixture.Inject<IManifest>(Manifest.Current);

            fixture.Customize<AssetQualifier>(
                x =>
                x.FromFactory<int>(i =>
                {
                    var index = i % Manifest.Current.AssetTypes.Count();

                    var typeDefinition = Manifest.Current.AssetTypes.ElementAt(index);

                    var dictionary = new Dictionary<string, string>();
                    dictionary.Add("AssetType", typeDefinition.Id.ToString());

                    bool isApiAsset = typeDefinition.Id.ToString().Equals("API");
                    foreach (var prop in typeDefinition.Properties)
                    {
                        if (prop.Id == "RelativePath")
                        {
                            dictionary.Add(prop.Id, $"/local/{fixture.Create<string>()}");
                        }
                        else if (isApiAsset && prop.Id == "Host")
                        {
                            dictionary.Add(prop.Id, $"http://host-{fixture.Create<string>()}.com");
                        }
                        else if (isApiAsset && prop.Id == "Method")
                        {
                            dictionary.Add(prop.Id, $"PUT");
                        }
                        else
                        {
                            dictionary.Add(prop.Id, fixture.Create<string>());
                        }
                    }

                    return AssetQualifier.CreateFromDictionary(dictionary);
                }));

            return fixture;
        }
    }
}
