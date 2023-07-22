// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Privacy.GenevaTest.Common
{
    using System.Collections.Generic;

    using Microsoft.Membership.MemberServices.Privacy.Core.Helpers.FileSystem;
    using Microsoft.Azure.Cosmos.Table;

    /// <summary>
    ///     state for processing a 
    /// </summary>
    public class FileSetState : TableEntity
    {
        /// <summary>
        ///     Gets or sets the names of the files in the file set
        /// </summary>
        public ICollection<string> FileNames { get; set; }

        /// <summary>
        ///     Gets or sets agent id
        /// </summary>
        public string AgentId { get; set; }

        /// <summary>
        ///     Gets or sets request file
        /// </summary>
        public IFile RequestManifest { get; set; }

        /// <summary>
        ///     Gets or sets request file
        /// </summary>
        public IFile DataManifest { get; set; }
    }
}
