using Microsoft.EntityFrameworkCore;
using SkillsSwap.Models;

namespace SkillsSwap.Data;

internal static class DirectSchedulingModel
{
    internal static void Configure(ModelBuilder builder)
    {
        var requests = builder.Entity<LearningSessionRequest>();
        requests.HasOne<LearningRequest>().WithMany().HasForeignKey(x => x.LearningRequestId).OnDelete(DeleteBehavior.Restrict);
        requests.HasOne<LearningSession>().WithMany().HasForeignKey(x => x.FulfilledSessionId).OnDelete(DeleteBehavior.Restrict);
        requests.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        requests.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        requests.HasIndex(x => x.LearningRequestId).IsUnique()
            .HasFilter("[Status] = 0").HasDatabaseName("UX_LearningSessionRequests_Pending");
        requests.HasIndex(x => x.FulfilledSessionId).IsUnique()
            .HasFilter("[FulfilledSessionId] IS NOT NULL").HasDatabaseName("UX_LearningSessionRequests_FulfilledSession");
        requests.HasIndex(x => new { x.LearningRequestId, x.CreatedAtUtc, x.Id });
        requests.ToTable(t =>
        {
            t.HasCheckConstraint("CK_LearningSessionRequest_Status", "[Status] BETWEEN 0 AND 3");
            t.HasCheckConstraint("CK_LearningSessionRequest_Resolution", """
                ([Status] = 0 AND [FulfilledSessionId] IS NULL AND [ResolvedAtUtc] IS NULL AND [ResolvedByUserId] IS NULL)
                OR ([Status] IN (1, 2, 3) AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedByUserId] IS NOT NULL
                    AND (([Status] = 1 AND [FulfilledSessionId] IS NOT NULL AND [ResolvedByUserId] <> [RequestedByUserId])
                         OR ([Status] = 2 AND [FulfilledSessionId] IS NULL AND [ResolvedByUserId] = [RequestedByUserId])
                         OR ([Status] = 3 AND [FulfilledSessionId] IS NULL)))
                """);
            t.HasCheckConstraint("CK_LearningSessionRequest_Chronology", "[ResolvedAtUtc] IS NULL OR [ResolvedAtUtc] >= [CreatedAtUtc]");
        });

        var absences = builder.Entity<SessionAbsenceRequest>();
        absences.HasOne<LearningSession>().WithMany().HasForeignKey(x => x.LearningSessionId).OnDelete(DeleteBehavior.Restrict);
        absences.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        absences.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        absences.HasIndex(x => x.LearningSessionId).IsUnique()
            .HasFilter("[Status] = 0").HasDatabaseName("UX_SessionAbsenceRequests_Pending");
        absences.HasIndex(x => new { x.LearningSessionId, x.CreatedAtUtc, x.Id });
        absences.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SessionAbsenceRequest_Status", "[Status] BETWEEN 0 AND 4");
            t.HasCheckConstraint("CK_SessionAbsenceRequest_Reason", "LEN(LTRIM(RTRIM([Reason]))) > 0");
            t.HasCheckConstraint("CK_SessionAbsenceRequest_Resolution", """
                ([Status] = 0 AND [ResolvedAtUtc] IS NULL AND [ResolvedByUserId] IS NULL)
                OR ([Status] = 4 AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedByUserId] IS NULL)
                OR ([Status] IN (1, 2, 3) AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedByUserId] IS NOT NULL
                    AND (([Status] = 1 AND [ResolvedByUserId] = [RequestedByUserId])
                         OR ([Status] IN (2, 3) AND [ResolvedByUserId] <> [RequestedByUserId])))
                """);
            t.HasCheckConstraint("CK_SessionAbsenceRequest_Chronology", """
                [CreatedAtUtc] < [RequestedForStartsAtUtc]
                AND ([ResolvedAtUtc] IS NULL OR [ResolvedAtUtc] >= [CreatedAtUtc])
                AND ([Status] <> 1 OR [ResolvedAtUtc] < [RequestedForStartsAtUtc])
                AND ([Status] <> 4 OR [ResolvedAtUtc] >= [RequestedForStartsAtUtc])
                """);
        });

        var history = builder.Entity<SessionScheduleHistory>();
        history.HasOne<LearningSession>().WithMany().HasForeignKey(x => x.LearningSessionId).OnDelete(DeleteBehavior.Restrict);
        history.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
        history.HasIndex(x => new { x.LearningSessionId, x.ChangedAtUtc, x.Id });
        history.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SessionScheduleHistory_Durations",
                "[PreviousDurationMinutes] > 0 AND [NewDurationMinutes] IN (30, 45, 60, 90)");
            t.HasCheckConstraint("CK_SessionScheduleHistory_ActualChange",
                "[PreviousStartsAtUtc] <> [NewStartsAtUtc] OR [PreviousDurationMinutes] <> [NewDurationMinutes]");
            t.HasCheckConstraint("CK_SessionScheduleHistory_Chronology", "[ChangedAtUtc] < [NewStartsAtUtc]");
        });
    }
}
