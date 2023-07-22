// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.  
// </copyright>

namespace Microsoft.Membership.MemberServices.Common.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;

    public static class ArgumentCheck
    {
        /// <summary>
        ///     Used to validate a parameter
        /// </summary>
        /// <param name="value">object value</param>
        /// <param name="name">parameter name</param>
        /// <param name="message">optional message for the exception. If not specified, a default message will be used</param>
        /// <param name="callerMember">The name of the method calling this method</param>
        /// <param name="callerFile">The full path name of the file containing the calling method</param>
        public static void ThrowIfNull(
            object value,
            string name,
            string message = null,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFile = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name, ArgumentCheck.GetCallerMessage(callerMember, callerFile, message));
            }
        }

        /// <summary>
        ///     Used to validate a parameter
        /// </summary>
        /// <param name="value">string value</param>
        /// <param name="name">parameter name</param>
        /// <param name="message">optional message for the exception. If not specified, a default message will be used</param>
        /// <param name="callerMember">name of the method calling this method</param>
        /// <param name="callerFile">full path name of the file containing the calling method</param>
        public static void ThrowIfNullEmptyOrWhiteSpace(
            string value,
            string name,
            string message = null,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFile = null)
        {
            if (value == null)
            {
                string suffix = ArgumentCheck.GetCallerMessage(callerMember, callerFile);
                string msgFull = (message ?? string.Empty) + suffix;
                throw new ArgumentNullException(name, msgFull);
            }

            if (value.Length == 0 || value.Trim().Length == 0)
            {
                throw new ArgumentException(
                    ArgumentCheck.GetCallerMessage(
                        callerMember, 
                        callerFile, 
                        message ?? $"Parameter cannot be empty or consist of just whitespace. "),
                    name);
            }
        }

        /// <summary>
        ///     Used to validate a parameter
        /// </summary>
        /// <param name="value">string value</param>
        /// <param name="name">parameter name</param>
        /// <param name="message">optional message for the exception. If not specified, a default message will be used</param>
        /// <param name="callerMember">name of the method calling this method</param>
        /// <param name="callerFile">full path name of the file containing the calling method</param>
        public static string ReturnIfNotNullEmptyOrWhiteSpace(
            string value,
            string name,
            string message = null,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFile = null)
        {
            // ReSharper disable once ExplicitCallerInfoArgument
            ArgumentCheck.ThrowIfNullEmptyOrWhiteSpace(value, name, message, callerMember, callerFile);
            return value;
        }

        /// <summary>Used to validate a parameter</summary>
        /// <param name="value">parameter value</param>
        /// <param name="name">parameter name</param>
        /// <param name="message">optional message for the exception. If not specified, a default message will be used</param>
        /// <typeparam name="T">type of parameter to validate</typeparam>
        /// <param name="callerMember">The name of the method calling this method</param>
        /// <param name="callerFile">The full path name of the file containing the calling method</param>
        public static void ThrowIfEmptyOrNull<T>(
            IEnumerable<T> value,
            string name,
            string message = null,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFile = null) 
        {
            if (value == null || value.Any() == false)
            {
                throw new ArgumentException(
                    ArgumentCheck.GetCallerMessage(
                        callerMember,
                        callerFile,
                        message ?? $"Collection may not be empty or null. "),
                    name);
            }
        }

        /// <summary>Used to validate a parameter</summary>
        /// <param name="value">parameter value</param>
        /// <param name="minValue">minimum value</param>
        /// <param name="name">parameter name</param>
        /// <param name="message">optional message for the exception. If not specified, a default message will be used</param>
        /// <typeparam name="T">type of parameter to validate</typeparam>
        /// <param name="callerMember">The name of the method calling this method</param>
        /// <param name="callerFile">The full path name of the file containing the calling method</param>
        public static void ThrowIfLessThan<T>(
            T value,
            T minValue,
            string name,
            string message = null,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFile = null) 
            where T : IComparable<T>
        {
            if (value.CompareTo(minValue) < 0)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    ArgumentCheck.GetCallerMessage(
                        callerMember,
                        callerFile,
                        message ?? $"Parameter value of {value} is less than the minimum value of {minValue}. "));
            }
        }

        /// <summary>Used to validate a parameter</summary>
        /// <param name="value">parameter value</param>
        /// <param name="minValue">minimum value</param>
        /// <param name="name">parameter name</param>
        /// <param name="message">optional message for the exception. If not specified, a default message will be used</param>
        /// <typeparam name="T">type of parameter to validate</typeparam>
        /// <param name="callerMember">The name of the method calling this method</param>
        /// <param name="callerFile">The full path name of the file containing the calling method</param>
        public static void ThrowIfLessThanOrEqualTo<T>(
            T value,
            T minValue,
            string name,
            string message = null,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFile = null) 
            where T : IComparable<T>
        {
            if (value.CompareTo(minValue) <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    name,
                    ArgumentCheck.GetCallerMessage(
                        callerMember,
                        callerFile,
                        message ?? $"Parameter value of {value} is less than or equal to the minimum value of {minValue}. "));
            }
        }

        /// <summary>Used to validate a parameter</summary>
        /// <param name="value">parameter value</param>
        /// <param name="minValue">minimum value</param>
        /// <param name="name">parameter name</param>
        /// <param name="message">optional message for the exception. If not specified, a default message will be used</param>
        /// <typeparam name="T">type of parameter to validate</typeparam>
        /// <param name="callerMember">The name of the method calling this method</param>
        /// <param name="callerFile">The full path name of the file containing the calling method</param>
        public static T ReturnIfLessThanOrEqualToElseThrow<T>(
            T value,
            T minValue,
            string name,
            string message = null,
            [CallerMemberName] string callerMember = null,
            [CallerFilePath] string callerFile = null)
            where T : IComparable<T>
        {
            // ReSharper disable once ExplicitCallerInfoArgument
            ArgumentCheck.ThrowIfLessThanOrEqualTo(value, minValue, name, message, callerMember, callerFile);
            return value;
        }

        /// <summary>
        ///     Returns information about the calling method to the message
        /// </summary>
        /// <param name="messagePrefix">message prefix to append caller info to</param>
        /// <param name="callerMethod">name of method that called this class</param>
        /// <param name="callerFile">full path and filename for the file that contains the calling class</param>
        /// <returns>message with the caller information appended</returns>
        private static string GetCallerMessage(
            string callerMethod,
            string callerFile,
            string messagePrefix)
        {
            return messagePrefix != null ?
                $"{messagePrefix} Occurred in {Path.GetFileNameWithoutExtension(callerFile)}.{callerMethod}" :
                $"Occurred in {Path.GetFileNameWithoutExtension(callerFile)}.{callerMethod}";
        }

        /// <summary>
        ///     Returns information about the calling method to the message
        /// </summary>
        /// <param name="callerMethod">name of method that called this class</param>
        /// <param name="callerFile">full path and filename for the file that contains the calling class</param>
        /// <returns>message with the caller information appended</returns>
        private static string GetCallerMessage(
            string callerMethod,
            string callerFile)
        {
            return ArgumentCheck.GetCallerMessage(callerFile, callerMethod, null);
        }
    }
}
