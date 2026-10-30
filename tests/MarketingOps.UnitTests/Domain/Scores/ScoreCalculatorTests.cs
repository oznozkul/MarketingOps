using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Scores;

namespace MarketingOps.UnitTests.Domain.Scores;

public class ScoreCalculatorTests
{
    private static readonly Guid Product = Guid.CreateVersion7();
    private static readonly Guid Category = Guid.CreateVersion7();

    private static ScoreInput Finding(string rule, Severity severity, int affected, int pageCount = 300, Guid? pageType = null) =>
        new(Guid.CreateVersion7(), rule, severity, rule, pageType ?? Product, affected, pageCount);

    [Fact]
    public void No_findings_scores_full()
    {
        ScoreCalculator.Score([]).ShouldBe(100);
    }

    [Theory]
    [InlineData(Severity.Critical, 15)]
    [InlineData(Severity.High, 8)]
    [InlineData(Severity.Medium, 3)]
    [InlineData(Severity.Low, 1)]
    public void A_finding_on_every_page_costs_its_weight(Severity severity, int weight)
    {
        ScoreCalculator.Score([Finding("r", severity, 300)]).ShouldBe(100 - weight);
    }

    [Fact]
    public void Deduction_scales_with_the_share_of_affected_pages()
    {
        // 75 / 300 sayfa → High (8) × 0,25 = 2
        ScoreCalculator.Score([Finding("r", Severity.High, 75)]).ShouldBe(98);
    }

    [Fact]
    public void One_rule_across_many_page_types_costs_at_most_its_weight()
    {
        // Şablondan gelen sorun yedi sayfa tipinin hepsinde; site 7 × 40 = 280 sayfa, hepsi etkileniyor.
        var findings = Enumerable.Range(0, 7).Select(_ => Finding("seo.link.empty_href", Severity.Medium, 40, 280, Guid.CreateVersion7())).ToList();

        ScoreCalculator.Score(findings).ShouldBe(97);
        ScoreCalculator.Breakdown(findings).Sum(d => d.Points).ShouldBe(3m, 0.0001m);
    }

    [Fact]
    public void A_rule_split_across_page_types_adds_up_by_site_share_below_the_cap()
    {
        // 30 ürün + 30 kategori sayfası etkilenmiş, site 300 sayfa → High × 60/300 = 1,6
        var findings = new[] { Finding("r", Severity.High, 30, pageType: Product), Finding("r", Severity.High, 30, pageType: Category) };

        ScoreCalculator.Breakdown(findings).Sum(d => d.Points).ShouldBe(1.6m);
        ScoreCalculator.Score(findings).ShouldBe(98);
    }

    [Fact]
    public void The_cap_is_the_highest_severity_the_rule_has_on_any_page_type()
    {
        // Sayfa tipine göre önem farklı olabilir: Ürün'de High, Kategori'de Low; ikisi de tam etkilenmiş.
        var findings = new[] { Finding("r", Severity.High, 300), Finding("r", Severity.Low, 300, pageType: Category) };

        ScoreCalculator.Breakdown(findings).Sum(d => d.Points).ShouldBe(8m);
    }

    [Fact]
    public void Different_rules_add_up_and_the_score_never_drops_below_zero()
    {
        var findings = Enumerable.Range(0, 20).Select(i => Finding($"r{i}", Severity.Critical, 300)).ToList();

        ScoreCalculator.Score(findings).ShouldBe(0);
        ScoreCalculator.Score([Finding("a", Severity.High, 300), Finding("b", Severity.Medium, 300)]).ShouldBe(89);
    }

    [Fact]
    public void Findings_without_a_page_count_cost_their_full_weight()
    {
        ScoreCalculator.Score([new ScoreInput(Guid.CreateVersion7(), "Purchase farkı", Severity.Critical, "track.purchase.gap", null, 1, 0)]).ShouldBe(85);
        ScoreCalculator.Score([Finding("r", Severity.High, 3, pageCount: 0)]).ShouldBe(92);
    }

    [Fact]
    public void Pages_outside_any_page_type_still_count_by_their_share()
    {
        // Hiçbir sayfa tipine uymayan tek bir sayfa: 1 / 300 × 8
        ScoreCalculator.Breakdown([new ScoreInput(Guid.CreateVersion7(), "Viewport", Severity.High, "mobile.viewport.invalid", null, 1, 300)])
            .Single().Points.ShouldBe(8m / 300, 0.0001m);
    }

    [Fact]
    public void Breakdown_lists_the_biggest_deductions_first()
    {
        var breakdown = ScoreCalculator.Breakdown([Finding("small", Severity.Low, 300), Finding("big", Severity.Critical, 300)]);

        breakdown.Select(d => d.Title).ShouldBe(["big", "small"]);
        breakdown[0].PageCount.ShouldBe(300);
    }
}
