#nullable enable
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HEAppE.RestUtils;

/// <summary>
/// Provides shared detection and redaction of sensitive fields (tokens, session codes, passwords, API keys, secrets)
/// for HTTP request/response logging.
/// </summary>
public static partial class SensitiveDataRedactor
{
    public const string RedactedValue = "***REDACTED***";

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "SessionCode", "Password", "Token", "Secret", "Key", "Ott",
        "Authorization", "Cookie", "Set-Cookie", "X-API-Key"
    };

    private static readonly string[] SensitiveSubstrings =
    {
        "token", "password", "secret", "passphrase", "sessioncode",
        "privatekey", "signature", "apikey", "api_key", "credential"
    };

    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    /// <summary>
    /// Returns true if a header, property, or parameter name may hold a secret or token.
    /// </summary>
    public static bool IsSensitiveKey(string? key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (SensitiveKeys.Contains(key)) return true;

        foreach (var substring in SensitiveSubstrings)
        {
            if (key.Contains(substring, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Redacts sensitive JSON properties or sensitive key=value / Bearer patterns in a logged HTTP body.
    /// </summary>
    public static string RedactBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return body ?? string.Empty;

        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                using var doc = JsonDocument.Parse(body, DocumentOptions);
                var buffer = new ArrayBufferWriter<byte>(body.Length);
                using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
                {
                    WriteRedactedElement(writer, doc.RootElement);
                }

                return Encoding.UTF8.GetString(buffer.WrittenSpan);
            }
            catch (JsonException)
            {
                // Fall through to text-based redaction for malformed/truncated JSON.
            }
        }

        return RedactPlainText(body);
    }

    private static void WriteRedactedElement(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    writer.WritePropertyName(property.Name);
                    if (IsSensitiveKey(property.Name) && property.Value.ValueKind != JsonValueKind.Null)
                    {
                        writer.WriteStringValue(RedactedValue);
                    }
                    else
                    {
                        WriteRedactedElement(writer, property.Value);
                    }
                }
                writer.WriteEndObject();
                break;

            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteRedactedElement(writer, item);
                }
                writer.WriteEndArray();
                break;

            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string RedactPlainText(string text)
    {
        var redacted = BearerRegex().Replace(text, $"$1{RedactedValue}");
        redacted = KeyValueRegex().Replace(redacted, $"$1{RedactedValue}");
        return redacted;
    }

    [GeneratedRegex(@"(Bearer\s+)[^\s""'&,;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"((?:sessionCode|password|passphrase|secret|token|ott|api[_-]?key|private[_-]?key|client_secret|subject_token|access_token|refresh_token)\s*[=:]\s*)[^\s&""',;]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex KeyValueRegex();
}
