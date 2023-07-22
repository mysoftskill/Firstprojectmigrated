namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Writer.UnitTest
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest;
    using Microsoft.PrivacyServices.DataManagement.Models.V2;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture.Xunit2;
    using Xunit;

    public class PrivacyDataStorageWriterTests
    {
        [Theory(DisplayName = "When DataOwner is created, then returned value has valid ETag."), AutoMoqData(DisableRecursionCheck = true)]
        public Task VerifyDataOwnerCreation(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            PrivacyDataStorageWriter writer,
            DataOwner dataOwner,
            string activityId,
            double requestCharge)
        {
            dataOwner.ETag = null;
            dataOwner.IsDeleted = false;

            return this.VerifyEntityUpdate(documentClientMock, dataOwner, writer.CreateDataOwnerAsync, WriteAction.Create, activityId, requestCharge);
        }

        [Theory(DisplayName = "When DataOwner is updated, then returned value has valid ETag."), AutoMoqData(DisableRecursionCheck = true)]
        public Task VerifyDataOwnerUpdate(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            PrivacyDataStorageWriter writer,
            DataOwner dataOwner,
            string activityId,
            double requestCharge)
        {
            dataOwner.IsDeleted = false;

            return this.VerifyEntityUpdate(documentClientMock, dataOwner, writer.UpdateDataOwnerAsync, WriteAction.Update, activityId, requestCharge);
        }

        [Theory(DisplayName = "When DataOwner is soft deleted, then returned value has valid ETag."), AutoMoqData(DisableRecursionCheck = true)]
        public Task VerifyDataOwnerSoftDelete(
            [Frozen] Mock<IDocumentClient> documentClientMock,
            PrivacyDataStorageWriter writer,
            DataOwner dataOwner,
            string activityId,
            double requestCharge)
        {
            dataOwner.IsDeleted = true;

            return this.VerifyEntityUpdate(documentClientMock, dataOwner, writer.UpdateDataOwnerAsync, WriteAction.SoftDelete, activityId, requestCharge);
        }
        
        /// <summary>
        /// Helper function to verify entity update method.
        /// </summary>
        /// <typeparam name="T">The parameter type.</typeparam>
        /// <param name="documentClientMock">Mock for IDocumentClient type.</param>
        /// <param name="entity">DataEntityBase type.</param>
        /// <param name="updateEntityAsyncMethod">The entity update method to be verified.</param>
        /// <param name="writeAction">The specific update write action: update or soft delete.</param>
        /// <param name="activityId">The activity ID header value.</param>
        /// <param name="requestCharge">The request charge header value.</param>
        /// <returns>A task.</returns>
        private async Task VerifyEntityUpdate<T>(
            Mock<IDocumentClient> documentClientMock,
            T entity,
            Func<T, Task<T>> updateEntityAsyncMethod,
            WriteAction writeAction,
            string activityId,
            double requestCharge) where T : Entity
        {
            var headers = new NameValueCollection();
            headers.Add("x-ms-activity-id", activityId);
            headers.Add("x-ms-request-charge", requestCharge.ToString());
            
            var entityDoc = DocumentDb.UnitTest.DocumentModule.Create(entity);
            var historyItemDoc = DocumentDb.UnitTest.DocumentModule.Create(new HistoryItem(entity, WriteAction.Update, Guid.NewGuid()));

            var response = StoredProcedureResponseModule.Create<Document[]>(new[] { entityDoc, historyItemDoc }, headers);

            Action<object[]> verifyUpserts = x =>
            {
                Assert.Equal(entity, x.ElementAt(0));
                Assert.Equal(entity, ((HistoryItem)x.ElementAt(1))?.Entity);
                Assert.Equal(writeAction, ((HistoryItem)x.ElementAt(1))?.WriteAction);
            };

            Action<object[]> verifyDeletes = x =>
            {
                Assert.True(x.Count() == 0);
            };

            // Return response is required since mock object creates Response but however Response.Resource 
            // throws NullReferenceException.
            documentClientMock
                .Setup(m =>
                m.ExecuteStoredProcedureAsync<Document[]>(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), Is.Value(verifyUpserts), Is.Value(verifyDeletes)))
                .Returns(Task.FromResult(response));

            await updateEntityAsyncMethod(entity).ConfigureAwait(false);

            documentClientMock.Verify(m => m.ExecuteStoredProcedureAsync<Document[]>(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<string>(), Is.Value(verifyUpserts), Is.Value(verifyDeletes)), Times.Once);
        }
    }
}
