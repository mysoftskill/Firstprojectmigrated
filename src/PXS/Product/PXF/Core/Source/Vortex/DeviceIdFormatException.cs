// <copyright company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
namespace Microsoft.Membership.MemberServices.Privacy.Core.Vortex
{
    using System;

    /// <summary>
    ///     Exception specific to when the device ID is not in the expected format
    /// </summary>
    /// <seealso cref="System.ArgumentException" />
    public class DeviceIdFormatException : ArgumentException
    {
        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Microsoft.Membership.MemberServices.Privacy.Core.Vortex.DeviceIdFormatException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception. If the <paramref name="innerException" /> parameter is not a null reference, the current
        ///     exception is raised in a <see langword="catch" /> block that handles the inner exception.
        /// </param>
        public DeviceIdFormatException(string message, string paramName, Exception innerException)
            : base(message, paramName, innerException)
        {
        }

        /// <inheritdoc />
        /// <summary>
        ///     Initializes a new instance of the <see cref="T:Microsoft.Membership.MemberServices.Privacy.Core.Vortex.DeviceIdFormatException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="paramName">The name of the parameter that caused the current exception.</param>
        public DeviceIdFormatException(string message, string paramName)
            : base(message, paramName)
        {
        }
    }
}
