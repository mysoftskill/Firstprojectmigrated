// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Helpers.Aggregation
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.PrivacyServices.Policy;

    /// <summary>
    ///     Class containing logic to segregate Data Types from the Policy
    /// </summary>
    public class DataTypesClassifier
    {
        private readonly IList<string> timeLineSupportedDataTypes;

        /// <summary>
        ///     DataTypes Classifier based on Provided provided
        /// </summary>
        /// <param name="privacyPolicy"></param>
        public DataTypesClassifier(Policy privacyPolicy)
        {
            if (privacyPolicy == null)
                throw new ArgumentNullException(nameof(privacyPolicy));

            this.timeLineSupportedDataTypes = new List<string>
            {
                privacyPolicy.DataTypes.Ids.ProductAndServiceUsage.Value,
                privacyPolicy.DataTypes.Ids.InkingTypingAndSpeechUtterance.Value,
                privacyPolicy.DataTypes.Ids.BrowsingHistory.Value,
                privacyPolicy.DataTypes.Ids.SearchRequestsAndQuery.Value,
                privacyPolicy.DataTypes.Ids.PreciseUserLocation.Value,
                privacyPolicy.DataTypes.Ids.ContentConsumption.Value
            };
        }

        /// <summary>
        ///     Segregate Time line supported and PCF supported
        /// </summary>
        /// <param name="dataTypesList">Data Types List which needs segregation</param>
        /// <param name="dataTypesForTimeline">Data Types List for Timeline processing</param>
        /// <param name="dataTypesForPcf">Data Types List for PCF processing</param>
        public void Classify(IList<string> dataTypesList, out IList<string> dataTypesForTimeline, out IList<string> dataTypesForPcf)
        {
            if (!dataTypesList.Any() || dataTypesList == null)
                throw new ArgumentNullException(nameof(dataTypesList));

            dataTypesForTimeline = new List<string>();
            dataTypesForPcf = new List<string>();

            foreach (var dataType in dataTypesList)
            {
                if (this.timeLineSupportedDataTypes.Contains(dataType))
                {
                    dataTypesForTimeline.Add(dataType);
                }
                else
                {
                    dataTypesForPcf.Add(dataType);
                }
            }
        }
    }
}
