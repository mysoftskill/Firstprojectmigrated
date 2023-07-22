namespace Microsoft.PrivacyServices.DataManagement.Client.ServiceTree
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Newtonsoft.Json;

    /// <summary>
    /// Additional helper methods for the IHttpResult type.
    /// </summary>
    public static class IHttpResultModule
    {
        /// <summary>
        /// Get the result or throw an exception if the service call failed.
        /// </summary>
        /// <param name="result">The result object.</param>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="CallerError">
        /// Thrown for issues due to user error.
        /// </exception>
        /// <returns>The result.</returns>
        internal static IHttpResult Get(this IHttpResult result)
        {
            if ((int)result.HttpStatusCode < 400)
            {
                return result;
            }
            else
            {
                var data = new Dictionary<string, object>();
                data["code"] = result.HttpStatusCode.ToString();
                if (string.IsNullOrEmpty(result.ResponseContent))
                {
                    data["message"] = string.Empty;
                }
                else
                {
                    try
                    {
                        // Read the Service Tree Error Response, which has it's own format,
                        // then convert it to the ResponseError form that the pdms client code expects.
                        var stResponseError = JsonConvert.DeserializeObject<ServiceTreeResponseError>(result.ResponseContent);

                        data["message"] = stResponseError?.Message ?? result.ResponseContent;
                        data["target"] = stResponseError?.Details ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceInformation($"ServiceTreeClient: could not deserialize error response: {ex}");

                        // Response was not in json format, just pass the response string as the message.
                        data["message"] = result.ResponseContent;
                    }
                }
                
                ResponseError responseError = new ResponseError(data);
                if ((int)result.HttpStatusCode < 500)
                {
                    var exn = CallerErrorModule.Create(result, responseError);

                    if (exn != null)
                    {
                        throw exn;
                    }
                    else
                    {
                        throw new ServiceFault(result, responseError);
                    }
                }
                else
                {
                    throw new ServiceFault(result, responseError);
                }
            }
        }

        /// <summary>
        /// Get the result or throw an exception if the service call failed.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="result">The result object.</param>
        /// <exception cref="ServiceFault">
        /// Thrown for unknown service responses.
        /// </exception>
        /// <exception cref="CallerError">
        /// Thrown for issues due to user error.
        /// </exception>
        /// <returns>The result.</returns>
        internal static IHttpResult<T> Get<T>(this IHttpResult<T> result)
        {
            return Get((IHttpResult)result) as IHttpResult<T>;
        }

        /// <summary>
        /// Convert this result into a result of another type.
        /// </summary>
        /// <typeparam name="TOriginal">The original result type.</typeparam>
        /// <typeparam name="TNew">The new result type.</typeparam>
        /// <param name="result">The original result object.</param>
        /// <param name="converter">A function to convert the object.</param>
        /// <returns>The converted object as an <see cref="IHttpResult{TNew}" />.</returns>
        internal static IHttpResult<TNew> Convert<TOriginal, TNew>(this IHttpResult<TOriginal> result, Func<IHttpResult<TOriginal>, TNew> converter)
        {
            return (new HttpResult<TNew>(result, converter.Invoke(result))).Get();
        }

        /// <summary>
        /// Convert a collection result into an IEnumerable result.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="result">The original result object.</param>
        /// <returns>The converted object as an <see cref="IHttpResult{T}" />.</returns>
        internal static IHttpResult<IEnumerable<T>> ConvertCollection<T>(this IHttpResult<Collection<T>> result)
        {
            return Convert<Collection<T>, IEnumerable<T>>(result, v => v.Response?.Value);
        }
    }
}
