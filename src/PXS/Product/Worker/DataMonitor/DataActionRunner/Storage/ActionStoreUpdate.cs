// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.Runner.Storage
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.TemplateBuilder;
    using Microsoft.PrivacyServices.DataMonitor.DataAction.Data;
    using Microsoft.PrivacyServices.DataMonitor.Runner.Data;

    /// <summary>
    ///     represents an update to the action store
    /// </summary>
    public class ActionStoreUpdate
    {
        /// <summary>
        ///     Gets or sets the set of new or updated action references
        /// </summary>
        public ICollection<ActionRefRunnable> ActionReferenceUpdates { get; set; }

        /// <summary>
        ///     Gets or sets the set of ids of action reference to delete
        /// </summary>
        public ICollection<string> ActionReferenceDeletes { get; set; }

        /// <summary>
        ///     Gets or sets the set of new or updated templates
        /// </summary>
        public ICollection<TemplateDef> TemplateUpdates { get; set; }

        /// <summary>
        ///     Gets or sets the set of tags of template to delete
        /// </summary>
        public ICollection<string> TemplateDeletes { get; set; }

        /// <summary>
        ///     Gets or sets the set of new or updated actions
        /// </summary>
        public ICollection<ActionDef> ActionUpdates { get; set; }

        /// <summary>
        ///     Gets or sets the set of tags of action to delete
        /// </summary>
        public ICollection<string> ActionDeletes { get; set; }
    }
}
