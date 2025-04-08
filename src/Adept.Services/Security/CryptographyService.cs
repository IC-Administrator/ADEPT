using Adept.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Adept.Services.Security
{
    /// <summary>
    /// Service for encryption and decryption operations
    /// </summary>
    public class CryptographyService : ICryptographyService
    {
        private readonly byte[] _key;
        private readonly ILogger<CryptographyService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptographyService"/> class
        /// </summary>
        /// <param name="logger">The logger</param>
        public CryptographyService(ILogger<CryptographyService> logger)
        {
            _logger = logger;

            // Generate or retrieve a key
            // In a real application, this should be securely stored and retrieved
            // For now, we'll use a fixed key derived from the machine name for simplicity
            var machineName = Environment.MachineName;
            using var sha256 = SHA256.Create();
            _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineName + "Adept_Encryption_Key"));
        }

        /// <summary>
        /// Encrypts a string value
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <returns>A tuple containing the encrypted value and the initialization vector</returns>
        public (byte[] EncryptedValue, byte[] Iv) Encrypt(string plainText)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var memoryStream = new MemoryStream();
                using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                using (var streamWriter = new StreamWriter(cryptoStream))
                {
                    streamWriter.Write(plainText);
                }

                return (memoryStream.ToArray(), aes.IV);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting value");
                throw;
            }
        }

        /// <summary>
        /// Decrypts an encrypted value
        /// </summary>
        /// <param name="encryptedValue">The encrypted value</param>
        /// <param name="iv">The initialization vector used for encryption</param>
        /// <returns>The decrypted plain text</returns>
        public string Decrypt(byte[] encryptedValue, byte[] iv)
        {
            try
            {
                using var aes = Aes.Create();
                aes.Key = _key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var memoryStream = new MemoryStream(encryptedValue);
                using var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                using var streamReader = new StreamReader(cryptoStream);

                return streamReader.ReadToEnd();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting value");
                throw;
            }
        }
    }
}
