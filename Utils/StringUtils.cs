using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.Utils
{
    public class StringUtils
    {
        public static string[] SplitStringToArray(string source, char delimiter)
        {
            if (source == null || source.Trim().Length == 0)
                return null;
            string[] result = source.Split(delimiter);
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = result[i].Trim();
            }
            return result;
        }

        public static string ConvertDictionaryToString(Dictionary<string, string> dictionary)
        {
            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, string> keyValuePair in dictionary)
            {
                builder.AppendLine(keyValuePair.Key + ": " + keyValuePair.Value);
            }
            return builder.ToString();
        }

        public static int ExtractInt(string source)
        {
            MatchCollection matches = Regex.Matches(source, @"(\d+)$");
            if (matches.Count < 1)
            {
                throw new FormatException("Input string does not contain a number at the end.");
            }
            return Convert.ToInt32(matches[0].Value);
        }

        public static string RemoveWhitespace(string source)
        {
            return Regex.Replace(source, @"\s+", "");
        }

        public static string CreateIdentifierHash(List<string> strings)
        {
            StringBuilder builder = new StringBuilder();
            strings.ForEach(s => builder.Append(s));
            return builder.ToString();
        }


        /// <summary>
        ///   Combines username with random salt.
        ///   Username is inserted into salt string on position
        ///   given by integer value of first character of the salt moduFlo length of the salt.
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="salt">Salt</param>
        /// <returns>Combined string</returns>
        public static string CombineContentWithSalt(string username)
        {
            string salt = GetRandomString();
            int val = (Encoding.UTF8.GetBytes(salt)[0]) % salt.Length;
            StringBuilder sb = new StringBuilder(salt);
            sb.Insert(val, username);
            return sb.ToString();
        }

        private static string GetRandomString()
        {
            var random = new byte[16];
            var rng = new RNGCryptoServiceProvider();
            rng.GetNonZeroBytes(random);
            return Convert.ToBase64String(random);
        }
    }
}