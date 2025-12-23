using System;
using System.IO;
using System.Security.Cryptography;

namespace Etiquette.Services
{
    public class CryptoService : IDisposable
    {
        private ECDiffieHellmanCng _ecdh;
        private byte[] _derivedKey;

        public CryptoService()
        {
            // Initialisation de la courbe elliptique P-256
            _ecdh = new ECDiffieHellmanCng(256);
            _ecdh.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            _ecdh.HashAlgorithm = CngAlgorithm.Sha256;
        }

        public byte[] GetPublicKey() => _ecdh.PublicKey.ToByteArray();

        public void DeriveSharedSecret(byte[] otherPublicKey)
        {
            var otherKey = CngKey.Import(otherPublicKey, CngKeyBlobFormat.EccPublicBlob);
            _derivedKey = _ecdh.DeriveKeyMaterial(otherKey);
        }

        public byte[] EncryptData(string plainText)
        {
            if (_derivedKey == null) throw new InvalidOperationException("Clé non initialisée");

            using (Aes aes = Aes.Create())
            {
                aes.Key = _derivedKey;
                aes.GenerateIV();

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length); // IV en clair au début
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (var sw = new StreamWriter(cs))
                    {
                        sw.Write(plainText);
                    }
                    return ms.ToArray();
                }
            }
        }

        public string DecryptData(byte[] cipherText)
        {
            if (_derivedKey == null) throw new InvalidOperationException("Clé non initialisée");

            using (Aes aes = Aes.Create())
            {
                aes.Key = _derivedKey;
                byte[] iv = new byte[16];

                if (cipherText.Length < 16) return null;
                Array.Copy(cipherText, 0, iv, 0, iv.Length);
                aes.IV = iv;

                try
                {
                    using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                    using (var ms = new MemoryStream(cipherText, 16, cipherText.Length - 16))
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    using (var sr = new StreamReader(cs))
                    {
                        return sr.ReadToEnd();
                    }
                }
                catch { return null; }
            }
        }

        public void Dispose() => _ecdh?.Dispose();
    }
}