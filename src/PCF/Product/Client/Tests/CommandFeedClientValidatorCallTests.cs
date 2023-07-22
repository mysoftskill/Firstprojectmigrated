using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Contracts.Subjects;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.Policy;
    using Moq;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;

    [TestClass]
    public class CommandFeedClientValidatorCallTests
    {
        [TestMethod]
        public async Task ClientSetsAllCommandClaims()
        {
            const string verifier = "verifier1";

            CommandFeedClient client = this.GetCommandFeedClient(verifier, CommandFeedEndpointConfiguration.Production);

            MockValidationService validationService = new MockValidationService();
            validationService.Verifier = verifier;
            client.ValidationService = validationService;

            var result = await client.GetCommandsAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(validationService.VerifyEnsureValidAsyncCallSucccessfully);
        }

        private CommandFeedClient GetCommandFeedClient(string verifier, CommandFeedEndpointConfiguration configuration)
        {
            GetCommandsResponse commandResponse = this.GetSampleCommandsResponse(verifier);
            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(commandResponse)) };

            var authClientMock = new Mock<IAuthClient>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpClientMock = new Mock<IHttpClient>();

            authClientMock.Setup(authClient => authClient.GetAccessTokenAsync()).ReturnsAsync("token");
            authClientMock.SetupGet(authClient => authClient.Scheme).Returns("Bearer");

            httpClientFactoryMock.Setup(factory => factory.CreateHttpClient(null)).Returns(httpClientMock.Object);
            httpClientMock.Setup(httpClient => httpClient.SendAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(httpResponse);

            return new CommandFeedClient(
                Guid.NewGuid(),
                authClientMock.Object,
                new ConsoleCommandFeedLogger(),
                factory: httpClientFactoryMock.Object,
                endpointConfiguration: configuration);
        }

        private GetCommandsResponse GetSampleCommandsResponse(string verifier)
        {
            return new GetCommandsResponse()
            {
                new ExportCommand(
                    "commandId1",
                    "assetGroup1",
                    "assetGroupQ1",
                    verifier,
                    "cv1",
                    "lr1",
                    DateTime.UtcNow.AddMinutes(5),
                    DateTime.UtcNow,
                    new MsaSubject
                    {
                        Puid = 12345,
                        Anid = "12345",
                        Cid = 12345,
                        Opid = "12345",
                        Xuid = "12345",
                    },
                    "state",
                    new List<DataTypeId>  { Policies.Current.DataTypes.Ids.BrowsingHistory },
                    new Uri("https://mytest.com"),
                    null,
                    Policies.Current.CloudInstances.Ids.Public.Value)
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = true
                },
                new DeleteCommand(
                    "commandId2",
                    "assetGroup1",
                    "assetGroupQ1",
                    verifier,
                    "cv1",
                    "lr1",
                    DateTime.UtcNow.AddMinutes(5),
                    DateTime.UtcNow,
                    new MsaSubject
                    {
                        Puid = 12345,
                        Anid = "12345",
                        Cid = 12345,
                        Opid = "12345",
                        Xuid = "12345",
                    },
                    "state",
                    null,
                    Policies.Current.DataTypes.Ids.BrowsingHistory,
                    null,
                    null)
                {
                    ControllerApplicable = true,
                    ProcessorApplicable = true
                }
            };
        }

        private class MockValidationService : IValidationService
        {
            private bool verifyEnsureValidAsyncCallSucccessfully = false;

            public bool VerifyEnsureValidAsyncCallSucccessfully
            {
                get { return verifyEnsureValidAsyncCallSucccessfully; }
            }

            public string Verifier { get; set; }

            public List<KeyDiscoveryConfiguration> SovereignCloudConfigurations { get; set; }

            public Task EnsureValidAsync(string verifier, CommandClaims commandClaims, CancellationToken cancellationToken)
            {
                verifyEnsureValidAsyncCallSucccessfully = true;

                if (verifier != Verifier)
                {
                    verifyEnsureValidAsyncCallSucccessfully = false;
                }

                Type type = typeof(CommandClaims);
                PropertyInfo[] commandClaimsProperties = type.GetProperties();

                foreach (PropertyInfo property in commandClaimsProperties)
                {
                    // make sure we set all the values in the CommandClaims
                    // this is to ensure we don't forget setting a claim
                    object value = property.GetValue(commandClaims);
                    if (value == null)
                    {
                        bool acceptableNullCases =
                            (commandClaims.Operation != ValidOperation.Delete && property.Name == "DataType") ||
                            (commandClaims.Operation != ValidOperation.Export && property.Name == "AzureBlobContainerTargetUri");

                        if (acceptableNullCases)
                        {
                            continue;
                        }
                        
                        verifyEnsureValidAsyncCallSucccessfully = false;
                    }

                    if (value is bool && (bool)value != true)
                    {
                        // this is to ensure that we set the bool values also
                        // and for this test we always test with setting the bool values to true
                        verifyEnsureValidAsyncCallSucccessfully = false;
                    }
                }

                return Task.CompletedTask;
            }
        }
    }
}
