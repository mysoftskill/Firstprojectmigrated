namespace Microsoft.Membership.MemberServices.Privacy.QuickExportWorker.DataProcessingAgents
{
    using System;
    using Microsoft.Practices.TransientFaultHandling;

    /// <summary>
    ///     Detects if exceptions thrown by quick export PXF adapters are transient or not.
    /// </summary>
    public class QuickExportTransientErrorDetector : ITransientErrorDetectionStrategy
    {
        /// <summary>
        ///      Gets a singleton instance of this class
        /// </summary>
        public static QuickExportTransientErrorDetector Instance { get; } = new QuickExportTransientErrorDetector();

        /// <summary>
        ///      Determines whether the specified exception is transient
        /// </summary>
        /// <param name="e">Exception to test</param>
        /// <returns>True if the exception is transient, False otherwise</returns>
        public bool IsTransient(Exception e)
        {
            return true;
        }
    }
}