using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApi.IntegrationTests.Infrastructure;
using HEAppE.RestApiModels.Management;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.Management;

[Trait("Category", "Integration")]
public class UserAndRoleManagementCrudTests : ManagementTestBase
{
    public UserAndRoleManagementCrudTests(HEAppEWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task AdaptorUser_Roles_And_Assignments_FullCrudLifecycle_Succeeds()
    {
        var sessionCode = await GetAdminSessionCodeAsync();
        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var initialUsername = $"usr_{uniqueSuffix}";

        // Project 1 and UserGroup 1 exist from seed.ci.njson
        const long projectId = 1;
        const long userGroupId = 1;

        // 1. CREATE AdaptorUser
        var createModel = new CreateAdaptorUserModel
        {
            Username = initialUsername,
            SessionCode = sessionCode
        };

        var createdUser = await _client.PostJsonAsync<CreateAdaptorUserModel, AdaptorUserCreatedExt>(
            "/heappe/Management/AdaptorUser", createModel);
        createdUser.Should().NotBeNull();
        createdUser.Username.Should().Be(initialUsername);

        var currentUsername = initialUsername;

        try
        {
            // 2. READ AdaptorUser by Username
            var fetchedUser = await _client.GetJsonAsync<AdaptorUserExt>(
                $"/heappe/Management/AdaptorUser?username={currentUsername}&sessionCode={sessionCode}");
            fetchedUser.Should().NotBeNull();
            fetchedUser.Username.Should().Be(currentUsername);

            // 3. LIST AdaptorUsers
            var allUsers = await _client.GetJsonAsync<List<AdaptorUserExt>>(
                $"/heappe/Management/AdaptorUsers?sessionCode={sessionCode}");
            allUsers.Should().NotBeNull();
            allUsers.Should().Contain(u => u.Username == currentUsername);

            // 4. MODIFY AdaptorUser (Rename)
            var newUsername = $"renamed_{uniqueSuffix}";
            var modifyModel = new ModifyAdaptorUserModel
            {
                OldUsername = currentUsername,
                NewUsername = newUsername,
                SessionCode = sessionCode
            };

            var modifiedUser = await _client.PutJsonAsync<ModifyAdaptorUserModel, AdaptorUserExt>(
                "/heappe/Management/AdaptorUser", modifyModel);
            modifiedUser.Should().NotBeNull();
            modifiedUser.Username.Should().Be(newUsername);
            currentUsername = newUsername;

            // 5. BLOCK and UNBLOCK User
            var blockModel = new SetAdaptorUserBlockStatusModel
            {
                Username = currentUsername,
                IsBlocked = true,
                SessionCode = sessionCode
            };
            var blockResp = await _client.PostJsonAsync("/heappe/Management/SetAdaptorUserBlockStatus", blockModel);
            blockResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var unblockModel = new SetAdaptorUserBlockStatusModel
            {
                Username = currentUsername,
                IsBlocked = false,
                SessionCode = sessionCode
            };
            var unblockResp = await _client.PostJsonAsync("/heappe/Management/SetAdaptorUserBlockStatus", unblockModel);
            unblockResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 6. ASSIGN TO PROJECT
            var assignProjectModel = new AssignAdaptorUserToProjectModel
            {
                Username = currentUsername,
                ProjectId = projectId,
                Role = AdaptorUserRoleType.Submitter,
                SessionCode = sessionCode
            };
            var assignProjResp = await _client.PostJsonAsync("/heappe/Management/AssignAdaptorUserToProject", assignProjectModel);
            assignProjResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify User in Project
            var projectUsers = await _client.GetJsonAsync<List<AdaptorUserExt>>(
                $"/heappe/Management/AdaptorUsersInProject?projectId={projectId}&sessionCode={sessionCode}");
            projectUsers.Should().NotBeNull();
            projectUsers.Should().Contain(u => u.Username == currentUsername);

            // Remove User from Project
            var removeProjResp = await _client.DeleteJsonAsync("/heappe/Management/RemoveAdaptorUserFromProject", assignProjectModel);
            removeProjResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 7. ASSIGN TO USER GROUP
            var assignGroupModel = new AssignAdaptorUserToUserGroupModel
            {
                Username = currentUsername,
                UserGroupId = userGroupId,
                Role = AdaptorUserRoleType.Submitter,
                SessionCode = sessionCode
            };
            var assignGrpResp = await _client.PostJsonAsync("/heappe/Management/AssignAdaptorUserToUserGroup", assignGroupModel);
            assignGrpResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // Verify User in UserGroup
            var groupUsers = await _client.GetJsonAsync<List<AdaptorUserExt>>(
                $"/heappe/Management/AdaptorUsersInUserGroup?userGroupId={userGroupId}&sessionCode={sessionCode}");
            groupUsers.Should().NotBeNull();
            groupUsers.Should().Contain(u => u.Username == currentUsername);

            // Remove User from UserGroup
            var removeGrpResp = await _client.PostJsonAsync("/heappe/Management/RemoveAdaptorUserFromUserGroup", assignGroupModel);
            removeGrpResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // 8. SYSTEM ROLES
            var assignSystemRoleModel = new AssignSystemRoleToUserModel
            {
                Username = currentUsername,
                Role = AdaptorUserRoleType.Reporter,
                SessionCode = sessionCode
            };
            var assignSysRoleResp = await _client.PostJsonAsync("/heappe/Management/AssignSystemRoleToUser", assignSystemRoleModel);
            assignSysRoleResp.StatusCode.Should().Be(HttpStatusCode.OK);

            // List System Role Assignments
            var sysRoleAssignments = await _client.GetAsync($"/heappe/Management/SystemRoleAssignments?sessionCode={sessionCode}");
            sysRoleAssignments.StatusCode.Should().Be(HttpStatusCode.OK);

            // Remove System Role from User
            var removeSystemRoleModel = new RemoveSystemRoleFromUserModel
            {
                Username = currentUsername,
                Role = AdaptorUserRoleType.Reporter,
                SessionCode = sessionCode
            };
            var removeSysRoleResp = await _client.PostJsonAsync("/heappe/Management/RemoveSystemRoleFromUser", removeSystemRoleModel);
            removeSysRoleResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            // 9. DELETE AdaptorUser
            var deleteModel = new DeleteAdaptorUserModel
            {
                Username = currentUsername,
                SessionCode = sessionCode
            };
            var deleteResp = await _client.DeleteJsonAsync("/heappe/Management/AdaptorUser", deleteModel);
            deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
