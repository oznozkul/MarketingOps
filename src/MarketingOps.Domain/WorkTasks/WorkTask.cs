using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.WorkTasks;

/// <summary>
/// Bir bulgunun ekibe açılmış işi (bulgu başına tek görev). Kapanış kuralı: tüm kabul kriterleri sağlanınca kapanır;
/// otomatik kriterleri yalnızca sistem (bulgu çözülünce) işaretler, ekip kriterlerini kullanıcı onaylar.
/// </summary>
public class WorkTask : Entity
{
    public const int CommentMaxLength = 4000;
    public const int FeedbackNoteMaxLength = 1000;

    private readonly List<AcceptanceCriterion> _criteria = [];
    private readonly List<TaskHistoryEntry> _history = [];
    private readonly List<TaskComment> _comments = [];
    private readonly List<SuggestionFeedback> _feedback = [];
    private List<string> _references = [];

    public Guid SiteId { get; private set; }
    public Guid FindingId { get; private set; }
    public int Number { get; private set; }
    public string Key { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Problem { get; private set; } = null!;
    public string Impact { get; private set; } = null!;
    public TaskSuggestion Suggestion { get; private set; } = null!;
    public IReadOnlyList<string> References => _references;
    public Team AssignedTeam { get; private set; }
    public WorkTaskStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public IReadOnlyList<AcceptanceCriterion> Criteria => _criteria;
    public IReadOnlyList<TaskHistoryEntry> History => _history;
    public IReadOnlyList<TaskComment> Comments => _comments;
    public IReadOnlyList<SuggestionFeedback> Feedback => _feedback;

    private WorkTask() { }

    public static WorkTask Create(Guid siteId, Guid findingId, int number, string key, TaskDraft draft, DateTimeOffset now)
    {
        if (draft.Criteria.Count == 0) throw new DomainException("Görevin en az bir kabul kriteri olmalı.");
        var task = new WorkTask
        {
            SiteId = siteId,
            FindingId = findingId,
            Number = number,
            Key = key,
            Title = draft.Title,
            Problem = draft.Problem,
            Impact = draft.Impact,
            Suggestion = draft.Suggestion,
            _references = [.. draft.References],
            AssignedTeam = draft.Team,
            Status = WorkTaskStatus.Todo,
            CreatedAt = now,
            UpdatedAt = now
        };
        task._criteria.AddRange(draft.Criteria.Select(c => new AcceptanceCriterion(Guid.CreateVersion7(), c.Text, c.Type)));
        task.Record(now, TaskEventType.Created, "Görev bulgudan oluşturuldu.");
        return task;
    }

    /// <summary>Bağlı bulgu taramada görülmedi: otomatik kriterler sağlanır; ekip onayı bekleniyorsa doğrulama bekler.</summary>
    public void FindingResolved(int crawlNumber, DateTimeOffset now)
    {
        if (Status == WorkTaskStatus.Closed) return;
        for (var i = 0; i < _criteria.Count; i++)
            if (_criteria[i] is { VerificationType: VerificationType.Automatic, IsMet: false } c)
                _criteria[i] = c with { IsMet = true, MetAt = now, MetBy = null };
        Record(now, TaskEventType.CriterionMet, $"Tarama #{crawlNumber}: sorun görülmedi, otomatik kriterler sağlandı.");
        if (!TryClose(now))
        {
            Status = WorkTaskStatus.AwaitingVerification;
            Record(now, TaskEventType.StatusChanged, "Ekip onayı bekleyen kriterler var; durum Doğrulama bekliyor oldu.");
        }
    }

    /// <summary>Bağlı bulgu tekrar açıldı: otomatik kriterler geri alınır, görev Yapılacak'a döner.</summary>
    public void FindingReopened(int crawlNumber, DateTimeOffset now)
    {
        for (var i = 0; i < _criteria.Count; i++)
            if (_criteria[i] is { VerificationType: VerificationType.Automatic, IsMet: true } c)
                _criteria[i] = c with { IsMet = false, MetAt = null };
        Status = WorkTaskStatus.Todo;
        ClosedAt = null;
        Record(now, TaskEventType.Reopened, $"Tarama #{crawlNumber}: sorun yeniden görüldü; görev tekrar Yapılacak durumuna alındı. Yeni görev açılmadı.");
    }

    /// <summary>Kullanıcının durum değişikliği. Kapanış kriterlere bağlıdır; elle kapatılamaz.</summary>
    public void ChangeStatus(WorkTaskStatus status, string actor, DateTimeOffset now)
    {
        if (status == WorkTaskStatus.Closed) throw new DomainException("Görev, tüm kabul kriterleri sağlanınca kendiliğinden kapanır.");
        if (Status == WorkTaskStatus.Closed) throw new DomainException("Kapanmış görev yalnızca bulgu yeniden görülürse açılır.");
        if (status == Status) return;
        Status = status;
        Record(now, TaskEventType.StatusChanged, $"Durum değişti.", actor);
    }

    public void ConfirmCriterion(Guid criterionId, string actor, DateTimeOffset now)
    {
        var (index, criterion) = TeamCriterion(criterionId);
        if (criterion.IsMet) return;
        _criteria[index] = criterion with { IsMet = true, MetAt = now, MetBy = actor };
        Record(now, TaskEventType.CriterionMet, $"Kriter onaylandı: {criterion.Text}", actor);
        TryClose(now);
    }

    public void RevertCriterion(Guid criterionId, string actor, DateTimeOffset now)
    {
        if (Status == WorkTaskStatus.Closed) throw new DomainException("Kapanmış görevin kriterleri değiştirilemez.");
        var (index, criterion) = TeamCriterion(criterionId);
        if (!criterion.IsMet) return;
        _criteria[index] = criterion with { IsMet = false, MetAt = null, MetBy = null };
        Record(now, TaskEventType.CriterionReverted, $"Kriter onayı geri alındı: {criterion.Text}", actor);
    }

    public void AssignTeam(Team team, string actor, DateTimeOffset now)
    {
        if (team == AssignedTeam) return;
        AssignedTeam = team;
        Record(now, TaskEventType.TeamAssigned, "Ekip değişti.", actor);
    }

    public void AddComment(string author, string text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Yorum boş olamaz.");
        if (text.Length > CommentMaxLength) throw new DomainException($"Yorum en fazla {CommentMaxLength} karakter olabilir.");
        _comments.Add(new TaskComment(Guid.CreateVersion7(), author, text.Trim(), now));
        UpdatedAt = now;
    }

    /// <summary>
    /// Şablon adımlarını son taramanın kanıtına göre yeniler. Eski AI metni ölçülen adımların yerine geçmesin diye
    /// model işareti de kalkar; öneri yeniden şablondan sayılır.
    /// </summary>
    public void ReplaceTemplateContent(string content, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        if (Suggestion.Model is null && content == Suggestion.Content) return;
        Suggestion = Suggestion with { Content = content, Model = null };
        UpdatedAt = now;
    }

    /// <summary>AI zenginleştirmesi. Sayılar ve örnek URL'ler şablondan kalır; yalnızca etki ve öneri metni değişir.</summary>
    public void ApplyAiSuggestion(string impact, TaskSuggestion suggestion, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(suggestion.Model)) throw new DomainException("AI önerisinin modeli belirtilmeli.");
        if (!string.IsNullOrWhiteSpace(impact)) Impact = impact.Trim();
        Suggestion = suggestion;
        Record(now, TaskEventType.SuggestionGenerated, $"Öneri AI ile zenginleştirildi ({suggestion.Model}).");
    }

