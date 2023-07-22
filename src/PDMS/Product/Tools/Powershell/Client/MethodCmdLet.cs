[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Linq;
    using System.Management.Automation;

    /// <summary>
    /// A <c>cmdlet</c> for setting  properties on PDMS objects.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "PdmsMethod")]
    public class MethodCmdLet : Cmdlet
    {
        /// <summary>
        /// The PDMS object.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        [ValidateNotNull]
        public object Object { get; set; }

        /// <summary>
        /// The PDMS object method name.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        /// <summary>
        /// The method arguments.
        /// </summary>
        [Parameter(Position = 2, Mandatory = false)]
        public object[] Arguments { get; set; }

        /// <summary>
        /// Processes the current record in the PowerShell pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            this.Object = this.GetObject(this.Object);

            if (this.Arguments != null && this.Arguments.Length > 0)
            {
                this.Arguments = this.Arguments.Select(this.GetObject).ToArray();
            }
            else
            {
                this.Arguments = null;
            }

            var method = this.Object.GetType().GetMethod(this.Name);

            if (method.ReturnType == typeof(void))
            {
                method.Invoke(this.Object, this.Arguments);
            }
            else
            {
                this.WriteObject(method.Invoke(this.Object, this.Arguments));
            }
        }

        private object GetObject(object obj)
        {
            var o = obj as PSObject;
            if (o != null)
            {
                return o.BaseObject;
            }

            return obj;
        }
    }
}