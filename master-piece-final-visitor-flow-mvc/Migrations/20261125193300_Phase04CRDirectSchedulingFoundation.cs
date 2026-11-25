using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsSwap.Migrations
{
    /// <inheritdoc />
    public partial class Phase04CRDirectSchedulingFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningSessionRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LearningRequestId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    FulfilledSessionId = table.Column<long>(type: "bigint", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningSessionRequests", x => x.Id);
                    table.CheckConstraint("CK_LearningSessionRequest_Chronology", "[ResolvedAtUtc] IS NULL OR [ResolvedAtUtc] >= [CreatedAtUtc]");
                    table.CheckConstraint("CK_LearningSessionRequest_Resolution", "([Status] = 0 AND [FulfilledSessionId] IS NULL AND [ResolvedAtUtc] IS NULL AND [ResolvedByUserId] IS NULL)\nOR ([Status] IN (1, 2, 3) AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedByUserId] IS NOT NULL\n    AND (([Status] = 1 AND [FulfilledSessionId] IS NOT NULL AND [ResolvedByUserId] <> [RequestedByUserId])\n         OR ([Status] = 2 AND [FulfilledSessionId] IS NULL AND [ResolvedByUserId] = [RequestedByUserId])\n         OR ([Status] = 3 AND [FulfilledSessionId] IS NULL)))");
                    table.CheckConstraint("CK_LearningSessionRequest_Status", "[Status] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_LearningSessionRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningSessionRequests_AspNetUsers_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningSessionRequests_LearningRequests_LearningRequestId",
                        column: x => x.LearningRequestId,
                        principalTable: "LearningRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningSessionRequests_LearningSessions_FulfilledSessionId",
                        column: x => x.FulfilledSessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionAbsenceRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LearningSessionId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RequestedForStartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionAbsenceRequests", x => x.Id);
                    table.CheckConstraint("CK_SessionAbsenceRequest_Chronology", "[CreatedAtUtc] < [RequestedForStartsAtUtc]\nAND ([ResolvedAtUtc] IS NULL OR [ResolvedAtUtc] >= [CreatedAtUtc])\nAND ([Status] <> 1 OR [ResolvedAtUtc] < [RequestedForStartsAtUtc])\nAND ([Status] <> 4 OR [ResolvedAtUtc] >= [RequestedForStartsAtUtc])");
                    table.CheckConstraint("CK_SessionAbsenceRequest_Reason", "LEN(LTRIM(RTRIM([Reason]))) > 0");
                    table.CheckConstraint("CK_SessionAbsenceRequest_Resolution", "([Status] = 0 AND [ResolvedAtUtc] IS NULL AND [ResolvedByUserId] IS NULL)\nOR ([Status] = 4 AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedByUserId] IS NULL)\nOR ([Status] IN (1, 2, 3) AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedByUserId] IS NOT NULL\n    AND (([Status] = 1 AND [ResolvedByUserId] = [RequestedByUserId])\n         OR ([Status] IN (2, 3) AND [ResolvedByUserId] <> [RequestedByUserId])))");
                    table.CheckConstraint("CK_SessionAbsenceRequest_Status", "[Status] BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_SessionAbsenceRequests_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionAbsenceRequests_AspNetUsers_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionAbsenceRequests_LearningSessions_LearningSessionId",
                        column: x => x.LearningSessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SessionScheduleHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LearningSessionId = table.Column<long>(type: "bigint", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NewStartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PreviousDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    NewDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    ChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionScheduleHistory", x => x.Id);
                    table.CheckConstraint("CK_SessionScheduleHistory_ActualChange", "[PreviousStartsAtUtc] <> [NewStartsAtUtc] OR [PreviousDurationMinutes] <> [NewDurationMinutes]");
                    table.CheckConstraint("CK_SessionScheduleHistory_Chronology", "[ChangedAtUtc] < [NewStartsAtUtc]");
                    table.CheckConstraint("CK_SessionScheduleHistory_Durations", "[PreviousDurationMinutes] > 0 AND [NewDurationMinutes] IN (30, 45, 60, 90)");
                    table.ForeignKey(
                        name: "FK_SessionScheduleHistory_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionScheduleHistory_LearningSessions_LearningSessionId",
                        column: x => x.LearningSessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessionRequests_LearningRequestId_CreatedAtUtc_Id",
                table: "LearningSessionRequests",
                columns: new[] { "LearningRequestId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessionRequests_RequestedByUserId",
                table: "LearningSessionRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessionRequests_ResolvedByUserId",
                table: "LearningSessionRequests",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_LearningSessionRequests_FulfilledSession",
                table: "LearningSessionRequests",
                column: "FulfilledSessionId",
                unique: true,
                filter: "[FulfilledSessionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_LearningSessionRequests_Pending",
                table: "LearningSessionRequests",
                column: "LearningRequestId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAbsenceRequests_LearningSessionId_CreatedAtUtc_Id",
                table: "SessionAbsenceRequests",
                columns: new[] { "LearningSessionId", "CreatedAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SessionAbsenceRequests_RequestedByUserId",
                table: "SessionAbsenceRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionAbsenceRequests_ResolvedByUserId",
                table: "SessionAbsenceRequests",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionAbsenceRequests_Pending",
                table: "SessionAbsenceRequests",
                column: "LearningSessionId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SessionScheduleHistory_ChangedByUserId",
                table: "SessionScheduleHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionScheduleHistory_LearningSessionId_ChangedAtUtc_Id",
                table: "SessionScheduleHistory",
                columns: new[] { "LearningSessionId", "ChangedAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningSessionRequests");

            migrationBuilder.DropTable(
                name: "SessionAbsenceRequests");

            migrationBuilder.DropTable(
                name: "SessionScheduleHistory");
        }
    }
}
