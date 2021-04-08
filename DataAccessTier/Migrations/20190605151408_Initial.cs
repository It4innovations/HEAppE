using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HEAppE.DataAccessTier.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdaptorUserGroup",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    AccountingString = table.Column<string>(maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdministrationRole",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    AccessCode = table.Column<string>(maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrationRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RelativePath = table.Column<string>(maxLength: 50, nullable: false),
                    NameSpecification = table.Column<int>(nullable: false),
                    SynchronizationType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileSpecification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileTransferMethod",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ServerHostname = table.Column<string>(maxLength: 50, nullable: false),
                    SharedBasepath = table.Column<string>(maxLength: 50, nullable: false),
                    Protocol = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTransferMethod", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    MinCores = table.Column<int>(nullable: true),
                    MaxCores = table.Column<int>(nullable: true),
                    Priority = table.Column<int>(nullable: true),
                    Project = table.Column<string>(maxLength: 50, nullable: true),
                    WalltimeLimit = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Language",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    IsoCode = table.Column<string>(maxLength: 10, nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Language", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    Event = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PropertyChangeSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PropertyName = table.Column<string>(maxLength: 30, nullable: false),
                    ChangeMethod = table.Column<int>(nullable: false),
                    JobTemplateId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyChangeSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyChangeSpecification_JobTemplate_JobTemplateId",
                        column: x => x.JobTemplateId,
                        principalTable: "JobTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUser",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(maxLength: 50, nullable: false),
                    Password = table.Column<string>(maxLength: 50, nullable: true),
                    PublicKey = table.Column<string>(type: "text", nullable: true),
                    Synchronize = table.Column<bool>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    LanguageId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdaptorUser_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdministrationUser",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Email = table.Column<string>(maxLength: 50, nullable: false),
                    Password = table.Column<string>(maxLength: 30, nullable: false),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    LastModificationTime = table.Column<DateTime>(nullable: false),
                    Deleted = table.Column<bool>(nullable: false),
                    LanguageId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrationUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdministrationUser_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageLocalization",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    LocalizedHeader = table.Column<string>(maxLength: 100, nullable: false),
                    LocalizedText = table.Column<string>(type: "text", nullable: false),
                    LanguageId = table.Column<long>(nullable: true),
                    MessageTemplateId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageLocalization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageLocalization_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessageLocalization_MessageTemplate_MessageTemplateId",
                        column: x => x.MessageTemplateId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageTemplateParameter",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Identifier = table.Column<string>(maxLength: 30, nullable: false),
                    Query = table.Column<string>(maxLength: 500, nullable: false),
                    MessageTemplateId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTemplateParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageTemplateParameter_MessageTemplate_MessageTemplateId",
                        column: x => x.MessageTemplateId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Header = table.Column<string>(maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(maxLength: 20, nullable: true),
                    OccurrenceTime = table.Column<DateTime>(nullable: false),
                    SentTime = table.Column<DateTime>(nullable: true),
                    LanguageId = table.Column<long>(nullable: true),
                    MessageTemplateId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notification_Language_LanguageId",
                        column: x => x.LanguageId,
                        principalTable: "Language",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Notification_MessageTemplate_MessageTemplateId",
                        column: x => x.MessageTemplateId,
                        principalTable: "MessageTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdaptorUserUserGroup",
                columns: table => new
                {
                    AdaptorUserId = table.Column<long>(nullable: false),
                    AdaptorUserGroupId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdaptorUserUserGroup", x => new { x.AdaptorUserId, x.AdaptorUserGroupId });
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroup_AdaptorUserGroup_AdaptorUserGroupId",
                        column: x => x.AdaptorUserGroupId,
                        principalTable: "AdaptorUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdaptorUserUserGroup_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionCode",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    UniqueCode = table.Column<string>(maxLength: 50, nullable: false),
                    AuthenticationTime = table.Column<DateTime>(nullable: false),
                    LastAccessTime = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionCode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionCode_AdaptorUser_UserId",
                        column: x => x.UserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdministrationUserRole",
                columns: table => new
                {
                    AdministrationUserId = table.Column<long>(nullable: false),
                    AdministrationRoleId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdministrationUserRole", x => new { x.AdministrationRoleId, x.AdministrationUserId });
                    table.ForeignKey(
                        name: "FK_AdministrationUserRole_AdministrationRole_AdministrationRoleId",
                        column: x => x.AdministrationRoleId,
                        principalTable: "AdministrationRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdministrationUserRole_AdministrationUser_AdministrationUserId",
                        column: x => x.AdministrationUserId,
                        principalTable: "AdministrationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    MinCores = table.Column<int>(nullable: true),
                    MaxCores = table.Column<int>(nullable: true),
                    Priority = table.Column<int>(nullable: true),
                    Project = table.Column<string>(maxLength: 50, nullable: true),
                    WalltimeLimit = table.Column<int>(nullable: true),
                    WaitingLimit = table.Column<int>(nullable: true),
                    NotificationEmail = table.Column<string>(maxLength: 50, nullable: true),
                    PhoneNumber = table.Column<string>(maxLength: 20, nullable: true),
                    NotifyOnAbort = table.Column<bool>(nullable: true),
                    NotifyOnFinish = table.Column<bool>(nullable: true),
                    NotifyOnStart = table.Column<bool>(nullable: true),
                    SubmitterId = table.Column<long>(nullable: true),
                    SubmitterGroupId = table.Column<long>(nullable: true),
                    NodeTypeId = table.Column<long>(nullable: true),
                    ClusterUserId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSpecification_AdaptorUserGroup_SubmitterGroupId",
                        column: x => x.SubmitterGroupId,
                        principalTable: "AdaptorUserGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobSpecification_AdaptorUser_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceLimitation",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    TotalMaxCores = table.Column<int>(nullable: true),
                    MaxCoresPerJob = table.Column<int>(nullable: true),
                    NodeTypeId = table.Column<long>(nullable: true),
                    AdaptorUserId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceLimitation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceLimitation_AdaptorUser_AdaptorUserId",
                        column: x => x.AdaptorUserId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmittedJobInfo",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ScheduledJobId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    State = table.Column<int>(nullable: false),
                    Priority = table.Column<int>(nullable: false),
                    Project = table.Column<string>(maxLength: 50, nullable: true),
                    CreationTime = table.Column<DateTime>(nullable: false),
                    SubmitTime = table.Column<DateTime>(nullable: true),
                    StartTime = table.Column<DateTime>(nullable: true),
                    EndTime = table.Column<DateTime>(nullable: true),
                    TotalAllocatedTime = table.Column<double>(nullable: true),
                    AllParameters = table.Column<string>(type: "text", nullable: true),
                    SubmitterId = table.Column<long>(nullable: true),
                    NodeTypeId = table.Column<long>(nullable: true),
                    SpecificationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedJobInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedJobInfo_JobSpecification_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "JobSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmittedJobInfo_AdaptorUser_SubmitterId",
                        column: x => x.SubmitterId,
                        principalTable: "AdaptorUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClusterAuthenticationCredentials",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(maxLength: 50, nullable: false),
                    Password = table.Column<string>(maxLength: 50, nullable: true),
                    PrivateKeyFile = table.Column<string>(maxLength: 200, nullable: true),
                    PrivateKeyPassword = table.Column<string>(maxLength: 50, nullable: true),
                    ClusterId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterAuthenticationCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cluster",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    MasterNodeName = table.Column<string>(maxLength: 30, nullable: false),
                    SchedulerType = table.Column<int>(nullable: false),
                    ConnectionProtocol = table.Column<int>(nullable: false),
                    ServiceAccountCredentialsId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cluster", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cluster_ClusterAuthenticationCredentials_ServiceAccountCredentialsId",
                        column: x => x.ServiceAccountCredentialsId,
                        principalTable: "ClusterAuthenticationCredentials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClusterNodeType",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    ClusterLocalBasepath = table.Column<string>(maxLength: 100, nullable: false),
                    NumberOfNodes = table.Column<int>(nullable: true),
                    CoresPerNode = table.Column<int>(nullable: false),
                    Queue = table.Column<string>(maxLength: 30, nullable: true),
                    RequestedNodeGroups = table.Column<string>(maxLength: 500, nullable: true),
                    MaxWalltime = table.Column<int>(nullable: true),
                    ClusterId = table.Column<long>(nullable: true),
                    FileTransferMethodId = table.Column<long>(nullable: true),
                    JobTemplateId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClusterNodeType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClusterNodeType_Cluster_ClusterId",
                        column: x => x.ClusterId,
                        principalTable: "Cluster",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClusterNodeType_FileTransferMethod_FileTransferMethodId",
                        column: x => x.FileTransferMethodId,
                        principalTable: "FileTransferMethod",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClusterNodeType_JobTemplate_JobTemplateId",
                        column: x => x.JobTemplateId,
                        principalTable: "JobTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplate",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 30, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    Code = table.Column<string>(maxLength: 50, nullable: false),
                    ExecutableFile = table.Column<string>(maxLength: 100, nullable: false),
                    CommandParameters = table.Column<string>(maxLength: 200, nullable: true),
                    PreparationScript = table.Column<string>(maxLength: 1000, nullable: true),
                    ClusterNodeTypeId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandTemplate_ClusterNodeType_ClusterNodeTypeId",
                        column: x => x.ClusterNodeTypeId,
                        principalTable: "ClusterNodeType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplateParameter",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Identifier = table.Column<string>(maxLength: 20, nullable: false),
                    Query = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 200, nullable: false),
                    CommandTemplateId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplateParameter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandTemplateParameter_CommandTemplate_CommandTemplateId",
                        column: x => x.CommandTemplateId,
                        principalTable: "CommandTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskSpecification",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    MinCores = table.Column<int>(nullable: true),
                    MaxCores = table.Column<int>(nullable: true),
                    WalltimeLimit = table.Column<int>(nullable: true),
                    RequiredNodes = table.Column<string>(maxLength: 500, nullable: true),
                    IsExclusive = table.Column<bool>(nullable: true),
                    IsRerunnable = table.Column<bool>(nullable: true),
                    StandardInputFile = table.Column<string>(maxLength: 30, nullable: true),
                    StandardOutputFile = table.Column<string>(maxLength: 30, nullable: true),
                    StandardErrorFile = table.Column<string>(maxLength: 30, nullable: true),
                    LocalDirectory = table.Column<string>(maxLength: 200, nullable: true),
                    ClusterTaskSubdirectory = table.Column<string>(maxLength: 50, nullable: true),
                    CommandTemplateId = table.Column<long>(nullable: true),
                    ProgressFileId = table.Column<long>(nullable: true),
                    LogFileId = table.Column<long>(nullable: true),
                    JobSpecificationId = table.Column<long>(nullable: true),
                    TaskSpecificationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskSpecification", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_CommandTemplate_CommandTemplateId",
                        column: x => x.CommandTemplateId,
                        principalTable: "CommandTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_JobSpecification_JobSpecificationId",
                        column: x => x.JobSpecificationId,
                        principalTable: "JobSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_FileSpecification_LogFileId",
                        column: x => x.LogFileId,
                        principalTable: "FileSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_FileSpecification_ProgressFileId",
                        column: x => x.ProgressFileId,
                        principalTable: "FileSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskSpecification_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CommandTemplateParameterValue",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Value = table.Column<string>(maxLength: 1000, nullable: false),
                    TemplateParameterId = table.Column<long>(nullable: true),
                    TaskSpecificationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandTemplateParameterValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommandTemplateParameterValue_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CommandTemplateParameterValue_CommandTemplateParameter_TemplateParameterId",
                        column: x => x.TemplateParameterId,
                        principalTable: "CommandTemplateParameter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentVariable",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    Value = table.Column<string>(maxLength: 100, nullable: true),
                    JobSpecificationId = table.Column<long>(nullable: true),
                    JobTemplateId = table.Column<long>(nullable: true),
                    TaskSpecificationId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentVariable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_JobSpecification_JobSpecificationId",
                        column: x => x.JobSpecificationId,
                        principalTable: "JobSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_JobTemplate_JobTemplateId",
                        column: x => x.JobTemplateId,
                        principalTable: "JobTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_TaskSpecification_TaskSpecificationId",
                        column: x => x.TaskSpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmittedTaskInfo",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: false),
                    State = table.Column<int>(nullable: false),
                    AllocatedTime = table.Column<double>(nullable: true),
                    AllocatedCoreIds = table.Column<string>(nullable: true),
                    StartTime = table.Column<DateTime>(nullable: true),
                    EndTime = table.Column<DateTime>(nullable: true),
                    ErrorMessage = table.Column<string>(maxLength: 500, nullable: true),
                    AllParameters = table.Column<string>(type: "text", nullable: true),
                    SpecificationId = table.Column<long>(nullable: true),
                    SubmittedJobInfoId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmittedTaskInfo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmittedTaskInfo_TaskSpecification_SpecificationId",
                        column: x => x.SpecificationId,
                        principalTable: "TaskSpecification",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmittedTaskInfo_SubmittedJobInfo_SubmittedJobInfoId",
                        column: x => x.SubmittedJobInfoId,
                        principalTable: "SubmittedJobInfo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUser_LanguageId",
                table: "AdaptorUser",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_AdaptorUserUserGroup_AdaptorUserGroupId",
                table: "AdaptorUserUserGroup",
                column: "AdaptorUserGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrationUser_LanguageId",
                table: "AdministrationUser",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_AdministrationUserRole_AdministrationUserId",
                table: "AdministrationUserRole",
                column: "AdministrationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cluster_ServiceAccountCredentialsId",
                table: "Cluster",
                column: "ServiceAccountCredentialsId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterAuthenticationCredentials_ClusterId",
                table: "ClusterAuthenticationCredentials",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_ClusterId",
                table: "ClusterNodeType",
                column: "ClusterId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_FileTransferMethodId",
                table: "ClusterNodeType",
                column: "FileTransferMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodeType_JobTemplateId",
                table: "ClusterNodeType",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CommandTemplate_ClusterNodeTypeId",
                table: "CommandTemplate",
                column: "ClusterNodeTypeId");

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
                name: "IX_EnvironmentVariable_JobSpecificationId",
                table: "EnvironmentVariable",
                column: "JobSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_JobTemplateId",
                table: "EnvironmentVariable",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_TaskSpecificationId",
                table: "EnvironmentVariable",
                column: "TaskSpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_ClusterUserId",
                table: "JobSpecification",
                column: "ClusterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_NodeTypeId",
                table: "JobSpecification",
                column: "NodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_SubmitterGroupId",
                table: "JobSpecification",
                column: "SubmitterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_JobSpecification_SubmitterId",
                table: "JobSpecification",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLocalization_LanguageId",
                table: "MessageLocalization",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageLocalization_MessageTemplateId",
                table: "MessageLocalization",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTemplateParameter_MessageTemplateId",
                table: "MessageTemplateParameter",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_LanguageId",
                table: "Notification",
                column: "LanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_MessageTemplateId",
                table: "Notification",
                column: "MessageTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyChangeSpecification_JobTemplateId",
                table: "PropertyChangeSpecification",
                column: "JobTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLimitation_AdaptorUserId",
                table: "ResourceLimitation",
                column: "AdaptorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceLimitation_NodeTypeId",
                table: "ResourceLimitation",
                column: "NodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionCode_UserId",
                table: "SessionCode",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_NodeTypeId",
                table: "SubmittedJobInfo",
                column: "NodeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SpecificationId",
                table: "SubmittedJobInfo",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedJobInfo_SubmitterId",
                table: "SubmittedJobInfo",
                column: "SubmitterId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SpecificationId",
                table: "SubmittedTaskInfo",
                column: "SpecificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmittedTaskInfo_SubmittedJobInfoId",
                table: "SubmittedTaskInfo",
                column: "SubmittedJobInfoId");

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
                name: "IX_TaskSpecification_TaskSpecificationId",
                table: "TaskSpecification",
                column: "TaskSpecificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_ClusterAuthenticationCredentials_ClusterUserId",
                table: "JobSpecification",
                column: "ClusterUserId",
                principalTable: "ClusterAuthenticationCredentials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobSpecification_ClusterNodeType_NodeTypeId",
                table: "JobSpecification",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceLimitation_ClusterNodeType_NodeTypeId",
                table: "ResourceLimitation",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SubmittedJobInfo_ClusterNodeType_NodeTypeId",
                table: "SubmittedJobInfo",
                column: "NodeTypeId",
                principalTable: "ClusterNodeType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ClusterAuthenticationCredentials_Cluster_ClusterId",
                table: "ClusterAuthenticationCredentials",
                column: "ClusterId",
                principalTable: "Cluster",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cluster_ClusterAuthenticationCredentials_ServiceAccountCredentialsId",
                table: "Cluster");

            migrationBuilder.DropTable(
                name: "AdaptorUserUserGroup");

            migrationBuilder.DropTable(
                name: "AdministrationUserRole");

            migrationBuilder.DropTable(
                name: "CommandTemplateParameterValue");

            migrationBuilder.DropTable(
                name: "EnvironmentVariable");

            migrationBuilder.DropTable(
                name: "MessageLocalization");

            migrationBuilder.DropTable(
                name: "MessageTemplateParameter");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PropertyChangeSpecification");

            migrationBuilder.DropTable(
                name: "ResourceLimitation");

            migrationBuilder.DropTable(
                name: "SessionCode");

            migrationBuilder.DropTable(
                name: "SubmittedTaskInfo");

            migrationBuilder.DropTable(
                name: "AdministrationRole");

            migrationBuilder.DropTable(
                name: "AdministrationUser");

            migrationBuilder.DropTable(
                name: "CommandTemplateParameter");

            migrationBuilder.DropTable(
                name: "MessageTemplate");

            migrationBuilder.DropTable(
                name: "TaskSpecification");

            migrationBuilder.DropTable(
                name: "SubmittedJobInfo");

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
                name: "FileTransferMethod");

            migrationBuilder.DropTable(
                name: "JobTemplate");

            migrationBuilder.DropTable(
                name: "Language");

            migrationBuilder.DropTable(
                name: "ClusterAuthenticationCredentials");

            migrationBuilder.DropTable(
                name: "Cluster");
        }
    }
}
