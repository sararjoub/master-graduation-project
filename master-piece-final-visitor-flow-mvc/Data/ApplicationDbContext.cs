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
        b.Entity<LearningSession>().HasIndex(x => x.LearningRequestId).IsUnique();
        b.Entity<Message>().HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Message>().HasIndex(x => new { x.LearningRequestId, x.SentAtUtc });
        b.Entity<Review>().HasIndex(x => new { x.LearningSessionId, x.ReviewerId }).IsUnique();
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
}
