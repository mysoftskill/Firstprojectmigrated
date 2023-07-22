//--------------------------------------------------------------------------------
// <copyright file="AdapterResponse.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Adapters.Common
{
    using System.Collections;
    using System.Globalization;
    using Microsoft.Membership.MemberServices.Common;
    using Microsoft.Membership.MemberServices.Contracts.Exposed;

    /// <summary>
    /// An adapter response indicating an operations success. Use for operations which do not return a result.
    /// </summary>
    public class AdapterResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterResponse"/> class.
        /// </summary>
        public AdapterResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterResponse"/> class.
        /// </summary>
        /// <param name="errorInfo">The error information.</param>
        public AdapterResponse(ErrorInfo errorInfo)
        {
            this.ErrorInfo = errorInfo;
        }

        /// <summary>
        /// Gets or sets the detailed error information (if any).
        /// </summary>
        public ErrorInfo ErrorInfo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this response was a success or failure.
        /// </summary>
        public bool IsSuccess
        {
            get { return this.ErrorInfo == null; }
        }

        /// <summary>
        /// Determines whether this response failed with the specified <paramref name="errorCode"/>.
        /// </summary>
        /// <param name="errorCode">The error code to test for.</param>
        /// <returns>true if this resposne failed; otherwise false.</returns>
        public bool HasError(ErrorCode errorCode)
        {
            return !this.IsSuccess && this.ErrorInfo.ErrorCode == errorCode;
        }

        /// <summary>
        /// Converts the value of this instance to a <see cref="System.String"/>.
        /// </summary>
        /// <returns>A string whose value is the same as this instance.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "(ErrorInfo={0})", this.ErrorInfo);
        }
    }

    /// <summary>
    /// Generic adapter response consisting of a result or an error.
    /// </summary>
    /// <typeparam name="T">The type of result.</typeparam>
    public class AdapterResponse<T> : AdapterResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterResponse"/> class.
        /// </summary>
        public AdapterResponse()
        {
            // TODO: remove once all references are removed
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterResponse"/> class.
        /// </summary>
        /// <param name="result">The operation result.</param>
        public AdapterResponse(T result)
        {
            this.Result = result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterResponse"/> class.
        /// </summary>
        /// <param name="errorInfo">The error information.</param>
        public AdapterResponse(ErrorInfo errorInfo)
        {
            this.ErrorInfo = errorInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterResponse"/> class.
        /// </summary>
        /// <param name="errorCode">The error code.</param>
        /// <param name="errorMessage">The error message.</param>
        public AdapterResponse(ErrorCode errorCode, string errorMessage)
        {
            this.ErrorInfo = new ErrorInfo(errorCode, errorMessage);
        }

        /// <summary>
        /// Gets or sets the operation result.
        /// </summary>
        public T Result { get; set; }

        /// <summary>
        /// Converts the value of this instance to a <see cref="System.String"/>.
        /// </summary>
        /// <returns>A string whose value is the same as this instance.</returns>
        public override string ToString()
        {
            IEnumerable resultEnumerable = this.Result as IEnumerable;
            string resultString = this.Result == null ?
                string.Empty :
                resultEnumerable != null ?
                    resultEnumerable.ToJoinedString() :
                    this.Result.ToString();
            return string.Format(CultureInfo.InvariantCulture, "(Result={0}, base={1})", resultString, base.ToString());
        }
    }
}
