namespace Microsoft.PrivacyServices.CommandFeed.Service.Common
{
    /// <summary>
    /// Interface used for tracking objects where changes have been made.
    /// </summary>
    public interface ICommandHistoryChangeTrackedObject
    {
        /// <summary>
        /// Indicates if the current object or its sub-objects has mutated.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Clears the set of fragments that have changed.
        /// </summary>
        void ClearDirty();
    }
}
