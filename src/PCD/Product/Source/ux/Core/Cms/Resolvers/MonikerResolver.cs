//-----------------------------------------------------------------------
// <copyright>
//     Copyright (C) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Globalization;
using Microsoft.Osgs.Core.Extensions;
using Microsoft.Osgs.Core.Helpers;
using Microsoft.Windows.Services.CompassService.Client;

namespace Microsoft.PrivacyServices.UX.Core.Cms.Resolvers
{
    /// <summary>
    /// Class returns values for given monikers
    /// </summary>
    public class MonikerResolver : IMonikerResolver
    {
        /// <summary>
        /// Gets the value for the given moniker.
        /// An exception is thrown if the moniker is not found
        /// </summary>
        /// <param name="monikerName">The moniker name to get a value for.</param>
        /// <returns>The value for the moniker.</returns>
        public string GetMonikerValue(string monikerName)
        {
            EnsureArgument.NotNull(monikerName, "monikerName");

            string monikerValue = string.Empty;

            switch (monikerName.ToUpperInvariant())
            {
                case "CLCID":
                    //monikerValue = UserSession.Current.Culture.LCID.ToString(CultureInfo.InvariantCulture);
                    break;

                case "CLCID_HEX":
                    //monikerValue = UserSession.Current.Culture.LCID.ToString("X", CultureInfo.InvariantCulture);
                    break;

                case "LANG":
                case "LOCALE":
                    //monikerValue = UserSession.Current.Culture.Name;
                    break;

                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Did not resolve moniker '{0}'", monikerName));
            }

            return monikerValue;
        }
    }
}