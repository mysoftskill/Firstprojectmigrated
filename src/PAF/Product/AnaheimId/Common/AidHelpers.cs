namespace Microsoft.PrivacyServices.AnaheimId
{
    using System;
    using System.IO;
    using System.Reflection;
    using Microsoft.ComplianceServices.AnaheimIdLib.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// AnaheimIdHelpers.
    /// </summary>
    public static class AidHelpers
    {
        /// <summary>
        /// Get deployment environment.
        /// </summary>
        /// <returns>DeploymentEnvironment.</returns>
        public static DeploymentEnvironment GetDeploymentEnvironment()
        {
            DeploymentEnvironment deploymentEnvironment;

            string environmentVariable = Environment.GetEnvironmentVariable("DeploymentEnvironment", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(environmentVariable))
            {
                deploymentEnvironment = DeploymentEnvironment.ONEBOX;
            }
            else
            {
                if (!Enum.TryParse(environmentVariable, true, out deploymentEnvironment))
                {
                    throw new ArgumentException($"Cannot parse DeploymentEnvironment={environmentVariable}", nameof(deploymentEnvironment));
                }
            }

            return deploymentEnvironment;
        }

        /// <summary>
        /// Get Azure Region.
        /// </summary>
        /// <returns>DeploymentEnvironment.</returns>
        public static AzureRegion GetAzureRegion()
        {
            AzureRegion azureRegion;

            string environmentVariable = Environment.GetEnvironmentVariable("AzureRegion", EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(environmentVariable))
            {
                azureRegion = AzureRegion.Local;
            }
            else
            {
                var env = Environment.GetEnvironmentVariable("AzureRegion", EnvironmentVariableTarget.Process);

                if (!Enum.TryParse(env, true, out azureRegion))
                {
                    throw new ArgumentException($"Cannot parse AzureRegion={env}", nameof(azureRegion));
                }
            }

            return azureRegion;
        }

        /// <summary>
        /// Get Azure Functions root directory.
        /// </summary>
        /// <returns>Full path to the function app root directory.</returns>
        public static string GetAzureFunctionsRootDirectory()
        {
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            return rootDirectory;
        }

        /// <summary>
        /// Parse AnaheimId request from string.
        /// </summary>
        /// <param name="message">AnaheimId Request.</param>
        /// <returns>Parsed AnaheimId request.</returns>
        public static AnaheimIdRequest ParseAnaheimIdRequest(string message)
        {
            AnaheimIdRequest anaheimIdRequest = JsonConvert.DeserializeObject<AnaheimIdRequest>(message);

            if (anaheimIdRequest.AnaheimIds == null)
            {
                throw new ArgumentNullException(nameof(anaheimIdRequest.AnaheimIds));
            }

            if (anaheimIdRequest.DeleteDeviceIdRequest == null)
            {
                throw new ArgumentNullException(nameof(anaheimIdRequest.DeleteDeviceIdRequest));
            }

            return anaheimIdRequest;
        }
    }
}
