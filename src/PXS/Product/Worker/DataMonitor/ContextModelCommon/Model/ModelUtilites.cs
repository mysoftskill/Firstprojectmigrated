// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using System.Collections.Generic;

    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     utilities used by the DataAction data class validators
    /// </summary>
    public static class ModelUtilites
    {
        /// <summary>
        ///     Validates the argument map
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="context">parse context to log errors into</param>
        /// <param name="map">model value map to validate</param>
        /// <returns>true if the object validated ok; false otherwise</returns>
        public static bool ValidateModelValueMap<T>(
            IContext context,
            IEnumerable<KeyValuePair<string, T>> map)
            where T : ModelValue
        {
            bool result = true;

            if (map != null)
            {
                int i = 0;

                foreach (KeyValuePair<string, T> p in map)
                {
                    string name = p.Key ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        context.LogError($"target variable [{name}] (list index {i}): name is null or empty");
                        result = false;
                    }

                    if (p.Value != null)
                    {
                        int idxLocal = i;

                        context.PushErrorIntroMessage(() => $"target variable [{name}] (list index {idxLocal}) errors found:");

                        result = p.Value.ValidateAndNormalize(context) && result;

                        context.PopErrorIntroMessage();
                    }
                    else
                    {
                        context.LogError($"target variable [{name}] (list index {i}): null model values are not permitted");
                        result = false;
                    }

                    ++i;
                }
            }

            return result;
        }
    }
}
