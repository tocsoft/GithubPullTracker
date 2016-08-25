using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace GithubPullTracker
{

    public static class Encrypter
    {
        public static IStringEncrypter Create()
        {
            var password = SettingsManager.Settings.GetSetting("EncryptionKey");
            return Create(password);
        }

        public static IStringEncrypter Create(string passphrase)
        {
            if (string.IsNullOrWhiteSpace(passphrase))
            {
                return new NullEncrypter();
            }

            return new StringEncrypter(passphrase);
        }

        private class NullEncrypter : IStringEncrypter
        {
            public string Decrypt(string encrypted)
            {
                return encrypted;
            }

            public void Dispose()
            {
            }

            public string Encrypt(string unencrypted)
            {
                return unencrypted;
            }
        }
        private class StringEncrypter : IStringEncrypter
        {
            private readonly Random random;
            private readonly byte[] key;
            private readonly RijndaelManaged rm;
            private readonly UTF8Encoding encoder;

            public StringEncrypter(string key)
            {
                this.random = new Random();
                this.rm = new RijndaelManaged();
                this.encoder = new UTF8Encoding();
                using (var db = new Rfc2898DeriveBytes(key, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, 100))
                {
                    this.key = db.GetBytes(rm.LegalKeySizes[0].MaxSize / 8);
                }
            }

            public void Dispose()
            {
                if (rm != null)
                {
                    rm.Dispose();
                }
            }

            public string Encrypt(string unencrypted)
            {
                var vector = new byte[16];
                this.random.NextBytes(vector);
                var cryptogram = vector.Concat(this.Encrypt(this.encoder.GetBytes(unencrypted), vector));
                return Convert.ToBase64String(cryptogram.ToArray());
            }

            public string Decrypt(string encrypted)
            {
                var cryptogram = Convert.FromBase64String(encrypted);
                if (cryptogram.Length < 17)
                {
                    throw new ArgumentException("Not a valid encrypted string", "encrypted");
                }

                var vector = cryptogram.Take(16).ToArray();
                var buffer = cryptogram.Skip(16).ToArray();
                return this.encoder.GetString(this.Decrypt(buffer, vector));
            }

            private byte[] Encrypt(byte[] buffer, byte[] vector)
            {
                var encryptor = this.rm.CreateEncryptor(this.key, vector);
                return this.Transform(buffer, encryptor);
            }

            private byte[] Decrypt(byte[] buffer, byte[] vector)
            {
                var decryptor = this.rm.CreateDecryptor(this.key, vector);
                return this.Transform(buffer, decryptor);
            }

            private byte[] Transform(byte[] buffer, ICryptoTransform transform)
            {
                using (var stream = new MemoryStream())
                {
                    using (var cs = new CryptoStream(stream, transform, CryptoStreamMode.Write))
                    {
                        cs.Write(buffer, 0, buffer.Length);
                    }

                    return stream.ToArray();
                }
            }
        }
    }
}