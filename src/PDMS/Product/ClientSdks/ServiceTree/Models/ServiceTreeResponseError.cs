namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Newtonsoft.Json;

    /// <summary>
    /// Defines the error response for the service tree client.
    /// ServiceTreeResponseError class has two strings: Message and Details.
    /// </summary>
    [JsonConverter(typeof(ServiceTreeResponseErrorConverter))]
    public sealed class ServiceTreeResponseError
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceTreeResponseError" /> class.
        /// </summary>
        /// <param name="data">The parsed JSON data.</param>
        public ServiceTreeResponseError(IDictionary<string, object> data)
        {
            this.Message = data["Message"] as string;

            object value = null;
            if (data.TryGetValue("Details", out value))
            {
                this.Details = value as string;
            }
        }

        /// <summary>
        /// Gets or sets a friendly error message. 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets additional details about the error.
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// Converts the object to a simple string value.
        /// </summary>
        /// <returns>The object as a string.</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Message);

            if (this.Details != null) 
            { 
                stringBuilder.Append(":");
                stringBuilder.Append(this.Details);
            }

            return stringBuilder.ToString();
        }
    }
}