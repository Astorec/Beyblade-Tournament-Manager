using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BeybladeTournamentManager.Components.Pages.ViewModels;

namespace BeybladeTournamentManager.Components
{
    public class TokenEncryption
    {
        private static readonly string EncryptionKey;
        static TokenEncryption()
        {
            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: false)
           .AddUserSecrets<Program>();
            var configuration = builder.Build();
            EncryptionKey = configuration["EncryptionKey"];

            if (string.IsNullOrEmpty(EncryptionKey))
            {
                EncryptionKey = GenerateAndSaveEncryptionKey(configuration);
            }
        }
        public static void SaveToken(string filePath, string token)
        {
            var encryptedToken = Encrypt(token);

            // check if the file exists
            if (File.Exists(filePath))
                File.Delete(filePath);

            File.WriteAllText(filePath, encryptedToken);
        }

        public static string LoadToken(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            var encryptedToken = File.ReadAllText(filePath);
            return Decrypt(encryptedToken);
        }

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            var keyBytes = Convert.FromBase64String(EncryptionKey);
            aes.Key = keyBytes;

            var iv = aes.IV;
            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();
            ms.Write(iv, 0, iv.Length);
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            var keyBytes = Convert.FromBase64String(EncryptionKey);
            aes.Key = keyBytes;

            using var decryptor = aes.CreateDecryptor(aes.Key, iv);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }

        private static string GenerateAndSaveEncryptionKey(IConfiguration configuration)
        {
            // Generate a new encryption key
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                var newKey = Convert.ToBase64String(aes.Key);

                // Update the settings file with the new key
                var configFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.user.json");
                var configJson = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : "{}";
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);

                if (!config.ContainsKey("EncryptionSettings"))
                {
                    config["EncryptionSettings"] = new Dictionary<string, string>();
                }

                var encryptionSettings = config["EncryptionSettings"] as Dictionary<string, string>;
                if (encryptionSettings == null)
                {
                    encryptionSettings = new Dictionary<string, string>();
                    config["EncryptionSettings"] = encryptionSettings;
                }

                encryptionSettings["EncryptionKey"] = newKey;

                var updatedConfigJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configFilePath, updatedConfigJson);

                // Reload the configuration to reflect the changes
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile("appsettings.user.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<Program>();

                configuration = builder.Build();
                return newKey;
            }
        }
    }

}