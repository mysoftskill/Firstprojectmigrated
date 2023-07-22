﻿[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Linq;
    using System.Management.Automation;

    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    /// A <c>cmdlet</c> for creating CapabilityIds.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsCapability")]
    public class NewCapabilityCmdlet : Cmdlet
    {
        /// <summary>
        /// The capability string.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public string[] Value { get; set; }

        /// <summary>
        /// Whether to return the data as an array or not.
        /// </summary>
        [Parameter(Position = 1, Mandatory = false)]
        public SwitchParameter Array { get; set; }

        /// <summary>
        /// Processes the current record in the PowerShell pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (this.Array.IsPresent)
            {
                this.WriteObject(this.Value.Select(Policies.Current.Capabilities.CreateId).ToArray());
            }
            else
            {
                foreach (var value in this.Value)
                {
                    this.WriteObject(Policies.Current.Capabilities.CreateId(value));
                }
            }
        }
    }
}