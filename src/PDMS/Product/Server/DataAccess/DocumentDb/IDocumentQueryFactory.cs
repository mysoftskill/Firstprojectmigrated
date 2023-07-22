namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// Defines methods for altering document queries.
    /// </summary>
    public interface IDocumentQueryFactory
    {
        /// <summary>
        /// Decorate a query.
        /// </summary>
        /// <typeparam name="T">The query type.</typeparam>
        /// <param name="original">The original query.</param>
        /// <returns>The decorated query.</returns>
        IDocumentQuery<T> Decorate<T>(IDocumentQuery<T> original);
    }
}