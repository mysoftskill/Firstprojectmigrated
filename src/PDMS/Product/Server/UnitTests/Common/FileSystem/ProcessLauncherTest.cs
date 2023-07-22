namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem.UnitTest
{
    using System.IO;

    using Microsoft.PrivacyServices.DataManagement.Common.FileSystem;
    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class ProcessLauncherTest
    {
        [Theory(DisplayName = "Verify exception is thrown for unknown command."), AutoMoqData]
        public void VerifyExceptionIsThrownWhenProcessLaunched(string command, string arguments)
        {
            IProcessLauncher processLauncher = new ProcessLauncher();
            Assert.Throws<FileNotFoundException>(() => processLauncher.Run(command, arguments)); 
        }
    }
}