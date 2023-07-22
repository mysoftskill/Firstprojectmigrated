using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    public class SetAgentRelationshipRequest
    {
        public IEnumerable<Relationship> Relationships { get; set; }

        public class Relationship
        {
            public string AssetGroupId { get; set; }

            public string AssetGroupETag { get; set; }

            public IEnumerable<Action> Actions { get; set; }
        }

        public class Action
        {
            public Capability Capability { get; set; }
            public string AgentId { get; set; }
            public ActionVerb Verb { get; set; }
        }

        public enum ActionVerb
        {
            Set,
            Clear
        }

        public enum Capability
        {
            Delete,
            Export
        }
    }
}
