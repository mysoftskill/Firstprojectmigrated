// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Tasks
{
    /// <summary>
    ///     generates messages based on writer status
    /// </summary>
    public static class WriterDescriptionGenerator
    {
        /// <summary>
        ///     Generates a description for a data writer
        /// </summary>
        /// <param name="writer">data writer to generate description for</param>
        /// <returns>resulting description</returns>
        public static string GenerateWriterDescription(ICommandDataWriter writer)
        {
            WriterStatuses status = writer.Statuses;
            string result;
            string errMsg = WriterDescriptionGenerator.GenerateAbandonedDescription(status);

            string GenMessage(
                WriterStatuses expected,
                string store)
            {
                string localResult = null;

                if ((status & expected) != 0)
                {
                    if (errMsg == null)
                    {
                        return "Wrote all data to " + store;
                    }

                    localResult = $"Initially wrote data to {store}, then discarded data because {errMsg}.";

                    if (string.IsNullOrWhiteSpace(writer.LastErrorCode) == false)
                    {
                        localResult += " Error code: " + writer.LastErrorCode;
                    }
                    else if (string.IsNullOrWhiteSpace(writer.LastErrorDetails) == false)
                    {
                        localResult += " Error details: " + writer.LastErrorDetails;
                    }
                }

                return localResult;
            }

            result = GenMessage(WriterStatuses.NormalDataWriter, "blob store");

            if (result == null)
            {
                result = GenMessage(WriterStatuses.DeadLetterWriter, "missing command holding store");
            }

            if (result == null)
            {
                if (errMsg == null)
                {
                    return "Discarded all data";
                }

                result = "Discarded all data because " + errMsg;
                if (string.IsNullOrWhiteSpace(writer.LastErrorCode) == false)
                {
                    result += " Error code: " + writer.LastErrorCode;
                }
                else if (string.IsNullOrWhiteSpace(writer.LastErrorDetails) == false)
                {
                    result += " Error details: " + writer.LastErrorDetails;
                }
            }

            return result;
        }

        /// <summary>
        ///     Generates a description for a data writer
        /// </summary>
        /// <param name="writer">data writer to generate description for</param>
        /// <returns>resulting description</returns>
        public static WriterStatuses GetOriginalType(ICommandDataWriter writer)
        {
            if (writer.Statuses == WriterStatuses.NormalDataWriter ||
                writer.Statuses == WriterStatuses.AbandonedNoDeadLetter)
            {
                return writer.Statuses;
            }

            return WriterStatuses.AbandonedGeneral;
        }

        /// <summary>
        ///     Generates a description for a data writer
        /// </summary>
        /// <param name="writer">data writer to generate description for</param>
        /// <returns>resulting description</returns>
        public static string GetAbandonedType(ICommandDataWriter writer)
        {
            WriterStatuses status = writer.Statuses & WriterStatuses.AbandonedAll;

            return status != WriterStatuses.None ? status.ToString() : string.Empty;
        }

        /// <summary>
        ///     Generates a description for an abandoned status
        /// </summary>
        /// <param name="status">status to generate description for</param>
        /// <returns>resulting description</returns>
        private static string GenerateAbandonedDescription(WriterStatuses status)
        {
            if (status.HasFlag(WriterStatuses.AbandonedNotApplicable))
            {
                return "the command has been determined to be not applicable to this agent.";
            }

            if (status.HasFlag(WriterStatuses.AbandonedNoCommand) || status.HasFlag(WriterStatuses.AbandonedAlreadyComplete))
            {
                return 
                    "the command became completed for this agent during or before data processing. This may be due to force " + 
                    "complete, the storage location becoming unavailable, or a prior batch submitting the command.";
            }

            if (status.HasFlag(WriterStatuses.AbandonedNoStorage))
            {
                return "there was a fatal error writing to storage";
            }

            if (status.HasFlag(WriterStatuses.AbandonedNoDeadLetter))
            {
                return "missing command holding storage currently discards all data sent to it";
            }

            if (status.HasFlag(WriterStatuses.AbandonedWriteError))
            {
                return "there was an error writing to storage";
            }

            if (status.HasFlag(WriterStatuses.AbandonedTest))
            {
                return "the command is a synthetic command for testing purposes and the agent is not a test agent";
            }

            if (status.HasFlag(WriterStatuses.AbandonedGeneral))
            {
                return "the command was abandoned";
            }

            return null;
        }
    }
}
