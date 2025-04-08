namespace Adept.Common.Interfaces
{
    /// <summary>
    /// Service for encryption and decryption operations
    /// </summary>
    public interface ICryptographyService
    {
        /// <summary>
        /// Encrypts a string value
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <returns>A tuple containing the encrypted value and the initialization vector</returns>
        (byte[] EncryptedValue, byte[] Iv) Encrypt(string plainText);

        /// <summary>
        /// Decrypts an encrypted value
        /// </summary>
        /// <param name="encryptedValue">The encrypted value</param>
        /// <param name="iv">The initialization vector used for encryption</param>
        /// <returns>The decrypted plain text</returns>
        string Decrypt(byte[] encryptedValue, byte[] iv);
    }
}
