// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.PrivacyMockService.Controllers.RecurringDeletes
{
    using Microsoft.Azure.ComplianceServices.Common;
    using Microsoft.Membership.MemberServices.Privacy.ExperienceContracts.V2;
    using Microsoft.Membership.MemberServices.ScheduleDbClient;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Exceptions;
    using Microsoft.Membership.MemberServices.ScheduleDbClient.Model;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;

    public class ScheduleDbController : ApiController
    {
        private ILogger Logger { get; } = DualLogger.Instance;

        private static ScheduleDbCosmosClient scheduleDbCosmosClient;

        private List<long> documentsToDelete;

        private IAppConfiguration appConfiguration;

        [HttpPost]
        [Route("scheduleDb/createRecurringDeleteDocument")]
        public async Task<HttpResponseMessage> CreateRecurringDeletesScheduleDbAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.CreateRecurringDeletesScheduleDbAsync), "Inside ScheduleDbController.CreateRecurringDeletesScheduleDbAsync");
            InitializeScheduleDbCosmosClient();
            var recurrentDeleteScheduleDbDocument = this.GetRecurrentDeleteScheduleDbDocument();
            this.documentsToDelete = new List<long>() { recurrentDeleteScheduleDbDocument.Puid};
            try
            {
                var response = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument).ConfigureAwait(false);

                if (response == null
                    || recurrentDeleteScheduleDbDocument.Puid != response.Puid
                    || recurrentDeleteScheduleDbDocument.DataType != response.DataType
                    || recurrentDeleteScheduleDbDocument.DocumentId != response.DocumentId)
                {
                    this.Logger.Error(nameof(ScheduleDbController.CreateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.CreateRecurringDeletesScheduleDbAsync assertion failed");
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
                }

                this.Logger.Information(nameof(ScheduleDbController.CreateRecurringDeletesScheduleDbAsync), "CreateRecurringDeletesScheduleDbAsync finished successfully");
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                this.Logger.Error(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync failed with error {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [Route("scheduleDb/createUpdateRecurringDeleteDocument")]
        public async Task<HttpResponseMessage> CreateOrUpdateRecurringDeletesScheduleDbAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), "Inside ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync");
            InitializeScheduleDbCosmosClient();
            var recurrentDeleteScheduleDbDocument = this.GetRecurrentDeleteScheduleDbDocument();
            this.documentsToDelete = new List<long>() { recurrentDeleteScheduleDbDocument.Puid };
            
            var response = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument).ConfigureAwait(false);

            if (response == null
                || recurrentDeleteScheduleDbDocument.Puid != response.Puid
                || recurrentDeleteScheduleDbDocument.DataType != response.DataType
                || recurrentDeleteScheduleDbDocument.DocumentId != response.DocumentId)
            {
                this.Logger.Error(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync assertion failed");
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
            }

            // updating a document with null documentId, should result in updation of the document identified by puid + datatype
            var updateDocument = new RecurrentDeleteScheduleDbDocument(recurrentDeleteScheduleDbDocument.Puid,
                    recurrentDeleteScheduleDbDocument.DataType,
                    String.Empty);
                updateDocument.RecurrentDeleteStatus = RecurrentDeleteStatus.Unknown;

            var updateResponse = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(updateDocument).ConfigureAwait(false);

            if (updateResponse == null
                    || updateDocument.Puid != updateResponse.Puid
                    || updateDocument.DataType != updateResponse.DataType
                    || recurrentDeleteScheduleDbDocument.DocumentId != updateResponse.DocumentId
                    || updateDocument.RecurrentDeleteStatus != updateResponse.RecurrentDeleteStatus)
                {
                    this.Logger.Error(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync assertion failed");
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
                }

            // trying to update the dataType or Puid on an existing document should throw conflict exception,
            // since changing puid or dataype the document would not be found in db, create flow will be called but conflict exception will be thrown because of existing documentid
            var updateDocument2 = recurrentDeleteScheduleDbDocument;
            updateDocument2.Puid = 1;
            updateDocument2.DataType = "dataType2";

            try
            {
                var updateResponse2 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(updateDocument2).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException)
            {
                this.Logger.Information(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), "threw exception as expected");
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }

            this.Logger.Error(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync did not threw exception as expected");
            return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
        }

        [HttpPost]
        [Route("scheduleDb/updateRecurringDeleteDocument")]
        public async Task<HttpResponseMessage> UpdateRecurringDeletesScheduleDbAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync), "Inside ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync");
            InitializeScheduleDbCosmosClient();
            var recurrentDeleteScheduleDbDocument = this.GetRecurrentDeleteScheduleDbDocument();
            this.documentsToDelete = new List<long>() { recurrentDeleteScheduleDbDocument.Puid };
            var createResponse = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument).ConfigureAwait(false);

            if (createResponse == null
                    || recurrentDeleteScheduleDbDocument.Puid != createResponse.Puid
                    || recurrentDeleteScheduleDbDocument.DataType != createResponse.DataType
                    || recurrentDeleteScheduleDbDocument.DocumentId != createResponse.DocumentId)
            {
                this.Logger.Error(nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync assertion failed during creation");
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
            }

            this.Logger.Information(nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync), "Creating doc finished successfully");

            var documentToUpdate = createResponse;
            documentToUpdate.RecurrentDeleteStatus = RecurrentDeleteStatus.Unknown;

            // update
            var updateResponse = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(documentToUpdate).ConfigureAwait(false);

            if (updateResponse == null
                || documentToUpdate.Puid != updateResponse.Puid
                || documentToUpdate.DataType != updateResponse.DataType
                || documentToUpdate.DocumentId != updateResponse.DocumentId
                || documentToUpdate.RecurrentDeleteStatus != updateResponse.RecurrentDeleteStatus)
            {
                this.Logger.Error(nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync assertion failed during updation");
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
            }

            var documentToUpdate1 = createResponse;
            documentToUpdate1.DataType = "dataType3";

            // should throw exception for same etag
            try
            {
                var updateResponse1 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(documentToUpdate1).ConfigureAwait(false);
            }
            catch (ScheduleDbClientException)
            {
                this.Logger.Information(nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync), "Updating doc finished successfully");
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }

            this.Logger.Error(nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync assertion failed during updation for same etag");
            return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
        }

        [HttpPost]
        [Route("scheduleDb/deleteRecurringDeleteDocument")]
        public async Task<HttpResponseMessage> DeleteRecurringDeletesScheduleDbAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.DeleteRecurringDeletesScheduleDbAsync), "Inside ScheduleDbController.DeleteRecurringDeletesScheduleDbAsync");
            InitializeScheduleDbCosmosClient();
            var recurrentDeleteScheduleDbDocument = this.GetRecurrentDeleteScheduleDbDocument();
            this.documentsToDelete = new List<long>() { recurrentDeleteScheduleDbDocument.Puid };

            try
            {
                var response = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument).ConfigureAwait(false);

                if (response == null
                    || recurrentDeleteScheduleDbDocument.Puid != response.Puid
                    || recurrentDeleteScheduleDbDocument.DataType != response.DataType
                    || recurrentDeleteScheduleDbDocument.DocumentId != response.DocumentId)
                {
                    this.Logger.Error(nameof(ScheduleDbController.DeleteRecurringDeletesScheduleDbAsync), $"ScheduleDbController.DeleteRecurringDeletesScheduleDbAsync assertion failed during creation");
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed);
                }

                await scheduleDbCosmosClient.DeleteRecurringDeletesScheduleDbAsync(response.Puid, response.DataType, CancellationToken.None).ConfigureAwait(false);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                this.Logger.Error(nameof(ScheduleDbController.DeleteRecurringDeletesScheduleDbAsync), $"ScheduleDbController.DeleteRecurringDeletesScheduleDbAsync failed with error {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [Route("scheduleDb/getRecurringDeleteDocumentByPuid")]
        public async Task<HttpResponseMessage> GetRecurringDeletesScheduleDbAsyncByPuid()
        {
            this.Logger.Information(nameof(ScheduleDbController.GetRecurringDeletesScheduleDbAsyncByPuid), "Inside ScheduleDbController.GetRecurringDeletesScheduleDbAsyncByPuid");
            InitializeScheduleDbCosmosClient();

            var puid = new Random().Next(1, 10000);

            // create first document
            var document1 = this.GetRecurrentDeleteScheduleDbDocument();
            document1.Puid = puid;
            this.documentsToDelete = new List<long>() { document1.Puid };
            try
            {
                var response1 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document1).ConfigureAwait(false);
                if (response1 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // create second document with same puid
                var document2 = this.GetRecurrentDeleteScheduleDbDocument();
                document2.Puid = puid;
                document2.DataType = "dataType2";
                this.documentsToDelete.Add(document2.Puid);
                var response2 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document2).ConfigureAwait(false);
                if (response2 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                var getResponse = await scheduleDbCosmosClient.GetRecurringDeletesScheduleDbAsync(puid, CancellationToken.None).ConfigureAwait(false);
                if (getResponse == null
                    || getResponse.Count != 2
                    || getResponse[0].DocumentId != document1.DocumentId
                    || getResponse[1].DocumentId != document2.DocumentId
                    || getResponse[0].Puid != document1.Puid
                    || getResponse[1].Puid != document2.Puid)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [Route("scheduleDb/hasRecurringDeleteDocument")]
        public async Task<HttpResponseMessage> HasRecurringDeletesScheduleDbRecordAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.HasRecurringDeletesScheduleDbRecordAsync), "Inside ScheduleDbController.HasRecurringDeletesScheduleDbRecordAsync");
            InitializeScheduleDbCosmosClient();

            var document = this.GetRecurrentDeleteScheduleDbDocument();
            this.documentsToDelete = new List<long>() { document.Puid };

            try
            {
                var response = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document).ConfigureAwait(false);
                if (response == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                var hasResponse = await scheduleDbCosmosClient.HasRecurringDeletesScheduleDbRecordAsync(response.Puid, response.DataType, CancellationToken.None).ConfigureAwait(false);

                return hasResponse == true ? Request.CreateResponse(HttpStatusCode.OK) : Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [Route("scheduleDb/getRecurringDeletesScheduleDbDocument")]
        public async Task<HttpResponseMessage> GetRecurringDeletesScheduleDbDocumentAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.GetRecurringDeletesScheduleDbDocumentAsync), "Inside ScheduleDbController.GetRecurringDeletesScheduleDbDocumentAsync");
            InitializeScheduleDbCosmosClient();

            var document = this.GetRecurrentDeleteScheduleDbDocument();
            this.documentsToDelete = new List<long>() { document.Puid };
            try
            {
                var response = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document).ConfigureAwait(false);
                if (response == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                var getResponse = await scheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync(response.DocumentId, CancellationToken.None).ConfigureAwait(false);

                if (getResponse == null
                    || getResponse.Puid != document.Puid
                    || getResponse.DocumentId != document.DocumentId)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [Route("scheduleDb/GetExpiredPreVerifiers")]
        public async Task<HttpResponseMessage> GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync), "Inside ScheduleDbController.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync");
            InitializeScheduleDbCosmosClient();

            // create first document
            var document1 = this.GetRecurrentDeleteScheduleDbDocument();
            document1.PreVerifierExpirationDateUtc = DateTime.UtcNow.AddDays(-2);
            document1.RecurrentDeleteStatus = RecurrentDeleteStatus.Active;
            this.documentsToDelete = new List<long>() { document1.Puid };
            try
            {
                var response1 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document1).ConfigureAwait(false);
                if (response1 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // create second document
                var document2 = this.GetRecurrentDeleteScheduleDbDocument();
                document2.PreVerifierExpirationDateUtc = DateTime.UtcNow.AddDays(-1);
                document2.RecurrentDeleteStatus = RecurrentDeleteStatus.Active;
                this.documentsToDelete.Add(document2.Puid);
                var response2 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document2).ConfigureAwait(false);
                if (response2 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // create third document with PreVerifierExpirationDateUtc +1 day. Should not return in query
                var document3 = this.GetRecurrentDeleteScheduleDbDocument();
                document3.PreVerifierExpirationDateUtc = DateTime.UtcNow.AddDays(1);
                document3.RecurrentDeleteStatus = RecurrentDeleteStatus.Active;
                this.documentsToDelete.Add(document3.Puid);
                var response3 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document3).ConfigureAwait(false);
                if (response3 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                string continuationToken = null;
                var result = new List<RecurrentDeleteScheduleDbDocument>();

                // query for 1 document at a time to test continuation token logic
                var getResponse1 = await scheduleDbCosmosClient.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync(DateTimeOffset.UtcNow, continuationToken, maxItemCount: 1).ConfigureAwait(false);

                if (getResponse1.Item1 == null
                    || getResponse1.continuationToken == null
                    || getResponse1.Item1.Count != 1
                    || getResponse1.Item1.First().DocumentId != document1.DocumentId
                    || getResponse1.Item1.First().RecurrentDeleteStatus != document1.RecurrentDeleteStatus)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // call using continuation token from previous call
                var getResponse2 = await scheduleDbCosmosClient.GetExpiredPreVerifiersRecurringDeletesScheduleDbAsync(DateTimeOffset.UtcNow, getResponse1.continuationToken, maxItemCount: 1).ConfigureAwait(false);

                // continuation token should be null and third record should not be returned
                if (getResponse2.Item1 == null
                    || getResponse2.continuationToken != null
                    || getResponse2.Item1.Count != 1
                    || getResponse2.Item1.First().DocumentId != document2.DocumentId
                    || getResponse2.Item1.First().RecurrentDeleteStatus != document2.RecurrentDeleteStatus)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [Route("scheduleDb/GetApplicableRecurringDeletes")]
        public async Task<HttpResponseMessage> GetApplicableRecurringDeletesScheduleDbAsync()
        {
            this.Logger.Information(nameof(ScheduleDbController.GetApplicableRecurringDeletesScheduleDbAsync), "Inside ScheduleDbController.GetApplicableRecurringDeletesScheduleDbAsync");
            InitializeScheduleDbCosmosClient();

            // create first document
            var document1 = this.GetRecurrentDeleteScheduleDbDocument();
            document1.NextDeleteOccurrenceUtc = DateTime.UtcNow.AddDays(-2);
            document1.RecurrentDeleteStatus = RecurrentDeleteStatus.Active;
            this.documentsToDelete = new List<long>() { document1.Puid};
            try
            {
                var response1 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document1).ConfigureAwait(false);
                if (response1 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // create second document
                var document2 = this.GetRecurrentDeleteScheduleDbDocument();
                document2.NextDeleteOccurrenceUtc = DateTime.UtcNow.AddDays(-1);
                document2.RecurrentDeleteStatus = RecurrentDeleteStatus.Active;
                this.documentsToDelete.Add(document2.Puid);
                var response2 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document2).ConfigureAwait(false);
                if (response2 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // create third document with NextDeleteOccurrenceUtc +1 day. Should not return in query
                var document3 = this.GetRecurrentDeleteScheduleDbDocument();
                document3.NextDeleteOccurrenceUtc = DateTime.UtcNow.AddDays(1);
                document3.RecurrentDeleteStatus = RecurrentDeleteStatus.Active;
                this.documentsToDelete.Add(document3.Puid);
                var response3 = await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(document3).ConfigureAwait(false);
                if (response3 == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                string continuationToken = null;
                var result = new List<RecurrentDeleteScheduleDbDocument>();

                // query for 1 document at a time to test continuation token logic
                var getResponse1 = await scheduleDbCosmosClient.GetApplicableRecurringDeletesScheduleDbAsync(DateTimeOffset.UtcNow, continuationToken, maxItemCount: 1).ConfigureAwait(false);

                if (getResponse1.Item1 == null
                    || getResponse1.continuationToken == null
                    || getResponse1.Item1.Count != 1
                    || getResponse1.Item1.First().DocumentId != document1.DocumentId
                    || getResponse1.Item1.First().RecurrentDeleteStatus != document1.RecurrentDeleteStatus)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                // call using continuation token from previous call
                var getResponse2 = await scheduleDbCosmosClient.GetApplicableRecurringDeletesScheduleDbAsync(DateTimeOffset.UtcNow, getResponse1.continuationToken, maxItemCount: 1).ConfigureAwait(false);

                // continuation token should be null as third record should not be returned
                if (getResponse2.Item1 == null
                    || getResponse2.continuationToken != null
                    || getResponse2.Item1.Count != 1
                    || getResponse2.Item1.First().DocumentId != document2.DocumentId
                    || getResponse2.Item1.First().RecurrentDeleteStatus != document2.RecurrentDeleteStatus)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest);
                }

                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
            finally
            {
                await this.DeleteScheduleDbTestDocuments(documentsToDelete, nameof(ScheduleDbController.UpdateRecurringDeletesScheduleDbAsync)).ConfigureAwait(false);
            }
        }

        [HttpPost]
        [Route("scheduleDb/getrecurringdeletebypuidanddatatype")]
        public async Task<HttpResponseMessage> GetRecurringDeleteByPuidAndDataType()
        {
            this.Logger.Information(nameof(ScheduleDbController.GetRecurringDeleteByPuidAndDataType), "Inside scheduleDb/getrecurringdeletebypuidanddatatype");
            InitializeScheduleDbCosmosClient();
            var recurrentDeleteScheduleDbDocument = this.GetRecurrentDeleteScheduleDbDocument();
            try
            {
                await scheduleDbCosmosClient.CreateOrUpdateRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument).ConfigureAwait(false);
                this.Logger.Information(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), "CreateOrUpdateRecurringDeletesScheduleDbAsync finished successfully");
            }
            catch (Exception ex)
            {
                this.Logger.Error(nameof(ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync), $"ScheduleDbController.CreateOrUpdateRecurringDeletesScheduleDbAsync failed with error {ex.Message}");
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }

            var response = await scheduleDbCosmosClient.GetRecurringDeletesScheduleDbDocumentAsync(recurrentDeleteScheduleDbDocument.Puid, recurrentDeleteScheduleDbDocument.DataType, CancellationToken.None);

            Assert.IsNotNull(response);
            Assert.AreEqual(recurrentDeleteScheduleDbDocument.Puid, response.Puid);
            Assert.AreEqual(recurrentDeleteScheduleDbDocument.DataType, response.DataType);

            // cleanup
            await scheduleDbCosmosClient.DeleteRecurringDeletesScheduleDbAsync(recurrentDeleteScheduleDbDocument.Puid, recurrentDeleteScheduleDbDocument.DataType, CancellationToken.None);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private void InitializeScheduleDbCosmosClient()
        {
            this.Logger.Information(nameof(ScheduleDbController.InitializeScheduleDbCosmosClient), "beginning InitializeScheduleDbCosmosClient");
            var appConfiguration = new AppConfiguration(@"local.settings.json");
            try
            {
                if (scheduleDbCosmosClient == null)
                {
                    scheduleDbCosmosClient = new ScheduleDbCosmosClient(Program.PartnerMockConfigurations, appConfiguration, this.Logger);
                }
            }
            catch (Exception ex)
            {
                this.Logger.Error(nameof(ScheduleDbController.InitializeScheduleDbCosmosClient), ex.Message);
            }
        }

        private RecurrentDeleteScheduleDbDocument GetRecurrentDeleteScheduleDbDocument()
        {
            return new RecurrentDeleteScheduleDbDocument(
                    new Random().Next(1, 10000),
                    "dataType1",
                    Guid.NewGuid().ToString(),
                    null,
                    DateTime.UtcNow.AddDays(+5)
                );
        }

        private async Task DeleteScheduleDbTestDocuments(List<long> documentsToDelete, string componentName)
        {
            try
            {
                foreach (var puid in documentsToDelete)
                {
                    await scheduleDbCosmosClient.DeleteRecurringDeletesByPuidScheduleDbAsync(puid, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                throw new ScheduleDbClientException(componentName, ex.Message, ex);
            }
        }
    }
}

