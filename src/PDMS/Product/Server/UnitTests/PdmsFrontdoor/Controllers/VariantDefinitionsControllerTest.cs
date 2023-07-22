[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:ElementParametersMustBeDocumented", Justification = "Swagger documentation.")]

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1615:ElementReturnValueMustBeDocumented", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using global::Autofac;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.UnitTests;

    using Moq;

    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    using Core = Models.V2;

    public class VariantDefinitionsControllerTest
    {
        [Theory(DisplayName = "Verify VariantDefinitions.Create."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyCreate(
            VariantDefinition variantDefinition,
            [Frozen] Core.VariantDefinition coreVariantDefinition,
            Mock<IVariantDefinitionWriter> writer)
        {
            coreVariantDefinition.Owner = null;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IVariantDefinitionWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PostAsync(
                            "api/v2/variantDefinitions",
                            TestHelper.Serialize(variantDefinition)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantDefinition>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.Created, response.StatusCode);
                writer.Verify(m => m.CreateAsync(It.IsAny<Core.VariantDefinition>()), Times.Once);

                Likenesses.ForVariantDefinition(actual).ShouldEqual(coreVariantDefinition);
            }
        }

        [Theory(DisplayName = "When ReadById is called with expansions, then parse appropriately.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        public async Task When_ReadByIdWithExpansions_Then_ParseAppropriately(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Guid id,
            Mock<IVariantDefinitionReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IVariantDefinitionReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/variantDefinitions('{id}'){queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantDefinition>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByIdAsync(id, expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "When ReadById is called without expansions, then exclude all expansions."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByIdWithoutExpansions_Then_ExcludeAll(Mock<IVariantDefinitionReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IVariantDefinitionReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/variantDefinitions('{Guid.Empty}')?$select=id").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantDefinition>(response).ConfigureAwait(false);

                // Assert.
                Assert.Null(actual.TrackingDetails);
            }
        }

        [Theory(DisplayName = "When ReadByFilters is called with expansions, then parse properly.")]
        [AutofixtureCustomizations.InlineTypeCorrections("", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=id", Core.ExpandOptions.None)]
        [AutofixtureCustomizations.InlineTypeCorrections("?$select=trackingDetails", Core.ExpandOptions.TrackingDetails)]
        public async Task When_ReadByFiltersHasExpansions_Then_ParseProperly(
            string queryParameters,
            Core.ExpandOptions expandOptions,
            Mock<IVariantDefinitionReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IVariantDefinitionReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/variantDefinitions{queryParameters}").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<VariantDefinition>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByFiltersAsync(It.IsAny<Core.VariantDefinitionFilterCriteria>(), expandOptions), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify VariantDefinitions.Update."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyUpdate(
            VariantDefinition variantDefinition,
            [Frozen] Core.VariantDefinition coreVariantDefinition,
            Mock<IVariantDefinitionWriter> writer)
        {
            coreVariantDefinition.Owner = null;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(writer.Object).As<IVariantDefinitionWriter>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.PutAsync(
                            $"api/v2/variantDefinitions('{variantDefinition.Id}')",
                            TestHelper.Serialize(variantDefinition)).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.Deserialize<VariantDefinition>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                writer.Verify(m => m.UpdateAsync(It.IsAny<Core.VariantDefinition>()), Times.Once);

                Likenesses.ForVariantDefinition(actual).ShouldEqual(coreVariantDefinition);
            }
        }
    }
}
