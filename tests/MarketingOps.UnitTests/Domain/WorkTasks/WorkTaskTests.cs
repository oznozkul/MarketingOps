using MarketingOps.Domain.Common;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.UnitTests.Domain.WorkTasks;

public class WorkTaskTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    private static WorkTask NewTask() => WorkTask.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 7, "LB-7",
        new TaskDraft("3 sayfada title tekrarlanıyor", "Ürün sayfalarından 3 tanesi etkileniyor.", "Etki",
            new TaskSuggestion(SuggestionKind.Text, "Özet", "Kalıp", []), ["google:title-links"], Team.Seo,
            [("Sonraki taramada sorun görülmüyor.", VerificationType.Automatic), ("Şablon güncellendi.", VerificationType.TeamConfirmation)]),
        T0);

    private static Guid TeamCriterion(WorkTask task) => task.Criteria.Single(c => c.VerificationType == VerificationType.TeamConfirmation).Id;
    private static Guid AutomaticCriterion(WorkTask task) => task.Criteria.Single(c => c.VerificationType == VerificationType.Automatic).Id;

    [Fact]
    public void New_task_is_todo_with_unmet_criteria()
    {
        var task = NewTask();
        task.Status.ShouldBe(WorkTaskStatus.Todo);
        task.Key.ShouldBe("LB-7");
        task.Criteria.ShouldAllBe(c => !c.IsMet);
        task.History.ShouldHaveSingleItem().Type.ShouldBe(TaskEventType.Created);
    }

    [Fact]
    public void Resolved_finding_meets_automatic_criteria_and_waits_for_team()
    {
        var task = NewTask();
        task.FindingResolved(5, T0.AddDays(1));

        task.Criteria.Single(c => c.VerificationType == VerificationType.Automatic).IsMet.ShouldBeTrue();
        task.Status.ShouldBe(WorkTaskStatus.AwaitingVerification);
        task.ClosedAt.ShouldBeNull();
    }

    [Fact]
    public void Task_closes_when_all_criteria_are_met_in_either_order()
    {
        var teamFirst = NewTask();
        teamFirst.ConfirmCriterion(TeamCriterion(teamFirst), "Ayşe", T0.AddHours(1));
        teamFirst.Status.ShouldBe(WorkTaskStatus.Todo);
        teamFirst.FindingResolved(5, T0.AddDays(1));
        teamFirst.Status.ShouldBe(WorkTaskStatus.Closed);

        var scanFirst = NewTask();
        scanFirst.FindingResolved(5, T0.AddDays(1));
        scanFirst.ConfirmCriterion(TeamCriterion(scanFirst), "Ayşe", T0.AddDays(2));
        scanFirst.Status.ShouldBe(WorkTaskStatus.Closed);
        scanFirst.ClosedAt.ShouldBe(T0.AddDays(2));
    }

    [Fact]
    public void Automatic_criteria_cannot_be_confirmed_by_users()
    {
        var task = NewTask();
        Should.Throw<DomainException>(() => task.ConfirmCriterion(AutomaticCriterion(task), "Ayşe", T0));
    }

    [Fact]
    public void Reopened_finding_returns_a_closed_task_to_todo()
    {
        var task = NewTask();
        task.ConfirmCriterion(TeamCriterion(task), "Ayşe", T0.AddHours(1));
        task.FindingResolved(5, T0.AddDays(1));
        task.Status.ShouldBe(WorkTaskStatus.Closed);

        task.FindingReopened(6, T0.AddDays(2));
        task.Status.ShouldBe(WorkTaskStatus.Todo);
        task.ClosedAt.ShouldBeNull();
        task.Criteria.Single(c => c.VerificationType == VerificationType.Automatic).IsMet.ShouldBeFalse();
        task.Criteria.Single(c => c.VerificationType == VerificationType.TeamConfirmation).IsMet.ShouldBeTrue();
        task.History.Last().Type.ShouldBe(TaskEventType.Reopened);
    }

    [Fact]
    public void Users_cannot_close_or_change_a_closed_task()
    {
        var task = NewTask();
        Should.Throw<DomainException>(() => task.ChangeStatus(WorkTaskStatus.Closed, "Ayşe", T0));
        task.ChangeStatus(WorkTaskStatus.InProgress, "Ayşe", T0);
        task.Status.ShouldBe(WorkTaskStatus.InProgress);

        task.ConfirmCriterion(TeamCriterion(task), "Ayşe", T0);
        task.FindingResolved(5, T0.AddDays(1));
        Should.Throw<DomainException>(() => task.ChangeStatus(WorkTaskStatus.Todo, "Ayşe", T0.AddDays(2)));
        Should.Throw<DomainException>(() => task.RevertCriterion(TeamCriterion(task), "Ayşe", T0.AddDays(2)));
    }

    [Fact]
    public void Ai_suggestion_requires_a_model_and_keeps_history()
    {
        var task = NewTask();
        Should.Throw<DomainException>(() => task.ApplyAiSuggestion("Etki", new TaskSuggestion(SuggestionKind.Text, "Ö", "İ", []), T0));
        task.ApplyAiSuggestion("Yeni etki", new TaskSuggestion(SuggestionKind.Text, "Ö", "İ", [], "deepseek-chat"), T0);
        task.Impact.ShouldBe("Yeni etki");
        task.Suggestion.Model.ShouldBe("deepseek-chat");
    }

    [Fact]
    public void Feedback_is_tied_to_the_current_suggestion_model()
    {
        var task = NewTask();
        task.AddFeedback(true, "  işe yaradı ", "Ayşe", T0);
        task.Feedback.ShouldHaveSingleItem().ShouldSatisfyAllConditions(f => f.Note.ShouldBe("işe yaradı"), f => f.Model.ShouldBeNull());
        Should.Throw<DomainException>(() => task.AddComment("Ayşe", " ", T0));
    }
}
