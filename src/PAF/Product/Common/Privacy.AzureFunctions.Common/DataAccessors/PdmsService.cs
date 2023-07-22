namespace Microsoft.PrivacyServices.AzureFunctions.Common.DataAccessors
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    using Microsoft.PrivacyServices.AzureFunctions.Common.Configuration;
    using Microsoft.PrivacyServices.AzureFunctions.Common.Models;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Implementing IPdmsService
    /// </summary>
    public class PdmsService : IPdmsService
    {
        private const string ComponentName = nameof(PdmsService);
        private readonly IFunctionConfiguration configuration;
        private readonly ILogger logger;

        private readonly IHttpClientWrapper httpClientWrapper;
        private readonly IAuthenticationProvider authenticationProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PdmsService"/> class.
        /// Construct a new PdmsService
        /// </summary>
        /// <param name="configuration">Implementation of IFunctionConfiguration</param>
        /// <param name="logger">Implementation of ILogger</param>
        /// <param name="httpClientWrapper">Implementation of IHttpClientWrapper</param>
        /// <param name="authenticationProvider">Implementation of IAuthenticationProvider</param>
        public PdmsService(IFunctionConfiguration configuration, ILogger logger, IHttpClientWrapper httpClientWrapper, IAuthenticationProvider authenticationProvider)
        {
            this.configuration = configuration ?? throw new ArgumentException(nameof(configuration));
            this.logger = this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.httpClientWrapper = httpClientWrapper ?? throw new ArgumentNullException(nameof(httpClientWrapper));
            this.authenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
        }

        /// <inheritdoc/>
        public async Task<bool> ApproveVariantRequestAsync(Guid variantRequestId)
        {
            string apiUrl = $"api/v2/variantRequests('{variantRequestId}')/v2.approve";
            VariantRequest variantRequest;
            try
            {
                // Gets the variant request to use the etag
                string getApiUrl = $"api/v2/variantRequests('{variantRequestId}')";
                variantRequest = await this.httpClientWrapper.GetAsync<VariantRequest>(getApiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(ComponentName, $"{nameof(this.ApproveVariantRequestAsync)}: Error in retrieving Variant Request {variantRequestId}");
                throw ex;
            }

            if (variantRequest == null)
            {
                this.logger.Information(ComponentName, $"{nameof(this.ApproveVariantRequestAsync)}: {variantRequestId} was not found in PDMS");
                return false;
            }

            var pdmsResponseMessage = await this.httpClientWrapper.PostAsync(apiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger), variantRequest.ETag).ConfigureAwait(false);

            if (pdmsResponseMessage.IsSuccessStatusCode)
            {
                this.logger.Information(ComponentName, $"{nameof(this.ApproveVariantRequestAsync)}: Variant Request {variantRequestId}: Call to PDMS succeeded status code: {pdmsResponseMessage.StatusCode}");
                return true;
            }
            else
            {
                this.logger.Information(ComponentName, $"{nameof(this.ApproveVariantRequestAsync)}: Variant Request {variantRequestId}: Call to PDMS succeeded status code: {pdmsResponseMessage.StatusCode}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteVariantRequestAsync(Guid variantRequestId)
        {
            string apiUrl = $"api/v2/variantRequests('{variantRequestId}')";
            VariantRequest variantRequest;
            try
            {
                // Gets the variant request to use the etag
                string getApiUrl = $"api/v2/variantRequests('{variantRequestId}')";
                variantRequest = await this.httpClientWrapper.GetAsync<VariantRequest>(getApiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.Error(ComponentName, $"{nameof(this.DeleteVariantRequestAsync)}: Error in retrieving Variant Request {variantRequestId}");
                throw ex;
            }

            if (variantRequest == null)
            {
                this.logger.Information(ComponentName, $"{nameof(this.DeleteVariantRequestAsync)}: {variantRequestId} was already deleted in PDMS");
                return true;
            }

            var pdmsResponseMessage = await this.httpClientWrapper.DeleteAsync(apiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger), variantRequest.ETag).ConfigureAwait(false);

            if (pdmsResponseMessage.IsSuccessStatusCode)
            {
                this.logger.Information(ComponentName, $"{nameof(this.DeleteVariantRequestAsync)}: Variant Request {variantRequestId}: Call to PDMS succeeded status code: {pdmsResponseMessage.StatusCode}");
                return true;
            }
            else
            {
                this.logger.Information(nameof(PdmsService), $"{nameof(this.DeleteVariantRequestAsync)}: Variant Request {variantRequestId}: Status of response {pdmsResponseMessage.StatusCode} \n Details of Response: {pdmsResponseMessage.Content} \n {pdmsResponseMessage}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<VariantRequest> GetVariantRequestAsync(Guid variantRequestId)
        {
            VariantRequest variantRequest;
            try
            {
                if (variantRequestId == Guid.Empty)
                {
                    this.logger.Information(ComponentName, $"{nameof(this.GetVariantRequestAsync)}: variantRequestId is empty");
                    throw new ArgumentException("GetVariantRequestAsync: variantRequestId is empty", nameof(variantRequestId));
                }

                string apiUrl = $"api/v2/variantRequests('{variantRequestId}')";
                variantRequest = await this.httpClientWrapper.GetAsync<VariantRequest>(apiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger)).ConfigureAwait(false);

                if (variantRequest == null)
                {
                    this.logger.Warning(ComponentName, $"{nameof(this.GetVariantRequestAsync)}: Unable to retrieve Variant Request with Id : {variantRequestId}");
                    throw new ArgumentException($"Unable to retrieve Variant Request with Id : {variantRequestId}", nameof(variantRequestId));
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ComponentName, $"{nameof(this.GetVariantRequestAsync)}: Error in retrieving Variant Request {variantRequestId}" + " - " + ex.Message);
                throw ex;
            }

            return variantRequest;
        }

        /// <inheritdoc/>
        public async Task<VariantDefinition> GetVariantDefinitionAsync(Guid variantDefinitionId)
        {
            if (variantDefinitionId == Guid.Empty)
            {
                this.logger.Information(ComponentName, "GetVariantDefinitionAsync: variantDefinitionId is empty");
                throw new ArgumentException("GetVariantDefinitionAsync: variantDefinitionId is empty", nameof(variantDefinitionId));
            }

            string apiUrl = $"/api/v2/VariantDefinitions('{variantDefinitionId}')?$select=dataTypes,subjectTypes,capabilities";
            VariantDefinition variantDefinition = await this.httpClientWrapper.GetAsync<VariantDefinition>(apiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger)).ConfigureAwait(false);

            if (variantDefinition == null)
            {
                this.logger.Error(ComponentName, $"Variant definition {variantDefinitionId} was not found.");
                throw new ArgumentException($"Variant definition {variantDefinitionId} was not found.", nameof(variantDefinitionId));
            }

            return variantDefinition;
        }

        /// <inheritdoc/>
        public async Task<VariantRequest> UpdateVariantRequestAsync(VariantRequest variantRequest)
        {
            if (variantRequest == null)
            {
                throw new ArgumentNullException("Null VariantRequest", nameof(variantRequest));
            }

            string apiUrl = $"api/v2/variantRequests('{variantRequest.Id}')";
            var updatedVariantRequest = await this.httpClientWrapper.UpdateAsync<VariantRequest>(HttpMethod.Put, apiUrl, () => this.authenticationProvider.GetAccessTokenAsync(this.logger), variantRequest).ConfigureAwait(false);

            if (updatedVariantRequest == null)
            {
                this.logger.Information(ComponentName, $"{nameof(this.UpdateVariantRequestAsync)}: Error updating Variant request {variantRequest.Id}.");
                throw new InvalidOperationException($"Error updating {variantRequest.Id}");
            }

            return updatedVariantRequest;
        }
    }
}
