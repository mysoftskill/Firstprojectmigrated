// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    /// <summary>
    ///     command writer that does no actual writing
    /// </summary>
    public sealed class NoOpCommandDataWriter : DataWriter
    {
        /// <summary>
        ///     Initializes a new instance of the TestCommandDataWriter class
        /// </summary>
        /// <param name="commandId">command id</param>
        /// <param name="fileName">file name</param>
        /// <param name="abandonReason">reason that the data is being abandonded</param>
        public NoOpCommandDataWriter(
            string commandId,
            string fileName,
            WriterStatuses abandonReason) :
            base(commandId, fileName)
        {
            const WriterStatuses Allowed =
                WriterStatuses.AbandonedGeneral |
                WriterStatuses.AbandonedTest |
                WriterStatuses.AbandonedNoCommand |
                WriterStatuses.AbandonedNoStorage |
                WriterStatuses.AbandonedNoDeadLetter |
                WriterStatuses.AbandonedWriteError;

            WriterStatuses status = abandonReason & Allowed;

            this.Statuses = status == WriterStatuses.None ? WriterStatuses.AbandonedGeneral : status;
        }
    }
}
