namespace Microsoft.PrivacyServices.AnaheimId.AidFunctions
{
    using System.Threading.Tasks;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Microsoft.PrivacyServices.Common.Azure;

    /// <summary>
    /// Anaheim Id Azure Function implementation interface.
    /// </summary>
    public interface IAidFunction
    {
        /// <summary>
        /// Process AnaheimId Request.
        /// </summary>
        /// <param name="anaheimIdRequest">AnaheimId Request.</param>
        /// <param name="logger">Azure function logger.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task RunAsync(AnaheimIdRequest anaheimIdRequest, ILogger logger);
    }
}
