// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.Common.DataModel
{
    using System;

    using Microsoft.PrivacyServices.Common.Context;

    /// <summary>
    ///     provides read only access to extension property models
    /// </summary>
    public class ExtensionPropertyModelReader : IModelReader
    {
        private static readonly char[] Separator = { '.' };

        /// <summary>
        ///     extracts a value from the model
        /// </summary>
        /// <typeparam name="T">type of result value</typeparam>
        /// <param name="context">model context</param>
        /// <param name="source">model to extract a value from</param>
        /// <param name="selector">path to value</param>
        /// <param name="defaultValue">default value to assign if the value could not be found</param>
        /// <param name="result">receives the resulting value if found</param>
        /// <returns>true if the requested object could be found, false otherwise</returns>
        public bool TryExtractValue<T>(
            IContext context,
            object source,
            string selector,
            T defaultValue,
            out T result)
        {
            string[] props = null;

            if (string.IsNullOrWhiteSpace(selector) == false)
            {
                props = selector.Split(ExtensionPropertyModelReader.Separator, StringSplitOptions.RemoveEmptyEntries);
            }

            if (props != null &&
                props.Length == 3 &&
                props[0].Length == 1 &&
                props[0][0] == '@')
            {
                string local = context.GetExtensionPropertyValue(props[1].Trim(), props[2].Trim());
                if (local != null)
                {
                    result = (T)(object)local;
                    return true;
                }
            }

            result = defaultValue;
            return false;
        }
    }
}
