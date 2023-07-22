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
    ///     represents the contents of the action store
    /// </summary>
    public class ActionStoreContents
    {
        /// <summary>
        ///     Gets or sets action references
        /// </summary>
        public ICollection<ActionRefRunnable> ActionReferences { get; set; }

        /// <summary>
        ///     Gets or sets templates
        /// </summary>
        public ICollection<TemplateDef> Templates { get; set; }

        /// <summary>
        ///     Gets or sets actions
        /// </summary>
        public ICollection<ActionDef> Actions { get; set; }
    }
}
