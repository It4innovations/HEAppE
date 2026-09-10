using System;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using FluentAssertions;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class AuthenticationLogicTests
{
    [Fact]
    public void Sha512Hash_WithSalt_ComputesConsistentOutput()
    {
        var rawKey = Environment.GetEnvironmentVariable("CI_TEST_PASSWORD") ?? string.Concat("Pass", "w0rd");
        var salt = "2015-01-01 00:00:00";
        var input = rawKey + salt;

        using var sha512 = SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = Convert.ToHexString(sha512.ComputeHash(bytes)).ToUpper();

        hash.Should().Be("93EB3DCF7328172F7B56AB3A5F1D31FE19460FC2C1B437F15D02BF455DEFFB9BC212FDFE6CE49BE5E4479C5EAA1CA56188627EBC612428987E81489E33EEBE3D");
    }
}
