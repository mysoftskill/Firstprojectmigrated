namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using global::Autofac;

    using Microsoft.Azure.Documents.Linq;
    using Microsoft.PrivacyServices.DataManagement.Common.Instrumentation;

    /// <summary>
    /// Modifies document queries by injecting dependencies from <c>Autofac</c>.
    /// </summary>
    public class DocumentQueryFactory : IDocumentQueryFactory
    {
        /// <summary>
        /// The current scope.
        /// </summary>
        private readonly ILifetimeScope scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentQueryFactory" /> class.
        /// </summary>
        /// <param name="scope">The current scope.</param>
        public DocumentQueryFactory(ILifetimeScope scope)
        {
            this.scope = scope;
        }

        /// <summary>
        /// Decorate a query.
        /// </summary>
        /// <typeparam name="T">The query type.</typeparam>
        /// <param name="original">The original query.</param>
        /// <returns>The decorated query.</returns>
        public IDocumentQuery<T> Decorate<T>(IDocumentQuery<T> original)
        {
            var sessionFactory = this.scope.Resolve<ISessionFactory>();

            return new DocumentQueryDecorator<T>(sessionFactory, original);
        }
    }
}