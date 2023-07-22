using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Windows.Services.CompassService.Client.Model;

namespace Microsoft.PrivacyServices.UX.Models.Cms
{
    public interface IBasePageTemplate : IBaseCompassType
    {
        /// <summary>
        /// Gets the page title value.
        /// </summary>
        string PageTitle { get; }

        /// <summary>
        /// Gets a dictionary of the page's strings.
        /// </summary>
        IDictionary<string, string> Strings { get; }
    }
}
