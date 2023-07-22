[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Linq;
    using System.Management.Automation;

    using Microsoft.PrivacyServices.Identity;

    /// <summary>
    /// A <c>cmdlet</c> for creating asset qualifiers.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsAssetQualifier")]
    public class NewAssetQualifierCmdlet : Cmdlet
    {
        /// <summary>
        /// The asset qualifier string.
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
                this.WriteObject(this.Value.Select(AssetQualifier.Parse).ToArray());
            }
            else
            {
                foreach (var value in this.Value)
                {
                    this.WriteObject(AssetQualifier.Parse(value));
                }
            }
        }
    }
}