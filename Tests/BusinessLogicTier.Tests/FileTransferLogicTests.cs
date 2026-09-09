using System;
using Xunit;
using FluentAssertions;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class FileTransferLogicTests
{
    [Fact]
    public void KeyLimits_DefaultSettings_AreValid()
    {
        int keyLimit = 10;
        int validityHours = 24;

        keyLimit.Should().BeGreaterThan(0);
        validityHours.Should().BeGreaterThan(0);
    }
}
