// ---------------------------------------------------------------------------
// <copyright file="Templates.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.UnitTests.Integration
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.TemplateBuilder;

    public static class TestTemplateStore
    {
        public static List<TemplateDef> Templates = 
            new List<TemplateDef>
            {
                new TemplateDef { Tag = "KustoQuery", Text = TestTemplateStore.KustoQueryText.ReplaceNewLines() },
                new TemplateDef { Tag = "IncidentBody", Text = TestTemplateStore.IncidentBodyText.ReplaceNewLines() },
                new TemplateDef { Tag = "EmailBody", Text = TestTemplateStore.EmailBodyText.ReplaceNewLines() },
            };

        private const string KustoQueryText =
            @"AgentInfoTable | where AgentId !in ([[<var s:'$.Consts.ExcludedAgents' f:n0 var>]]) | project AgentId";

        private const string IncidentBodyText =
@"
[[<var s:'$.AgentInfo.AgentId' var>]] is not doing stuff well
";

        private const string EmailBodyText =
@"
Incidents filed: 
<ul>
[[<foreach sel:'$.Incidents' foreach>]]
<li>[[<var s:'AgentId' var>]]: [[<var s:'IncidentId' var>]] ([[<var s:'IncidentStatusText' var>]])</li>
[[<foreachend>]]
</ul>
</p>
";

        private static string ReplaceNewLines(this string s)
        {
            return s?.Replace("\r\n", string.Empty);
        }
    }
}
