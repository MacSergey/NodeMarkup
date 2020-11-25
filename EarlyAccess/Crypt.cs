using EarlyAccess.Properties;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace IMT
{
    public class Crypt
    {
        private static string PrivateKey { get; } = LoadPrivateKey();
        private static string PublicKey { get; } = Resource.ResourceManager.GetString("Key");

        public static string Sign(string id)
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(PrivateKey);
                var param = rsa.ExportParameters(true);

                var idBytes = Encoding.UTF8.GetBytes(id);
                var sign = rsa.SignData(idBytes, new MD5CryptoServiceProvider());
                var result = BytesToHex(sign);
                return result;
            }
        }
        public static bool Verify(string id, string sign)
        {
            try
            {
                using (var rsa = new RSACryptoServiceProvider())
                {
                    rsa.FromXmlString(PublicKey);

                    var idBytes = Encoding.UTF8.GetBytes(id);
                    var signBytes = HexToBytes(sign);
                    var allowed = rsa.VerifyData(idBytes, new MD5CryptoServiceProvider(), signBytes);
                    return allowed;
                }
            }
            catch
            {
                return false;
            }
        }
        public static string GetHash(string input)
        {
            using (var hash = MD5.Create())
            {
                var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                var hashString = BytesToHex(hashBytes);
                return hashString;
            }
        }

        private static string BytesToHex(byte[] data) => string.Join("", data.Select(b => b.ToString("x2")).ToArray());
        private static byte[] HexToBytes(string hex)
        {
            var data = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length / 2; i += 1)
            {
                var part = hex.Substring(i * 2, 2);
                data[i] = Convert.ToByte(part, 16);
            }
            return data;
        }

        static void Generate()
        {
            using (var rsa = new RSACryptoServiceProvider())
            {
                var privateParam = rsa.ToXmlString(true);
                var publicParam = rsa.ToXmlString(false);
            }
        }
        public static string LoadPrivateKey()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Colossal Order\\Cities_Skylines\\NodeMarkup.key");
                var key = File.ReadAllText(path);
                return key;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
