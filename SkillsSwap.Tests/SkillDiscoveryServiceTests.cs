using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SkillsSwap.Data;
using SkillsSwap.Models;
using SkillsSwap.Services;

namespace SkillsSwap.Tests;

public sealed class SkillDiscoveryServiceTests
{
    [Fact]
    public async Task Exact_teaching_skill_member_appears_without_reciprocal_match()
    {
        await using var db = CreateDb(); var data = await SeedCatalogueAsync(db);
        var learner = await AddMemberAsync(db, "Learner");
        var teacher = await AddMemberAsync(db, "Teacher");
        db.UserSkills.Add(new() { UserId=teacher.Id, SkillId=data.Skill.Id, Intent=SkillIntent.Teach, Level=SkillLevel.Intermediate });
        await db.SaveChangesAsync();
        var result = await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug, learner.Id, new());
        Assert.Equal(teacher.Id, Assert.Single(result!.Partners).UserId);
    }

    [Theory]
    [InlineData(SkillIntent.Learn, AccountStatus.Active, true, true, false)]
    [InlineData(SkillIntent.Teach, AccountStatus.Suspended, true, true, false)]
    [InlineData(SkillIntent.Teach, AccountStatus.Closed, true, true, false)]
    [InlineData(SkillIntent.Teach, AccountStatus.Active, false, true, false)]
    [InlineData(SkillIntent.Teach, AccountStatus.Active, true, false, false)]
    [InlineData(SkillIntent.Teach, AccountStatus.Active, true, true, true)]
    public async Task Partner_eligibility_is_enforced(SkillIntent intent, AccountStatus status,
        bool confirmed, bool complete, bool expected)
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var learner=await AddMemberAsync(db,"Learner");
        var candidate=await AddMemberAsync(db,"Candidate",status,confirmed,complete);
        db.UserSkills.Add(new(){UserId=candidate.Id,SkillId=data.Skill.Id,Intent=intent,Level=SkillLevel.Beginner});
        await db.SaveChangesAsync();
        var result=await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,learner.Id,new());
        Assert.Equal(expected,result!.Partners.Any(x=>x.UserId==candidate.Id));
    }

    [Fact]
    public async Task Current_user_and_other_skill_teachers_are_excluded()
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var learner=await AddMemberAsync(db,"Learner"); var other=await AddMemberAsync(db,"Other");
        db.UserSkills.AddRange(
            new(){UserId=learner.Id,SkillId=data.Skill.Id,Intent=SkillIntent.Teach,Level=SkillLevel.Beginner},
            new(){UserId=other.Id,SkillId=data.OtherSkill.Id,Intent=SkillIntent.Teach,Level=SkillLevel.Beginner});
        await db.SaveChangesAsync();
        var result=await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,learner.Id,new());
        Assert.Empty(result!.Partners);
    }

    [Theory]
    [InlineData(ExpertApplicationStatus.Approved,true)]
    [InlineData(ExpertApplicationStatus.UnderReview,false)]
    [InlineData(ExpertApplicationStatus.NeedsInformation,false)]
    [InlineData(ExpertApplicationStatus.Rejected,false)]
    [InlineData(ExpertApplicationStatus.Withdrawn,false)]
    public async Task Only_approved_skill_specific_experts_appear(ExpertApplicationStatus status,bool expected)
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var learner=await AddMemberAsync(db,"Learner"); var expert=await AddMemberAsync(db,"Expert");
        db.UserSkills.Add(new(){UserId=expert.Id,SkillId=data.Skill.Id,Intent=SkillIntent.Teach,Level=SkillLevel.Advanced});
        db.ExpertApplications.Add(new(){UserId=expert.Id,SkillId=data.Skill.Id,Status=status,Experience="Real experience",TeachingApproach="Practice"});
        await db.SaveChangesAsync();
        var result=await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,learner.Id,new());
        Assert.Equal(expected,result!.Experts.Any(x=>x.UserId==expert.Id));
    }

    [Fact]
    public async Task Approval_for_another_skill_does_not_verify_candidate()
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var learner=await AddMemberAsync(db,"Learner"); var expert=await AddMemberAsync(db,"Expert");
        db.UserSkills.Add(new(){UserId=expert.Id,SkillId=data.Skill.Id,Intent=SkillIntent.Teach,Level=SkillLevel.Advanced});
        db.ExpertApplications.Add(new(){UserId=expert.Id,SkillId=data.OtherSkill.Id,Status=ExpertApplicationStatus.Approved,Experience="X",TeachingApproach="Y"});
        await db.SaveChangesAsync();
        var result=await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,learner.Id,new());
        Assert.Empty(result!.Experts); Assert.Single(result.Partners);
    }

    [Theory]
    [InlineData(PublicationStatus.Archived,true,true)]
    [InlineData(PublicationStatus.Draft,true,true)]
    [InlineData(PublicationStatus.Published,false,true)]
    [InlineData(PublicationStatus.Published,true,false)]
    public async Task Unavailable_catalogue_hierarchy_is_not_exposed(PublicationStatus status,bool subActive,bool categoryActive)
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        data.Skill.Status=status; data.SubCategory.IsActive=subActive; data.Category.IsActive=categoryActive; await db.SaveChangesAsync();
        Assert.Null(await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,Guid.NewGuid(),new()));
    }

    [Fact]
    public async Task Invalid_skill_is_safe_and_empty_results_are_truthful()
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var service=new SkillDiscoveryService(db);
        Assert.Null(await service.DiscoverAsync("does-not-exist",Guid.NewGuid(),new()));
        var result=await service.DiscoverAsync(data.Skill.Slug,Guid.NewGuid(),new());
        Assert.NotNull(result); Assert.Empty(result.Partners); Assert.Empty(result.Experts);
    }

    [Fact]
    public void Public_discovery_model_does_not_expose_private_account_fields()
    {
        var names=typeof(DiscoveryPersonViewModel).GetProperties().Select(x=>x.Name).ToHashSet();
        Assert.DoesNotContain("Email",names); Assert.DoesNotContain("PasswordHash",names);
        Assert.DoesNotContain("SecurityStamp",names); Assert.DoesNotContain("PhoneNumber",names);
    }

    [Fact]
    public async Task Search_level_and_new_partner_without_reviews_work()
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var learner=await AddMemberAsync(db,"Learner"); var candidate=await AddMemberAsync(db,"Nora Teacher");
        db.UserSkills.Add(new(){UserId=candidate.Id,SkillId=data.Skill.Id,Intent=SkillIntent.Teach,Level=SkillLevel.Advanced}); await db.SaveChangesAsync();
        var result=await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,learner.Id,new(){Search="Nora",Level=SkillLevel.Advanced});
        var person=Assert.Single(result!.Partners); Assert.Null(person.Rating); Assert.Equal(0,person.ReviewCount);
    }

    [Fact]
    public async Task Administrative_accounts_are_not_learning_partners()
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var learner=await AddMemberAsync(db,"Learner"); var admin=await AddMemberAsync(db,"Admin member");
        var role=new IdentityRole<Guid>{Id=Guid.NewGuid(),Name="Admin",NormalizedName="ADMIN"}; db.Roles.Add(role);
        db.UserRoles.Add(new(){UserId=admin.Id,RoleId=role.Id});
        db.UserSkills.Add(new(){UserId=admin.Id,SkillId=data.Skill.Id,Intent=SkillIntent.Teach,Level=SkillLevel.Advanced});
        await db.SaveChangesAsync();
        var result=await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,learner.Id,new());
        Assert.Empty(result!.Partners);
    }

    [Fact]
    public async Task Unsafe_profile_image_url_is_not_exposed()
    {
        await using var db=CreateDb(); var data=await SeedCatalogueAsync(db);
        var learner=await AddMemberAsync(db,"Learner"); var teacher=await AddMemberAsync(db,"Teacher");
        teacher.ProfileImageUrl="javascript:alert(1)";
        db.UserSkills.Add(new(){UserId=teacher.Id,SkillId=data.Skill.Id,Intent=SkillIntent.Teach,Level=SkillLevel.Beginner});
        await db.SaveChangesAsync();
        var result=await new SkillDiscoveryService(db).DiscoverAsync(data.Skill.Slug,learner.Id,new());
        Assert.Null(Assert.Single(result!.Partners).ProfileImageUrl);
    }

    private static ApplicationDbContext CreateDb() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<(Category Category,SubCategory SubCategory,Skill Skill,Skill OtherSkill)> SeedCatalogueAsync(ApplicationDbContext db)
    {
        var category=new Category{NameEn="Technology",NameAr="Technology",Slug="technology",IsActive=true};
        var sub=new SubCategory{Category=category,NameEn="Development",NameAr="Development",Slug="development",IsActive=true};
        var skill=new Skill{SubCategory=sub,NameEn="C Sharp",NameAr="C Sharp",Slug="c-sharp",Description="Test",Status=PublicationStatus.Published};
        var other=new Skill{SubCategory=sub,NameEn="Python",NameAr="Python",Slug="python",Description="Test",Status=PublicationStatus.Published};
        db.AddRange(category,sub,skill,other);
        var memberRole=new IdentityRole<Guid>{Id=Guid.NewGuid(),Name="Member",NormalizedName="MEMBER"}; db.Roles.Add(memberRole);
        await db.SaveChangesAsync(); return(category,sub,skill,other);
    }

    private static async Task<ApplicationUser> AddMemberAsync(ApplicationDbContext db,string name,
        AccountStatus status=AccountStatus.Active,bool confirmed=true,bool complete=true)
    {
        var user=new ApplicationUser{Id=Guid.NewGuid(),FullName=name,UserName=$"{Guid.NewGuid()}@test.local",NormalizedUserName=Guid.NewGuid().ToString(),EmailConfirmed=confirmed,IsProfileComplete=complete,Status=status};
        db.Users.Add(user); await db.SaveChangesAsync();
        var roleId=await db.Roles.Where(x=>x.NormalizedName=="MEMBER").Select(x=>x.Id).SingleAsync();
        db.UserRoles.Add(new IdentityUserRole<Guid>{UserId=user.Id,RoleId=roleId}); await db.SaveChangesAsync(); return user;
    }
}
