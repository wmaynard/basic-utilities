using System.Security.Cryptography;
using System.Text;
using Maynard.Logging;

namespace Maynard.Extensions;

public static class StringExtension
{
    private static readonly byte[] _maskingKey = 
    {
        0x3A, 0xE1, 0x91, 0x5B, 0x4C, 0xB8, 0x92, 0x7F,
        0xD1, 0x14, 0x6A, 0x33, 0xE8, 0x47, 0x5D, 0x9B,
        0xA3, 0x6E, 0x2F, 0x50, 0x8C, 0x7A, 0xB9, 0x65,
        0xE2, 0xC1, 0x4D, 0xF8, 0x71, 0x0E, 0x39, 0xAC
    };
    public static bool IsEmpty(this string _string) => string.IsNullOrWhiteSpace(_string);
    public static string GetDigits(this string _string) => new string(_string.Where(char.IsDigit).ToArray());
    public static int DigitsAsInt(this string _string) => int.Parse(_string.GetDigits());
    public static long DigitsAsLong(this string _string) => long.Parse(_string.GetDigits());
    public static string Limit(this string _string, int length) => _string?[..Math.Min(_string.Length, length)];

    /// <summary>
    /// Uses AES to mask a string's value.  Important note: this should not be used as a general-purpose encryption!
    /// The key used for it is stored and committed in the <see ref="StringExtension"/> class.  This means that anyone
    /// using this library can easily decrypt any masked string if they know the package is used.
    /// TODO: Make the encryption key optionally configurable so this can be used as general-purpose encryption.
    /// </summary>
    /// <param name="value">A regular string to encrypt.</param>
    /// <returns>A masked string, with the initialization vector prepended to the value.</returns>
    public static string Mask(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        using Aes aes = Aes.Create();
        aes.Key = _maskingKey;
        aes.GenerateIV();

        using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using MemoryStream ms = new();
        using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
        using (StreamWriter sw = new(cs))
            sw.Write(value.Trim());

        byte[] encryptedBytes = ms.ToArray();

        // Prepend IV to the encrypted bytes
        byte[] combined = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, combined, aes.IV.Length, encryptedBytes.Length);

        // <-- RETURN THE COMBINED ARRAY (IV + CIPHERTEXT)
        return Convert.ToBase64String(combined);
    }
    
    /// <summary>
    /// Unmasks a previously masked string.  See <see cref="Mask"/> for more information.
    /// </summary>
    /// <param name="maskedValue">The masked string.</param>
    /// <returns>The unmasked string, if able.  If the decryption fails for any reason, the original string
    /// will be returned.</returns>
    public static string Unmask(this string maskedValue)
    {
        if (string.IsNullOrWhiteSpace(maskedValue))
            return maskedValue;
    
        try
        {
            byte[] combined = Convert.FromBase64String(maskedValue);
    
            // Extract IV (first 16 bytes)
            byte[] iv = new byte[16];
            Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
    
            // Extract ciphertext
            byte[] cipher = new byte[combined.Length - iv.Length];
            Buffer.BlockCopy(combined, iv.Length, cipher, 0, cipher.Length);
    
            using Aes aes = Aes.Create();
            aes.Key = _maskingKey;
            aes.IV = iv;
    
            using ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using MemoryStream ms = new(cipher);
            using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new(cs);
            return sr.ReadToEnd();
        }
        catch (Exception e)
        {
            Log.Error("Unable to unmask string; string will returned as its original value.", e);
            return maskedValue;
        }
    }
    
    public static string ToCamelCase(this string input) => string.IsNullOrWhiteSpace(input) || char.IsLower(input[0])
        ? input
        : char.ToLowerInvariant(input[0]) + input[1..];

    /// <summary>
    /// Combines two strings into a path / URI.
    /// </summary>
    /// <param name="baseUri"></param>
    /// <param name="relative"></param>
    /// <returns></returns>
    public static string Combine(this string baseUri, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative))
            return baseUri;
        if (string.IsNullOrWhiteSpace(baseUri))
            return relative;
        
        if (baseUri.EndsWith('/'))
            baseUri = baseUri[..^1];
        if (relative.StartsWith('/'))
            relative = relative[1..];

        return $"{baseUri}/{relative}";
    }
}