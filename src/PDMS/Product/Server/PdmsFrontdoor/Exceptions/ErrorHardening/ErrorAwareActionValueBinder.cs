namespace Microsoft.PrivacyServices.DataManagement.Frontdoor.Exceptions.ErrorHardening
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http.Controllers;
    using System.Web.Http.ModelBinding;

    /// <summary>
    /// Wraps action binding with a controlled error handler.
    /// </summary>
    public class ErrorAwareActionValueBinder : DefaultActionValueBinder
    {
        /// <summary>
        /// Wraps the binding in a controlled error handler.
        /// </summary>
        /// <param name="actionDescriptor">The action descriptor.</param>
        /// <returns>The binding.</returns>
        public override HttpActionBinding GetBinding(HttpActionDescriptor actionDescriptor)
        {
            var binding = base.GetBinding(actionDescriptor);
            return new ErrorAwareActionBinding(binding);
        }

        /// <summary>
        /// Wraps an action binding with a controlled error handler.
        /// </summary>
        private class ErrorAwareActionBinding : HttpActionBinding
        {
            private HttpActionBinding originalBinding;

            /// <summary>
            /// Initializes a new instance of the <see cref="ErrorAwareActionBinding" /> class.
            /// </summary>
            /// <param name="originalBinding">The original binding.</param>
            public ErrorAwareActionBinding(HttpActionBinding originalBinding)
            {
                this.originalBinding = originalBinding;
                this.ActionDescriptor = originalBinding.ActionDescriptor;
                this.ParameterBindings = originalBinding.ParameterBindings;
            }

            /// <summary>
            /// Executes the binding.
            /// </summary>
            /// <param name="actionContext">The action context.</param>
            /// <param name="cancellationToken">The cancellation token.</param>
            /// <returns>A void task.</returns>
            public override async Task ExecuteBindingAsync(HttpActionContext actionContext, CancellationToken cancellationToken)
            {
                try
                {
                    await this.originalBinding.ExecuteBindingAsync(actionContext, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    throw new InvalidRequestError(ex.Message);
                }
            }
        }
    }
}