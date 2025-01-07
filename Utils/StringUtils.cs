using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace HEAppE.Utils;

public static class StringUtils
{
    public static string ConvertDictionaryToString(Dictionary<string, string> dictionary)
    {
        var builder = new StringBuilder();
        foreach (var keyValuePair in dictionary) builder.AppendLine(keyValuePair.Key + ": " + keyValuePair.Value);
        return builder.ToString();
    }

    public static string CreateIdentifierHash(List<string> strings)
    {
        var builder = new StringBuilder();
        strings.ForEach(s => builder.Append(s));
        return builder.ToString();
    }

    /// <summary>
    ///     Combines username with random salt.
    ///     Username is inserted into salt string on position
    ///     given by integer value of first character of the salt moduFlo length of the salt.
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="salt">Salt</param>
    /// <returns>Combined string</returns>
    public static string CombineContentWithSalt(string username)
    {
        var salt = GetRandomString();
        var val = Encoding.UTF8.GetBytes(salt)[0] % salt.Length;
        var sb = new StringBuilder(salt);
        sb.Insert(val, username);
        return sb.ToString();
    }

    public static string GetRandomString()
    {
        var random = new byte[16];
        var rng = RandomNumberGenerator.Create();
        rng.GetNonZeroBytes(random);
        return Convert.ToBase64String(random);
    }

    public static Stream ToStream(this string str, Encoding enc = null)
    {
        enc = enc ?? Encoding.UTF8;
        return new MemoryStream(enc.GetBytes(str ?? ""));
    }
}