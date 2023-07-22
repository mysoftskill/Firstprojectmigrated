namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb
{
    using System.Collections.Generic;

    /// <summary>
    /// Retrieves the stored procedure information.
    /// </summary>
    public interface IStoredProcedureProvider
    {
        /// <summary>
        /// Retrieves the stored procedure data for database initialization.
        /// </summary>
        /// <returns>The stored procedure information.</returns>
        IEnumerable<StoredProcedure> GetStoredProcedures();
    }

    /// <summary>
    /// Stored procedure data for database initialization.
    /// </summary>
    public class StoredProcedure
    {
        /// <summary>
        /// What action to perform with the stored procedure.
        /// </summary>
        public enum Actions
        {
            /// <summary>
            /// Install the stored procedure.
            /// </summary>
            Install,

            /// <summary>
            /// Remove the stored procedure.
            /// </summary>
            Remove
        }

        /// <summary>
        /// Gets or sets the name value.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name value.
        /// </summary>
        public Actions Action { get; set; }

        /// <summary>
        /// Gets or sets the stored procedure data. This will be null for removals.
        /// </summary>
        public string Value { get; set; }
    }
}