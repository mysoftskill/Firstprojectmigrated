//--------------------------------------------------------------------------------
// <copyright file="InnerErrorDetails.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation, all rights reserved.
// </copyright>
//--------------------------------------------------------------------------------

namespace Microsoft.Membership.MemberServices.Contracts.Exposed
{
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Inner Error Details
    /// </summary>
    public class InnerErrorDetails
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InnerErrorDetails"/> class.
        /// </summary>
        /// <param name="code">The inner-error-code.</param>
        /// <param name="message">The inner-error-message.</param>
        /// <param name="innerDetails">The inner-error-details.</param>
        [JsonConstructor]
        public InnerErrorDetails(string code, string message, InnerErrorDetails innerDetails)
        {
            this.Code = code;
            this.Message = message;
            this.InnerDetails = innerDetails;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InnerErrorDetails"/> class.
        /// </summary>
        /// <param name="code">The inner-error-code.</param>
        /// <param name="message">The inner-error-message.</param>
        public InnerErrorDetails(string code, string message)
        {
            this.Code = code;
            this.Message = message;
        }

        /// <summary>
        /// Gets the inner-error-code.
        /// </summary>
        [JsonProperty("Code")]
        public string Code { get; private set; }

        /// <summary>
        /// Gets the inner-error-message.
        /// </summary>
        [JsonProperty("Message")]
        public string Message { get; private set; }

        /// <summary>
        /// Gets the inner-error-details.
        /// </summary>
        [JsonProperty("InnerErrorDetails")]
        public InnerErrorDetails InnerDetails { get; private set; }

        /// <summary>
        /// Returns all of the codes nested in within InnerDetails concatenated by periods.
        /// </summary>
        public string FlattenedCode
        {
            get
            {
                StringBuilder codes = new StringBuilder();

                codes.Append(Code);

                var innerDetails = InnerDetails;
                while (innerDetails != null)
                {
                    codes.Append(".");
                    codes.Append(innerDetails.Code);

                    innerDetails = innerDetails.InnerDetails;
                }

                return codes.ToString();
            }            
        }

        /// <summary>
        /// Adds the details to the end of the InnerDetail nested list.
        /// </summary>
        /// <param name="details">Details to add</param>
        public void AddInnerErrorDetails(InnerErrorDetails details)
        {
            var leafInnerDetails = this;
            while (leafInnerDetails.InnerDetails != null)
            {
                leafInnerDetails = leafInnerDetails.InnerDetails;
            }

            leafInnerDetails.InnerDetails = details;
        }
    }
}