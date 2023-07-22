namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem
{
    /// <summary>
    /// An interface that defines methods for interacting with the file system.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Reads the raw bytes from a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The data.</returns>
        byte[] ReadFile(string filePath);

        /// <summary>
        /// Writes the raw bytes to a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="data">The data.</param>
        void WriteFile(string filePath, byte[] data);

        /// <summary>
        /// Determines if the given file exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Deletes the file if it exists.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        void DeleteFile(string filePath);

        /// <summary>
        /// Create directory.
        /// </summary>
        /// <param name="directory">Directory needs to be created.</param>
        /// <returns>The path of the folder that was created.</returns>
        string CreateDirectory(string directory);

        /// <summary>
        /// Delete a directory and all files/folders inside of it.
        /// If the directory does not exist, then this method does nothing.
        /// </summary>
        /// <param name="directory">The directory that needs to be delete.</param>
        void DeleteDirectory(string directory);
    }
}