using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Security;
using SkillsSwap.Services;

namespace SkillsSwap.Tests;

public sealed class PeerRequestTests
{
    [Theory]
    [InlineData("learner", "view", true)]
    [InlineData("teacher", "view", true)]
    [InlineData("stranger", "view", false)]
    [InlineData("teacher", "respond", true)]
    [InlineData("learner", "respond", false)]
    [InlineData("stranger", "respond", false)]
    [InlineData("learner", "cancel", true)]
    [InlineData("teacher", "cancel", false)]
    public async Task Authorization_checks_the_resource_participant(string participant, string action, bool allowed)
    {
        var request = new LearningRequest { LearnerId = Guid.NewGuid(), TeacherId = Guid.NewGuid() };
        var actor = participant == "learner" ? request.LearnerId :
            participant == "teacher" ? request.TeacherId : Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new(ClaimTypes.NameIdentifier, actor.ToString()), new(ClaimTypes.Role, "Member")], "Test"));
        var operation = action switch
        {
            "view" => RequestOperation.View,
            "respond" => RequestOperation.Respond,
            _ => RequestOperation.Cancel
        };
        var context = new AuthorizationHandlerContext([operation], principal, request);
        await new RequestAuthorization().HandleAsync(context);
        Assert.Equal(allowed, context.HasSucceeded);
    }

    [Theory]
    [InlineData(AccountStatus.Active, true, true, true, true)]
    [InlineData(AccountStatus.Suspended, true, true, true, false)]
    [InlineData(AccountStatus.Closed, true, true, true, false)]
    [InlineData(AccountStatus.Active, false, true, true, false)]
    [InlineData(AccountStatus.Active, true, false, true, false)]
    [InlineData(AccountStatus.Active, true, true, false, false)]
    public async Task Target_revalidates_current_teacher_eligibility(AccountStatus status, bool confirmed,
        bool complete, bool teaches, bool eligible)
    {
        await using var db = CreateDb();
        var learner = Member("Learner");
        var teacher = Member("Teacher");
        teacher.Status = status;
        teacher.EmailConfirmed = confirmed;
        teacher.IsProfileComplete = complete;
        var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), NormalizedName = "MEMBER" };
        var skill = new Skill { NameEn = "Skill", Slug = "phase03-test", SubCategory = new SubCategory { Category = new Category() } };
        db.AddRange(learner, teacher, role, skill);
        db.UserRoles.AddRange(new() { UserId = learner.Id, RoleId = role.Id }, new() { UserId = teacher.Id, RoleId = role.Id });
        db.UserSkills.Add(new() { User = teacher, Skill = skill, Intent = teaches ? SkillIntent.Teach : SkillIntent.Learn });
        await db.SaveChangesAsync();

        var service = new PeerRequestService(db);
        Assert.Equal(eligible, await service.TargetAsync(learner.Id, teacher.Id, skill.Id, default) is not null);
        Assert.Null(await service.TargetAsync(teacher.Id, teacher.Id, skill.Id, default));
        Assert.Null(await service.TargetAsync(learner.Id, Guid.NewGuid(), skill.Id, default));
        Assert.Null(await service.TargetAsync(learner.Id, teacher.Id, int.MaxValue, default));
        skill.Status = PublicationStatus.Archived;
        await db.SaveChangesAsync();
        Assert.Null(await service.TargetAsync(learner.Id, teacher.Id, skill.Id, default));
    }

    [Fact]
    public async Task Lists_and_details_are_scoped_and_history_is_retained()
    {
        await using var db = CreateDb();
        var learner = Member("Learner");
        var teacher = Member("Teacher");
        var stranger = Member("Stranger");
        var skill = new Skill { NameEn = "Skill", Slug = "phase03-test", SubCategory = new SubCategory { Category = new Category() } };
        var request = new LearningRequest { Learner = learner, Teacher = teacher, Skill = skill, Status = RequestStatus.Declined };
        db.AddRange(stranger, request);
        await db.SaveChangesAsync();

        var service = new PeerRequestService(db);
        Assert.Null(await service.FindActiveIdAsync(learner.Id, teacher.Id, skill.Id, default));
        Assert.Equal(request.Id, Assert.Single((await service.ListAsync(teacher.Id, "incoming", 1, default)).Rows).Id);
        Assert.Equal(request.Id, Assert.Single((await service.ListAsync(learner.Id, "outgoing", 1, default)).Rows).Id);
        Assert.Equal(RequestStatus.Declined, Assert.Single((await service.ListAsync(learner.Id, "completed", 1, default)).Rows).Status);
        Assert.Empty((await service.ListAsync(stranger.Id, "incoming", 1, default)).Rows);
        Assert.Null(await service.DetailAsync(request.Id, stranger.Id, default));
        Assert.Single((await service.ListAsync(learner.Id, "outgoing", 1, default, "Teacher", RequestStatus.Declined)).Rows);
        Assert.Empty((await service.ListAsync(learner.Id, "outgoing", 1, default, "Teacher", RequestStatus.Pending)).Rows);
        Assert.Empty((await service.ListAsync(learner.Id, "outgoing", 1, default, "Stranger")).Rows);
        request.Status = RequestStatus.Accepted;
        await db.SaveChangesAsync();
        Assert.Equal(request.Id, await service.FindActiveIdAsync(learner.Id, teacher.Id, skill.Id, default));
        Assert.Null(await service.FindActiveIdAsync(stranger.Id, teacher.Id, skill.Id, default));
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ApplicationUser Member(string name) => new()
    {
        Id = Guid.NewGuid(), FullName = name, EmailConfirmed = true, IsProfileComplete = true
    };
}
