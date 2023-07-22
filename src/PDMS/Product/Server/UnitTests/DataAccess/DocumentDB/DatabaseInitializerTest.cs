namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.Testing;

    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Xunit2;

    using Xunit;

    public class DatabaseInitializerTest
    {
        [Theory(DisplayName = "When an initialization continuously fails, then rethrow that exception."), Install]
        public async Task VerifyContinuousExceptionRethrown([Frozen] Mock<IDocumentClient> documentClient, DatabaseInitializer initializer)
        {
            documentClient.Setup(m => m.ReadDatabaseAsync(It.IsAny<Uri>(), null)).ThrowsAsync(new InvalidOperationException());            

            await Assert.ThrowsAsync<InvalidOperationException>(initializer.InitializeAsync).ConfigureAwait(false);
        }
        
        [Theory(DisplayName = "When an initialization fails once, then retry until successful."), Install]
        public async Task VerifyExceptionRetry([Frozen] Mock<IDocumentClient> documentClient, DatabaseInitializer initializer)
        {
            int calls = 0;

            documentClient
                .Setup(m => m.ReadDatabaseAsync(It.IsAny<Uri>(), null))
                .Callback(() => calls++)
                .Returns(() =>
                {
                    if (calls == 1)
                    {
                        return Task.FromException<ResourceResponse<Database>>(new InvalidOperationException());
                    }
                    else
                    {
                        return Task.FromResult<ResourceResponse<Database>>(null);
                    }
                });

            await initializer.InitializeAsync().ConfigureAwait(false);

            Assert.Equal(2, calls); // Ensure the callback is made.
        }

        [Theory(DisplayName = "When initialization is called and database does not exist, then create the database."), Install]
        public async Task VerifyCreateDatabase([Frozen] Mock<IDocumentClient> documentClient, [Frozen] SetupProperties config, DatabaseInitializer initializer)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(config.DatabaseName);

            documentClient
                .Setup(m => m.ReadDatabaseAsync(databaseUri, null))
                .ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.NotFound));

            await initializer.InitializeAsync().ConfigureAwait(false);

            Action<Database> assert = d => Assert.Equal(config.DatabaseName, d.Id);

            documentClient.Verify(m => m.CreateDatabaseAsync(Is.Value(assert), null), Times.Once);
        }

        [Theory(DisplayName = "When initialization is called and database already exists, then do not create the database."), Install]
        public async Task VerifySkipCreateDatabase([Frozen] Mock<IDocumentClient> documentClient, DatabaseInitializer initializer)
        {
            await initializer.InitializeAsync().ConfigureAwait(false);

            documentClient.Verify(m => m.CreateDatabaseAsync(It.IsAny<Database>(), It.IsAny<RequestOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When initialization is called and database creation throws exception, then bubble it up."), Install]
        public async Task VerifyCreateDatabaseException([Frozen] Mock<IDocumentClient> documentClient, [Frozen] SetupProperties config, DatabaseInitializer initializer)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(config.DatabaseName);

            documentClient
                .Setup(m => m.ReadDatabaseAsync(databaseUri, null))
                .ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.Conflict));

            await Assert.ThrowsAsync<DocumentClientException>(initializer.InitializeAsync).ConfigureAwait(false);            
        }

        [Theory(DisplayName = "When initialization is called and collection does not exist, then create the collection."), Install]
        public async Task VerifyCreateCollection([Frozen] Mock<IDocumentClient> documentClient, [Frozen] SetupProperties config, DatabaseInitializer initializer)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(config.DatabaseName);
            var collectionUri = UriFactory.CreateDocumentCollectionUri(config.DatabaseName, config.CollectionName);

            documentClient
                .Setup(m => m.ReadDocumentCollectionAsync(collectionUri, null))
                .ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.NotFound));

            await initializer.InitializeAsync().ConfigureAwait(false);

            Action<DocumentCollection> assertCollection = d => Assert.Equal(config.CollectionName, d.Id);
            Action<RequestOptions> assertOptions = d => Assert.Equal(config.OfferThroughput, d.OfferThroughput);

            documentClient.Verify(m => m.CreateDocumentCollectionAsync(databaseUri, Is.Value(assertCollection), Is.Value(assertOptions)), Times.Once);
        }

        [Theory(DisplayName = "When initialization is called and collection already exists, then do not create the database."), Install]
        public async Task VerifySkipCreateCollection([Frozen] Mock<IDocumentClient> documentClient, [Frozen] SetupProperties config, DatabaseInitializer initializer)
        {
            var databaseUri = UriFactory.CreateDatabaseUri(config.DatabaseName);

            await initializer.InitializeAsync().ConfigureAwait(false);
            
            documentClient.Verify(m => m.CreateDocumentCollectionAsync(databaseUri, It.IsAny<DocumentCollection>(), It.IsAny<RequestOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When initialization is called and collection creation throws exception, then bubble it up."), Install]
        public async Task VerifyCreateCollectionException([Frozen] Mock<IDocumentClient> documentClient, [Frozen] SetupProperties config, DatabaseInitializer initializer)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(config.DatabaseName, config.CollectionName);

            documentClient
                .Setup(m => m.ReadDocumentCollectionAsync(collectionUri, null))
                .ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.Conflict));

            await Assert.ThrowsAsync<DocumentClientException>(initializer.InitializeAsync).ConfigureAwait(false);
        }

        [Theory(DisplayName = "When initialization is called, action is install, and stored procedure does not exist, then create the stored procedure."), Install]
        public async Task VerifyCreateStoredProcedure(
            [Frozen] Mock<IDocumentClient> documentClient,
            [Frozen] SetupProperties config,
            [Frozen] IEnumerable<DocumentDb.StoredProcedure> sprocs,
            DatabaseInitializer initializer)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(config.DatabaseName, config.CollectionName);
            
            foreach (var sproc in sprocs)
            {
                var sprocUri = UriFactory.CreateStoredProcedureUri(config.DatabaseName, config.CollectionName, sproc.Name);

                documentClient
                    .Setup(m => m.ReadStoredProcedureAsync(sprocUri, null))
                    .ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.NotFound));
            }

            await initializer.InitializeAsync().ConfigureAwait(false);

            foreach (var sproc in sprocs)
            {
                Expression<Func<StoredProcedure, bool>> assert = d => sproc.Name == d.Id;
                documentClient.Verify(m => m.CreateStoredProcedureAsync(collectionUri, It.Is(assert), null), Times.Once);
            }
        }

        [Theory(DisplayName = "When initialization is called, action is install, and stored procedure already exists, then do not create the stored procedure."), Install]
        public async Task VerifySkipCreateStoredProcedure(
            [Frozen] Mock<IDocumentClient> documentClient, 
            [Frozen] SetupProperties config,
            DatabaseInitializer initializer)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(config.DatabaseName, config.CollectionName);
            
            await initializer.InitializeAsync().ConfigureAwait(false);
            
            documentClient.Verify(m => m.CreateStoredProcedureAsync(collectionUri, It.IsAny<StoredProcedure>(), It.IsAny<RequestOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When initialization is called, action is remove, and stored procedure does not exist, then skip deleting the stored procedure."), Remove]
        public async Task VerifySkipDeleteStoredProcedure(
            [Frozen] Mock<IDocumentClient> documentClient,
            [Frozen] SetupProperties config,
            [Frozen] IEnumerable<DocumentDb.StoredProcedure> sprocs,
            DatabaseInitializer initializer)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(config.DatabaseName, config.CollectionName);

            foreach (var sproc in sprocs)
            {
                var sprocUri = UriFactory.CreateStoredProcedureUri(config.DatabaseName, config.CollectionName, sproc.Name);

                documentClient
                    .Setup(m => m.ReadStoredProcedureAsync(sprocUri, null))
                    .ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.NotFound));
            }

            await initializer.InitializeAsync().ConfigureAwait(false);

            documentClient.Verify(m => m.DeleteStoredProcedureAsync(It.IsAny<Uri>(), It.IsAny<RequestOptions>()), Times.Never);
        }

        [Theory(DisplayName = "When initialization is called, action is remove, and stored procedure already exists, then delete the stored procedure."), Remove]
        public async Task VerifyDeleteStoredProcedure(
            [Frozen] Mock<IDocumentClient> documentClient,
            [Frozen] SetupProperties config,
            [Frozen] IEnumerable<DocumentDb.StoredProcedure> sprocs,
            DatabaseInitializer initializer)
        {
            var collectionUri = UriFactory.CreateDocumentCollectionUri(config.DatabaseName, config.CollectionName);

            await initializer.InitializeAsync().ConfigureAwait(false);

            foreach (var sproc in sprocs)
            {
                var sprocUri = UriFactory.CreateStoredProcedureUri(config.DatabaseName, config.CollectionName, sproc.Name);
                Expression<Func<Uri, bool>> verify = x => sprocUri.OriginalString == x.OriginalString;
                documentClient.Verify(m => m.DeleteStoredProcedureAsync(It.Is<Uri>(verify), null), Times.Once);
            }
        }

        [Theory(DisplayName = "When initialization is called and stored procedure creation throws exception, then bubble it up."), Install]
        public async Task VerifyCreateStoredProcedureException(
            [Frozen] Mock<IDocumentClient> documentClient,
            [Frozen] SetupProperties config,
            [Frozen] IEnumerable<DocumentDb.StoredProcedure> sprocs,
            DatabaseInitializer initializer)
        {
            var sproc = sprocs.First();

            var sprocUri = UriFactory.CreateStoredProcedureUri(config.DatabaseName, config.CollectionName, sproc.Name);

            documentClient
                .Setup(m => m.ReadStoredProcedureAsync(sprocUri, null))
                .ThrowsAsync(DocumentClientExceptionModule.Create(System.Net.HttpStatusCode.Conflict));            

            await Assert.ThrowsAsync<DocumentClientException>(initializer.InitializeAsync).ConfigureAwait(false);
        }

        public class InstallAttribute : AutoMoqDataAttribute
        {
            public InstallAttribute()
            {
                this.Fixture.Inject(DocumentDb.StoredProcedure.Actions.Install);
            }
        }

        public class RemoveAttribute : AutoMoqDataAttribute
        {
            public RemoveAttribute()
            {
                this.Fixture.Inject(DocumentDb.StoredProcedure.Actions.Remove);
            }
        }
    }
}