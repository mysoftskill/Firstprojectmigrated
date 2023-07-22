// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Client
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    using Microsoft.OSGS.HttpClientCommon;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Clients.Interfaces;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Helpers;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Models;
    using Microsoft.PrivacyServices.PrivacyOperation.Client.Models.Requests;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts;
    using Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject;

    /// <summary>
    ///     PrivacyOperationClient
    /// </summary>
    public class PrivacyOperationClient : IPrivacyOperationClient
    {
        private const string DataTypeDelimiter = ",";

        private readonly IPrivacyOperationAuthClient authClient;

        private readonly IHttpClient httpClient;

        /// <inheritdoc />
        public PrivacyOperationClient(IHttpClient httpClient, IPrivacyOperationAuthClient privacyOperationAuthClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.authClient = privacyOperationAuthClient ?? throw new ArgumentNullException(nameof(this.authClient));
        }

        /// <inheritdoc />
        public async Task<IList<PrivacyRequestStatus>> ListRequestsAsync(ListOperationArgs request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            request.Validate();

            HttpRequestMessage requestMessage = await PrivacyOperationRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Get,
                PrivacyOperationUrlHelper.CreateListPathV1(string.Join(DataTypeDelimiter, request.RequestTypes ?? new List<PrivacyRequestType>())),
                OperationNames.ListRequests,
                request.CorrelationVector,
                this.authClient,
                request.AccessToken,
                request.UserAssertion).ConfigureAwait(false);

            HttpResponseMessage response = await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<IList<PrivacyRequestStatus>>(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<DeleteOperationResponse> PostDeleteRequestAsync(DeleteOperationArgs request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            request.Validate();

            DeleteOperationRequest requestData = this.CreateDeleteOperationRequest(request);

            HttpRequestMessage requestMessage = await PrivacyOperationRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyOperationUrlHelper.CreatePostDeletePathV1(string.Join(DataTypeDelimiter, request.DataTypes), request.StartTime, request.EndTime),
                OperationNames.PostDelete,
                request.CorrelationVector,
                this.authClient,
                request.AccessToken,
                request.UserAssertion).ConfigureAwait(false);
            requestMessage = this.SetRequestBody(this.AddAcceptJsonHeader(requestMessage), requestData);

            HttpResponseMessage response = await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<DeleteOperationResponse>(response).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ExportOperationResponse> PostExportRequestAsync(ExportOperationArgs request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            request.Validate();

            ExportOperationRequest requestData = this.CreateExportOperationRequest(request);

            HttpRequestMessage requestMessage = await PrivacyOperationRequestHelper.CreateS2SRequestAsync(
                HttpMethod.Post,
                PrivacyOperationUrlHelper.CreatePostExportPathV1(string.Join(DataTypeDelimiter, request.DataTypes), request.StartTime, request.EndTime),
                OperationNames.PostExport,
                request.CorrelationVector,
                this.authClient,
                request.AccessToken,
                request.UserAssertion).ConfigureAwait(false);
            requestMessage = this.SetRequestBody(this.AddAcceptJsonHeader(requestMessage), requestData);

            HttpResponseMessage response = await this.httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
            return await HttpHelper.HandleHttpResponseAsync<ExportOperationResponse>(response).ConfigureAwait(false);
        }

        private HttpRequestMessage AddAcceptJsonHeader(HttpRequestMessage requestMessage)
        {
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return requestMessage;
        }

        private DeleteOperationRequest CreateDeleteOperationRequest(DeleteOperationArgs args)
        {
            var request = new DeleteOperationRequest
            {
                Subject = args.Subject,
                Context = args.Context
            };
            return request;
        }

        private ExportOperationRequest CreateExportOperationRequest(ExportOperationArgs args)
        {
            var request = new ExportOperationRequest
            {
                Subject = args.Subject,
                Context = args.Context,
                StorageLocationUri = args.StorageLocationUri
            };
            return request;
        }

        /// <summary>
        ///     Sets <see cref="HttpRequestMessage.Content" /> to <see cref="ObjectContent" /> formatted to JSON.
        /// </summary>
        private HttpRequestMessage SetRequestBody<T>(HttpRequestMessage requestMessage, T data)
        {
            requestMessage.Content = new ObjectContent<T>(data, new JsonMediaTypeFormatter());
            return requestMessage;
        }
    }
}
