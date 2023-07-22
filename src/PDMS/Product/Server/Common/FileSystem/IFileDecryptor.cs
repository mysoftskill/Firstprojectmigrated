namespace Microsoft.PrivacyServices.DataManagement.Common.FileSystem
{
    /// <summary>
    /// An interface that defines methods for decrypting file data.
    /// </summary>
    public interface IFileDecryptor
    {
        /// <summary>
        /// Decrypts the given data.
        /// </summary>
        /// <param name="encryptedData">The encrypted data.</param>
        /// <returns>The decrypted data.</returns>
        byte[] DecryptData(byte[] encryptedData);
    }
}
