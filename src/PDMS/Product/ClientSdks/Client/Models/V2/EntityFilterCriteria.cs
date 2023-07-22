namespace Microsoft.PrivacyServices.DataManagement.Client.V2
{
    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// Entity filter.
    /// </summary>
    /// <typeparam name="TEntity">The entity that this applies to.</typeparam>
    public class EntityFilterCriteria<TEntity> : PagingCriteria, IFilterCriteria<TEntity>
        where TEntity : Entity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityFilterCriteria{TEntity}"/> class.
        /// </summary>
        public EntityFilterCriteria()
        {
        }

        /// <summary>
        /// Gets or sets a second filter criteria to apply as an OR clause.
        /// </summary>
        public EntityFilterCriteria<TEntity> Or { get; set; }

        /// <summary>
        /// Converts the filter criteria into a request string.
        /// </summary>
        /// <returns>The request string.</returns>
        protected virtual string BuildExpression()
        {
            return string.Empty;
        }

        /// <summary>
        /// Builds the filter string for the request.
        /// </summary>
        /// <returns>The filter string.</returns>
        protected override string BuildFilterString()
        {
            if (this.Or != null)
            {
                var left = this.Or.BuildFilterString();
                var right = this.BuildExpression();

                if (string.IsNullOrEmpty(left))
                {
                    return right;
                }
                else if (string.IsNullOrEmpty(right))
                {
                    return left;
                }
                else
                {
                    return $"({left}) or ({right})";
                }
            }
            else
            {
                return this.BuildExpression();
            }
        }
    }
}