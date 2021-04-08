using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HEAppE.MiddlewareUtils {
	public class StringUtils {
		public static string[] SplitStringToArray(string source, char delimiter) {
			if (source == null || source.Trim().Length == 0)
				return null;
			string[] result = source.Split(delimiter);
			for (int i = 0; i < result.Length; i++) {
				result[i] = result[i].Trim();
			}
			return result;
		}

		public static string ConvertDictionaryToString(Dictionary<string, string> dictionary) {
			StringBuilder builder = new StringBuilder();
			foreach (KeyValuePair<string, string> keyValuePair in dictionary) {
				builder.AppendLine(keyValuePair.Key + ": " + keyValuePair.Value);
			}
			return builder.ToString();
		}

		public static int ExtractInt(string source) {
			MatchCollection matches = Regex.Matches(source, @"(\d+)$");
			if (matches.Count < 1) {
				throw new FormatException("Input string does not contain a number at the end.");
			}
			return Convert.ToInt32(matches[0].Value);
		}

		public static string RemoveWhitespace(string source) {
			return Regex.Replace(source, @"\s+", "");
		}
	}
}