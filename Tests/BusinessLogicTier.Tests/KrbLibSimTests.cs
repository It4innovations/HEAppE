using System;
using System.Collections.Generic;
using FluentAssertions;
using Kerberos.NET.Client;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities;
using Tmds.Ssh;
using Xunit;

namespace BusinessLogicTier.Tests;

public class KrbLibSimTests : IDisposable
{
    public KrbLibSimTests()
    {
        KrbLibSim.ClearAllTickets();
    }

    public void Dispose()
    {
        KrbLibSim.ClearAllTickets();
    }

    private static byte[] CreateTestTicketBytes(string username, string realm, DateTimeOffset endTime)
    {
        var cache = new Krb5CredentialCache();
        var client = new PrincipalName(PrincipalNameType.NT_PRINCIPAL, realm, new[] { username });
        var server = new PrincipalName(PrincipalNameType.NT_SRV_INST, realm, new[] { "krbtgt", realm });

        var ticket = new KrbTicket
        {
            TicketNumber = 5,
            Realm = realm,
            SName = new KrbPrincipalName { Type = PrincipalNameType.NT_SRV_INST, Name = new[] { "krbtgt", realm } },
            EncryptedPart = new KrbEncryptedData { EType = EncryptionType.AES256_CTS_HMAC_SHA1_96, Cipher = new byte[16] }
        };

        cache.DefaultPrincipalName = client;
        cache.Credentials.Add(new Krb5CredentialCache.Krb5Credential
        {
            Client = client,
            Server = server,
            KeyBlock = new KeyValuePair<EncryptionType, ReadOnlyMemory<byte>>(EncryptionType.AES256_CTS_HMAC_SHA1_96, new byte[32]),
            AuthTime = DateTimeOffset.UtcNow.AddHours(-1),
            StartTime = DateTimeOffset.UtcNow.AddHours(-1),
            EndTime = endTime,
            RenewTill = endTime.AddHours(2),
            Ticket = ticket.EncodeApplication()
        });

        return cache.Serialize();
    }

    private static byte[] CreateEmptyTicketBytes(string username, string realm)
    {
        var cache = new Krb5CredentialCache();
        var client = new PrincipalName(PrincipalNameType.NT_PRINCIPAL, realm, new[] { username });
        cache.DefaultPrincipalName = client;
        return cache.Serialize();
    }

    [Fact]
    public void Debug_LookupCredentialInCache()
    {
        byte[] validBytes = CreateTestTicketBytes("valid_user", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(5));
        var ticketCache = new Krb5TicketCache(validBytes, null);

        var allItems = ticketCache.GetAll();
        allItems.Should().NotBeEmpty();

        var item = ticketCache.GetCacheItem("krbtgt/EXAMPLE.COM");
        item.Should().NotBeNull();
    }

    [Fact]
    public void AddOrUpdateTicketCache_ShouldStoreAndTrackTicket()
    {
        byte[] bytes = CreateTestTicketBytes("testuser1", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(2));

        string user = KrbLibSim.AddOrUpdateTicketCache(bytes);

        user.Should().Be("testuser1");
        KrbLibSim.HasCachedTicket("testuser1").Should().BeTrue();
        KrbLibSim.CachedTicketsCount.Should().Be(1);
    }

    [Fact]
    public void AddOrUpdateTicketCache_WhenReplacingExisting_ShouldNotLeakAndMaintainSingleEntry()
    {
        byte[] bytes1 = CreateTestTicketBytes("testuser2", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(1));
        byte[] bytes2 = CreateTestTicketBytes("testuser2", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(3));

        KrbLibSim.AddOrUpdateTicketCache(bytes1);
        KrbLibSim.AddOrUpdateTicketCache(bytes2);

        KrbLibSim.CachedTicketsCount.Should().Be(1);
        KrbLibSim.HasCachedTicket("testuser2").Should().BeTrue();
    }

    [Fact]
    public void InvalidateTicket_ShouldRemoveAndDisposeCachedTicket()
    {
        byte[] bytes = CreateTestTicketBytes("testuser3", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(2));
        KrbLibSim.AddOrUpdateTicketCache(bytes);
        KrbLibSim.HasCachedTicket("testuser3").Should().BeTrue();

        KrbLibSim.InvalidateTicket("testuser3");

        KrbLibSim.HasCachedTicket("testuser3").Should().BeFalse();
        KrbLibSim.CachedTicketsCount.Should().Be(0);
    }

    [Fact]
    public void InvalidateTicket_NonExistentUser_ShouldNotThrow()
    {
        Action act = () => KrbLibSim.InvalidateTicket("nonexistent");
        act.Should().NotThrow();
    }

    [Fact]
    public void TryCleanCache_WhenTicketHasExpired_ShouldRemoveFromCache()
    {
        byte[] bytesExpired = CreateTestTicketBytes("expired_user", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(-1));
        KrbLibSim.AddOrUpdateTicketCache(bytesExpired);
        KrbLibSim.CachedTicketsCount.Should().Be(1);

        KrbLibSim.TryCleanCache();

        KrbLibSim.HasCachedTicket("expired_user").Should().BeFalse();
        KrbLibSim.CachedTicketsCount.Should().Be(0);
    }

    [Fact]
    public void TryCleanCache_WhenTicketIsInValidityBuffer_ShouldRemoveFromCache()
    {
        byte[] bytesInBuffer = CreateTestTicketBytes("buffer_user", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddSeconds(60));
        KrbLibSim.AddOrUpdateTicketCache(bytesInBuffer);
        KrbLibSim.CachedTicketsCount.Should().Be(1);

        KrbLibSim.TryCleanCache();

        KrbLibSim.HasCachedTicket("buffer_user").Should().BeFalse();
        KrbLibSim.CachedTicketsCount.Should().Be(0);
    }

    [Fact]
    public void TryCleanCache_WhenTicketCacheLacksKrbTgtEntry_ShouldSafelyRemoveWithoutNullReferenceException()
    {
        byte[] emptyBytes = CreateEmptyTicketBytes("empty_user", "EXAMPLE.COM");
        KrbLibSim.AddOrUpdateTicketCache(emptyBytes);
        KrbLibSim.CachedTicketsCount.Should().Be(1);

        Action act = () => KrbLibSim.TryCleanCache();

        act.Should().NotThrow();
        KrbLibSim.HasCachedTicket("empty_user").Should().BeFalse();
        KrbLibSim.CachedTicketsCount.Should().Be(0);
    }

    [Fact]
    public void TryCleanCache_MixedTickets_ShouldOnlyRemoveExpiredAndInvalid()
    {
        byte[] validBytes = CreateTestTicketBytes("valid_user", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(5));
        byte[] expiredBytes = CreateTestTicketBytes("expired_user", "EXAMPLE.COM", DateTimeOffset.UtcNow.AddHours(-2));
        byte[] emptyBytes = CreateEmptyTicketBytes("invalid_user", "EXAMPLE.COM");

        KrbLibSim.AddOrUpdateTicketCache(validBytes);
        KrbLibSim.AddOrUpdateTicketCache(expiredBytes);
        KrbLibSim.AddOrUpdateTicketCache(emptyBytes);

        KrbLibSim.CachedTicketsCount.Should().Be(3);

        KrbLibSim.TryCleanCache();

        KrbLibSim.CachedTicketsCount.Should().Be(1);
        KrbLibSim.HasCachedTicket("valid_user").Should().BeTrue();
        KrbLibSim.HasCachedTicket("expired_user").Should().BeFalse();
        KrbLibSim.HasCachedTicket("invalid_user").Should().BeFalse();
    }
}
