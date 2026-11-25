using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Models;

namespace SkillsSwap.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<SubCategory> SubCategories => Set<SubCategory>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<ExpertApplication> ExpertApplications => Set<ExpertApplication>();
    public DbSet<ExpertAvailability> ExpertAvailabilities => Set<ExpertAvailability>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<LearningRequest> LearningRequests => Set<LearningRequest>();
    public DbSet<LearningSession> LearningSessions => Set<LearningSession>();
    public DbSet<SessionScheduleProposal> SessionScheduleProposals => Set<SessionScheduleProposal>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<CareerPath> CareerPaths => Set<CareerPath>();
    public DbSet<CareerPathStep> CareerPathSteps => Set<CareerPathStep>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<SubCategory>().HasIndex(x => new { x.CategoryId, x.Slug }).IsUnique();
        b.Entity<Skill>().HasIndex(x => x.Slug).IsUnique();
        b.Entity<UserSkill>().HasKey(x => new { x.UserId, x.SkillId, x.Intent });
        b.Entity<ExpertApplication>().HasIndex(x => new { x.UserId, x.SkillId }).IsUnique();
        b.Entity<ExpertAvailability>().ToTable(t => t.HasCheckConstraint("CK_ExpertAvailability_Time", "[StartsAt] < [EndsAt]"));
        b.Entity<Plan>().HasIndex(x => x.Code).IsUnique();
        b.Entity<Plan>().Property(x => x.MonthlyPrice).HasPrecision(10, 2);
        b.Entity<Plan>().Property(x => x.YearlyPrice).HasPrecision(10, 2);
        b.Entity<Payment>().Property(x => x.Amount).HasPrecision(10, 2);
        b.Entity<Payment>().HasIndex(x => new { x.Provider, x.ProviderReference }).IsUnique();

        b.Entity<LearningRequest>().HasOne(x => x.Learner).WithMany().HasForeignKey(x => x.LearnerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LearningRequest>().HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<LearningRequest>().ToTable(t => t.HasCheckConstraint("CK_LearningRequest_NotSelf", "[LearnerId] <> [TeacherId]"));
        ConfigureSessionFoundation(b);
        b.Entity<Message>().HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Message>().HasIndex(x => new { x.LearningRequestId, x.SentAtUtc });
        b.Entity<Review>().HasIndex(x => x.LearningSessionId).IsUnique();
        b.Entity<Review>().ToTable(t => t.HasCheckConstraint("CK_Review_Rating", "[Rating] BETWEEN 1 AND 5"));

        b.Entity<CareerPath>().HasIndex(x => new { x.UserId, x.Status });
        b.Entity<CareerPathStep>().HasIndex(x => new { x.CareerPathId, x.Order }).IsUnique();
        b.Entity<CareerPathStep>().ToTable(t => t.HasCheckConstraint("CK_CareerPathStep_Order", "[Order] > 0"));

        // Prevent cascade cycles and accidental loss of audit-worthy decisions.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            if (fk.PrincipalEntityType.ClrType == typeof(ApplicationUser)) fk.DeleteBehavior = DeleteBehavior.Restrict;

        b.Entity<Plan>().HasData(
            new Plan { Id = 1, Code = "FREE", Name = "Free", MonthlyPrice = 0, YearlyPrice = 0, MonthlyExpertCredits = 0 },
            new Plan { Id = 2, Code = "PLUS", Name = "Plus", MonthlyPrice = 9, YearlyPrice = 84, MonthlyExpertCredits = 1 });
        ModelSeed.Configure(b);
    }

    private static void ConfigureSessionFoundation(ModelBuilder b)
    {
        var request = b.Entity<LearningRequest>();
        request.HasIndex(x => x.LearnerId);
        request.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.EndedByUserId).OnDelete(DeleteBehavior.Restrict);
        request.HasIndex(x => new { x.LearnerId, x.TeacherId, x.SkillId })
            .HasDatabaseName("UX_LearningRequests_ActivePeer")
            .IsUnique().HasFilter("[IsExpertBooking] = 0 AND [EndedAtUtc] IS NULL AND [Status] IN (0, 1)");
        request.ToTable(t => t.HasCheckConstraint("CK_LearningRequest_EndMetadata",
            "([EndedAtUtc] IS NULL AND [EndedByUserId] IS NULL) OR " +
            "([EndedAtUtc] IS NOT NULL AND [EndedByUserId] IS NOT NULL AND [Status] = 1 AND [IsExpertBooking] = 0 " +
            "AND ([EndedByUserId] = [LearnerId] OR [EndedByUserId] = [TeacherId]))"));

        var session = b.Entity<LearningSession>();
        session.HasOne(x => x.LearningRequest).WithMany(x => x.Sessions).HasForeignKey(x => x.LearningRequestId);
        session.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);
        session.HasIndex(x => new { x.Status, x.StartsAt });
        session.Property(x => x.RowVersion).IsRowVersion();
        session.ToTable(t =>
        {
            t.HasCheckConstraint("CK_LearningSession_Duration", "[DurationMinutes] > 0");
            t.HasCheckConstraint("CK_LearningSession_Status", "[Status] BETWEEN 0 AND 7");
            t.HasCheckConstraint("CK_LearningSession_CancellationMetadata",
                "([CancelledAtUtc] IS NULL AND [CancelledByUserId] IS NULL AND [CancellationReason] IS NULL) OR " +
                "([Status] = 2 AND [CancelledAtUtc] IS NOT NULL AND [CancelledByUserId] IS NOT NULL " +
                "AND [CancellationReason] IS NOT NULL AND LEN(LTRIM(RTRIM([CancellationReason]))) > 0)");
            t.HasCheckConstraint("CK_LearningSession_CompletionMetadata",
                "([CompletedAtUtc] IS NULL AND ([LearnerCompletedAtUtc] IS NULL OR [TeacherCompletedAtUtc] IS NULL)) OR " +
                "([Status] = 1 AND [CompletedAtUtc] IS NOT NULL AND [LearnerCompletedAtUtc] IS NOT NULL " +
                "AND [TeacherCompletedAtUtc] IS NOT NULL AND [CompletedAtUtc] >= [LearnerCompletedAtUtc] " +
                "AND [CompletedAtUtc] >= [TeacherCompletedAtUtc])");
            t.HasCheckConstraint("CK_LearningSession_CompletionState",
                "([LearnerCompletedAtUtc] IS NULL AND [TeacherCompletedAtUtc] IS NULL) OR [Status] IN (0, 1)");
        });

        var proposal = b.Entity<SessionScheduleProposal>();
        proposal.HasOne(x => x.LearningSession).WithMany(x => x.ScheduleProposals)
            .HasForeignKey(x => x.LearningSessionId).OnDelete(DeleteBehavior.Restrict);
        proposal.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ProposedByUserId).OnDelete(DeleteBehavior.Restrict);
        proposal.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        proposal.HasIndex(x => new { x.LearningSessionId, x.Revision }).IsUnique();
        proposal.HasIndex(x => x.LearningSessionId).HasDatabaseName("UX_SessionScheduleProposals_Pending")
            .IsUnique().HasFilter("[Status] = 0");
        proposal.ToTable(t =>
        {
            t.HasCheckConstraint("CK_SessionScheduleProposal_Revision", "[Revision] > 0");
            t.HasCheckConstraint("CK_SessionScheduleProposal_Duration", "[DurationMinutes] IN (30, 45, 60, 90)");
            t.HasCheckConstraint("CK_SessionScheduleProposal_Status", "[Status] BETWEEN 0 AND 4");
            t.HasCheckConstraint("CK_SessionScheduleProposal_Resolution",
                "([Status] = 0 AND [ResolvedAtUtc] IS NULL AND [ResolvedByUserId] IS NULL) OR " +
                "([Status] BETWEEN 1 AND 4 AND [ResolvedAtUtc] IS NOT NULL AND [ResolvedAtUtc] >= [CreatedAtUtc] AND " +
                "(([Status] IN (1, 2) AND [ResolvedByUserId] IS NOT NULL AND [ResolvedByUserId] <> [ProposedByUserId]) OR " +
                "([Status] = 3 AND [ResolvedByUserId] IS NOT NULL AND [ResolvedByUserId] = [ProposedByUserId]) OR [Status] = 4))");
        });
    }
}
