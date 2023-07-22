// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [Flags]
    public enum WriterStatuses
    {
        /// <summary>
        ///     none option
        /// </summary>
        None = 0,

        /// <summary>
        ///     the writer is a normal data writer
        /// </summary>
        NormalDataWriter = 0x1,

        /// <summary>
        ///     the writer is a dead letter writer
        /// </summary>
        DeadLetterWriter = 0x2,

        /// <summary>
        ///     the data is being abandoned because it is a test command
        /// </summary>
        AbandonedTest = 0x4,

        /// <summary>
        ///     the data is being abandoned because the command is no longer available from command feed
        /// </summary>
        AbandonedNoCommand = 0x8,

        /// <summary>
        ///     the data is being abandoned because the storage location is no longer available (there was a non-transient error
        ///      writing to it)
        /// </summary>
        AbandonedNoStorage = 0x10,

        /// <summary>
        ///     the data is (at least partially) abandoned due to a write error (generally this can occur writing to the dead 
        ///      letter store, which is considered not a fatal error)
        /// </summary>
        AbandonedWriteError = 0x20,

        /// <summary>
        ///     the data is (at least partially) abandoned for unknown reasons
        /// </summary>
        AbandonedGeneral = 0x40,

        /// <summary>
        ///     the data is being abandoned because dead lettering is not currently supported
        /// </summary>
        AbandonedNoDeadLetter = 0x80,

        /// <summary>
        ///     the command was not applicable to the agent
        /// </summary>
        AbandonedNotApplicable = 0x100,

        /// <summary>
        ///     the command was not applicable to the agent
        /// </summary>
        AbandonedAlreadyComplete = 0x200,

        AbandonedAll =
            WriterStatuses.AbandonedTest |
            WriterStatuses.AbandonedNoCommand |
            WriterStatuses.AbandonedNoStorage |
            WriterStatuses.AbandonedWriteError |
            WriterStatuses.AbandonedGeneral |
            WriterStatuses.AbandonedNoDeadLetter |
            WriterStatuses.AbandonedAlreadyComplete |
            WriterStatuses.AbandonedNotApplicable,
    }

    /// <summary>
    ///     transient failure mode
    /// </summary>
    public enum TransientFailureMode 
    {
        /// <summary>
        ///     Possibly transient errors are treated as transient
        /// </summary>
        /// <remarks>
        ///     this is intentionally using the default value of 0 because we want to assume potentailly transient errors are
        ///      transient unless explicitly told otherwise
        /// </remarks>
        AssumeTransient = 0,

        /// <summary>
        ///     Possibly transient errors are treated as non-transient
        /// </summary>
        AssumeNonTransient
    }

    /// <summary>
    ///     contract for objects that allow writing to export streams
    /// </summary>
    public interface ICommandDataWriter : IDisposable
    {
        /// <summary>
        ///     Gets file details
        /// </summary>
        IEnumerable<IFileDetails> FileDetails { get; }

        /// <summary>
        ///     Gets or sets the transient failure mode
        /// </summary>
        /// <value>the transient failure mode</value>
        TransientFailureMode TransientFailureMode { get; set; }

        /// <summary>
        ///     Gets the writer statuses
        /// </summary>
        WriterStatuses Statuses { get; }

        /// <summary>
        ///     Gets a text representation of the last detected error or null if no errors ocurred
        /// </summary>
        string LastErrorDetails { get; }

        /// <summary>
        ///      Gets a text representation of the error code of the last detected error or null if no errors ocurred
        /// </summary>
        string LastErrorCode { get; }

        /// <summary>
        ///    Gets the command id
        /// </summary>
        string CommandId { get; }

        /// <summary>
        ///     Gets a value indicating whether to log size and/or row data for command feed
        /// </summary>
        bool LogForCommandFeed { get; }

        /// <summary>
        ///     Gets the approximate number of bytes waiting to be written
        /// </summary>
        /// <remarks>
        ///     this keeps track of the total number of pending data bytes, including any overhead bytes added by a 
        ///      particular writer (such as extra characters to implement JSON array syntax)
        ///     the behavior difference between this and Size is due to the different use cases.  Size is intended
        ///      for metric tracking while PendingSize is intended for making decisions on when to flush writers to
        ///      storage; thus, PendingSize needs to report a value closer to approximate memory usage.
        /// </remarks>
        long PendingSize { get; }

        /// <summary>
        ///     Gets the approximate number of data bytes written by the writer
        /// </summary>
        /// <remarks>
        ///     this keeps track of the number of data bytes written and does NOT include overhead bytes added by a 
        ///      particular writer (such as extra characters to implement JSON array syntax)
        /// </remarks>
        long Size { get; }

        /// <summary>
        ///     Gets the number of rows written by the writer
        /// </summary>
        int RowCount { get; }

        /// <summary>
        ///     Writes a json blob to the export package drop location
        /// </summary>
        /// <param name="productId">product id</param>
        /// <param name="json">json to write</param>
        /// <param name="pendingBytesThreshold">minimum number of bytes to accumulate before sending to storage</param>
        /// <returns>number of bytes added to or subtracted from the writer's pending byte count</returns>
        Task<long> WriteAsync(
            string productId,
            string json,
            int pendingBytesThreshold);

        /// <summary>
        ///     Closes the writer and flushes any pending data 
        /// </summary>
        /// <returns>resulting value</returns>
        Task CloseAsync();

        /// <summary>
        ///      Flushes any pending data
        /// </summary>
        /// <returns>number of bytes written</returns>
        Task<long> FlushAsync();
    }
}
