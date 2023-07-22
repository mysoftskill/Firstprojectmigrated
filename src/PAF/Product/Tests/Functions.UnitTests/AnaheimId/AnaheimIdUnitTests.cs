namespace Functions.UnitTests.AnaheimId
{
    using System;
    using Microsoft.PrivacyServices.AnaheimId;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// AnaheimId unit tests.
    /// </summary>
    [TestClass]
    public class AnaheimIdUnitTests
    {
        /// <summary>
        /// Test GetDeploymentEnvironment.
        /// </summary>
        /// <param name="envVariable">Environment variable.</param>
        /// <param name="expected">Expected value.</param>
        [DataTestMethod]
        [DataRow("", DeploymentEnvironment.ONEBOX)]
        [DataRow("ONEBOX", DeploymentEnvironment.ONEBOX)]
        [DataRow("CI1", DeploymentEnvironment.CI1)]
        [DataRow("CI2", DeploymentEnvironment.CI2)]
        [DataRow("PPE", DeploymentEnvironment.PPE)]
        [DataRow("PROD", DeploymentEnvironment.PROD)]
        public void AnaheimIdGetDeploymentEnvironmentTest(string envVariable, DeploymentEnvironment expected)
        {
            string variableName = "DeploymentEnvironment";
            string backUp = Environment.GetEnvironmentVariable(variableName);

            // delete
            Environment.SetEnvironmentVariable(variableName, null);

            // set
            Environment.SetEnvironmentVariable(variableName, envVariable);

            // test
            DeploymentEnvironment deploymentEnvironment = AidHelpers.GetDeploymentEnvironment();
            Assert.AreEqual(expected, deploymentEnvironment);

            // restore
            Environment.SetEnvironmentVariable(variableName, backUp);
        }

        /// <summary>
        /// Test GetDeploymentEnvironment.
        /// </summary>
        /// <param name="envVariable">Environment variable.</param>
        /// <param name="expected">Expected value.</param>
        [DataTestMethod]
        [DataRow("", AzureRegion.Local)]
        [DataRow("EastUS", AzureRegion.EastUS)]
        [DataRow("WestUS2", AzureRegion.WestUS2)]
        [DataRow("WestCentralUS", AzureRegion.WestCentralUS)]
        public void AnaheimIdGetAzureRegionTest(string envVariable, AzureRegion expected)
        {
            string variableName = "AzureRegion";
            string backUp = Environment.GetEnvironmentVariable(variableName);

            // delete
            Environment.SetEnvironmentVariable(variableName, null);

            // set
            Environment.SetEnvironmentVariable(variableName, envVariable);

            // test
            AzureRegion region = AidHelpers.GetAzureRegion();
            Assert.AreEqual(expected, region);

            // restore
            Environment.SetEnvironmentVariable(variableName, backUp);
        }

        [TestMethod]
        public void AnaheimIdGetAzureRegionExceptionTest()
        {
            string variableName = "AzureRegion";
            string backUp = Environment.GetEnvironmentVariable(variableName);

            // delete
            Environment.SetEnvironmentVariable(variableName, null);

            // set
            Environment.SetEnvironmentVariable(variableName, "Bla-Bla-Wrong");

            // test
            Assert.ThrowsException<ArgumentException>(() => AidHelpers.GetAzureRegion());

            // restore
            Environment.SetEnvironmentVariable(variableName, backUp);
        }

        [TestMethod]
        public void AnaheimIdGetDeploymentEnvironmentExceptionTest()
        {
            string variableName = "DeploymentEnvironment";
            string backUp = Environment.GetEnvironmentVariable(variableName);

            // delete
            Environment.SetEnvironmentVariable(variableName, null);

            // set
            Environment.SetEnvironmentVariable(variableName, "Bla-Bla-Wrong");

            // test
            Assert.ThrowsException<ArgumentException>(() => AidHelpers.GetDeploymentEnvironment());

            // restore
            Environment.SetEnvironmentVariable(variableName, backUp);
        }
    }
}