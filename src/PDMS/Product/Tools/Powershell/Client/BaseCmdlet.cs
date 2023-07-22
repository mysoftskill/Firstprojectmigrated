namespace Microsoft.PrivacyServices.DataManagement.Client.Powershell
{
    using System;
    using System.Management.Automation;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    /// <summary>
    /// A base class that adds support for running generic client methods.
    /// </summary>
    /// <typeparam name="T">The response type for the client action represented by this <c>cmdlet</c>.</typeparam>
    /// <typeparam name="V">The client type.</typeparam>
    public abstract class BaseCmdlet<T, V> : Cmdlet where T : class, IHttpResult
    {
        /// <summary>
        /// Gets or sets an optional warning message to write when an issue occurs.
        /// </summary>
        protected string WarningMessage { get; set; }

        /// <summary>
        /// Performs validation before executing any steps.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (ServiceCmdlet.DataManagementClient == null)
            {
                throw new InvalidOperationException("Must connect to the service by calling Connect-PdmsService");
            }
        }
        
        /// <summary>
        /// Processes the current record in the PowerShell pipeline.
        /// </summary>
        protected override void ProcessRecord()
        {
            // Clear any previous messages.
            this.WarningMessage = null;
            
            this.Execute();
        }

        /// <summary>
        /// Execute custom behavior in the non-generated classes.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The data from the custom call or null if no action was taken..</returns>
        protected virtual Task<T> CustomExecuteAsync(V client, RequestContext context)
        {
            return Task.FromResult<T>(null);
        }

        /// <summary>
        /// Executes the specific client call for this <c>cmdlet</c>.
        /// </summary>
        /// <param name="client">The PDMS client.</param>
        /// <param name="context">The request context.</param>
        /// <returns>The result of the call.</returns>
        protected abstract Task<T> ExecuteAsync(V client, RequestContext context);

        /// <summary>
        /// Write the result of the action to the PowerShell pipeline.
        /// </summary>
        /// <param name="result">The result.</param>
        protected abstract void WriteResult(T result);

        /// <summary>
        /// Get the client.
        /// </summary>
        /// <returns>The client.</returns>
        protected abstract V GetClient();

        /// <summary>
        /// Creates the request context.
        /// </summary>
        /// <returns>The request context.</returns>
        protected abstract RequestContext CreateRequestContext();

        /// <summary>
        /// Executes the client method synchronously and writes the result to the PowerShell pipeline.
        /// Handles any service errors and writes them as errors to the pipeline.
        /// </summary>
        private void Execute()
        {
            try
            {
                var customTask = this.CustomExecuteAsync(this.GetClient(), this.CreateRequestContext());

                var result = customTask.ConfigureAwait(false).GetAwaiter().GetResult();

                if (result == null)
                {
                    var task = this.ExecuteAsync(this.GetClient(), this.CreateRequestContext());

                    result = task.ConfigureAwait(false).GetAwaiter().GetResult();
                }

                if (!string.IsNullOrEmpty(this.WarningMessage))
                {
                    this.WriteWarning(this.WarningMessage);
                }

                this.WriteResult(result);
            }
            catch (PipelineStoppedException)
            {
                // Do nothing if pipeline stops.                    
            }
            catch (PipelineClosedException)
            {
                // Do nothing if pipeline stops.                    
            }
            catch (CallerError ex)
            {
                var exn = new Exception(JsonConvert.SerializeObject(ex, Formatting.Indented));
                this.WriteError(new ErrorRecord(exn, ex.Code, ErrorCategory.NotSpecified, this));
            }
            catch (ServiceFault ex)
            {
                var exn = new Exception(JsonConvert.SerializeObject(ex, Formatting.Indented));
                this.ThrowTerminatingError(new ErrorRecord(exn, ex.Code, ErrorCategory.NotSpecified, this));
            }
            catch (Exception ex)
            {
                this.ThrowTerminatingError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.NotSpecified, this));
            }
        }
    }
}