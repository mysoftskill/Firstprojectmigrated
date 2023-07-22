[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1611:ElementParametersMustBeDocumented", Justification = "Swagger documentation.")]

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1615:ElementReturnValueMustBeDocumented", Justification = "Swagger documentation.")]


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
    using Microsoft.PrivacyServices.Testing;
    using Moq;

    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;
    using Core = Microsoft.PrivacyServices.DataManagement.Models.V2;

    public class HistoryItemsControllerTest
    {
        [Theory(DisplayName = "When ReadByFilters is called, then parse properly."), AutofixtureCustomizations.TypeCorrections]
        public async Task When_ReadByFilters_Then_ParseProperly(
            [Frozen] IEnumerable<Core.HistoryItem> coreHistoryItems,
            Mock<IHistoryItemReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IHistoryItemReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                HttpResponseMessage response =
                    await
                        server.HttpClient.GetAsync(
                            $"api/v2/historyItems?$expand=entity").ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<HistoryItem>(response).ConfigureAwait(false);

                // Assert.
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                reader.Verify(m => m.ReadByFiltersAsync(It.IsAny<Core.HistoryItemFilterCriteria>()), Times.Once);

                Assert.True(actual.Value.SequenceLike(coreHistoryItems, Likenesses.ForHistoryItem));
                Assert.True(actual.Value.All(x => x.Entity != null));
            }
        }

        [Theory(DisplayName = "Verify ReadByFilters query strings.")]
        [AutofixtureCustomizations.InlineTypeCorrections("&$filter=entity/id eq '{0}' and entity/trackingDetails/updatedOn ge {1} and entity/trackingDetails/updatedOn le {2}", true, true, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("&$filter=entity/trackingDetails/updatedOn ge {1} and entity/trackingDetails/updatedOn le {2}", false, true, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("&$filter=entity/id eq '{0}' and entity/trackingDetails/updatedOn ge {1}", true, true, false)]
        [AutofixtureCustomizations.InlineTypeCorrections("&$filter=entity/id eq '{0}'", true, false, false)]
        [AutofixtureCustomizations.InlineTypeCorrections("&$filter=entity/trackingDetails/updatedOn ge {1}", false, true, false)]
        [AutofixtureCustomizations.InlineTypeCorrections("&$filter=entity/trackingDetails/updatedOn le {2}", false, false, true)]
        [AutofixtureCustomizations.InlineTypeCorrections("", false, false, false)]
        public async Task When_ReadByFilters_Then_MapPropertiesCorrectly(
            string filterString,
            bool hasEntityIdFilter,
            bool hasEntityUpdatedAfterFilter,
            bool hasEntityUpdatedBeforeFilter,
            Guid entityId,
            Mock<IHistoryItemReader> reader)
        {
            Action<ContainerBuilder> registration = cb =>
            {
                cb.RegisterInstance(reader.Object).As<IHistoryItemReader>();
            };

            var serverContainer = TestServer.CreateDependencies(registration);

            using (var server = TestServer.Create(serverContainer))
            {
                var startTime = "2018-01-31T00:00:00.0000000-08:00";
                var endTime = "2018-02-01T00:00:00.0000000-08:00";

                var request = $"api/v2/historyItems?$expand=entity" + string.Format(filterString, entityId, startTime, endTime);

                HttpResponseMessage response = await server.HttpClient.GetAsync(request).ConfigureAwait(true);

                var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                // Act.
                var actual = await TestHelper.DeserializePagingResponse<HistoryItem>(response).ConfigureAwait(false);

                // Assert.
                Assert.True(response.StatusCode == HttpStatusCode.OK, jsonString);

                Action<Core.HistoryItemFilterCriteria> verify = filter =>
                {
                    if (hasEntityIdFilter)
                    {
                        Assert.Equal(entityId, filter.EntityId.Value);
                    }

                    if (hasEntityUpdatedAfterFilter)
                    {
                        Assert.Equal(DateTimeOffset.Parse(startTime), filter.EntityUpdatedAfter.Value);
                    }
                    
                    if (hasEntityUpdatedBeforeFilter)
                    {
                        Assert.Equal(DateTimeOffset.Parse(endTime), filter.EntityUpdatedBefore.Value);
                    }
                };

                reader.Verify(m => m.ReadByFiltersAsync(Is.Value(verify)), Times.Once);
            }
        }
    }
}