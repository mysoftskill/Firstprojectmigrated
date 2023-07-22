// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.PrivacyServices.PrivacyOperation.Contracts.PrivacySubject
{
    /// <summary>
    ///     Describes what operation privacy subject will be used for.
    ///     Knowing use context allows for more precise validation of subject data.
    /// </summary>
    public enum SubjectUseContext
    {
        /// <summary>
        ///     Subject data could be used in any context. Data validation might be 
        ///     minimal or non-existent, depending on the subject.
        /// </summary>
        Any,
        /// <summary>
        ///     Subject data will be used for delete operations.
        /// </summary>
        Delete,
        /// <summary>
        ///     Subject data will be used for export operations.
        /// </summary>
        Export
    }
}
