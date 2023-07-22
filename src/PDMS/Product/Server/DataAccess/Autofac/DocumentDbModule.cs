namespace Microsoft.PrivacyServices.DataManagement.DataAccess.Autofac
{
    using System;

    using global::Autofac;
    using global::Autofac.Extras.DynamicProxy;

    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.PrivacyServices.DataManagement.Common.Configuration;
    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem.KeyVault;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;
    using Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb;

    public class DocumentDbModule : Module
    {
        /// <summary>
        /// Registers dependencies for this component with <c>Autofac</c>.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DocumentQueryFactory>().As<IDocumentQueryFactory>().InstancePerLifetimeScope();

            this.RegisterInstrumentation(builder);
        }

        /// <summary>
        /// Registers all instrumentation related components.
        /// All instrumentation registrations must be per lifetime scope.
        /// </summary>
        /// <param name="builder">The container builder to which it should add the registrations.</param>
        private void RegisterInstrumentation(ContainerBuilder builder)
        {
            string documentClientName = "Instance";

            // This must be a singleton to improve performance.
            builder
                .Register(ctx =>
                {
                    var configManager = ctx.Resolve<IPrivacyConfigurationManager>();

                    var azureKeyVaultReader = ctx.Resolve<IAzureKeyVaultReader>();
                    string primaryKey = azureKeyVaultReader.GetSecretByNameAsync(configManager.DocumentDatabaseConfig.KeyVaultPrimaryKeyName).GetAwaiter().GetResult();

                    return new DocumentClient(new Uri(configManager.DocumentDatabaseConfig.EndpointUri), primaryKey);
                })
                .As<DocumentClient>()
                .Named<IDocumentClient>(documentClientName)
                .SingleInstance();

            // Use an interceptor to auto-instrument the document client.
            builder.RegisterType<DocumentClientInterceptor>().InstancePerLifetimeScope();
            builder
                .Register(ctx => ctx.ResolveNamed<IDocumentClient>(documentClientName))
                .As<IDocumentClient>()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(DocumentClientInterceptor))
                .InstancePerLifetimeScope()
                .OnRelease(_ => { }); // Do not call dispose, because it disposes the singleton registered above.

            // Register the adapter.
            builder.RegisterType<DocumentClientAdapter>().Named<IDocumentClientAdapter>(documentClientName).SingleInstance();

            // Register an interceptor for the adapter.
            builder
                .Register(ctx => ctx.ResolveNamed<IDocumentClientAdapter>(documentClientName))
                .As<IDocumentClientAdapter>()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(DocumentClientInterceptor))
                .InstancePerLifetimeScope()
                .OnRelease(_ => { }); // Do not call dispose, because it disposes the singleton registered above.

            // Register success mapping types.
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Attachment>>().As<ISessionWriter<Tuple<string, ResourceResponse<Attachment>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Conflict>>().As<ISessionWriter<Tuple<string, ResourceResponse<Conflict>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Database>>().As<ISessionWriter<Tuple<string, ResourceResponse<Database>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Document>>().As<ISessionWriter<Tuple<string, ResourceResponse<Document>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<DocumentCollection>>().As<ISessionWriter<Tuple<string, ResourceResponse<DocumentCollection>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Offer>>().As<ISessionWriter<Tuple<string, ResourceResponse<Offer>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Permission>>().As<ISessionWriter<Tuple<string, ResourceResponse<Permission>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Microsoft.Azure.Documents.StoredProcedure>>().As<ISessionWriter<Tuple<string, ResourceResponse<Microsoft.Azure.Documents.StoredProcedure>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<Trigger>>().As<ISessionWriter<Tuple<string, ResourceResponse<Trigger>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<User>>().As<ISessionWriter<Tuple<string, ResourceResponse<User>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<PartitionKeyRange>>().As<ISessionWriter<Tuple<string, ResourceResponse<PartitionKeyRange>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.ResourceResponseSessionWriter<UserDefinedFunction>>().As<ISessionWriter<Tuple<string, ResourceResponse<UserDefinedFunction>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.DocumentResultSessionWriter>().As<ISessionWriter<Sll.DocumentResult>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.StoredProcedureResponseSessionWriter<Document[]>>().As<ISessionWriter<Tuple<string, StoredProcedureResponse<Document[]>>>>().InstancePerLifetimeScope();
            builder.RegisterType<Sll.FeedResponseSessionWriter<PartitionKeyRange>>().As<ISessionWriter<Tuple<string, FeedResponse<PartitionKeyRange>>>>().InstancePerLifetimeScope();

            // Register exception mapping types.
            builder.RegisterType<Sll.DocumentClientExceptionSessionWriter>().As<ISessionWriter<Tuple<string, DocumentClientException>>>().InstancePerLifetimeScope();
        }
    }
}