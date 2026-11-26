using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillsSwap.Migrations
{
    /// <inheritdoc />
    public partial class Phase05AutomaticSessionCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningSession_CompletionMetadata",
                table: "LearningSessions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningSession_CompletionMetadata",
                table: "LearningSessions",
                sql: "([CompletedAtUtc] IS NULL AND ([LearnerCompletedAtUtc] IS NULL OR [TeacherCompletedAtUtc] IS NULL)) OR ([Status] = 1 AND [CompletedAtUtc] IS NOT NULL AND ([LearnerCompletedAtUtc] IS NULL OR [CompletedAtUtc] >= [LearnerCompletedAtUtc]) AND ([TeacherCompletedAtUtc] IS NULL OR [CompletedAtUtc] >= [TeacherCompletedAtUtc]))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_LearningSession_CompletionMetadata",
                table: "LearningSessions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LearningSession_CompletionMetadata",
                table: "LearningSessions",
                sql: "([CompletedAtUtc] IS NULL AND ([LearnerCompletedAtUtc] IS NULL OR [TeacherCompletedAtUtc] IS NULL)) OR ([Status] = 1 AND [CompletedAtUtc] IS NOT NULL AND [LearnerCompletedAtUtc] IS NOT NULL AND [TeacherCompletedAtUtc] IS NOT NULL AND [CompletedAtUtc] >= [LearnerCompletedAtUtc] AND [CompletedAtUtc] >= [TeacherCompletedAtUtc])");
        }
    }
}
