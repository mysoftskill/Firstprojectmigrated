// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.CosmosExport.Utility
{
    using System;

    /// <summary>
    ///     cosmos export constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        ///     performance counter category
        /// </summary>
        public const string CounterCategory = "Cosmos Export Processor";

        /// <summary>
        ///     string inserted into the middle of the request manifest when generating the name of the activity log
        ///      file that will hold the set of commands not delievered by command feed
        /// </summary>
        public const string RequestManifestNameDeadLetterInsert = "_MissingCommand_";
        
        /// <summary>
        ///     string inserted into the middle of the request manifest when generating the name of the activity log
        ///      file that will hold the command summary
        /// </summary>
        public const string RequestManifestNameCommandSummaryInsert = "_CommandSummary_";

        /// <summary>
        ///     data file manifest name prefix
        /// </summary>
        public const string DataFileManifestNamePrefix = "DataFileManifest";

        /// <summary>
        ///     request manifest name prefix
        /// </summary>
        public const string RequestManifestNamePrefix = "RequestManifest";

        /// <summary>
        ///     file extension to be added to data file names in the export package
        /// </summary>
        public const string PackageDataFileExtension = ".json";

        /// <summary>
        ///     timeout to use when dequeueing items
        /// </summary>
        public static readonly TimeSpan DequeueTimeout = TimeSpan.FromSeconds(2);
    }
}
