using MarketingOps.Domain.Common;
using MarketingOps.Domain.Findings;

namespace MarketingOps.UnitTests.Domain.Findings;

public class FindingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 2, 0, 0, TimeSpan.Zero);
    private static readonly Guid SiteId = Guid.CreateVersion7();
    private static readonly Guid PageTypeId = Guid.CreateVersion7();

    private static CrawlObservation Crawl(int number, bool partial = false) => new(Guid.CreateVersion7(), number, partial, T0.AddDays(number));

    private static IReadOnlyList<FindingOccurrence> Pages(int count, Guid crawlId = default) =>
        Enumerable.Range(1, count).Select(i => new FindingOccurrence($"https://example.com/p{i}", "değer", null, crawlId)).ToList();

    private static Finding Open(CrawlObservation? crawl = null) =>
        Finding.Open(SiteId, "seo.title.duplicate", PageTypeId, FindingCategory.TechnicalSeo, Severity.High, "3 sayfada title tekrarlanıyor", 3, Pages(3), crawl ?? Crawl(1));

    private static void Seen(Finding finding, CrawlObservation crawl) => finding.Observe("3 sayfada title tekrarlanıyor", Severity.High, 3, Pages(3), crawl);

    [Fact]
    public void Opening_records_history_and_raises_event()
    {
        var crawl = Crawl(1);
        var finding = Open(crawl);

        finding.Status.ShouldBe(FindingStatus.Open);
        finding.FirstSeenAt.ShouldBe(crawl.At);
        finding.LastOpenedCrawlId.ShouldBe(crawl.CrawlId);
        finding.History.ShouldHaveSingleItem().Type.ShouldBe(FindingEventType.Opened);
        finding.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<FindingOpened>();
    }

    [Fact]
    public void Fingerprint_is_stable_for_the_same_rule_and_page_type()
    {
        Finding.ComputeFingerprint(SiteId, "seo.title.duplicate", PageTypeId).ShouldBe(Open().Fingerprint);
        Finding.ComputeFingerprint(SiteId, "seo.title.duplicate", null).ShouldNotBe(Open().Fingerprint);
    }

    [Fact]
    public void Seen_again_updates_the_same_finding()
    {
        var finding = Open();
        finding.Observe("5 sayfada title tekrarlanıyor", Severity.Critical, 5, Pages(5), Crawl(2));

        finding.Status.ShouldBe(FindingStatus.Open);
        finding.AffectedCount.ShouldBe(5);
        finding.Severity.ShouldBe(Severity.Critical);
        finding.LastSeenAt.ShouldBe(Crawl(2).At);
        finding.History.Last().Type.ShouldBe(FindingEventType.SeenAgain);
    }

    [Fact]
    public void Not_seen_in_a_complete_crawl_resolves_then_seen_again_reopens()
    {
        var finding = Open();
        finding.MarkNotObserved(Crawl(2));
        finding.Status.ShouldBe(FindingStatus.Resolved);
        finding.ResolvedAt.ShouldBe(Crawl(2).At);

        finding.ClearDomainEvents();
        var third = Crawl(3);
        Seen(finding, third);
        finding.Status.ShouldBe(FindingStatus.Reopened);
        finding.ResolvedAt.ShouldBeNull();
        finding.LastOpenedCrawlId.ShouldBe(third.CrawlId);
        finding.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<FindingReopened>();
    }

    [Fact]
    public void Partial_crawl_cannot_resolve_a_finding()
    {
        var finding = Open();
        finding.MarkNotObserved(Crawl(2, partial: true));

        finding.Status.ShouldBe(FindingStatus.Open);
        finding.ResolvedAt.ShouldBeNull();
        finding.History.ShouldHaveSingleItem();
    }

    [Fact]
    public void Partial_crawl_cannot_resolve_a_reopened_finding()
    {
        var finding = Open();
        finding.MarkNotObserved(Crawl(2));
        Seen(finding, Crawl(3));
        finding.MarkNotObserved(Crawl(4, partial: true));
        finding.Status.ShouldBe(FindingStatus.Reopened);
    }

    [Fact]
    public void False_positive_is_not_changed_by_crawls_and_can_be_reverted()
    {
        var finding = Open();
        finding.MarkFalsePositive("Bu sayfalarda tekrar bilinçli.", "Ayşe", T0.AddHours(1));

        Seen(finding, Crawl(2));
        finding.MarkNotObserved(Crawl(3));
        finding.Status.ShouldBe(FindingStatus.FalsePositive);

        finding.RevertFalsePositive("Ayşe", T0.AddDays(4));
        finding.Status.ShouldBe(FindingStatus.Open);
        finding.FalsePositiveReason.ShouldBeNull();
    }

    [Fact]
    public void False_positive_requires_a_reason()
    {
        Should.Throw<DomainException>(() => Open().MarkFalsePositive(" ", "Ayşe", T0));
        Should.Throw<DomainException>(() => Open().RevertFalsePositive("Ayşe", T0));
    }

    [Fact]
    public void Snoozed_finding_keeps_its_status_until_the_date_then_opens()
    {
        var finding = Open();
        finding.Snooze(T0.AddDays(5), "Ayşe", T0.AddHours(1));

        Seen(finding, Crawl(2));
        finding.Status.ShouldBe(FindingStatus.Snoozed);
        finding.LastSeenAt.ShouldBe(Crawl(2).At);
        finding.MarkNotObserved(Crawl(3));
        finding.Status.ShouldBe(FindingStatus.Snoozed);

        Seen(finding, Crawl(6));
        finding.Status.ShouldBe(FindingStatus.Open);
        finding.SnoozedUntil.ShouldBeNull();
        finding.History.Last().Type.ShouldBe(FindingEventType.SnoozeExpired);
    }

    [Fact]
    public void Expired_snooze_not_seen_resolves()
    {
        var finding = Open();
        finding.Snooze(T0.AddDays(2), "Ayşe", T0.AddHours(1));
        finding.MarkNotObserved(Crawl(3));
        finding.Status.ShouldBe(FindingStatus.Resolved);
    }

    [Fact]
    public void Snooze_needs_a_future_date_and_an_active_finding()
    {
        Should.Throw<DomainException>(() => Open().Snooze(T0, "Ayşe", T0.AddHours(1)));

        var resolved = Open();
        resolved.MarkNotObserved(Crawl(2));
        Should.Throw<DomainException>(() => resolved.Snooze(T0.AddDays(10), "Ayşe", T0.AddDays(3)));
    }

    [Fact]
    public void Only_a_bounded_number_of_occurrences_is_kept()
    {
        var count = Finding.MaxOccurrencesKept + 10;
        var finding = Finding.Open(SiteId, "seo.h1", null, FindingCategory.Content, Severity.Low, "başlık", count, Pages(count), Crawl(1));
        finding.AffectedCount.ShouldBe(count);
        finding.Occurrences.Count.ShouldBe(Finding.MaxOccurrencesKept);
    }
}
