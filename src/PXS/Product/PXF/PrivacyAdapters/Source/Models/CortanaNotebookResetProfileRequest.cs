// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.PrivacyAdapters.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Class to represent empty profile
    /// Delete all Cortana Notebook user features works as resetting profile. Since, we always clear the interests but won't add any default data, we just have to pass an empty profile request.
    /// </summary>
    public class CortanaNotebookResetProfileRequest
    {
        public IList<string> CategoryIds => new List<string>();

        public bool Configure => false;

        public bool PrivateData => true;

        /// <summary>
        /// Set to 'false' to skip creating default values.
        /// </summary>
        public bool CreateDefaults => false;
    }
}