namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// Named entity filter.
    /// </summary>
    /// <typeparam name="TEntity">The entity that this applies to.</typeparam>
    public class NamedEntityFilterCriteria<TEntity> : EntityFilterCriteria<TEntity>
        where TEntity : NamedEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NamedEntityFilterCriteria{TEntity}"/> class.
        /// </summary>
        public NamedEntityFilterCriteria()
            : base()
        {
        }

        /// <summary>
        /// Gets or sets friendly name.
        /// </summary>
        public StringFilter Name { get; set; }

        /// <summary>
        /// Converts the filter criteria into a request string.
        /// </summary>
        /// <returns>The request string.</returns>
        protected override string BuildExpression()
        {
            var requestString = base.BuildExpression();
            
            if (this.Name != null)
            {
                requestString = this.Name.BuildFilterString("name").And(requestString);
            }

            return requestString;
        }
    }
}