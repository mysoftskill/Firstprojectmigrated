// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility.ProgressTracker
{
    /// <summary>
    ///     list of file progress tracker types
    /// </summary>
    public static class TrackerTypes
    {
        public const string DataFileStarting = "DataFile.Starting";
        public const string DataFileComplete = "DataFile.Complete";
        public const string DataFileCommand = "DataFile.Command";
        public const string DataFileError = "DataFile.Error";

        public const string BatchDataFileNames = "Batch.DataFileNames";
        public const string BatchDataFiles = "Batch.DataFiles";
        public const string BatchCommands = "Batch.Commands";
        public const string BatchComplete = "Batch.Complete";

        public const string GeneralError = "General.Error";
        public const string GeneralInfo = "General.Info";
    }
}
