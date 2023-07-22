[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Management.Automation;

    /// <summary>
    /// A <c>cmdlet</c> for setting  properties on PDMS objects.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "PdmsProperty")]
    public class PropertyCmdLet : Cmdlet
    {
        /// <summary>
        /// The PDMS object.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public object Object { get; set; }

        /// <summary>
        /// The PDMS object property name.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// The PDMS object property value.
        /// </summary>
        [Parameter(Position = 2)]
        public object Value { get; set; }

        /// <summary>
        /// The PDMS object property value.
        /// </summary>
        [Parameter(Position = 3, Mandatory = false)]
        public object Index { get; set; }

        /// <summary>
        /// Processes the current record in the PowerShell pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            var o = this.Object as PSObject;
            if (o != null)
            {
                this.Object = o.BaseObject;
            }

            var v = this.Value as PSObject;
            if (v != null)
            {
                this.Value = v.BaseObject;
            }
            
            var property = this.Object.GetType().GetProperty(this.Name);
            
            if (this.Index != null)
            {
                var i = this.Index as PSObject;
                if (i != null)
                {
                    this.Index = i.BaseObject;
                }

                property.SetValue(this.Object, this.Value, new[] { this.Index });
            }
            else
            {
                property.SetValue(this.Object, this.Value, null);
            }
        }
    }
}