using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GridMonitor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Provinces",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EskomId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                    table.UniqueConstraint("AK_Provinces_EskomId", x => x.EskomId);
                });

            migrationBuilder.CreateTable(
                name: "StageSnapshots",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Stage = table.Column<short>(type: "smallint", nullable: false),
                    RawText = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StageSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncRuns",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    MunicipalitiesProcessed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SuburbProcessed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeycloakId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Password = table.Column<string>(type: "text", nullable: true),
                    Tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Free"),
                    Active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Municipalities",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EskomId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ProvinceId = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.Id);
                    table.UniqueConstraint("AK_Municipalities_EskomId", x => x.EskomId);
                    table.ForeignKey(
                        name: "FK_Municipalities_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalSchema: "dbo",
                        principalTable: "Provinces",
                        principalColumn: "EskomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DailyCallLimit = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Suburbs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EskomId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    MunicipalityId = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suburbs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suburbs_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalSchema: "dbo",
                        principalTable: "Municipalities",
                        principalColumn: "EskomId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AlertSubscriptions",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SuburbId = table.Column<int>(type: "integer", nullable: false),
                    AlertMinutesBefore = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)30),
                    Active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    AlertSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertSubscriptions_AlertSubscriptions_AlertSubscriptionId",
                        column: x => x.AlertSubscriptionId,
                        principalSchema: "dbo",
                        principalTable: "AlertSubscriptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertSubscriptions_Suburbs_SuburbId",
                        column: x => x.SuburbId,
                        principalSchema: "dbo",
                        principalTable: "Suburbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleSlots",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SuburbId = table.Column<int>(type: "integer", nullable: false),
                    Stage = table.Column<short>(type: "smallint", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ScheduleDay = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DataHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleSlots_Suburbs_SuburbId",
                        column: x => x.SuburbId,
                        principalSchema: "dbo",
                        principalTable: "Suburbs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertChannels",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Destination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    WebhookSecret = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertChannels_AlertSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "dbo",
                        principalTable: "AlertSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlertLogs",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Destination = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IdemptotencyKey = table.Column<string>(type: "text", nullable: true),
                    Stage = table.Column<short>(type: "smallint", nullable: false),
                    Event = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AttemptCount = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertLogs_AlertSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalSchema: "dbo",
                        principalTable: "AlertSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Provinces",
                columns: new[] { "Id", "EskomId", "Name" },
                values: new object[,]
                {
                    { 1, 1, "Eastern Cape" },
                    { 2, 2, "Free State" },
                    { 3, 3, "Gauteng" },
                    { 4, 4, "KwaZulu-Natal" },
                    { 5, 5, "Limpopo" },
                    { 6, 6, "Mpumalanga" },
                    { 7, 7, "North West" },
                    { 8, 8, "Northern Cape" },
                    { 9, 9, "Western Cape" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertChannels_SubscriptionId",
                schema: "dbo",
                table: "AlertChannels",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_SentAt",
                schema: "dbo",
                table: "AlertLogs",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_AlertLogs_SubscriptionId",
                schema: "dbo",
                table: "AlertLogs",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscriptions_Active",
                schema: "dbo",
                table: "AlertSubscriptions",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscriptions_AlertSubscriptionId",
                schema: "dbo",
                table: "AlertSubscriptions",
                column: "AlertSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscriptions_SuburbId",
                schema: "dbo",
                table: "AlertSubscriptions",
                column: "SuburbId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertSubscriptions_UserId_SuburbId",
                schema: "dbo",
                table: "AlertSubscriptions",
                columns: new[] { "UserId", "SuburbId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                schema: "dbo",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId",
                schema: "dbo",
                table: "ApiKeys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_EskomId",
                schema: "dbo",
                table: "Municipalities",
                column: "EskomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipalities_ProvinceId",
                schema: "dbo",
                table: "Municipalities",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Provinces_EskomId",
                schema: "dbo",
                table: "Provinces",
                column: "EskomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_SuburbId",
                schema: "dbo",
                table: "ScheduleSlots",
                column: "SuburbId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSlots_SuburbId_Stage_ScheduleDay_StartTime",
                schema: "dbo",
                table: "ScheduleSlots",
                columns: new[] { "SuburbId", "Stage", "ScheduleDay", "StartTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StageSnapshots_CreatedAt",
                schema: "dbo",
                table: "StageSnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Suburbs_EskomId",
                schema: "dbo",
                table: "Suburbs",
                column: "EskomId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suburbs_MunicipalityId",
                schema: "dbo",
                table: "Suburbs",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Suburbs_Name",
                schema: "dbo",
                table: "Suburbs",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SyncRuns_StartedAt",
                schema: "dbo",
                table: "SyncRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_KeycloakId",
                schema: "dbo",
                table: "Users",
                columns: new[] { "Email", "KeycloakId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertChannels",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AlertLogs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ApiKeys",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ScheduleSlots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "StageSnapshots",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "SyncRuns",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AlertSubscriptions",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Suburbs",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Municipalities",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Provinces",
                schema: "dbo");
        }
    }
}
