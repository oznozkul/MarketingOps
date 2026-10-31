using System.Reflection;
using Hangfire;
using MarketingOps.Infrastructure.Jobs;

namespace MarketingOps.UnitTests.Infrastructure;

/// <summary>
/// Aynı taramanın iki değerlendirme işi aynı anda çalışınca bulgunun benzersiz indeksi çakışıyordu. Değerlendirme ve render
/// işleri tarama başına kilitlenir; kilit taramaya özeldir, farklı taramalar birbirini beklemez.
/// </summary>
public class JobConcurrencyTests
{
    [Theory]
    [InlineData(typeof(EvaluateCrawlJob))]
    [InlineData(typeof(RenderCrawlJob))]
    public void Crawl_jobs_do_not_run_concurrently_for_the_same_crawl(Type job)
    {
        var method = job.GetMethod("RunAsync")!;
        method.GetCustomAttribute<DisableConcurrentExecutionAttribute>().ShouldNotBeNull(job.Name);
    }

    [Fact]
    public void Evaluation_lock_is_per_crawl()
    {
        // Hangfire kilidin adını işin argümanlarıyla kendisi doldurur; doğrudan onun ürettiği ad denetlenir.
        var attribute = typeof(EvaluateCrawlJob).GetMethod("RunAsync")!.GetCustomAttribute<DisableConcurrentExecutionAttribute>()!;
        var getResource = typeof(DisableConcurrentExecutionAttribute).GetMethod("GetResource", BindingFlags.Instance | BindingFlags.NonPublic)!;
        string Resource(Guid crawlId) =>
            (string)getResource.Invoke(attribute, [Hangfire.Common.Job.FromExpression<EvaluateCrawlJob>(j => j.RunAsync(crawlId, CancellationToken.None))])!;

        var crawl = Guid.CreateVersion7();
        Resource(crawl).ShouldBe($"crawl-evaluation:{crawl}");
        Resource(Guid.CreateVersion7()).ShouldNotBe(Resource(crawl));
    }
}
