using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using PdmsIdentityModels = Microsoft.PrivacyServices.Identity;

namespace Microsoft.PrivacyServices.UX.Models.Pdms
{
    /// <summary>
    /// Represents a data asset group qualifier.
    /// </summary>
    public class AssetGroupQualifier
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupQualifier"/> class.
        /// </summary>
        public AssetGroupQualifier()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetGroupQualifier"/> class.
        /// </summary>
        /// <param name="properties">Property bag to initialize <see cref="Properties"/> property.</param>
        public AssetGroupQualifier(IDictionary<string, string> properties)
        {
            Properties = new Dictionary<string, string>(properties) ?? throw new ArgumentNullException(nameof(properties));

            if (Properties.Any())
            {
                try
                {
                    var qualifier = PdmsIdentityModels.AssetQualifier.CreateFromDictionary(Properties);
                    var dataGridSearch = PdmsIdentityModels.DataGridSearch.CreateFromAssetQualifier(qualifier, "", "");

                    DataGridLink = dataGridSearch.DataGridSearchUri.ToString();
                }
                catch
                {
                    //  Do nothing, it's a best-effort attempt.
                }
            }
        }

        /// <summary>
        /// Gets or sets property bag associated with the asset group qualifier.
        /// </summary>
        [JsonProperty("props")]
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        /// Gets or sets a link to find the asset in DataGrid.
        /// </summary>
        public string DataGridLink { get; set; }
    }
}
