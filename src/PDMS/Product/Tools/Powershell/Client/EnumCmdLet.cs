[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System;
    using System.Linq;
    using System.Management.Automation;

    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    /// <summary>
    /// A <c>cmdlet</c> for creating PDMS client enum values.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsEnum")]
    public class EnumCmdLet : Cmdlet
    {
        /// <summary>
        /// The PDMS client enum type.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Type { get; set; }

        /// <summary>
        /// The PDMS client enum value.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Value { get; set; }

        /// <summary>
        /// Processes the current record in the PowerShell pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            var type = 
                typeof(IFilterCriteria).Assembly
                .GetTypes()
                .Where(t => t.IsEnum && t.Name.Equals(this.Type))
                .Single();
            
            var instance = Enum.Parse(type, this.Value);

            this.WriteObject(instance);
        }
    }
}