    public void AddFeedback(bool isHelpful, string? note, string author, DateTimeOffset now)
    {
        if (note?.Length > FeedbackNoteMaxLength) throw new DomainException($"Not en fazla {FeedbackNoteMaxLength} karakter olabilir.");
        _feedback.Add(new SuggestionFeedback(isHelpful, string.IsNullOrWhiteSpace(note) ? null : note.Trim(), author, now, Suggestion.Model));
        UpdatedAt = now;
    }

    private bool TryClose(DateTimeOffset now)
    {
        if (_criteria.Any(c => !c.IsMet)) return false;
        Status = WorkTaskStatus.Closed;
        ClosedAt = now;
        Record(now, TaskEventType.Closed, "Tüm kabul kriterleri sağlandı; görev kapandı.");
        return true;
    }

    private (int Index, AcceptanceCriterion Criterion) TeamCriterion(Guid criterionId)
    {
        var index = _criteria.FindIndex(c => c.Id == criterionId);
        if (index < 0) throw new DomainException("Kabul kriteri bulunamadı.");
        if (_criteria[index].VerificationType != VerificationType.TeamConfirmation)
            throw new DomainException("Otomatik kriterler yalnızca sistem tarafından, sonraki taramada işaretlenir.");
        return (index, _criteria[index]);
    }

    private void Record(DateTimeOffset at, TaskEventType type, string description, string? actor = null)
    {
        _history.Add(new TaskHistoryEntry(at, type, description, actor));
        UpdatedAt = at;
    }
}
