namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem
{
    using System;
    using System.IO;

    /// <summary>
    /// Contains methods for interacting with the file system.
    /// </summary>
    public class FileSystem : IFileSystem
    {
        /// <summary>
        /// Reads the raw bytes from a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The data.</returns>
        public byte[] ReadFile(string filePath)
        {
            return File.ReadAllBytes(this.ExpandFilePath(filePath));
        }

        /// <summary>
        /// Writes the raw bytes to a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="data">The data.</param>
        public void WriteFile(string filePath, byte[] data)
        {
            File.WriteAllBytes(this.ExpandFilePath(filePath), data);
        }

        /// <summary>
        /// Determines if the given file exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public bool FileExists(string filePath)
        {
            return File.Exists(this.ExpandFilePath(filePath));
        }

        /// <summary>
        /// Deletes the file if it exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        public void DeleteFile(string filePath)
        {
            if (this.FileExists(filePath))
            {
                Retry.Do(() => File.Delete(filePath), TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// Create directory.
        /// </summary>
        /// <param name="directory">Directory needs to be created.</param>
        /// <returns>The path of the folder that was created.</returns>
        public string CreateDirectory(string directory)
        {
            var path = this.ExpandFilePath(directory);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Delete a directory and all files/folders inside of it.
        /// If the directory does not exist, then this method does nothing.
        /// </summary>
        /// <param name="directory">The directory that needs to be delete.</param>
        public void DeleteDirectory(string directory)
        {
            var path = this.ExpandFilePath(directory);

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private string ExpandFilePath(string filePath)
        {
            return Environment.ExpandEnvironmentVariables(filePath);
        }
    }
}