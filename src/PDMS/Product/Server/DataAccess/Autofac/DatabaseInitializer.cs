namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using Microsoft.Azure.Documents;
    using Microsoft.PrivacyServices.DataManagement.Common;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// Implements initialization logic for Document DB.
    /// </summary>
    public sealed class DatabaseInitializer : DocumentDb.DatabaseInitializer, IInitializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseInitializer"/> class.
        /// Initializes the document database service in Azure storage.
        /// </summary>
        /// <param name="configuration">The database configuration information.</param>
        /// <param name="documentClient">The client for talking to Document DB.</param>
        /// <param name="eventWriterFactory">Event factory for instrumentation.</param>
        /// <param name="storedProcedureProvider">Provider for stored procedure information.</param>
        public DatabaseInitializer(
            IDocumentDatabaseConfig configuration,
            IDocumentClient documentClient,
            IEventWriterFactory eventWriterFactory,
            DocumentDb.IStoredProcedureProvider storedProcedureProvider)
            : base(
                  new DocumentDb.SetupProperties
                  {
                      DatabaseName = configuration.DatabaseName,
                      CollectionName = configuration.EntityCollectionName,
                      OfferThroughput = 400
                  },
                  documentClient,
                  storedProcedureProvider,
                  eventWriterFactory.Trace)
        {
        }
    }
}
