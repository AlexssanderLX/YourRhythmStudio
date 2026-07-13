using System.ComponentModel.DataAnnotations;
using YourRhythmStudio.ViewModels.Auth;

namespace YourRhythmStudio.Tests;

public sealed class RegisterViewModelTests
{
    [Fact]
    public void ProfessorPlan_AllowsEmptySchoolName()
    {
        var model = ValidModel(planCode: "professor", schoolName: string.Empty);

        var errors = Validate(model);

        Assert.DoesNotContain(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.SchoolName)));
    }

    [Fact]
    public void SchoolPlan_RequiresSchoolName()
    {
        var model = ValidModel(planCode: "escola", schoolName: string.Empty);

        var errors = Validate(model);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.SchoolName)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("professor<script>")]
    public void PlanCode_RejectsMissingOrUnsupportedValues(string planCode)
    {
        var model = ValidModel(planCode: planCode, schoolName: "YourRhythm Studio");

        var errors = Validate(model);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.PlanCode)));
    }

    [Theory]
    [InlineData("professor")]
    [InlineData("escola")]
    public void PlanCode_AcceptsRequestablePlans(string planCode)
    {
        var model = ValidModel(planCode: planCode, schoolName: "YourRhythm Studio");

        var errors = Validate(model);

        Assert.DoesNotContain(errors, error => error.MemberNames.Contains(nameof(RegisterViewModel.PlanCode)));
    }

    private static RegisterViewModel ValidModel(string planCode, string schoolName)
    {
        return new RegisterViewModel
        {
            PlanCode = planCode,
            DisplayName = "Alexssander Almeida",
            Email = "alexssander@example.com",
            SchoolName = schoolName,
            Phone = "(11) 99999-9999"
        };
    }

    private static List<ValidationResult> Validate(RegisterViewModel model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
