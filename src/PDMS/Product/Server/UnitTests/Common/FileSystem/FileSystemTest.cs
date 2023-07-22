namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem.UnitTest
{
    using System;
    using System.IO;

    using Microsoft.PrivacyServices.Testing;

    using Xunit;

    public class FileSystemTest
    {
        [Theory(DisplayName = "When provided a file path with an environment variable, then expand it before read."), AutoMoqData]
        public void When_Read_Then_ExpandEnvironmentVariables(FileSystem fileSystem, string fileName, byte[] fileData)
        {
            var filePath = $"%temp%\\{fileName}.txt";

            File.WriteAllBytes(Environment.ExpandEnvironmentVariables(filePath), fileData);

            Assert.Equal(fileData, fileSystem.ReadFile(filePath));
        }

        [Theory(DisplayName = "When provided a file path with an environment variable, then expand it before exists."), AutoMoqData]
        public void When_Exists_Then_ExpandEnvironmentVariables(FileSystem fileSystem, string fileName, byte[] fileData)
        {
            var filePath = $"%temp%\\{fileName}.txt";

            File.WriteAllBytes(Environment.ExpandEnvironmentVariables(filePath), fileData);

            Assert.True(fileSystem.FileExists(filePath));
        }

        [Theory(DisplayName = "When provided a file path with an environment variable, then expand it before write."), AutoMoqData]
        public void When_Write_Then_ExpandEnvironmentVariables(FileSystem fileSystem, string fileName, byte[] fileData)
        {
            var filePath = $"%temp%\\{fileName}.txt";

            fileSystem.WriteFile(filePath, fileData);
            
            Assert.Equal(fileData, File.ReadAllBytes(Environment.ExpandEnvironmentVariables(filePath)));
        }

        [Theory(DisplayName = "When provided a directory with an environment variable, then expand it before create directory."), AutoMoqData]
        public void When_Create_Then_ExpandEnvironmentVariables(FileSystem fileSystem, string directoryName)
        {
            var path = $"%temp%\\{directoryName}";

            fileSystem.CreateDirectory(path);

            Assert.True(Directory.Exists(Environment.ExpandEnvironmentVariables(path)));
        }

        [Theory(DisplayName = "When directory does not exist, then do not fail when deleting the directory."), AutoMoqData]
        public void When_DeleteNotExists_Then_DoNotFail(FileSystem fileSystem, string directoryName)
        {
            var path = $"%temp%\\{directoryName}";

            fileSystem.DeleteDirectory(path);
        }
    }
}