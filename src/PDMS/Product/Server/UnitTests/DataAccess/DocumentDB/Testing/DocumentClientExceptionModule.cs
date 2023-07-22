namespace Microsoft.PrivacyServices.DataManagement.DataAccess.DocumentDb.UnitTest
{
    using System;
    using System.Net;
    using System.Reflection;

    using Microsoft.Azure.Documents;

    /// <summary>
    /// Contains helpful static functions for working with DocumentClientExceptions in test code.
    /// </summary>
    public static class DocumentClientExceptionModule
    {
        /// <summary>
        /// Create a new DocumentClientException using reflection.
        /// </summary>
        /// <param name="httpStatusCode">The http status code for the error.</param>
        /// <param name="error">The error details.</param>
        /// <returns>A DocumentClientException.</returns>
        public static DocumentClientException Create(HttpStatusCode httpStatusCode, Error error = null)
        {
            if (error == null)
            {
                error = new Error
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = "MockCode",
                    Message = "MockMessage"
                };
            }

            var type = typeof(DocumentClientException);

            // We are using the overload with 3 parameters (error, responseheaders, statuscode).
            // We can change this if different values are needed.
            var documentClientExceptionInstance =
                type.Assembly.CreateInstance(
                    type.FullName,
                    false,
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new object[] { error, null, httpStatusCode },
                    null,
                    null);

            return (DocumentClientException)documentClientExceptionInstance;
        }
    }
}