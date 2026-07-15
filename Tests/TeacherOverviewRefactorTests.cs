using YourRhythmStudio.Application.Learning;
using YourRhythmStudio.Domain.Learning.Enums;  // AssignmentStatus (from DLL, not linked source)
using YourRhythmStudio.ViewModels.Learning;

namespace YourRhythmStudio.Tests;

public sealed class TeacherOverviewRefactorTests
{
    // ── DevolutivasViewModel ──────────────────────────────────────────────────

    [Fact]
    public void DevolutivasViewModel_StatusFilter_DefaultsToNull()
    {
        var vm = new DevolutivasViewModel
        {
            Missions = Array.Empty<MissionSummary>(),
            Pending  = Array.Empty<MissionSummary>()
        };

        Assert.Null(vm.StatusFilter);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("completed")]
    public void DevolutivasViewModel_AcceptsValidStatusFilter(string status)
    {
        var vm = new DevolutivasViewModel
        {
            Missions     = Array.Empty<MissionSummary>(),
            Pending      = Array.Empty<MissionSummary>(),
            StatusFilter = status
        };

        Assert.Equal(status, vm.StatusFilter);
    }

    // ── Filter logic (mirrors controller's switch expression) ────────────────

    private static IReadOnlyList<MissionSummary> ApplyFilter(
        IReadOnlyList<MissionSummary> all, string? rawStatus)
    {
        var normalized = rawStatus?.Trim().ToLowerInvariant();
        var valid = normalized is "pending" or "completed" ? normalized : null;

        return valid switch
        {
            "pending"   => all.Where(m => m.Status == AssignmentStatus.Pending
                                       || m.Status == AssignmentStatus.InProgress
                                       || m.Status == AssignmentStatus.AdjustmentsRequested).ToList(),
            "completed" => all.Where(m => m.Status == AssignmentStatus.Completed).ToList(),
            _           => all
        };
    }

    private static MissionSummary Stub(AssignmentStatus status) =>
        new(Id:                      Guid.NewGuid(),
            StudentProfileId:        Guid.NewGuid(),
            StudentName:             "Aluno",
            Title:                   "Missao",
            Status:                  status,
            CurrentRound:            1,
            SubmittedForReviewAtUtc: null,
            CreatedAtUtc:            DateTime.UtcNow,
            XpReward:                100,
            Rarity:                  default);

    [Fact]
    public void Filter_Pending_ReturnsPendingInProgressAndAdjustments()
    {
        var all = new[]
        {
            Stub(AssignmentStatus.Pending),
            Stub(AssignmentStatus.InProgress),
            Stub(AssignmentStatus.AdjustmentsRequested),
            Stub(AssignmentStatus.Completed),
            Stub(AssignmentStatus.AwaitingReview),
        };

        var result = ApplyFilter(all, "pending");

        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, m => m.Status == AssignmentStatus.Completed);
        Assert.DoesNotContain(result, m => m.Status == AssignmentStatus.AwaitingReview);
    }

    [Fact]
    public void Filter_Completed_ReturnsOnlyCompleted()
    {
        var all = new[]
        {
            Stub(AssignmentStatus.Pending),
            Stub(AssignmentStatus.Completed),
            Stub(AssignmentStatus.Completed),
        };

        var result = ApplyFilter(all, "completed");

        Assert.Equal(2, result.Count);
        Assert.All(result, m => Assert.Equal(AssignmentStatus.Completed, m.Status));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("INVALID")]
    [InlineData("all")]
    public void Filter_InvalidOrNull_ReturnsAll(string? raw)
    {
        var all = new[]
        {
            Stub(AssignmentStatus.Pending),
            Stub(AssignmentStatus.Completed),
            Stub(AssignmentStatus.InProgress),
        };

        var result = ApplyFilter(all, raw);

        Assert.Equal(all.Length, result.Count);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("PENDING")]
    [InlineData("Completed")]
    [InlineData("COMPLETED")]
    public void Filter_IsCaseInsensitive(string raw)
    {
        var all = new[]
        {
            Stub(AssignmentStatus.Pending),
            Stub(AssignmentStatus.Completed),
        };

        var result = ApplyFilter(all, raw);

        Assert.Equal(1, result.Count);
    }

    [Fact]
    public void Filter_Pending_EmptySourceReturnsEmpty()
    {
        var result = ApplyFilter(Array.Empty<MissionSummary>(), "pending");
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_Completed_EmptySourceReturnsEmpty()
    {
        var result = ApplyFilter(Array.Empty<MissionSummary>(), "completed");
        Assert.Empty(result);
    }

    // ── TeacherDashboardViewModel — no avgLevel / activityScore ─────────────

    [Fact]
    public void TeacherDashboardViewModel_DoesNotExposeAvgLevelProperty()
    {
        var props = typeof(TeacherDashboardViewModel).GetProperties();
        Assert.DoesNotContain(props, p => p.Name is "AvgLevel" or "avgLevel" or "ActivityScore" or "activityScore");
    }

    [Fact]
    public void TeacherDashboardSummary_DoesNotExposeAvgLevelProperty()
    {
        // If these were removed from the summary record they should not appear
        var props = typeof(TeacherDashboardSummary).GetProperties();
        Assert.DoesNotContain(props, p => p.Name is "AvgLevel" or "avgLevel" or "ActivityScore" or "activityScore");
    }
}
