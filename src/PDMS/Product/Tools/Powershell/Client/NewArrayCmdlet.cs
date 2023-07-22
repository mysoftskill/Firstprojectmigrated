[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;

    /// <summary>
    /// A <c>cmdlet</c> for converting PowerShell arrays into .Net IEnumerable.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PdmsArray")]
    public class NewArrayCmdlet : Cmdlet
    {
        /// <summary>
        /// The data type string.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public object[] Values { get; set; }

        /// <summary>
        /// Processes the current record in the PowerShell pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {            
            var type = this.GetObject(this.Values.First()).GetType();
            var listType = typeof(List<object>);
            var genericType = listType.GetGenericTypeDefinition().MakeGenericType(type);

            var list = Activator.CreateInstance(genericType);
            var method = genericType.GetMethod("Add");

            foreach (var o in this.Values)
            {
                method.Invoke(list, new[] { this.GetObject(o) });
            }

            this.WriteObject(list);
        }

        private object GetObject(object obj)
        {
            var o = obj as PSObject;
            if (o != null)
            {
                return o.BaseObject;
            }
            else
            {
                return obj;
            }
        }
    }
}