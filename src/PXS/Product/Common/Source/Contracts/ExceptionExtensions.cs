//--------------------------------------------------------------------------------
// <copyright file="ExceptionExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Common
{
    using System;
    using System.Globalization;
    using System.Reflection;

    /// <summary>
    /// Miscellaneous <seealso cref="Exception"/> extensions.
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Used to freeze stack trace of an exception before re-throwing a stored exception.
        /// If this method is not called before re-throwing a stored exception, the original stack trace of the exception will be lost.
        /// </summary>
        /// <param name="ex">The exception whose stack trace is to be frozen.</param>
        public static void PrepareForRemoting(this Exception ex)
        {
            typeof(Exception).InvokeMember(
                "PrepForRemoting",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod,
                null,
                ex,
                new object[0],
                CultureInfo.InvariantCulture);
        }

        public static T FindInnerException<T>(this Exception ex)
            where T : Exception
        {
            if (ex == null)
                return null;
            var result = ex as T;
            if (result != null)
                return result;

            var aggEx = ex as AggregateException;
            if (aggEx != null)
            {
                foreach (var inner in aggEx.InnerExceptions)
                {
                    result = FindInnerException<T>(inner);
                    if (result != null)
                        return result;
                }
                return null;
            }

            return FindInnerException<T>(ex.InnerException);
        }
    }
}
