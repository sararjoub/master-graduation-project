using System.ComponentModel.DataAnnotations;
using SkillsSwap.Models;

namespace SkillsSwap.Tests;

public sealed class ProfileSetupValidationTests
{
    [Fact]
    public void Teaching_skills_can_be_skipped()
    {
        var model=new ProfileSetupViewModel { FullName="Valid Member",LearnSkillIds=[1],TeachSkillIds=[] };
        var results=new List<ValidationResult>();

        var valid=Validator.TryValidateObject(model,new ValidationContext(model),results,true);

        Assert.True(valid);
        Assert.Empty(results);
    }

    [Fact]
    public void At_least_one_learning_skill_remains_required()
    {
        var model=new ProfileSetupViewModel { FullName="Valid Member",LearnSkillIds=[],TeachSkillIds=[] };
        var results=new List<ValidationResult>();

        var valid=Validator.TryValidateObject(model,new ValidationContext(model),results,true);

        Assert.False(valid);
        Assert.Contains(results,result=>result.MemberNames.Contains(nameof(ProfileSetupViewModel.LearnSkillIds)));
    }
}
