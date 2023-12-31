namespace Microsoft.PrivacyServices.CommandFeed.Service.Frontdoor
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.PrivacyServices.CommandFeed.Service.Common;
    using Microsoft.PrivacyServices.Common.Azure;
    using Microsoft.Windows.Services.AuthN.Server;

    /// <summary>
    /// A base HTTP action result (ie, work item).
    /// </summary>
    public abstract class BaseHttpActionResult : IHttpActionResult
    {
        /// <summary>
        /// Provides top-line exception handling for requests.
        /// </summary>
        public async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                CancellationToken? processCancellationToken = PrivacyApplication.Instance?.CancellationToken;
                if (processCancellationToken != null)
                {
                    // If we have a cancellation token that represents the process shutting down,
                    // then create a linked token source between what ASP.NET gives us as a paramter to this method,
                    // and the overall cancellation token for the process. That way, when either of these is true,
                    // the request observes that things are cancelled. This is not that useful for fast-twitch API calls,
                    // but it can be impactful for something like GetCommands, which can take up to 25 seconds.
                    using (CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, processCancellationToken.Value))
                    {
                        return await this.ExecuteInnerAsync(source.Token);
                    }
                }
                else
                {
                    // If there is no process-wide cancellation source, then just use the parameter to this method.
                    return await this.ExecuteInnerAsync(cancellationToken);
                }
            }
            catch (AuthNException ex)
            {
                DualLogger.Instance.Error(nameof(BaseHttpActionResult), ex, "AuthNException caught");
                return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized)
                {
                    Content = new StringContent(ex.Message)
                };
            }
            catch (BadRequestException ex)
            {
                IncomingEvent.Current?.SetException(ex);

                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
                {
                    Content = ex.ResponseContent ?? new StringContent("Bad request: " + ex.Message)
                };
            }
            catch (OperationCanceledException)
            {
                // HTTP code 499 is used by NGINX to signal a client-canceled request.
                return new HttpResponseMessage((System.Net.HttpStatusCode)499);
            }
            catch (HttpResponseException ex)
            {
                return new HttpResponseMessage(ex.Response?.StatusCode ?? HttpStatusCode.InternalServerError)
                {
                    Content = ex.Response?.Content ?? new StringContent($"An unknown error occurred: {ex.Message}")
                };
            }
        }

        /// <summary>
        /// Specifies how the request is to be processed.
        /// </summary>
        protected abstract Task<HttpResponseMessage> ExecuteInnerAsync(CancellationToken cancellationToken);
    }
}
