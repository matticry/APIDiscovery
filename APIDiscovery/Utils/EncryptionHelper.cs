using System.Security.Cryptography;
using System.Text;

namespace APIDiscovery.Utils;

public class EncryptionHelper
{
    private readonly IConfiguration _configuration;
    private readonly byte[] _key;

    public EncryptionHelper(IConfiguration configuration)
    {
        _configuration = configuration;
        var encryptionKey = _configuration["AppSettings:EncryptionKey"] ?? "DefaultKey123DefaultKey123DefaultKey123";

        // Asegurar que la clave sea de 32 bytes (256 bits)
        _key = CreateKey(encryptionKey, 32);
    }

    private byte[] CreateKey(string password, int keyBytes)
    {
        // Si es una clave hexadecimal de longitud correcta (64 caracteres = 32 bytes)
        if (password.Length == 64 && password.All(c => "0123456789abcdefABCDEF".Contains(c)))
        {
            byte[] keyArray = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                keyArray[i] = Convert.ToByte(password.Substring(i * 2, 2), 16);
            }
            return keyArray;
        }
    
        // Método original para manejo de claves no hexadecimales
        byte[] result = new byte[keyBytes];
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        int length = Math.Min(passwordBytes.Length, keyBytes);
        Array.Copy(passwordBytes, result, length);
        return result;
    }

    public string Encrypt(string text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        using (var aes = Aes.Create())
        {
            aes.Key = _key;
            aes.GenerateIV(); // Generar IV aleatorio

            using (var encryptor = aes.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                // Escribir el IV en la salida
                ms.Write(aes.IV, 0, aes.IV.Length);

                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(text);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return null;

        var cipherBytes = Convert.FromBase64String(cipherText);

        using (var aes = Aes.Create())
        {
            aes.Key = _key;

            // El IV está almacenado al inicio del array de bytes cifrados
            var iv = new byte[aes.IV.Length];
            Array.Copy(cipherBytes, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using (var decryptor = aes.CreateDecryptor())
            using (var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var sr = new StreamReader(cs))
            {
                return sr.ReadToEnd();
            }
        }
    }
}