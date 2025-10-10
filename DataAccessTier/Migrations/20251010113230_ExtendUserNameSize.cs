using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HEAppE.DataAccessTier.Migrations
{
    /// <inheritdoc />
    public partial class ExtendUserNameSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounting",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Formula = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidityFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Synchronize = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserType = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUserRole",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserRole", x => x.Id);
                    table.UniqueConstraint("AK_AdaptorUserRole_Name", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "ClusterAuthenticationCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    AuthenticationType = table.Column<int>(type: "int", nullable: false),
                    CipherType = table.Column<int>(type: "int", nullable: false),
                    PublicKeyFingerprint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublicKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsGenerated = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterAuthenticationCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClusterNodeTypeAggregation",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AllocationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidityFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidityTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeTypeAggregation", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClusterProxyConnection",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Host = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterProxyConnection", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contact",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublicKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contact", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RelativePath = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameSpecification = table.Column<int>(type: "int", nullable: false),
                    SynchronizationType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileSpecification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredential",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredential", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackInstance",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InstanceUrl = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackInstance", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackProjectDomain",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UID = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackProjectDomain", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountingString = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UseAccountingStringForScheduler = table.Column<bool>(type: "bit", nullable: false),
                    UsageType = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsOneToOneMapping = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackSession",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationCredentialsId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationCredentialsSecret = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuthenticationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackSession", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackSession_AdaptorUser_UserId",
                        column: x => x.UserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionCode",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniqueCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AuthenticationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionCode_AdaptorUser_UserId",
                        column: x => x.UserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClusterNodeTypeAggregationAccounting",
                columns: table => new
                {
                    ClusterNodeTypeAggregationId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeTypeAggregationAccounting", x => new { x.ClusterNodeTypeAggregationId, x.AccountingId });
                    table.ForeignKey(
                        name: "FK_ClusterNodeTypeAggregationAccounting_Accounting_AccountingId",
                        column: x => x.AccountingId,
                        principalTable: "Accounting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClusterNodeTypeAggregationAccounting_ClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                        column: x => x.ClusterNodeTypeAggregationId,
                        principalTable: "ClusterNodeTypeAggregation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cluster",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MasterNodeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DomainName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Port = table.Column<int>(type: "int", nullable: true),
                    TimeZone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    UpdateJobStateByServiceAccount = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SchedulerType = table.Column<int>(type: "int", nullable: false),
                    ConnectionProtocol = table.Column<int>(type: "int", nullable: false),
                    ProxyConnectionId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cluster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cluster_ClusterProxyConnection_ProxyConnectionId",
                        column: x => x.ProxyConnectionId,
                        principalTable: "ClusterProxyConnection",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OpenStackDomain",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UID = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OpenStackInstanceId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackDomain", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackDomain_OpenStackInstance_OpenStackInstanceId",
                        column: x => x.OpenStackInstanceId,
                        principalTable: "OpenStackInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountingState",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingStateType = table.Column<int>(type: "int", nullable: false),
                    ComputingStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComputingEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountingState", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountingState_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUserGroup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdaptorUserGroup_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProjectClusterNodeTypeAggregation",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ClusterNodeTypeAggregationId = table.Column<long>(type: "bigint", nullable: false),
                    AllocationAmount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectClusterNodeTypeAggregation", x => new { x.ProjectId, x.ClusterNodeTypeAggregationId });
                    table.ForeignKey(
                        name: "FK_ProjectClusterNodeTypeAggregation_ClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                        column: x => x.ClusterNodeTypeAggregationId,
                        principalTable: "ClusterNodeTypeAggregation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectClusterNodeTypeAggregation_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectContact",
                columns: table => new
                {
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ContactId = table.Column<long>(type: "bigint", nullable: false),
                    IsPI = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectContact", x => new { x.ProjectId, x.ContactId });
                    table.ForeignKey(
                        name: "FK_ProjectContact_Contact_ContactId",
                        column: x => x.ContactId,
                        principalTable: "Contact",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectContact_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubProject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubProject_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClusterProject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClusterId = table.Column<long>(type: "bigint", nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    LocalBasepath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterProject_Cluster_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Cluster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClusterProject_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileTransferMethod",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServerHostname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Protocol = table.Column<int>(type: "int", nullable: false),
                    Port = table.Column<int>(type: "int", nullable: true),
                    ClusterId = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTransferMethod", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileTransferMethod_Cluster_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Cluster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredentialDomain",
                columns: table => new
                {
                    OpenStackAuthenticationCredentialId = table.Column<long>(type: "bigint", nullable: false),
                    OpenStackDomainId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredentialDomain", x => new { x.OpenStackAuthenticationCredentialId, x.OpenStackDomainId });
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialDomain_OpenStackAuthenticationCredential_OpenStackAuthenticationCredentialId",
                        column: x => x.OpenStackAuthenticationCredentialId,
                        principalTable: "OpenStackAuthenticationCredential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialDomain_OpenStackDomain_OpenStackDomainId",
                        column: x => x.OpenStackDomainId,
                        principalTable: "OpenStackDomain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUserUserGroupRole",
                columns: table => new
                {
                    AdaptorUserId = table.Column<long>(type: "bigint", nullable: false),
                    AdaptorUserGroupId = table.Column<long>(type: "bigint", nullable: false),
                    AdaptorUserRoleId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserUserGroupRole", x => new { x.AdaptorUserId, x.AdaptorUserGroupId, x.AdaptorUserRoleId });
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroupRole_AdaptorUserGroup_AdaptorUserGroupId",
                        column: x => x.AdaptorUserGroupId,
                        principalTable: "AdaptorUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroupRole_AdaptorUserRole_AdaptorUserRoleId",
                        column: x => x.AdaptorUserRoleId,
                        principalTable: "AdaptorUserRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroupRole_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpenStackProject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UID = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    OpenStackDomainId = table.Column<long>(type: "bigint", nullable: false),
                    OpenStackProjectDomainId = table.Column<long>(type: "bigint", nullable: false),
                    AdaptorUserGroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpenStackProject_AdaptorUserGroup_AdaptorUserGroupId",
                        column: x => x.AdaptorUserGroupId,
                        principalTable: "AdaptorUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenStackProject_OpenStackDomain_OpenStackDomainId",
                        column: x => x.OpenStackDomainId,
                        principalTable: "OpenStackDomain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenStackProject_OpenStackProjectDomain_OpenStackProjectDomainId",
                        column: x => x.OpenStackProjectDomainId,
                        principalTable: "OpenStackProjectDomain",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClusterProjectCredentials",
                columns: table => new
                {
                    ClusterProjectId = table.Column<long>(type: "bigint", nullable: false),
                    ClusterAuthenticationCredentialsId = table.Column<long>(type: "bigint", nullable: false),
                    IsServiceAccount = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    AdaptorUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsInitialized = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterProjectCredentials", x => new { x.ClusterProjectId, x.ClusterAuthenticationCredentialsId });
                    table.ForeignKey(
                        name: "FK_ClusterProjectCredentials_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClusterProjectCredentials_ClusterAuthenticationCredentials_ClusterAuthenticationCredentialsId",
                        column: x => x.ClusterAuthenticationCredentialsId,
                        principalTable: "ClusterAuthenticationCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClusterProjectCredentials_ClusterProject_ClusterProjectId",
                        column: x => x.ClusterProjectId,
                        principalTable: "ClusterProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClusterNodeType",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumberOfNodes = table.Column<int>(type: "int", nullable: true),
                    CoresPerNode = table.Column<int>(type: "int", nullable: false),
                    Queue = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    QualityOfService = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ClusterAllocationName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    MaxWalltime = table.Column<int>(type: "int", nullable: true),
                    MaxNodesPerUser = table.Column<int>(type: "int", nullable: true),
                    MaxNodesPerJob = table.Column<int>(type: "int", nullable: true),
                    ClusterId = table.Column<long>(type: "bigint", nullable: true),
                    FileTransferMethodId = table.Column<long>(type: "bigint", nullable: true),
                    ClusterNodeTypeAggregationId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterNodeType_ClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                        column: x => x.ClusterNodeTypeAggregationId,
                        principalTable: "ClusterNodeTypeAggregation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClusterNodeType_Cluster_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Cluster",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ClusterNodeType_FileTransferMethod_FileTransferMethodId",
                        column: x => x.FileTransferMethodId,
                        principalTable: "FileTransferMethod",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "JobSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WaitingLimit = table.Column<int>(type: "int", nullable: true),
                    NotificationEmail = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NotifyOnAbort = table.Column<bool>(type: "bit", nullable: true),
                    NotifyOnFinish = table.Column<bool>(type: "bit", nullable: true),
                    NotifyOnStart = table.Column<bool>(type: "bit", nullable: true),
                    SubmitterId = table.Column<long>(type: "bigint", nullable: true),
                    SubmitterGroupId = table.Column<long>(type: "bigint", nullable: true),
                    ClusterId = table.Column<long>(type: "bigint", nullable: false),
                    FileTransferMethodId = table.Column<long>(type: "bigint", nullable: true),
                    SubProjectId = table.Column<long>(type: "bigint", nullable: true),
                    ClusterUserId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    WalltimeLimit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSpecification_AdaptorUserGroup_SubmitterGroupId",
                        column: x => x.SubmitterGroupId,
                        principalTable: "AdaptorUserGroup",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobSpecification_AdaptorUser_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobSpecification_ClusterAuthenticationCredentials_ClusterUserId",
                        column: x => x.ClusterUserId,
                        principalTable: "ClusterAuthenticationCredentials",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobSpecification_Cluster_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Cluster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobSpecification_FileTransferMethod_FileTransferMethodId",
                        column: x => x.FileTransferMethodId,
                        principalTable: "FileTransferMethod",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JobSpecification_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobSpecification_SubProject_SubProjectId",
                        column: x => x.SubProjectId,
                        principalTable: "SubProject",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OpenStackAuthenticationCredentialProject",
                columns: table => new
                {
                    OpenStackAuthenticationCredentialId = table.Column<long>(type: "bigint", nullable: false),
                    OpenStackProjectId = table.Column<long>(type: "bigint", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpenStackAuthenticationCredentialProject", x => new { x.OpenStackAuthenticationCredentialId, x.OpenStackProjectId });
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialProject_OpenStackAuthenticationCredential_OpenStackAuthenticationCredentialId",
                        column: x => x.OpenStackAuthenticationCredentialId,
                        principalTable: "OpenStackAuthenticationCredential",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OpenStackAuthenticationCredentialProject_OpenStackProject_OpenStackProjectId",
                        column: x => x.OpenStackProjectId,
                        principalTable: "OpenStackProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClusterNodeTypeRequestedGroup",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ClusterNodeTypeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeTypeRequestedGroup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterNodeTypeRequestedGroup_ClusterNodeType_ClusterNodeTypeId",
                        column: x => x.ClusterNodeTypeId,
                        principalTable: "ClusterNodeType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExtendedAllocationCommand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExecutableFile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CommandParameters = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PreparationScript = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsGeneric = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ClusterNodeTypeId = table.Column<long>(type: "bigint", nullable: true),
                    ProjectId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedFromId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandTemplate_ClusterNodeType_ClusterNodeTypeId",
                        column: x => x.ClusterNodeTypeId,
                        principalTable: "ClusterNodeType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommandTemplate_CommandTemplate_CreatedFromId",
                        column: x => x.CreatedFromId,
                        principalTable: "CommandTemplate",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommandTemplate_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubmittedJobInfo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalAllocatedTime = table.Column<double>(type: "float", nullable: true),
                    ProjectId = table.Column<long>(type: "bigint", nullable: true),
                    SubmitterId = table.Column<long>(type: "bigint", nullable: true),
                    SpecificationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedJobInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedJobInfo_AdaptorUser_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmittedJobInfo_JobSpecification_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "JobSpecification",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmittedJobInfo_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplateParameter",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Identifier = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Query = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CommandTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplateParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandTemplateParameter_CommandTemplate_CommandTemplateId",
                        column: x => x.CommandTemplateId,
                        principalTable: "CommandTemplate",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobArrays = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PlacementPolicy = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    IsExclusive = table.Column<bool>(type: "bit", nullable: false),
                    IsRerunnable = table.Column<bool>(type: "bit", nullable: false),
                    StandardInputFile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    StandardOutputFile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    StandardErrorFile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LocalDirectory = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ClusterTaskSubdirectory = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CpuHyperThreading = table.Column<bool>(type: "bit", nullable: true),
                    ClusterNodeTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CommandTemplateId = table.Column<long>(type: "bigint", nullable: false),
                    ProgressFileId = table.Column<long>(type: "bigint", nullable: true),
                    LogFileId = table.Column<long>(type: "bigint", nullable: true),
                    JobSpecificationId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinCores = table.Column<int>(type: "int", nullable: true),
                    MaxCores = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<long>(type: "bigint", nullable: true),
                    WalltimeLimit = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_ClusterNodeType_ClusterNodeTypeId",
                        column: x => x.ClusterNodeTypeId,
                        principalTable: "ClusterNodeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                        column: x => x.CommandTemplateId,
                        principalTable: "CommandTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_FileSpecification_LogFileId",
                        column: x => x.LogFileId,
                        principalTable: "FileSpecification",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskSpecification_FileSpecification_ProgressFileId",
                        column: x => x.ProgressFileId,
                        principalTable: "FileSpecification",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskSpecification_JobSpecification_JobSpecificationId",
                        column: x => x.JobSpecificationId,
                        principalTable: "JobSpecification",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskSpecification_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FileTransferTemporaryKey",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicKey = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedJobId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTransferTemporaryKey", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileTransferTemporaryKey_SubmittedJobInfo_SubmittedJobId",
                        column: x => x.SubmittedJobId,
                        principalTable: "SubmittedJobInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplateParameterValue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TemplateParameterId = table.Column<long>(type: "bigint", nullable: true),
                    TaskSpecificationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplateParameterValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandTemplateParameterValue_CommandTemplateParameter_TemplateParameterId",
                        column: x => x.TemplateParameterId,
                        principalTable: "CommandTemplateParameter",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CommandTemplateParameterValue_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentVariable",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    JobSpecificationId = table.Column<long>(type: "bigint", nullable: true),
                    TaskSpecificationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentVariable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_JobSpecification_JobSpecificationId",
                        column: x => x.JobSpecificationId,
                        principalTable: "JobSpecification",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubmittedTaskInfo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduledJobId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AllocatedTime = table.Column<double>(type: "float", nullable: true),
                    AllocatedCores = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CpuHyperThreading = table.Column<bool>(type: "bit", nullable: true),
                    NodeTypeId = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AllParameters = table.Column<string>(type: "text", nullable: true),
                    ProjectId = table.Column<long>(type: "bigint", nullable: true),
                    SpecificationId = table.Column<long>(type: "bigint", nullable: true),
                    SubmittedJobInfoId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedTaskInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedTaskInfo_ClusterNodeType_NodeTypeId",
                        column: x => x.NodeTypeId,
                        principalTable: "ClusterNodeType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmittedTaskInfo_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmittedTaskInfo_SubmittedJobInfo_SubmittedJobInfoId",
                        column: x => x.SubmittedJobInfoId,
                        principalTable: "SubmittedJobInfo",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SubmittedTaskInfo_TaskSpecification_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskDependency",
                columns: table => new
                {
                    TaskSpecificationId = table.Column<long>(type: "bigint", nullable: false),
                    ParentTaskSpecificationId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDependency", x => new { x.TaskSpecificationId, x.ParentTaskSpecificationId });
                    table.ForeignKey(
                        name: "FK_TaskDependency_TaskSpecification_ParentTaskSpecificationId",
                        column: x => x.ParentTaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDependency_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskParalizationSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaxCores = table.Column<int>(type: "int", nullable: false),
                    MPIProcesses = table.Column<int>(type: "int", nullable: true),
                    OpenMPThreads = table.Column<int>(type: "int", nullable: true),
                    TaskSpecificationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskParalizationSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskParalizationSpecification_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskSpecificationRequiredNode",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NodeName = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TaskSpecificationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSpecificationRequiredNode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSpecificationRequiredNode_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConsumedResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubmittedTaskInfoId = table.Column<long>(type: "bigint", nullable: false),
                    AccountingId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsumedResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsumedResources_Accounting_AccountingId",
                        column: x => x.AccountingId,
                        principalTable: "Accounting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConsumedResources_SubmittedTaskInfo_SubmittedTaskInfoId",
                        column: x => x.SubmittedTaskInfoId,
                        principalTable: "SubmittedTaskInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubmittedTaskAllocationNodeInfo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllocationNodeId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedTaskInfoId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedTaskAllocationNodeInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfo_SubmittedTaskInfoId",
                        column: x => x.SubmittedTaskInfoId,
                        principalTable: "SubmittedTaskInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountingState_ProjectId",
                table: "AccountingState",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserGroup_ProjectId",
                table: "AdaptorUserGroup",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroupRole_AdaptorUserGroupId",
                table: "AdaptorUserUserGroupRole",
                column: "AdaptorUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroupRole_AdaptorUserRoleId",
                table: "AdaptorUserUserGroupRole",
                column: "AdaptorUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cluster_ProxyConnectionId",
                table: "Cluster",
                column: "ProxyConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_ClusterId",
                table: "ClusterNodeType",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_ClusterNodeTypeAggregationId",
                table: "ClusterNodeType",
                column: "ClusterNodeTypeAggregationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_FileTransferMethodId",
                table: "ClusterNodeType",
                column: "FileTransferMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeAggregationAccounting_AccountingId",
                table: "ClusterNodeTypeAggregationAccounting",
                column: "AccountingId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeTypeRequestedGroup_ClusterNodeTypeId",
                table: "ClusterNodeTypeRequestedGroup",
                column: "ClusterNodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProject_ClusterId_ProjectId",
                table: "ClusterProject",
                columns: new[] { "ClusterId", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProject_ProjectId",
                table: "ClusterProject",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentials_AdaptorUserId",
                table: "ClusterProjectCredentials",
                column: "AdaptorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterProjectCredentials_ClusterAuthenticationCredentialsId",
                table: "ClusterProjectCredentials",
                column: "ClusterAuthenticationCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplate_ClusterNodeTypeId",
                table: "CommandTemplate",
                column: "ClusterNodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplate_CreatedFromId",
                table: "CommandTemplate",
                column: "CreatedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplate_ProjectId",
                table: "CommandTemplate",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplateParameter_CommandTemplateId",
                table: "CommandTemplateParameter",
                column: "CommandTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplateParameterValue_TaskSpecificationId",
                table: "CommandTemplateParameterValue",
                column: "TaskSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplateParameterValue_TemplateParameterId",
                table: "CommandTemplateParameterValue",
                column: "TemplateParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedResources_AccountingId",
                table: "ConsumedResources",
                column: "AccountingId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsumedResources_SubmittedTaskInfoId",
                table: "ConsumedResources",
                column: "SubmittedTaskInfoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_JobSpecificationId",
                table: "EnvironmentVariable",
                column: "JobSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_TaskSpecificationId",
                table: "EnvironmentVariable",
                column: "TaskSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferMethod_ClusterId",
                table: "FileTransferMethod",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTransferTemporaryKey_SubmittedJobId",
                table: "FileTransferTemporaryKey",
                column: "SubmittedJobId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_ClusterId",
                table: "JobSpecification",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_ClusterUserId",
                table: "JobSpecification",
                column: "ClusterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_FileTransferMethodId",
                table: "JobSpecification",
                column: "FileTransferMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_ProjectId",
                table: "JobSpecification",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_SubmitterGroupId",
                table: "JobSpecification",
                column: "SubmitterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_SubmitterId",
                table: "JobSpecification",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_SubProjectId",
                table: "JobSpecification",
                column: "SubProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentialDomain_OpenStackDomainId",
                table: "OpenStackAuthenticationCredentialDomain",
                column: "OpenStackDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackAuthenticationCredentialProject_OpenStackProjectId",
                table: "OpenStackAuthenticationCredentialProject",
                column: "OpenStackProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackDomain_OpenStackInstanceId",
                table: "OpenStackDomain",
                column: "OpenStackInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackProject_AdaptorUserGroupId",
                table: "OpenStackProject",
                column: "AdaptorUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackProject_OpenStackDomainId",
                table: "OpenStackProject",
                column: "OpenStackDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackProject_OpenStackProjectDomainId",
                table: "OpenStackProject",
                column: "OpenStackProjectDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_OpenStackSession_UserId",
                table: "OpenStackSession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_AccountingString",
                table: "Project",
                column: "AccountingString",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectClusterNodeTypeAggregation_ClusterNodeTypeAggregationId",
                table: "ProjectClusterNodeTypeAggregation",
                column: "ClusterNodeTypeAggregationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectContact_ContactId",
                table: "ProjectContact",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionCode_UserId",
                table: "SessionCode",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_ProjectId",
                table: "SubmittedJobInfo",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SpecificationId",
                table: "SubmittedJobInfo",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SubmitterId",
                table: "SubmittedJobInfo",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskAllocationNodeInfo_SubmittedTaskInfoId",
                table: "SubmittedTaskAllocationNodeInfo",
                column: "SubmittedTaskInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_NodeTypeId",
                table: "SubmittedTaskInfo",
                column: "NodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_ProjectId",
                table: "SubmittedTaskInfo",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SpecificationId",
                table: "SubmittedTaskInfo",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId",
                table: "SubmittedTaskInfo",
                column: "SubmittedJobInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_Identifier_ProjectId",
                table: "SubProject",
                columns: new[] { "Identifier", "ProjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubProject_ProjectId",
                table: "SubProject",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDependency_ParentTaskSpecificationId",
                table: "TaskDependency",
                column: "ParentTaskSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskParalizationSpecification_TaskSpecificationId",
                table: "TaskParalizationSpecification",
                column: "TaskSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_ClusterNodeTypeId",
                table: "TaskSpecification",
                column: "ClusterNodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_CommandTemplateId",
                table: "TaskSpecification",
                column: "CommandTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_JobSpecificationId",
                table: "TaskSpecification",
                column: "JobSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_LogFileId",
                table: "TaskSpecification",
                column: "LogFileId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_ProgressFileId",
                table: "TaskSpecification",
                column: "ProgressFileId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecification_ProjectId",
                table: "TaskSpecification",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskSpecificationRequiredNode_TaskSpecificationId",
                table: "TaskSpecificationRequiredNode",
                column: "TaskSpecificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountingState");

            migrationBuilder.DropTable(
                name: "AdaptorUserUserGroupRole");

            migrationBuilder.DropTable(
                name: "ClusterNodeTypeAggregationAccounting");

            migrationBuilder.DropTable(
                name: "ClusterNodeTypeRequestedGroup");

            migrationBuilder.DropTable(
                name: "ClusterProjectCredentials");

            migrationBuilder.DropTable(
                name: "CommandTemplateParameterValue");

            migrationBuilder.DropTable(
                name: "ConsumedResources");

            migrationBuilder.DropTable(
                name: "EnvironmentVariable");

            migrationBuilder.DropTable(
                name: "FileTransferTemporaryKey");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentialDomain");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredentialProject");

            migrationBuilder.DropTable(
                name: "OpenStackSession");

            migrationBuilder.DropTable(
                name: "ProjectClusterNodeTypeAggregation");

            migrationBuilder.DropTable(
                name: "ProjectContact");

            migrationBuilder.DropTable(
                name: "SessionCode");

            migrationBuilder.DropTable(
                name: "SubmittedTaskAllocationNodeInfo");

            migrationBuilder.DropTable(
                name: "TaskDependency");

            migrationBuilder.DropTable(
                name: "TaskParalizationSpecification");

            migrationBuilder.DropTable(
                name: "TaskSpecificationRequiredNode");

            migrationBuilder.DropTable(
                name: "AdaptorUserRole");

            migrationBuilder.DropTable(
                name: "ClusterProject");

            migrationBuilder.DropTable(
                name: "CommandTemplateParameter");

            migrationBuilder.DropTable(
                name: "Accounting");

            migrationBuilder.DropTable(
                name: "OpenStackAuthenticationCredential");

            migrationBuilder.DropTable(
                name: "OpenStackProject");

            migrationBuilder.DropTable(
                name: "Contact");

            migrationBuilder.DropTable(
                name: "SubmittedTaskInfo");

            migrationBuilder.DropTable(
                name: "OpenStackDomain");

            migrationBuilder.DropTable(
                name: "OpenStackProjectDomain");

            migrationBuilder.DropTable(
                name: "SubmittedJobInfo");

            migrationBuilder.DropTable(
                name: "TaskSpecification");

            migrationBuilder.DropTable(
                name: "OpenStackInstance");

            migrationBuilder.DropTable(
                name: "CommandTemplate");

            migrationBuilder.DropTable(
                name: "FileSpecification");

            migrationBuilder.DropTable(
                name: "JobSpecification");

            migrationBuilder.DropTable(
                name: "ClusterNodeType");

            migrationBuilder.DropTable(
                name: "AdaptorUserGroup");

            migrationBuilder.DropTable(
                name: "AdaptorUser");

            migrationBuilder.DropTable(
                name: "ClusterAuthenticationCredentials");

            migrationBuilder.DropTable(
                name: "SubProject");

            migrationBuilder.DropTable(
                name: "ClusterNodeTypeAggregation");

            migrationBuilder.DropTable(
                name: "FileTransferMethod");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "Cluster");

            migrationBuilder.DropTable(
                name: "ClusterProxyConnection");
        }
    }
}
