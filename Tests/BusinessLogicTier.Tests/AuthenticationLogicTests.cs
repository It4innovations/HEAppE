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

    [Fact]
    public void UserInfoExtendedModel_Deserializes_UserOrgV2Response()
    {
        const string v2Json = """
        {
          "IdentityProviderUserId": "cbc93e81-fa99-43c1-b528-c648353377e3",
          "Name": null,
          "UserName": "f04be6cc-874b-4160-a51d-706518b84482@myaccessid.org",
          "Email": "jakub.konvicka@vsb.cz",
          "SystemRoles": [
            {
              "IdpUserId": "cbc93e81-fa99-43c1-b528-c648353377e3",
              "UserEmail": "jakub.konvicka@vsb.cz",
              "RoleName": "Member",
              "ProjectShortName": "tts",
              "SystemPermissionTypes": ["perm:heappe:exec"],
              "ProjectResources": [
                {
                  "ResourceName": "ATR-25-4",
                  "Description": "LEXIS Platform 2 feature development and validation",
                  "StartDate": "2025-07-16T08:40:28.172+00:00",
                  "EndDate": "2026-07-15T21:59:59.999+00:00",
                  "Clusters": [
                    {
                      "Name": "Karolina",
                      "Description": "Karolina cluster",
                      "Url": "karolina.it4i.cz",
                      "IsPrivate": false,
                      "ClusterType": "HPC",
                      "AllocationUnitCount": 1000,
                      "AllocationUnit": "NodeHours",
                      "Attributes": []
                    }
                  ]
                }
              ]
            }
          ]
        }
        """;

        var model = System.Text.Json.JsonSerializer.Deserialize<HEAppE.ExternalAuthentication.DTO.LexisAuth.UserInfoExtendedModel>(
            v2Json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        model.Should().NotBeNull();
        model!.KeycloakSid.Should().Be("cbc93e81-fa99-43c1-b528-c648353377e3");
        model.IdentityProviderUserId.Should().Be("cbc93e81-fa99-43c1-b528-c648353377e3");
        model.SystemRoles.Should().ContainSingle();
        var resource = model.SystemRoles[0].ProjectResources.Should().ContainSingle().Subject;
        resource.Name.Should().Be("ATR-25-4");
        resource.ResourceName.Should().Be("ATR-25-4");
        resource.Locations.Should().ContainSingle();
        resource.Locations[0].Name.Should().Be("Karolina");
        resource.Locations[0].Endpoint.Should().Be("karolina.it4i.cz");
        resource.Locations[0].Type.Should().Be("HPC");
    }

    [Theory]
    [InlineData("Forbidden", "Status code: Forbidden.\nContent: Forbidden: no permission mapping for this endpoint", true)]
    [InlineData("NotFound", "Status code: NotFound.\nContent: 404 Not Found", true)]
    [InlineData("MethodNotAllowed", "Status code: MethodNotAllowed.", true)]
    [InlineData("ExternalApiError", "Status code: NotImplemented.\nContent: 501 Not Implemented", true)]
    [InlineData("InvalidToken", "Status code: Unauthorized.\nContent: Invalid token", false)]
    [InlineData("ServerError", "Status code: InternalServerError.\nContent: Database error", false)]
    [InlineData("BadRequest", "Status code: BadRequest.\nContent: Invalid parameters", false)]
    public void IsV2FallbackWarranted_CorrectlyClassifiesExceptions(string message, string details, bool expected)
    {
        var ex = new HEAppE.Exceptions.External.AuthenticationTypeException(message, "UserOrg")
        {
            Details = details
        };

        var result = HEAppE.Services.UserOrg.UserOrgService.IsV2FallbackWarranted(ex);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task UserOrgV2Service_WhenAutoFallbackActiveAndFallbackWarranted_LogsWarningNotError()
    {
        var originalApiVersion = HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ApiVersion;
        try
        {
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ApiVersion = "auto";
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.EndpointPrefix = "userorg";
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ExtendedUserInfoV2Endpoint = "api/v2/UserInfo/Extended";

            var handler = new TestHandler((req) => new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
            {
                ReasonPhrase = "Forbidden",
                Content = new StringContent("Forbidden: no permission mapping for this endpoint")
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.workflows.my-eurohpc.eu/")
            };

            var mockFactory = new Moq.Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient("userOrgApi")).Returns(httpClient);

            var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger>();
            var service = new HEAppE.Services.UserOrg.UserOrgV2Service(mockFactory.Object);

            var act = () => service.GetUserInfoAsync("test-token", "instance-1", mockLogger.Object);
            await act.Should().ThrowAsync<HEAppE.Exceptions.External.AuthenticationTypeException>();

            mockLogger.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    Moq.It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                    Moq.It.Is<Moq.It.IsAnyType>((v, t) => v.ToString()!.Contains("[UserOrg v2 API Warning]") && v.ToString()!.Contains("Forbidden")),
                    Moq.It.IsAny<Exception>(),
                    Moq.It.IsAny<Func<Moq.It.IsAnyType, Exception?, string>>()),
                Moq.Times.Once);

            mockLogger.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    Moq.It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                    Moq.It.Is<Moq.It.IsAnyType>((v, t) => v.ToString()!.Contains("[UserOrg v2 API Error]")),
                    Moq.It.IsAny<Exception>(),
                    Moq.It.IsAny<Func<Moq.It.IsAnyType, Exception?, string>>()),
                Moq.Times.Never);
        }
        finally
        {
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ApiVersion = originalApiVersion;
        }
    }

    [Fact]
    public async Task UserOrgV2Service_WhenApiVersionIsV2_LogsErrorOnFailure()
    {
        var originalApiVersion = HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ApiVersion;
        try
        {
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ApiVersion = "v2";
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.EndpointPrefix = "userorg";
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ExtendedUserInfoV2Endpoint = "api/v2/UserInfo/Extended";

            var handler = new TestHandler((req) => new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
            {
                ReasonPhrase = "Forbidden",
                Content = new StringContent("Forbidden: no permission mapping for this endpoint")
            });

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.workflows.my-eurohpc.eu/")
            };

            var mockFactory = new Moq.Mock<IHttpClientFactory>();
            mockFactory.Setup(f => f.CreateClient("userOrgApi")).Returns(httpClient);

            var mockLogger = new Moq.Mock<Microsoft.Extensions.Logging.ILogger>();
            var service = new HEAppE.Services.UserOrg.UserOrgV2Service(mockFactory.Object);

            var act = () => service.GetUserInfoAsync("test-token", "instance-1", mockLogger.Object);
            await act.Should().ThrowAsync<HEAppE.Exceptions.External.AuthenticationTypeException>();

            mockLogger.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Error,
                    Moq.It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                    Moq.It.Is<Moq.It.IsAnyType>((v, t) => v.ToString()!.Contains("[UserOrg v2 API Error]")),
                    Moq.It.IsAny<Exception>(),
                    Moq.It.IsAny<Func<Moq.It.IsAnyType, Exception?, string>>()),
                Moq.Times.Once);

            mockLogger.Verify(
                x => x.Log(
                    Microsoft.Extensions.Logging.LogLevel.Warning,
                    Moq.It.IsAny<Microsoft.Extensions.Logging.EventId>(),
                    Moq.It.Is<Moq.It.IsAnyType>((v, t) => v.ToString()!.Contains("[UserOrg v2 API Warning]")),
                    Moq.It.IsAny<Exception>(),
                    Moq.It.IsAny<Func<Moq.It.IsAnyType, Exception?, string>>()),
                Moq.Times.Never);
        }
        finally
        {
            HEAppE.ExternalAuthentication.Configuration.LexisAuthenticationConfiguration.ApiVersion = originalApiVersion;
        }
    }

    private class TestHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
