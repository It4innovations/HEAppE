using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestUtils;

namespace SshCaAPITests;

public class SensitiveDataRedactorTests
{
    [Theory]
    [InlineData("SessionCode", true)]
    [InlineData("sessionCode", true)]
    [InlineData("Authorization", true)]
    [InlineData("X-API-Key", true)]
    [InlineData("apiKey", true)]
    [InlineData("ServiceApiKey", true)]
    [InlineData("credentials", true)]
    [InlineData("ott", true)]
    [InlineData("password", true)]
    [InlineData("Passphrase", true)]
    [InlineData("ProjectId", false)]
    [InlineData("ClusterName", false)]
    public void IsSensitiveKey_DetectsExpectedKeys(string key, bool expected)
    {
        Assert.Equal(expected, SensitiveDataRedactor.IsSensitiveKey(key));
    }

    [Fact]
    public void RedactBody_RedactsNestedJsonTokensAndPreservesNonSensitiveFields()
    {
        const string json = """
        {
          "SessionCode": "secret-session-123",
          "ProjectId": 42,
          "Ott": "eyJhbGciOiJSUzI1NiJ9.payload.sig",
          "Nested": {
            "ApiKey": "my-api-key",
            "UserCredentials": {
              "Password": "my-password"
            },
            "PublicKey": "ssh-ed25519 AAAAC3..."
          },
          "Items": [
            { "access_token": "tok-1", "Name": "job-1" }
          ]
        }
        """;

        var redacted = SensitiveDataRedactor.RedactBody(json);

        Assert.DoesNotContain("secret-session-123", redacted);
        Assert.DoesNotContain("eyJhbGciOiJSUzI1NiJ9.payload.sig", redacted);
        Assert.DoesNotContain("my-api-key", redacted);
        Assert.DoesNotContain("my-password", redacted);
        Assert.DoesNotContain("tok-1", redacted);
        Assert.Contains("\"ProjectId\":42", redacted);
        Assert.Contains("ssh-ed25519 AAAAC3...", redacted);
        Assert.Contains("\"Name\":\"job-1\"", redacted);
        Assert.Contains(SensitiveDataRedactor.RedactedValue, redacted);
    }

    [Fact]
    public void RedactBody_RedactsBearerAndFormTokensInPlainText()
    {
        const string form = "grant_type=token-exchange&subject_token=eyJabc123&Authorization=Bearer supersecret";

        var redacted = SensitiveDataRedactor.RedactBody(form);

        Assert.DoesNotContain("eyJabc123", redacted);
        Assert.DoesNotContain("supersecret", redacted);
        Assert.Contains("grant_type=token-exchange", redacted);
        Assert.Contains(SensitiveDataRedactor.RedactedValue, redacted);
    }

    [Fact]
    public void ClusterProxyConnection_ToString_RedactsPassword()
    {
        var proxy = new ClusterProxyConnection
        {
            Id = 1,
            Host = "proxy.example.com",
            Port = 1080,
            Type = ProxyType.Socks5,
            Username = "proxyuser",
            Password = "super-secret-proxy-password"
        };

        var text = proxy.ToString();

        Assert.DoesNotContain("super-secret-proxy-password", text);
        Assert.Contains("proxyuser", text);
        Assert.Contains("***REDACTED***", text);
    }
}
