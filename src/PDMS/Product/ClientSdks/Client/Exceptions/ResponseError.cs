namespace Microsoft.PrivacyServices.DataManagement.Client
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines the error response for the service.
    /// ResponseError class WILL have a Code and Message.
    /// It MAY have either an InnerError or Details value, but not both.
    /// It MAY have a Target value.
    /// The service contract defines a specific set of ResponseError Codes, and the caller MUST handle all of those. 
    /// The caller MAY choose to key off of InnerError or Detail Codes,
    /// but if the caller does not understand one of those values, it must fall back to the ResponseError Code,
    /// or the most specific InnerError Code that it understands (whichever is available).
    /// </summary>
    /// <remarks>
    /// Based on the ODATA standard and Microsoft OneAPI Guidelines.
    /// <c>https://docs.oasis-open.org/odata/odata-json-format/v4.0/os/odata-json-format-v4.0-os.html#_Toc372793091</c>
    /// </remarks>
    [JsonConverter(typeof(ResponseErrorConverter))]
    public sealed class ResponseError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseError" /> class.
        /// </summary>
        /// <param name="data">The parsed JSON data.</param>
        public ResponseError(IDictionary<string, object> data)
        {
            this.Code = data["code"] as string;
            this.Message = data["message"] as string;


            data.TryGetValue("target", out object value);
            this.Target = value as string;

            if (data.TryGetValue("innererror", out value))
            {
                this.InnerError = new SubError(value as IDictionary<string, object>);
            }
            else if (data.TryGetValue("details", out value))
            {
                this.Details =
                    (from v in (value as IEnumerable<object>)
                     select (new Detail(v as IDictionary<string, object>)))
                    .ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the error code. Callers will key off of this field.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets a friendly error message. Callers should not rely on this field. It is for debugging purposes only.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the source of the error. Optional.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets more specific error information. Should not be combined with details.
        /// </summary>
        public SubError InnerError { get; set; }

        /// <summary>
        /// Gets or sets multiple specific error values. Should not be combined with inner error.
        /// </summary>
        public Detail[] Details { get; set; }

        /// <summary>
        /// Converts the object to a simple string value.
        /// </summary>
        /// <returns>The object as a string.</returns>
        public override string ToString()
        {
            return this.FlattenErrorCode();
        }

        /// <summary>
        /// Iterates through all inner errors and joins the error codes into a single value.
        /// </summary>
        /// <returns>A unified error code.</returns>
        private string FlattenErrorCode()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Code);

            var innerError = this.InnerError;
            while (innerError != null)
            {
                stringBuilder.Append(":");
                stringBuilder.Append(innerError.Code);
                innerError = innerError.InnerError;
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// SubError class MUST contain a Code value.
        /// It MAY contain a nested SubError.
        /// It MAY contain any other service defined types.
        /// </summary>
        public class SubError
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SubError" /> class.
            /// </summary>
            /// <param name="data">The parsed JSOn data.</param>
            internal SubError(IDictionary<string, object> data)
            {
                this.Data = data;

                this.Code = data["code"] as string;


                if (data.TryGetValue("innererror", out object value))
                {
                    this.InnerError = new SubError(value as IDictionary<string, object>);
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SubError" /> class.
            /// Copies the values from the given error.
            /// </summary>
            /// <param name="error">The error to copy.</param>
            protected SubError(SubError error)                
            {
                this.Code = error.Code;
                this.Data = error.Data;
            }

            /// <summary>
            /// Gets or sets the error code.
            /// </summary>
            public string Code { get; set; }

            /// <summary>
            /// Gets or sets the service defined data for this error.
            /// </summary>
            public IDictionary<string, object> Data { get; set; }

            /// <summary>
            /// Gets or sets the nested inner error. This is optional.
            /// </summary>
            public SubError InnerError { get; set; }
        }

        /// <summary>
        /// Detail class MUST contain a Code value and Message value.
        /// It MAY contain a Target value, but no other properties.
        /// </summary>
        public sealed class Detail
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="Detail" /> class.
            /// </summary>
            /// <param name="data">The parsed JSOn data.</param>
            internal Detail(IDictionary<string, object> data)
            {
                this.Code = data["code"] as string;
                this.Message = data["message"] as string;


                data.TryGetValue("target", out object value);
                this.Target = value as string;
            }

            /// <summary>
            /// Gets or sets the error code.
            /// </summary>
            public string Code { get; set; }

            /// <summary>
            /// Gets or sets the friendly message.
            /// </summary>
            public string Message { get; set; }

            /// <summary>
            /// Gets or sets the target of the error. This is an optional value.
            /// </summary>
            public string Target { get; set; }
        }
    }
}