
namespace Microsoft.PrivacyServices.CommandFeed.Client
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.PrivacyServices.CommandFeed.Client.CommandFeedContracts;
    using Microsoft.PrivacyServices.CommandFeed.Client.Commands.V2;
    using Microsoft.PrivacyServices.CommandFeed.Client.Helpers;
    using Microsoft.PrivacyServices.CommandFeed.Validator;
    using Microsoft.PrivacyServices.CommandFeed.Validator.Configuration;
    using Microsoft.PrivacyServices.CommandFeed.Validator.TokenValidation;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Contains the default implementation of ICommandFeedClient.
    /// </summary>
    public sealed partial class CommandFeedClient : ICommandFeedClient
    {
        private const string OperationTypeDelete = "delete";
        private const string OperationTypeExport = "export";

        /// <summary>
        /// Gets the URI of the GetBatchCommand Api endpoint.
        /// </summary>
        public Uri GetBatchCommandUri { get; }

        /// <summary>
        /// Gets the URI of the GetAllBatchCommand Api endpoint.
        /// </summary>
        public Uri GetAllBatchCommandUri { get; }

        /// <summary>
        /// Gets the URI of the CompleteBatchCommand Api endpoint.
        /// </summary>
        public Uri CompleteBatchCommandUri { get; }

        /// <summary>
        /// Gets the URI of the GetAssetGroupDetails Api endpoint.
        /// </summary>
        public Uri GetAssetGroupDetailsUri { get; }

        /// <summary>
        /// Gets the URI of the GetResourceUriMap Api endpoint.
        /// </summary>
        public Uri GetResourceUriMapUri { get; }

        /// <summary>
        /// Gets the URI of the GetCommandConfiguration Api endpoint.
        /// </summary>
        public Uri GetCommandConfigurationUri { get; }

        /// <summary>
        /// Gets the URI of the GetWorkitemAsync Api endpoint.
        /// </summary>
        public Uri GetWorkitemUri { get; }

        /// <summary>
        /// Gets the URI of the QueryWorkitemAsync Api endpoint.
        /// </summary>
        public Uri QueryWorkitemUri { get; }

        /// <summary>
        /// Gets the URI of the UpdateWorkitemAsync Api endpoint.
        /// </summary>
        public Uri UpdateWorkitemUri { get; }

        /// <inheritdoc />
        public async Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken)
        {
            return await GetBatchCommandAsync(assetGroupId, startTime, endTime, OperationTypeDelete, returnOnlyTest, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GetBatchCommandResponse> GetBatchDeleteCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken)
        {
            return await GetAllBatchCommandAsync(startTime, endTime, OperationTypeDelete, maxResult, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GetBatchCommandResponse> GetNextBatchDeleteCommandAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return await GetNextBatchCommandAsync(nextPageUri, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GetBatchCommandResponse> GetBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, bool returnOnlyTest, CancellationToken cancellationToken)
        {
            return await GetBatchCommandAsync(assetGroupId, startTime, endTime, OperationTypeExport, returnOnlyTest, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GetBatchCommandResponse> GetBatchExportCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, int maxResult, CancellationToken cancellationToken)
        {
            return await GetAllBatchCommandAsync(startTime, endTime, OperationTypeExport, maxResult, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GetBatchCommandResponse> GetNextBatchExportCommandAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return await GetNextBatchCommandAsync(nextPageUri, cancellationToken);
        }

        /// <inheritdoc />
        public async Task CompleteBatchDeleteCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&assetGroupId={assetGroupId}&startTime={ToUnixTimeSeconds(startTime)}&endTime={ToUnixTimeSeconds(endTime)}&type={OperationTypeDelete}";
            var completeCommandUri = new Uri(this.CompleteBatchCommandUri + parameters);

            var request = new CommandCompleteRequest
            {
                Status = "Complete",
                CompletionToken = completionToken,
            };

            await SendBatchCompletionRequestAsync(completeCommandUri, request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task CompleteBatchDeleteCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, 
            string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&assetGroupId={assetGroupId}&startTime={ToUnixTimeSeconds(startTime)}&endTime={ToUnixTimeSeconds(endTime)}&type={OperationTypeDelete}";
            var completeCommandUri = new Uri(this.CompleteBatchCommandUri + parameters);

            var request = new CommandCompleteRequest
            {
                Status = "Complete",
                CompletionToken = completionToken,
                SucceededAssetUris = succeededAssetUris,
                FailedAssetUris = failedAssetUris
            };

            await SendBatchCompletionRequestAsync(completeCommandUri, request, cancellationToken);
        }

        /// <inheritdoc />
        public async Task CompleteBatchExportCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, 
            Uri stagingContainer, string stagingRootFolder, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&assetGroupId={assetGroupId}&startTime={ToUnixTimeSeconds(startTime)}&endTime={ToUnixTimeSeconds(endTime)}&type={OperationTypeExport}";
            var completeCommandUri = new Uri(this.CompleteBatchCommandUri + parameters);

            var request = new CommandCompleteRequest
            {
                Status = "Complete",
                StagingContainer = stagingContainer,
                StagingRootFolder = stagingRootFolder,
                CompletionToken = completionToken,
            };

            await SendBatchCompletionRequestAsync(completeCommandUri, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task CompleteBatchExportCommandWithAssetUrisAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string completionToken, 
            Uri stagingContainer, string stagingRootFolder, string[] succeededAssetUris, string[] failedAssetUris, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&assetGroupId={assetGroupId}&startTime={ToUnixTimeSeconds(startTime)}&endTime={ToUnixTimeSeconds(endTime)}&type={OperationTypeExport}";
            var completeCommandUri = new Uri(this.CompleteBatchCommandUri + parameters);

            var request = new CommandCompleteRequest
            {
                Status = "Complete",
                StagingContainer = stagingContainer,
                StagingRootFolder = stagingRootFolder,
                CompletionToken = completionToken,
                SucceededAssetUris = succeededAssetUris,
                FailedAssetUris = failedAssetUris
            };

            await SendBatchCompletionRequestAsync(completeCommandUri, request, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<AssetGroupDetailsResponse> GetAssetGroupDetailsAsync(Guid assetGroupId, Version version, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&assetGroupId={assetGroupId}&api-version={version}";

            var getAssetGroupDetailsUri = new Uri(this.GetAssetGroupDetailsUri, parameters);
            return await GetAssetGroupDetailsWithUrlAsync(getAssetGroupDetailsUri, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ResourceUriMapResponse> GetResourceUriMapAsync(Guid assetGroupId, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&assetGroupId={assetGroupId}";
            var getResourceUriMapUri = new Uri(this.GetResourceUriMapUri, parameters);
            return await GetResourceUriMapWithUrlAsync(getResourceUriMapUri, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<AssetGroupDetailsResponse> GetNextAssetGroupDetailsAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return await GetAssetGroupDetailsWithUrlAsync(new Uri($"{nextPageUri}"), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<ResourceUriMapResponse> GetNextResourceUriMapAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return await GetResourceUriMapWithUrlAsync(new Uri($"{nextPageUri}"), cancellationToken);
        }

        /// <inheritdoc />
        public async Task<string> GetCommandConfigurationAsync(CancellationToken cancellationToken)
        {
            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, this.GetCommandConfigurationUri);
            await this.AddCommonHeadersV2Async(getRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(getRequest, cancellationToken).ConfigureAwait(false);

            string responseBody = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.GetCommandConfiguration returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            return responseBody;
        }

        /// <inheritdoc />
        public async Task<Workitem> GetWorkitemAsync(Guid assetGroupId = default, int leaseDuration = 900, bool returnOnlyTest = false, CancellationToken cancellationToken = default)
        {
            var parameters = $"?agentId={agentId}&leaseDuration={leaseDuration}&returnOnlyTest={returnOnlyTest}";
            parameters += (assetGroupId != default) ? $"&assetGroupId={assetGroupId}" : string.Empty;
            var getWorkitemUri = new Uri(this.GetWorkitemUri, parameters);

            return await GetWorkitemWithUrlAsync(getWorkitemUri, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Workitem> QueryWorkitemAsync(string workitemId, CancellationToken cancellationToken)
        {
            var parameters = $"/{workitemId}?agentId={agentId}";
            var getWorkitemUri = new Uri(this.QueryWorkitemUri.ToString() + parameters);

            return await GetWorkitemWithUrlAsync(getWorkitemUri, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task UpdateWorkitemAsync(string workitemId, UpdateWorkitemRequest updateRequest, CancellationToken cancellationToken)
        {
            var parameters = $"/{workitemId}?agentId={agentId}";
            var updateWorkitemUri = new Uri(this.UpdateWorkitemUri.ToString() + parameters);

            HttpRequestMessage updateRequestMessage = new HttpRequestMessage(HttpMethod.Put, updateWorkitemUri);
            await this.AddCommonHeadersV2Async(updateRequestMessage).ConfigureAwait(false);

            updateRequestMessage.Content = new StringContent(
                JsonConvert.SerializeObject(updateRequest), Encoding.UTF8, "application/json");

            var response = await this.httpClient.SendAsync(updateRequestMessage, cancellationToken).ConfigureAwait(false);

            string responseBody = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.UpdateWorkitemAsync returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }
        }

        private async Task AddCommonHeadersV2Async(HttpRequestMessage request)
        {
            if (this.authClient is MicrosoftAccountAuthClient)
            {
                throw new InvalidOperationException("PCFV2 endpoint only supports AAD authentication");
            }

            var token = await this.authClient.GetAccessTokenAsync().ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue(this.authClient.Scheme, token);
            request.Headers.Add("x-client-version", this.clientVersion);
        }

        private async Task ValidateV2CommandAsync(PrivacyCommandV2 command, string cloudInstance, CancellationToken cancellationToken)
        {
            if (this.enforceValidation || !string.IsNullOrEmpty(command.Verifier))
            {
                await this.ValidationService.EnsureValidAsync(
                    command.Verifier,
                    new CommandClaims
                    {
                        CommandId = command.CommandId,
                        Subject = SubjectConverter.GetV1SubjectForValidation(command.Subject),
                        Operation = command.GetV1Operation(),
                        ControllerApplicable = command.ControllerApplicable,
                        ProcessorApplicable = command.ProcessorApplicable,
                        DataType = (command.GetV1Operation() == ValidOperation.Delete) ? command.GetV1DataType() : null,
                        CloudInstance = cloudInstance,
                        AzureBlobContainerTargetUri = command.AzureBlobContainerTargetUri,
                    },
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<GetBatchCommandResponse> GetBatchCommandAsync(Guid assetGroupId, DateTimeOffset startTime, DateTimeOffset endTime, string operationType, bool returnOnlyTest, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&assetGroupId={assetGroupId}&startTime={ToUnixTimeSeconds(startTime)}&endTime={ToUnixTimeSeconds(endTime)}&type={operationType}&returnOnlyTest={returnOnlyTest}";
            var getBatchCommandUri = new Uri(this.GetBatchCommandUri, parameters);
            return await GetBatchCommandWithUrlAsync(getBatchCommandUri, cancellationToken).ConfigureAwait(false);
        }

        private async Task<GetBatchCommandResponse> GetAllBatchCommandAsync(DateTimeOffset startTime, DateTimeOffset endTime, string operationType, int maxResult, CancellationToken cancellationToken)
        {
            var parameters = $"?agentId={agentId}&startTime={ToUnixTimeSeconds(startTime)}&endTime={ToUnixTimeSeconds(endTime)}&type={operationType}&maxResult={maxResult}";
            var getAllBatchCommandUri = new Uri(this.GetAllBatchCommandUri, parameters);
            return await GetBatchCommandWithUrlAsync(getAllBatchCommandUri, cancellationToken).ConfigureAwait(false);
        }

        private async Task<GetBatchCommandResponse> GetNextBatchCommandAsync(string nextPageUri, CancellationToken cancellationToken)
        {
            return await GetBatchCommandWithUrlAsync(new Uri($"{nextPageUri}"), cancellationToken).ConfigureAwait(false);
        }

        private async Task<GetBatchCommandResponse> GetBatchCommandWithUrlAsync(Uri getCommandPageUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, getCommandPageUri);
            await this.AddCommonHeadersV2Async(getRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(getRequest, cancellationToken).ConfigureAwait(false);

            if (string.Equals("multipart/mixed", response.Content?.Headers?.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase))
            {
                return await ProcessMultipleResponse(response, cancellationToken);
            }
            else
            {
                return await ProcessSingleResponse(response, cancellationToken);
            }
        }

        private async Task<Workitem> GetWorkitemWithUrlAsync(Uri getCommandPageUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, getCommandPageUri);
            await this.AddCommonHeadersV2Async(getRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(getRequest, cancellationToken).ConfigureAwait(false);

            string responseBody = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (string.IsNullOrEmpty(responseBody))
                {
                    return null;
                }
            }
            else if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.GetWorkitemWithUrlAsync returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var parsedResponse = JsonConvert.DeserializeObject<Workitem>(responseBody);
            if (parsedResponse == null)
            {
                throw CreateException($"CommandFeed.GetWorkitemWithUrlAsync returned empty response", response);
            }

            var commandPage = await ValidateCommandPageAsync(parsedResponse.CommandPage, throwOnError: true, cancellationToken).ConfigureAwait(false);
            parsedResponse.CommandPage = (commandPage != null) ? JsonConvert.SerializeObject(commandPage) : null;

            return parsedResponse;
        }

        private async Task<GetBatchCommandResponse> ProcessSingleResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            string responseBody = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (string.IsNullOrEmpty(responseBody))
                {
                    return null;
                }
            }
            else if (response.StatusCode == (HttpStatusCode)425)
            {
                throw CreateException($"CommandFeed.GetBatchCommandWithUrlAsync returned 425, Body = {responseBody}", response);
            }
            else if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.GetBatchCommandWithUrlAsync returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var parsedResponse = JsonConvert.DeserializeObject<GetBatchCommandResponse>(responseBody);
            if (parsedResponse == null)
            {
                throw CreateException($"CommandFeed.GetBatchCommandWithUrlAsync returned empty response", response);
            }

            try
            {
                var commandPage = await ValidateCommandPageAsync(parsedResponse.CommandPage, throwOnError: true, cancellationToken).ConfigureAwait(false);
                parsedResponse.CommandPage = (commandPage != null) ? JsonConvert.SerializeObject(commandPage) : null;
            }
            catch (CommandPageValidationException ex)
            {
                var request = new CommandCompleteRequest
                {
                    Status = "ValidationFailure",
                    CommandId = ex.CommandId,
                };

                var completeCommandUri = new Uri(this.CompleteBatchCommandUri + response.RequestMessage?.RequestUri?.Query);

                await SendBatchCompletionRequestAsync(completeCommandUri, request, cancellationToken);

                throw;
            }

            return parsedResponse;
        }

        private async Task<GetBatchCommandResponse> ProcessMultipleResponse(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.GetBatchCommandWithUrlAsync returned unexpected status code: {response.StatusCode}", response);
            }

            var multiParts = await response.Content.ReadAsMultipartAsync(cancellationToken);

            GetBatchCommandResponse parsedResponse = null;
            var allCommands = new List<PrivacyCommandV2>();
            foreach (var content in multiParts.Contents)
            {
                var jsonString = await content.ReadAsStringAsync();
                if (parsedResponse == null)
                {
                    parsedResponse = JsonConvert.DeserializeObject<GetBatchCommandResponse>(jsonString);
                }
                else
                {
                    var commandPage = await ValidateCommandPageAsync(jsonString, throwOnError: false, cancellationToken).ConfigureAwait(false);
                    if (commandPage != null)
                    {
                        allCommands.AddRange(commandPage.Commands);
                    }
                }
            }

            var newCommandPage = new CommandPage()
            {
                Commands = allCommands
            };

            parsedResponse.CommandPage = JsonConvert.SerializeObject(newCommandPage);
            return parsedResponse;
        }

        /// <summary>
        /// Parse the input command page json string, validate them, repack in PrivacyCommandV2 format.
        /// </summary>
        /// <param name="commandPageString">The content of the command page</param>
        /// <param name="throwOnError">If the method should throw when validation failed</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>The transformed CommandPage object.</returns>
        /// <exception cref="CommandPageValidationException"></exception>
        private async Task<CommandPage> ValidateCommandPageAsync(string commandPageString, bool throwOnError, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(commandPageString))
            {
                return null;
            }

            var commandPage = JsonConvert.DeserializeObject<CommandPage>(commandPageString);

            var commandCloudInstance = GetCommandCloudInstance(commandPage);

            List<PrivacyCommandV2> responsePrivacyCommands = new List<PrivacyCommandV2>();
            foreach (var privacyCommand in commandPage.Commands)
            {
                try
                {
                    privacyCommand.Operation = commandPage.Operation;
                    privacyCommand.CommandProperties = commandPage.CommandProperties.ToObject<IList<CommandProperty>>();
                    privacyCommand.CommandTypeId = commandPage.CommandTypeId;
                    privacyCommand.OperationType = commandPage.OperationType;

                    ExtractPaCaFromVerifier(privacyCommand);

                    await this.ValidateV2CommandAsync(privacyCommand, commandCloudInstance, cancellationToken).ConfigureAwait(false);
                    // strip the verifier to prevent accidental storage by the agents
                    privacyCommand.Verifier = string.Empty;
                    responsePrivacyCommands.Add(privacyCommand);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is InvalidPrivacyCommandException || ex is KeyDiscoveryException)
                {
                    this.logger.CommandValidationException(string.Empty, privacyCommand?.CommandId, ex);

                    if (throwOnError)
                    {
                        throw new CommandPageValidationException(privacyCommand?.CommandId, ex);
                    }
                }
            }

            commandPage.Commands = responsePrivacyCommands;
            return commandPage;
        }

        private async Task SendBatchCompletionRequestAsync(Uri completionUri, CommandCompleteRequest request, CancellationToken cancellationToken)
        {
            HttpRequestMessage completeRequest = new HttpRequestMessage(HttpMethod.Put, completionUri);
            await this.AddCommonHeadersV2Async(completeRequest).ConfigureAwait(false);

            completeRequest.Content = new StringContent(
                JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var response = await this.httpClient.SendAsync(completeRequest, cancellationToken).ConfigureAwait(false);
            var responseBody = string.Empty;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.Content != null)
                {
                    responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                throw CreateException($"CommandFeed.SendBatchCompletionRequestAsync returned unexpected status code: {response.StatusCode}, message: \"{responseBody}\".", response);
            }
        }

        private async Task<AssetGroupDetailsResponse> GetAssetGroupDetailsWithUrlAsync(Uri getAssetGroupDetailsUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, getAssetGroupDetailsUri);
            await this.AddCommonHeadersV2Async(getRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(getRequest, cancellationToken).ConfigureAwait(false);

            string responseBody = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.GetAssetGroupDetailAsync returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var parsedResponse = JsonConvert.DeserializeObject<AssetGroupDetailsResponse>(responseBody);
            if (parsedResponse == null)
            {
                throw CreateException($"CommandFeed.GetAssetGroupDetailAsync returned empty response", response);
            }

            return parsedResponse;
        }

        private async Task<ResourceUriMapResponse> GetResourceUriMapWithUrlAsync(Uri getResourceUriMapUri, CancellationToken cancellationToken)
        {
            HttpRequestMessage getRequest = new HttpRequestMessage(HttpMethod.Get, getResourceUriMapUri);
            await this.AddCommonHeadersV2Async(getRequest).ConfigureAwait(false);

            var response = await this.httpClient.SendAsync(getRequest, cancellationToken).ConfigureAwait(false);

            string responseBody = null;
            if (response.Content != null)
            {
                responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw CreateException($"CommandFeed.GetResourceUriMapAsync returned unexpected status code: {response.StatusCode}, Body = \"{responseBody}\".", response);
            }

            var parsedResponse = JsonConvert.DeserializeObject<ResourceUriMapResponse>(responseBody);
            if (parsedResponse == null)
            {
                throw CreateException($"CommandFeed.GetResourceUriMapAsync returned empty response", response);
            }

            return parsedResponse;
        }

        private void ExtractPaCaFromVerifier(PrivacyCommandV2 command)
        {
            if (!string.IsNullOrEmpty(command.Verifier))
            {
                var token = new JwtSecurityToken(command.Verifier);

                var claim = token.Claims.Where(n => n.Type.Equals("ca")).FirstOrDefault();
                command.ControllerApplicable = !string.IsNullOrEmpty(claim?.Value) && bool.Parse(claim.Value);

                claim = token.Claims.Where(n => n.Type.Equals("pa")).FirstOrDefault();
                command.ProcessorApplicable = !string.IsNullOrEmpty(claim?.Value) && bool.Parse(claim.Value);
            }
        }

        private long ToUnixTimeSeconds(DateTimeOffset time)
        {
#if NET452
            return (long)(time.UtcDateTime - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
#else
            return time.ToUnixTimeSeconds();
#endif
        }

        // A typical command page header looks like this:
        //{
        //  "OperationType": "Delete",
        //  "Operation": "AccountClose",
        //  "CommandTypeId": 1,
        //  "CommandProperties": [
        //    {
        //      "Property": "DataType",
        //      "Values": [ "*" ]
        //    },
        //    {
        //      "Property": "SubjectType",
        //      "Values": [ "AADUser-CN.Azure.Mooncake" ]
        //    }
        //  ],
        //  "IsDeprecated": false,
        //  "HighVolumeModel": true,
        //  "PageSize": 500
        //},
        // The goal for this method is to extract the SubjectType property and find out which cloud instance it belongs to
        private string GetCommandCloudInstance(CommandPage commandPage)
        {
            foreach (var cmdProperty in commandPage.CommandProperties as JArray)
            {
                bool foundSubjectType = false;
                foreach (var propertyToken in cmdProperty)
                {
                    if (propertyToken is JProperty property)
                    {
                        if (foundSubjectType)
                        {
                            var subjectTypeValue = property?.Value?.ToString();
                            foreach (var cloundInstance in CloudInstance.All)
                            {
                                if (subjectTypeValue?.IndexOf(cloundInstance, StringComparison.OrdinalIgnoreCase) > 0)
                                {
                                    return cloundInstance;
                                }
                            }

                            break;
                        }
                        else if (string.Equals(property?.Value?.ToString(), "SubjectType", StringComparison.OrdinalIgnoreCase))
                        {
                            foundSubjectType = true;
                        }
                    }
                }
            }

            return CloudInstance.Public;
        }
    }
}
