namespace LCPD_First_Response.Engine
{
    using System.IO;
    using System.Security.Cryptography;

    /// <summary>
    /// Provides function to en- and decrypt data.
    /// </summary>
    internal class Encryption
    {
        /// <summary>
        /// The salt used.
        /// </summary>
        private static readonly byte[] Salt = new byte[] { 0xFF, 0x07, 0x26, 0xAB, 0xFF, 0x00, 0xDA, 0xDE, 0x7A, 0xee, 0x4D, 0xFA, 0x01, 0xAF, 0x1D, 0x08, 0x22, 0x3B };

        /// <summary>
        /// Encrypts <paramref name="plain"/> using <paramref name="password"/>.
        /// </summary>
        /// <param name="plain">The plain data.</param>
        /// <param name="password">The password.</param>
        /// <returns>The encrypted data.</returns>
        public static byte[] Encrypt(byte[] plain, string password)
        {
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, Salt);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(plain, 0, plain.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Decrypts <paramref name="cipher"/> using <paramref name="password"/>.
        /// </summary>
        /// <param name="cipher">The cipher data.</param>
        /// <param name="password">The password.</param>
        /// <returns>The decrypted data.</returns>
        public static byte[] Decrypt(byte[] cipher, string password)
        {
            Rijndael rijndael = Rijndael.Create();
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(password, Salt);
            rijndael.Key = pdb.GetBytes(32);
            rijndael.IV = pdb.GetBytes(16);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(cipher, 0, cipher.Length);
            cryptoStream.Close();
            return memoryStream.ToArray();
        }

        /// <summary>
        /// Hashes <paramref name="data"/> using SHA1.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>The hash.</returns>
        public static byte[] HashDataSHA1(byte[] data)
        {
            SHA1CryptoServiceProvider hashProvider = new SHA1CryptoServiceProvider();
            return hashProvider.ComputeHash(data);
        }
    }
}