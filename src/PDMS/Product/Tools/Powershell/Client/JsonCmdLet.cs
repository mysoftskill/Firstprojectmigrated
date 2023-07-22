[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Swagger documentation.")]

namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Management.Automation;

    using Microsoft.PrivacyServices.DataManagement.Client.Filters;

    using Newtonsoft.Json;

    /// <summary>
    /// Container class for JSON <c>cmdlets</c>.
    /// </summary>
    public class JsonCmdLet
    {
        /// <summary>
        /// A <c>cmdlet</c> for creating JSON from PDMS client objects.
        /// </summary>
        [Cmdlet(VerbsData.ConvertTo, "PdmsJson")]
        public class ConvertTo : Cmdlet
        {
            /// <summary>
            /// The JSON data.
            /// </summary>
            [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
            [ValidateNotNullOrEmpty]
            public object[] Value { get; set; }
            
            /// <summary>
            /// Processes the current record in the PowerShell pipeline.
            /// </summary>
            protected override void ProcessRecord()
            {
                string data;

                if (this.Value.Length == 1)
                {
                    data = JsonConvert.SerializeObject(this.GetObject(this.Value[0]));
                }
                else
                {
                    data = JsonConvert.SerializeObject(this.Value.Select(this.GetObject).ToArray());
                }
                                
                this.WriteObject(data);
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

        /// <summary>
        /// A <c>cmdlet</c> for creating PDMS client objects from JSON.
        /// </summary>
        [Cmdlet(VerbsData.ConvertFrom, "PdmsJson")]
        public class ConvertFrom : Cmdlet
        {
            /// <summary>
            /// The PDMS client object type.
            /// </summary>
            [Parameter(Position = 0, Mandatory = true)]
            [ValidateNotNullOrEmpty]
            public string Type { get; set; }

            /// <summary>
            /// The JSON data.
            /// </summary>
            [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
            [ValidateNotNullOrEmpty]
            public string Value { get; set; }

            /// <summary>
            /// Whether or not the JSON data is an array.
            /// </summary>
            [Parameter(Position = 2)]
            public SwitchParameter Array { get; set; }

            /// <summary>
            /// Processes the current record in the PowerShell pipeline.
            /// </summary>
            protected override void ProcessRecord()
            {
                var type =
                    typeof(IFilterCriteria).Assembly
                    .GetTypes()
                    .Where(t => t.Name.Equals(this.Type))
                    .Single();

                if (this.Array.IsPresent)
                {
                    var listType = typeof(List<object>);
                    type = listType.GetGenericTypeDefinition().MakeGenericType(type);
                }

                var obj = JsonConvert.DeserializeObject(this.Value, type);

                this.WriteObject(obj);
            }
        }
    }
}