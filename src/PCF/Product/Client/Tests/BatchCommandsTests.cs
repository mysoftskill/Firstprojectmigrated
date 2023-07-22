namespace Microsoft.PrivacyServices.CommandFeed.Client.Test
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    [TestClass]
    public class BatchCommandsTests
    {
        private CommandFeedEndpointConfiguration endpointConfiguration = CommandFeedEndpointConfiguration.PreproductionAME;

        [TestMethod]
        public async Task GetBatchDeleteCommand_NoResponse()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(String.Empty) });
            var result = await client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None);
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public async Task GetBatchDeleteCommand_EmptyCommand()
        {
            var response = new GetBatchCommandResponse
            {
                CommandPage = "",
                CompletionLink = "test",
            };

            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") });
            var result = await client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.IsTrue(string.IsNullOrEmpty(result.CommandPage));
            Assert.AreEqual(response.CompletionLink, result.CompletionLink);
        }

        [TestMethod]
        public async Task GetBatchDeleteCommand_NotFound()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(String.Empty) });
            var result = await client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetBatchDeleteCommand_TooEarly425()
        {
            var highWaterMarkString = "01/01/2001 01:01:01";
            var endString = "01/01/2001 01:00:01";
            var testResponse = $"HighWaterMark Error: endTime ({endString}) " +
                       $"is greater than highWaterMark ({highWaterMarkString}). " +
                       $"Please try again later!";
            var expectedApiResponse = $"CommandFeed.GetBatchCommandWithUrlAsync returned 425, Body = " +
                       $"HighWaterMark Error: endTime ({endString}) " +
                       $"is greater than highWaterMark ({highWaterMarkString}). " +
                       $"Please try again later!";
       
            CommandFeedClient client = this.GetClient(new HttpResponseMessage() { Content = new StringContent(testResponse), StatusCode = (HttpStatusCode)425 });
            HttpRequestException httpRequestException = new HttpRequestException(expectedApiResponse);
            
            //Causes an exception
            var response = await Assert.ThrowsExceptionAsync<HttpRequestException>(() => client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None));
            
            //Catch and Test exception response
            try
            {
                var result = await client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(httpRequestException.Message, ex.Message.ToString());
            }
        }

        [TestMethod]
        public async Task GetBatchDeleteCommand_WithTestData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var streamReader = new StreamReader(assembly.GetManifestResourceStream("Microsoft.PrivacyServices.CommandFeed.Client.Test.TestData.DeleteCommand.json")))
            {
                var commandPage = JsonConvert.DeserializeObject<CommandPage>(streamReader.ReadToEnd());
                var response = new GetBatchCommandResponse()
                {
                    CommandPage = JsonConvert.SerializeObject(commandPage),
                    CompletionLink = "test"
                };

                CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") });
                var result = await client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None);
                var resultCommandPage = JsonConvert.DeserializeObject<CommandPage>(result.CommandPage);

                Assert.IsNotNull(result);
                Assert.AreEqual(5, resultCommandPage.Commands.Count());
                Assert.AreEqual(response.CompletionLink, result.CompletionLink);
                foreach (var command in resultCommandPage.Commands)
                {
                    Assert.AreEqual(resultCommandPage.Operation, command.Operation);
                    Assert.AreEqual(resultCommandPage.CommandTypeId, command.CommandTypeId);
                    Assert.AreEqual("Delete", command.OperationType);
                    Assert.AreEqual(resultCommandPage.CommandProperties.ToObject<IList<CommandProperty>>().Count, command.CommandProperties.Count);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(CommandPageValidationException))]
        public async Task GetBatchDeleteCommand_BadVerifier()
        {
            string jsonBlob = @"
{
  'OperationType': 'Delete',
  'Operation': 'AccountClose',
  'CommandTypeId': 3,
  'PageId': 1,
  'CommandProperties': [{ 'Property':'DataType','Values':['All']},{ 'Property':'SubjectType','Values':['AADUser-Public']}],
  'Commands': [
    {
      'commandId': '7be023d8-19dd-4589-9513-230b9564c486',
      'timeRangePredicate': null,
      'verifier': 'bad',
      'rowPredicate': null,
      'timestamp': '2022-03-16 13:10:17',
      'subject': {
        'aadObjectId': '5674b2a1-61ac-4c05-86c7-2b7d4843851c',
        'aadResourceTenantId': 'a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d',
        'aadPuid': '1154012207270968108',
        'aadTenantIdType': 'Home',
        'aadHomeTenantId': 'a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d'
      }
    }
  ]
}";

            var commandPage = JsonConvert.DeserializeObject<CommandPage>(jsonBlob);
            var response = new GetBatchCommandResponse()
            {
                CommandPage = JsonConvert.SerializeObject(commandPage),
            };
            
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") });
            var _ = await client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None);
        }

        [TestMethod]
        public async Task GetNextBatchDeleteCommand()
        {
            var response = new GetBatchCommandResponse
            {
                CommandPage = null,
                CompletionLink = "test",
            };

            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") });
            var result = await client.GetNextBatchDeleteCommandAsync("https://pcfdns:443/someurl", CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.IsTrue(string.IsNullOrEmpty(result.CommandPage));
            Assert.AreEqual(response.CompletionLink, result.CompletionLink);
        }

        [TestMethod]
        public async Task GetAssetGroupDetail_NotFound()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(string.Empty) });
            var result = await client.GetAssetGroupDetailsAsync(Guid.NewGuid(),new Version("1.0"), CancellationToken.None);

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetAssetGroupDetail_Succeeded()
        {
            var response = new AssetGroupDetailsResponse
            {
                AssetPage = string.Empty,
                NextLink = "test",
            };

            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") });
            var result = await client.GetAssetGroupDetailsAsync(Guid.NewGuid(), new Version("1.0"), CancellationToken.None);

            Assert.IsNotNull(result);
            Assert.IsTrue(string.IsNullOrEmpty(result.AssetPage));
            Assert.AreEqual(response.NextLink, result.NextLink);
        }

        [TestMethod]
        public async Task CompleteBatchCommandAsync_Succeeded()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(String.Empty) });
            await client.CompleteBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, "token", CancellationToken.None);
        }

        [TestMethod]
        public async Task GetWorkitemAsync_NotFound()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(String.Empty) });
            var result = await client.GetWorkitemAsync();
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetWorkitemAsync_WithTestData()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var streamReader = new StreamReader(assembly.GetManifestResourceStream("Microsoft.PrivacyServices.CommandFeed.Client.Test.TestData.DeleteCommand.json")))
            {
                var commandPage = JsonConvert.DeserializeObject<CommandPage>(streamReader.ReadToEnd());
                var response = new Workitem()
                {
                    WorkitemId = "test",
                    CommandPage = JsonConvert.SerializeObject(commandPage),
                    CompletionLink = "test"
                };

                CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") });
                var result = await client.GetWorkitemAsync();
                var resultCommandPage = JsonConvert.DeserializeObject<CommandPage>(result.CommandPage);

                Assert.IsNotNull(result);
                Assert.AreEqual(response.WorkitemId, result.WorkitemId);
                Assert.AreEqual(5, resultCommandPage.Commands.Count());
                Assert.AreEqual(response.CompletionLink, result.CompletionLink);
                foreach (var command in resultCommandPage.Commands)
                {
                    Assert.AreEqual(resultCommandPage.Operation, command.Operation);
                    Assert.AreEqual(resultCommandPage.CommandTypeId, command.CommandTypeId);
                    Assert.AreEqual("Delete", command.OperationType);
                    Assert.AreEqual(resultCommandPage.CommandProperties.ToObject<IList<CommandProperty>>().Count, command.CommandProperties.Count);
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task QueryWorkitemAsync_NotFound()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent(String.Empty) });
            var result = await client.QueryWorkitemAsync("abc", CancellationToken.None);
        }

        [TestMethod]
        public async Task QueryWorkitemAsync_OK()
        {
            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{'WorkitemId':'123'}") });
            var result = await client.QueryWorkitemAsync("abc", CancellationToken.None);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        [DataRow("AADUser-Public", CloudInstance.Public)]
        [DataRow("AADUser-US.Azure.Fairfax", CloudInstance.AzureFairfax)]
        [DataRow("AADUser-CN.Azure.Mooncake", CloudInstance.AzureMoonCake)]
        [DataRow("AADUser2-Public", CloudInstance.Public)]
        [DataRow("AADUser2-US.Azure.Fairfax", CloudInstance.AzureFairfax)]
        [DataRow("AADUser2-CN.Azure.Mooncake", CloudInstance.AzureMoonCake)]
        [DataRow("MSAUser", CloudInstance.Public)]
        public async Task ParseCloudInstance(string subjectType, string expectedCloudInstance)
        {
            string jsonBlob = @"
{
  'OperationType': 'Delete',
  'Operation': 'AccountClose',
  'CommandTypeId': 3,
  'PageId': 1,
  'CommandProperties': [{ 'Property':'DataType','Values':['All']},{ 'Property':'SubjectType','Values':['AADUser-Public']}],
  'Commands': [
    {
      'commandId': '7be023d8-19dd-4589-9513-230b9564c486',
      'timeRangePredicate': null,
      'verifier': '',
      'rowPredicate': null,
      'timestamp': '2022-03-16 13:10:17',
      'subject': {
        'aadObjectId': '5674b2a1-61ac-4c05-86c7-2b7d4843851c',
        'aadResourceTenantId': 'a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d',
        'aadPuid': '1154012207270968108',
        'aadTenantIdType': 'Home',
        'aadHomeTenantId': 'a80aafff-2a2a-4099-8fd4-bcbe5bcaff6d'
      }
    }
  ]
}";

            // Patch up test data with desired values
            JObject rawObj = JObject.Parse(jsonBlob);
            JArray cmdProperties = rawObj["CommandProperties"] as JArray;
            JArray subjectTypes = cmdProperties[1]["Values"] as JArray;
            subjectTypes.Clear();
            subjectTypes.Add(subjectType);

            JArray commands = rawObj["Commands"] as JArray;
            commands[0]["verifier"] = CreateDummyJwtToken();

            var commandPage = JsonConvert.DeserializeObject<CommandPage>(rawObj.ToString());
            var response = new GetBatchCommandResponse()
            {
                CommandPage = JsonConvert.SerializeObject(commandPage),
            };

            var validationServiceMock = new Mock<IValidationService>(MockBehavior.Strict);
            validationServiceMock.Setup(service => service.EnsureValidAsync(It.IsAny<string>(), It.IsAny<CommandClaims>(), It.IsAny<CancellationToken>()))
                .Callback<string, CommandClaims, CancellationToken>(
                    (verifier, commandClaims, cancellationToken) =>
                    {
                        Assert.AreEqual(commandClaims.CloudInstance, expectedCloudInstance);
                    })
                .Returns(Task.FromResult(true));

            CommandFeedClient client = this.GetClient(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json") });
            client.ValidationService = validationServiceMock.Object;

            var _ = await client.GetBatchDeleteCommandAsync(Guid.NewGuid(), DateTimeOffset.UtcNow - TimeSpan.FromHours(10), DateTimeOffset.UtcNow, false, CancellationToken.None);
        }

        private CommandFeedClient GetClient(HttpResponseMessage httpResponse = null)
        {
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
                endpointConfiguration: CommandFeedEndpointConfiguration.PreproductionAME);
        }

        private string CreateDummyJwtToken()
        {
            // Define private dummy key. There's length requirement for this key.
            string key = "1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";

            // Create Security key and credential using private key above.
            var securityKey = new IdentityModel.Tokens.SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new IdentityModel.Tokens.SigningCredentials(securityKey, "http://www.w3.org/2001/04/xmldsig-more#hmac-sha256");
            var header = new JwtHeader(credentials);

            // Create payload that only contains the expiration time which is valid for 100 days.
            var payload = new JwtPayload
            {
                { "refresh_token_expiry", DateTimeOffset.Now.AddDays(100).ToUnixTimeSeconds().ToString()}
            };

            var token = new JwtSecurityToken(header, payload);
            var handler = new JwtSecurityTokenHandler();

            return handler.WriteToken(token);
        }
    }
}
