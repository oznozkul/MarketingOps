using MarketingOps.Domain.Common;
using MarketingOps.Domain.Crawls;

namespace MarketingOps.UnitTests.Domain.Crawls;

public class CrawlTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 2, 0, 0, TimeSpan.Zero);

    private static Crawl Running()
    {
        var crawl = Crawl.Queue(Guid.CreateVersion7(), true, 1, CrawlTrigger.Manual, T0);
        crawl.Start(T0.AddSeconds(1));
        return crawl;
    }

    [Fact]
    public void Queued_crawl_starts_and_finishes_as_completed()
    {
        var crawl = Running();
        crawl.EnterStage(CrawlStage.Discovery, T0.AddSeconds(1));
        crawl.EnterStage(CrawlStage.Download, T0.AddSeconds(5));
        crawl.ReportProgress(20, 20, 0);
        crawl.Finish(T0.AddSeconds(30));

        crawl.Status.ShouldBe(CrawlStatus.Completed);
        crawl.FinishedAt.ShouldBe(T0.AddSeconds(30));
        crawl.Stages.Select(s => s.Stage).ShouldBe([CrawlStage.Discovery, CrawlStage.Download]);
        crawl.Stages[0].FinishedAt.ShouldBe(T0.AddSeconds(5));
        crawl.Stages[1].FinishedAt.ShouldBe(T0.AddSeconds(30));
    }

    [Fact]
    public void Many_failures_make_the_crawl_partial()
    {
        var crawl = Running();
        crawl.ReportProgress(100, 85, 15);
        crawl.Finish(T0.AddMinutes(1));

        crawl.Status.ShouldBe(CrawlStatus.PartiallyCompleted);
    }

    [Fact]
    public void Few_failures_still_complete()
    {
        var crawl = Running();
        crawl.ReportProgress(100, 95, 5);
        crawl.Finish(T0.AddMinutes(1));

        crawl.Status.ShouldBe(CrawlStatus.Completed);
    }

    [Fact]
    public void Completed_crawl_cannot_run_again()
    {
        var crawl = Running();
        crawl.Finish(T0.AddMinutes(1));

        Should.Throw<DomainException>(() => crawl.Start(T0.AddMinutes(2)));
        Should.Throw<DomainException>(() => crawl.Cancel(T0.AddMinutes(2)));
        Should.Throw<DomainException>(() => crawl.ReportProgress(1, 1, 0));
    }

    [Fact]
    public void Queued_crawl_can_be_cancelled_and_then_not_started()
    {
        var crawl = Crawl.Queue(Guid.CreateVersion7(), true, 3, CrawlTrigger.Manual, T0);
        crawl.Cancel(T0.AddSeconds(2));

        crawl.Status.ShouldBe(CrawlStatus.Cancelled);
        Should.Throw<DomainException>(() => crawl.Start(T0.AddSeconds(3)));
    }

    [Fact]
    public void Failure_closes_open_stage_and_keeps_message()
    {
        var crawl = Running();
        crawl.EnterStage(CrawlStage.Discovery, T0.AddSeconds(1));
        crawl.Fail("Sunucu yanıt vermedi.", T0.AddSeconds(9));

        crawl.Status.ShouldBe(CrawlStatus.Failed);
        crawl.ErrorMessage.ShouldBe("Sunucu yanıt vermedi.");
        crawl.Stages.ShouldHaveSingleItem().FinishedAt.ShouldBe(T0.AddSeconds(9));
    }

    [Fact]
    public void Scheduled_crawl_requires_verified_ownership()
    {
        var ex = Should.Throw<DomainException>(() => Crawl.Queue(Guid.CreateVersion7(), false, 1, CrawlTrigger.Schedule, T0));
        ex.Message.ShouldContain("Sahipliği doğrulanmamış");

        Crawl.Queue(Guid.CreateVersion7(), false, 1, CrawlTrigger.Manual, T0).Status.ShouldBe(CrawlStatus.Queued);
    }

    [Fact]
    public void Skipped_urls_are_counted_but_only_a_sample_is_kept()
    {
        var crawl = Running();
        for (var i = 0; i < Crawl.MaxSkippedUrlsKept + 20; i++) crawl.RecordSkipped($"https://a.com/{i}", SkipReason.RobotsTxt);

        crawl.SkippedCount.ShouldBe(Crawl.MaxSkippedUrlsKept + 20);
        crawl.SkippedUrls.Count.ShouldBe(Crawl.MaxSkippedUrlsKept);
    }
}
