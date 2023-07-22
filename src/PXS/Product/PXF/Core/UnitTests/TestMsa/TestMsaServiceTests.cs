// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyCore.UnitTests.TestMsa
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.Membership.MemberServices.Privacy.Core.AQS;
    using Microsoft.Membership.MemberServices.Privacy.Core.TestMsa;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts;
    using Microsoft.Membership.MemberServices.PrivacyAdapters;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.Models;
    using Microsoft.Membership.MemberServices.PrivacyAdapters.XboxAccounts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    [TestClass]
    public class TestMsaServiceTests : CoreServiceTestBase
    {
        private Mock<IAccountDeleteWriter> mockDeleteWriter;

        private Mock<IXboxAccountsAdapter> mockXboxAccountsAdapter;

        [TestMethod]
        public async Task PostTestMsaCloseAsync_Returns_Error_When_WriteDeleteAsync_Fails()
        {
            // Arrange
            string expectedErrorMsg = "any string";
            this.mockDeleteWriter.Setup(_ => _.WriteDeleteAsync(It.IsAny<AccountDeleteInformation>(), It.IsAny<string>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<AccountDeleteInformation>
                        {
                            Error = new AdapterError(AdapterErrorCode.Unknown, expectedErrorMsg, 0)
                        }));

            var sut = new TestMsaService(this.mockDeleteWriter.Object, this.mockXboxAccountsAdapter.Object);

            // Act
            ServiceResponse<Guid> actual = await sut.PostTestMsaCloseAsync(this.TestRequestContext).ConfigureAwait(false);

            // Assert
            Assert.AreEqual(expectedErrorMsg, actual.Error.Message, expectedErrorMsg);
        }

        [TestMethod]
        public async Task PostTestMsaCloseAsync_Returns_Response_With_RequestId_When_WriteDeleteAsync_Succeeds()
        {
            // Arrange
            this.mockDeleteWriter.Setup(_ => _.WriteDeleteAsync(It.IsAny<AccountDeleteInformation>(), It.IsAny<string>()))
                .Returns(
                    Task.FromResult(
                        new AdapterResponse<AccountDeleteInformation>
                        {
                            Result = new AccountDeleteInformation()
                        }));

            var sut = new TestMsaService(this.mockDeleteWriter.Object, this.mockXboxAccountsAdapter.Object);

            // Act
            ServiceResponse<Guid> actual = await sut.PostTestMsaCloseAsync(this.TestRequestContext).ConfigureAwait(false);

            // Assert
            Assert.IsFalse(string.IsNullOrWhiteSpace(actual.Result.ToString()), "Invalid requestId.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task PostTestMsaCloseAsync_Throws_When_RequestContext_Null()
        {
            // Arrange
            var sut = new TestMsaService(new Mock<IAccountDeleteWriter>().Object, this.mockXboxAccountsAdapter.Object);

            // Act
            await sut.PostTestMsaCloseAsync(null).ConfigureAwait(false);
        }

        [TestInitialize]
        public void TestInit()
        {
            this.mockDeleteWriter = new Mock<IAccountDeleteWriter>();
            this.mockXboxAccountsAdapter = new Mock<IXboxAccountsAdapter>();
            this.mockXboxAccountsAdapter.Setup(a => a.GetXuidAsync(It.IsAny<PxfRequestContext>())).Returns(Task.FromResult(new AdapterResponse<string> { Result = "xuid" }));
        }
    }
}
