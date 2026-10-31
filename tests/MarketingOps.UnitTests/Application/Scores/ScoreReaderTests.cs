using MarketingOps.Application.Scores;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Scores;

namespace MarketingOps.UnitTests.Application.Scores;

/// <summary>Skor girdileri: taranan sayfaya dayanan bulgu site oranıyla, örneklenmiş bulgu tipine yansıtılarak, diğerleri tam ağırlıkla.</summary>
public class ScoreReaderTests
{
    private static readonly Guid Product = Guid.CreateVersion7();

    private static ScoreFacts Facts(params ScoreFact[] findings) => new(
        new Dictionary<Guid, int> { [Product] = 200 },
        300,
        new Dictionary<(RuleDataSource Source, Guid PageTypeId), int> { [(RuleDataSource.Render, Product)] = 3 },
        findings);

    private static ScoreFact Fact(string rule, FindingCategory category, int affected) =>
        new(Guid.CreateVersion7(), rule, Severity.High, category, rule, Product, affected);

    [Fact]
    public void Crawl_findings_use_the_share_of_site_pages()
    {
        var input = ScoreReader.Inputs(Facts(Fact("seo.schema.missing", FindingCategory.TechnicalSeo, 60)), ScoreCategory.Seo).Single();

        input.AffectedCount.ShouldBe(60);
        input.PageCount.ShouldBe(300);
    }

    [Fact]
    public void Sampled_findings_project_the_sample_ratio_onto_the_page_type()
    {
        // 3 örneğin 2'si sorunlu → Ürün tipinin 200 sayfasının üçte ikisi.
        var input = ScoreReader.Inputs(Facts(Fact("mobile.overflow", FindingCategory.Accessibility, 2)), ScoreCategory.Accessibility).Single();

        input.AffectedCount.ShouldBe(133);
        input.PageCount.ShouldBe(300);
        ScoreCalculator.Score([input]).ShouldBe(96);
    }

    [Fact]
    public void Full_crawl_findings_use_the_share_of_site_pages()
    {
        var input = ScoreReader.Inputs(Facts(Fact("seo.links.orphan", FindingCategory.TechnicalSeo, 60)), ScoreCategory.Seo).Single();

        input.AffectedCount.ShouldBe(60);
        input.PageCount.ShouldBe(300);
    }

    [Fact]
    public void Sampled_page_type_score_uses_the_samples_themselves()
    {
        var input = ScoreReader.ToPageTypeInput(Fact("content.image.distorted", FindingCategory.Content, 2), Facts());

        input.AffectedCount.ShouldBe(2);
        input.PageCount.ShouldBe(3);
    }

    [Fact]
    public void Findings_from_sources_without_sample_counts_cost_full_weight()
    {
        var input = ScoreReader.Inputs(Facts(Fact("track.tag.missing", FindingCategory.Tracking, 1)), ScoreCategory.Tracking).Single();

        input.PageCount.ShouldBe(0);
        ScoreCalculator.Score([input]).ShouldBe(92);
    }
}
