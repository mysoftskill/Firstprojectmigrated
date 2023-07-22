// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.PrivacyServices.DataMonitor.DataAction.Utility
{
    using Microsoft.PrivacyServices.Common.Context;
    using Microsoft.PrivacyServices.Common.DataModel;
    using Microsoft.PrivacyServices.Common.Exceptions;

    /// <summary>
    ///     Utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        ///     Extracts the specified argument class from the object passed in
        /// </summary>
        /// <typeparam name="T">type of t</typeparam>
        /// <param name="context">execution context</param>
        /// <param name="modelManipulator">model manipulator</param>
        /// <param name="args">action arguments</param>
        /// <returns>resulting value</returns>
        public static T ExtractObject<T>(
            IExecuteContext context,
            IModelManipulator modelManipulator,
            object args)
            where T : class, IValidatable
        {
            T result;

            if (args == null)
            {
                context.LogError("action requires a non-null parameter model");
                throw new ActionExecuteException(context.Tag + " action missing arguments");
            }

            result = modelManipulator.TransformTo<T>(args);

            if (result.ValidateAndNormalize(context) == false)
            {
                context.LogError("did not validate after being sucessfully extracted");
                throw new ActionExecuteException(context.Tag + " did not validate after being sucessfully extracted");
            }

            return result;
        }
    }
}
