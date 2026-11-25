using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsSwap.Migrations
{
    /// <inheritdoc />
    public partial class Phase04LearningSessionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reviews_LearningSessionId_ReviewerId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_LearningSessions_LearningRequestId",
                table: "LearningSessions");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "LearningSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAtUtc",
                table: "LearningSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CancelledByUserId",
                table: "LearningSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                table: "LearningSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "LearningSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LearnerCompletedAtUtc",
                table: "LearningSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LearningObjectives",
                table: "LearningSessions",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "LearningSessions",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "LearningSessions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "TeacherCompletedAtUtc",
                table: "LearningSessions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "LearningSessions",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EndedAtUtc",
                table: "LearningRequests",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EndedByUserId",
                table: "LearningRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SessionScheduleProposals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LearningSessionId = table.Column<long>(type: "bigint", nullable: false),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    ProposedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResolvedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionScheduleProposals", x => x.Id);
                    table.CheckConstraint("CK_SessionScheduleProposal_Duration", "[DurationMinutes] IN (30, 45, 60, 90)");
                    table.CheckConstraint("CK_SessionScheduleProposal_Resolution", "([Status] = 0 AND [ResolvedAtUtc] IS NULL AND [ResolvedByUserId] IS NULL) OR ([Status] BETWEEN 1 AND 4 AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedAtUtc] >= [CreatedAtUtc] AND (([Status] IN (1, 2) AND [ResolvedByUserId] IS NOT NULL AND [ResolvedByUserId] <> [ProposedByUserId]) OR ([Status] = 3 AND [ResolvedByUserId] IS NOT NULL AND [ResolvedByUserId] = [ProposedByUserId]) OR [Status] = 4))");
                    table.CheckConstraint("CK_SessionScheduleProposal_Revision", "[Revision] > 0");
                    table.CheckConstraint("CK_SessionScheduleProposal_Status", "[Status] BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_SessionScheduleProposals_AspNetUsers_ProposedByUserId",
                        column: x => x.ProposedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionScheduleProposals_AspNetUsers_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SessionScheduleProposals_LearningSessions_LearningSessionId",
                        column: x => x.LearningSessionId,
                        principalTable: "LearningSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_LearningSessionId",
                table: "Reviews",
                column: "LearningSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessions_CancelledByUserId",
                table: "LearningSessions",
                column: "CancelledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessions_LearningRequestId",
                table: "LearningSessions",
                column: "LearningRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessions_Status_StartsAt",
                table: "LearningSessions",
                columns: new[] { "Status", "StartsAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningSession_CancellationMetadata",
                table: "LearningSessions",
                sql: "([CancelledAtUtc] IS NULL AND [CancelledByUserId] IS NULL AND [CancellationReason] IS NULL) OR ([Status] = 2 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByUserId] IS NOT NULL AND [CancellationReason] IS NOT NULL AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningSession_CompletionMetadata",
                table: "LearningSessions",
                sql: "([CompletedAtUtc] IS NULL AND ([LearnerCompletedAtUtc] IS NULL OR [TeacherCompletedAtUtc] IS NULL)) OR ([Status] = 1 AND [CompletedAtUtc] IS NOT NULL AND [LearnerCompletedAtUtc] IS NOT NULL AND [TeacherCompletedAtUtc] IS NOT NULL AND [CompletedAtUtc] >= [LearnerCompletedAtUtc] AND [CompletedAtUtc] >= [TeacherCompletedAtUtc])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningSession_CompletionState",
                table: "LearningSessions",
                sql: "([LearnerCompletedAtUtc] IS NULL AND [TeacherCompletedAtUtc] IS NULL) OR [Status] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningSession_Duration",
                table: "LearningSessions",
                sql: "[DurationMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningSession_Status",
                table: "LearningSessions",
                sql: "[Status] BETWEEN 0 AND 7");

            migrationBuilder.CreateIndex(
                name: "IX_LearningRequests_EndedByUserId",
                table: "LearningRequests",
                column: "EndedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_LearningRequests_ActivePeer",
                table: "LearningRequests",
                columns: new[] { "LearnerId", "TeacherId", "SkillId" },
                unique: true,
                filter: "[IsExpertBooking] = 0 AND [EndedAtUtc] IS NULL AND [Status] IN (0, 1)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningRequest_EndMetadata",
                table: "LearningRequests",
                sql: "([EndedAtUtc] IS NULL AND [EndedByUserId] IS NULL) OR ([EndedAtUtc] IS NOT NULL AND [EndedByUserId] IS NOT NULL AND [Status] = 1 AND [IsExpertBooking] = 0 AND ([EndedByUserId] = [LearnerId] OR [EndedByUserId] = [TeacherId]))");

            migrationBuilder.CreateIndex(
                name: "IX_SessionScheduleProposals_LearningSessionId_Revision",
                table: "SessionScheduleProposals",
                columns: new[] { "LearningSessionId", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SessionScheduleProposals_ProposedByUserId",
                table: "SessionScheduleProposals",
                column: "ProposedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionScheduleProposals_ResolvedByUserId",
                table: "SessionScheduleProposals",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "UX_SessionScheduleProposals_Pending",
                table: "SessionScheduleProposals",
                column: "LearningSessionId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_LearningRequests_AspNetUsers_EndedByUserId",
                table: "LearningRequests",
                column: "EndedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningSessions_AspNetUsers_CancelledByUserId",
                table: "LearningSessions",
                column: "CancelledByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningRequests_AspNetUsers_EndedByUserId",
                table: "LearningRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningSessions_AspNetUsers_CancelledByUserId",
                table: "LearningSessions");

            migrationBuilder.DropTable(
                name: "SessionScheduleProposals");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_LearningSessionId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_LearningSessions_CancelledByUserId",
                table: "LearningSessions");

            migrationBuilder.DropIndex(
                name: "IX_LearningSessions_LearningRequestId",
                table: "LearningSessions");

            migrationBuilder.DropIndex(
                name: "IX_LearningSessions_Status_StartsAt",
                table: "LearningSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningSession_CancellationMetadata",
                table: "LearningSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningSession_CompletionMetadata",
                table: "LearningSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningSession_CompletionState",
                table: "LearningSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningSession_Duration",
                table: "LearningSessions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningSession_Status",
                table: "LearningSessions");

            migrationBuilder.DropIndex(
                name: "IX_LearningRequests_EndedByUserId",
                table: "LearningRequests");

            migrationBuilder.DropIndex(
                name: "UX_LearningRequests_ActivePeer",
                table: "LearningRequests");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningRequest_EndMetadata",
                table: "LearningRequests");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "CancelledByUserId",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "LearnerCompletedAtUtc",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "LearningObjectives",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "TeacherCompletedAtUtc",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "LearningSessions");

            migrationBuilder.DropColumn(
                name: "EndedAtUtc",
                table: "LearningRequests");

            migrationBuilder.DropColumn(
                name: "EndedByUserId",
                table: "LearningRequests");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_LearningSessionId_ReviewerId",
                table: "Reviews",
                columns: new[] { "LearningSessionId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningSessions_LearningRequestId",
                table: "LearningSessions",
                column: "LearningRequestId",
                unique: true);
        }
    }
}
