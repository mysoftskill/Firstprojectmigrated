namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Controllers.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;

    using global::Autofac;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Models.V2;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.Writer;
    using Microsoft.PrivacyServices.DataManagement.Frontdoor.UnitTests;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.Identity;
    using Microsoft.PrivacyServices.Testing;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;
    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    public class DataAssetsControllerTest
    {
        [Theory(DisplayName = "When the asset qualifier is not valid, then DataAssets.FindByQualifier throws an InvalidArgumentError."), AutofixtureCustomizations.TypeCorrections()]
        public async Task When_QualifierValueIsNotValid_Then_ThrowInvalidArgumentError(
            Mock<IDataAssetReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataAssetReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {

                string url = $"/api/v2/dataAssets/v2.findByQualifier(qualifier=@value)?@value='AssetType=AzureBlob;DatabaseName=DB'";

                HttpResponseMessage response = await server.HttpClient.GetAsync(url).ConfigureAwait(false);

                // Act.
                var responseMessage = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains("\"code\":\"BadArgument\"", responseMessage);
                Assert.Contains("\"code\":\"InvalidValue\"", responseMessage);
                Assert.Contains($"\"target\":\"qualifier\"", responseMessage);
                Assert.Contains($"\"value\":\"AssetType=AzureBlob;DatabaseName=DB\"", responseMessage);
            }
        }

        [Theory(DisplayName = "Verify FindByQualifier query parameters."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyFindByQualifierQueryParameters(
            Mock<IDataAssetReader> reader,
            AssetQualifier qualifier)
        {
            var queryParameters = "&$top=1&$skip=0";

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataAssetReader>();
            };
            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {

                string url = $"/api/v2/dataAssets/v2.findByQualifier(qualifier=@value)?@value='{qualifier.Value}'{queryParameters}";

                HttpResponseMessage response = await server.HttpClient.GetAsync(url).ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<DataAsset>(response).ConfigureAwait(false);

                // Assert.
                Action<Core.DataAssetFilterCriteria> verify = filter =>
                {
                    Assert.Equal(0, filter.Index.Value);
                    Assert.Equal(1, filter.Count.Value);
                };

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.FindByQualifierAsync(Is.Value(verify), qualifier, false), Times.Once);
            }
        }

        [Theory(DisplayName = "Verify FindByQualifier returns the correct result."), AutofixtureCustomizations.TypeCorrections()]
        public async Task VerifyFindByQualifierReturnsCorrectResult(
            [Frozen] FilterResult<Core.DataAsset> filterResult,
            Mock<IDataAssetReader> reader,
            AssetQualifier qualifier)
        {
            filterResult.Total = 1;

            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IDataAssetReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                {
                    string url = $"/api/v2/dataAssets/v2.findByQualifier(qualifier=@value)?@value='{qualifier.Value}'&$top=10&$skip=0";

                    HttpResponseMessage response = await server.HttpClient.GetAsync(url).ConfigureAwait(false);

                    // Act.
                    var actual = await TestHelper.DeserializePagingResponse<DataAsset>(response).ConfigureAwait(false);

                    // Assert.
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Assert.Null(actual.NextLink);

                    filterResult.Values.SequenceAssert(actual.Value, (src, dest) => Likenesses.ForDataAsset(dest).ShouldEqual(src));
                }
            }
        }
    }
}