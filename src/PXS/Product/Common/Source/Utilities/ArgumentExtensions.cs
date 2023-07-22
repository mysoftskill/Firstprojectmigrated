// <copyright>
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;

    public static class ArgumentExtensions
    {
        /// <summary>
        /// Throws an exception if the argument is found null.
        /// </summary>
        /// <param name="arg">The argument to check.</param>
        /// <param name="paramName">The name of the parameter that is being validated.</param>
        /// <param name="message">The optional error message that explains the reason for the failure..</param>
        /// <exception cref="System.ArgumentNullException">Thrown if arg is null.</exception>
        public static void EnsureNotNull(this object arg, string paramName, string message = null)
        {
            if (arg == null)
            {
                if (message == null)
                {
                    throw new ArgumentNullException(paramName);
                }
                else
                {
                    throw new ArgumentNullException(paramName, message);
                }
            }
        }

        /// <summary>
        /// Throws an exception if the string argument is found null or empty.
        /// </summary>
        /// <param name="arg">The argument to check.</param>
        /// <param name="paramName">The name of the parameter that is being validated.</param>
        /// <param name="message">The optional error message that explains the reason for the failure.</param>
        public static void EnsureNotNullOrEmpty(this string arg, string paramName, string message = null)
        {
            arg.EnsureNotNull(paramName);

            // Test for null first to isolate the is-empty case
            if (string.IsNullOrEmpty(arg))
            {
                if (message == null)
                {
                    throw new ArgumentException(message, paramName);
                }
                else
                {
                    throw new ArgumentException("argument cannot be empty", paramName);
                }
            }
        }
    }
}
