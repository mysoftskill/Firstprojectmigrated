namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.DataManagement.DataAccess.Reader;
    using Microsoft.PrivacyServices.DataManagement.Models.Exceptions;
    using Microsoft.PrivacyServices.DataManagement.Models.Filters;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class NamedEntityWriterTest
    {
        [Theory(DisplayName = "When CreateAsync is called with name not set, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithoutName_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.Name = string.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("name", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with name that is too long, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledLongName_Then_Fail(
            DataOwner dataOwner,
            IFixture fixture,
            DataOwnerWriter writer)
        {
            dataOwner.Name = new string(fixture.CreateMany<char>(129).ToArray());

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("name", exn.ParamName);
            Assert.Equal(dataOwner.Name, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with name that has invalid characters, then fail.")]
        [DataOwnerWriterTest.InlineValidData("(")]
        [DataOwnerWriterTest.InlineValidData(")")]
        [DataOwnerWriterTest.InlineValidData("#")]
        [DataOwnerWriterTest.InlineValidData("\\")]
        [DataOwnerWriterTest.InlineValidData("/")]
        [DataOwnerWriterTest.InlineValidData("Ā")]
        public async Task When_CreateAsyncCalledInvalidName_Then_Fail(
            string name,
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.Name = name;

            var exn = await Assert.ThrowsAsync<InvalidCharacterException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("name", exn.ParamName);
            Assert.Equal(dataOwner.Name, exn.Value);
        }

        [Theory(DisplayName = "When CreateAsync is called with name that already exists, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledExistingName_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> entityReader,
            FilterResult<DataOwner> existingOwners,
            DataOwner dataOwner,
            IFixture fixture,
            DataOwnerWriter writer)
        {
            existingOwners.Values = fixture.CreateMany<DataOwner>();

            Action<DataOwnerFilterCriteria> filter = f =>
            {
                var expected = new DataOwnerFilterCriteria { Name = new StringFilter(dataOwner.Name) };
                f
                .Likeness()
                .With(m => m.EntityType).EqualsWhen((src, dest) => src.EntityType.LikenessShouldEqual(dest.EntityType))
                .With(m => m.Name).EqualsWhen((src, dest) => src.Name.LikenessShouldEqual(dest.Name))
                .ShouldEqual(expected);
            };

            entityReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filter), ExpandOptions.None)).ReturnsAsync(existingOwners);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.AlreadyExists, exn.ConflictType);
            Assert.Equal("name", exn.Target);
            Assert.Equal(dataOwner.Name, exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync is called with name that already exists, then fail."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncCalledExistingName_Then_Fail(
            [Frozen] Mock<IDataOwnerReader> entityReader,
            FilterResult<DataOwner> existingOwners,
            DataOwner dataOwner,
            IFixture fixture,
            DataOwnerWriter writer)
        {
            existingOwners.Values = fixture.CreateMany<DataOwner>();

            Action<DataOwnerFilterCriteria> filter = f =>
            {
                var expected = new DataOwnerFilterCriteria { Name = new StringFilter(dataOwner.Name) };
                f
                .Likeness()
                .With(m => m.EntityType).EqualsWhen((src, dest) => src.EntityType.LikenessShouldEqual(dest.EntityType))
                .With(m => m.Name).EqualsWhen((src, dest) => src.Name.LikenessShouldEqual(dest.Name))
                .ShouldEqual(expected);
            };

            entityReader.Setup(m => m.ReadByFiltersAsync(Is.Value(filter), ExpandOptions.None)).ReturnsAsync(existingOwners);

            var exn = await Assert.ThrowsAsync<ConflictException>(() => writer.UpdateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal(ConflictType.AlreadyExists, exn.ConflictType);
            Assert.Equal("name", exn.Target);
            Assert.Equal(dataOwner.Name, exn.Value);
        }

        [Theory(DisplayName = "When UpdateAsync without changing the name, then skip name unique checking."), DataOwnerWriterTest.ValidData(WriteAction.Update)]
        public async Task When_UpdateAsyncNotChangingTheName_Then_SkipNameUniqueChecking(
            [Frozen] Mock<IDataOwnerReader> entityReader,
            DataOwner dataOwner,
            [Frozen] DataOwner storageDataOwner,
            DataOwnerWriter writer,
            string name,
            TrackingDetails trackingDetails)
        {
            storageDataOwner.TrackingDetails = trackingDetails;

            dataOwner.Name = name;
            storageDataOwner.Name = name;

            await writer.UpdateAsync(dataOwner).ConfigureAwait(false);

            entityReader.Verify(m => m.ReadByFiltersAsync(It.IsAny<IFilterCriteria<DataOwner>>(), ExpandOptions.None), Times.Never);
        }

        [Theory(DisplayName = "When CreateAsync is called with description not set, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledWithoutDescription_Then_Fail(
            DataOwner dataOwner,
            DataOwnerWriter writer)
        {
            dataOwner.Description = string.Empty;

            var exn = await Assert.ThrowsAsync<MissingPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("description", exn.ParamName);
        }

        [Theory(DisplayName = "When CreateAsync is called with description that is too long, then fail."), DataOwnerWriterTest.ValidData]
        public async Task When_CreateAsyncCalledLongDescription_Then_Fail(
            DataOwner dataOwner,
            IFixture fixture,
            DataOwnerWriter writer)
        {
            dataOwner.Description = new string(fixture.CreateMany<char>(1025).ToArray());

            var exn = await Assert.ThrowsAsync<InvalidPropertyException>(() => writer.CreateAsync(dataOwner)).ConfigureAwait(false);

            Assert.Equal("description", exn.ParamName);
            Assert.Equal(dataOwner.Description, exn.Value);
        }
    }
}