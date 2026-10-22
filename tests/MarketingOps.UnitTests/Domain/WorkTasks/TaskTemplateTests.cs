using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.UnitTests.Domain.WorkTasks;

public class TaskTemplateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 2, 0, 0, TimeSpan.Zero);

    private static Finding FindingFor(IRule rule, int pages) => Finding.Open(Guid.CreateVersion7(), rule.Code, Guid.CreateVersion7(), rule.Category,
        rule.DefaultSeverity, rule.Title(pages), pages,
        Enumerable.Range(1, pages).Select(i => new FindingOccurrence($"https://ornek.com/p{i}", $"değer {i}", null, Guid.Empty)).ToList(),
        new CrawlObservation(Guid.CreateVersion7(), 3, false, T0));

    [Fact]
    public void Every_rule_produces_a_complete_task_without_ai()
    {
        foreach (var rule in RuleCatalog.All)
        {
            var draft = TaskTemplate.Build(rule, FindingFor(rule, 14), "Ebat");

            draft.Title.ShouldBe(rule.Title(14), rule.Code);
            draft.Problem.ShouldStartWith("Ebat sayfalarından 14 tanesi etkileniyor.", customMessage: rule.Code);
            draft.Impact.ShouldNotBeNullOrWhiteSpace(rule.Code);
            draft.Suggestion.Summary.ShouldNotBeNullOrWhiteSpace(rule.Code);
            draft.Suggestion.Content.ShouldNotBeNullOrWhiteSpace(rule.Code);
            draft.Suggestion.Model.ShouldBeNull(rule.Code);
            draft.Suggestion.Examples.Count.ShouldBe(TaskTemplate.ExampleCount, rule.Code);
            draft.References.ShouldBe([rule.ReferenceKey], rule.Code);
            draft.Criteria.Select(c => c.Type).ShouldBe([VerificationType.Automatic, VerificationType.TeamConfirmation], rule.Code);
            draft.Criteria[0].Text.ShouldContain("Ebat sayfalarında", customMessage: rule.Code);
        }
    }

    [Fact]
    public void Template_suggestions_contain_no_numbers_from_imagination()
    {
        // Şablon önerileri ve etki metinleri sayfa sayısı ya da yüzde içermez; sayılar yalnızca bulgudan gelir.
        foreach (var rule in RuleCatalog.All)
            rule.Impact.ShouldNotMatch(@"%\d|\b\d+\s*sayfa", rule.Code);
    }

    [Theory]
    [InlineData("Lastik Borsası", "LB")]
    [InlineData("Tyre Supply", "TS")]
    [InlineData("Çiçekçi", "CIC")]
    [InlineData("İyi Ürün Şirketi", "IUS")]
    public void Site_derives_task_key_prefix_from_its_name(string name, string expected)
    {
        Site.DerivePrefix(name).ShouldBe(expected);
        var site = Site.Create(Guid.CreateVersion7(), name, "ornek.com", T0);
        site.AllocateTaskKey().ShouldBe((1, $"{expected}-1"));
        site.AllocateTaskKey().ShouldBe((2, $"{expected}-2"));
    }

    [Fact]
    public void Task_key_prefix_is_validated()
    {
        var site = Site.Create(Guid.CreateVersion7(), "Örnek", "ornek.com", T0);
        Should.Throw<MarketingOps.Domain.Common.DomainException>(() => site.UpdateTaskKeyPrefix("1AB"));
        Should.Throw<MarketingOps.Domain.Common.DomainException>(() => site.UpdateTaskKeyPrefix("A-B"));
        site.UpdateTaskKeyPrefix("seo");
        site.AllocateTaskKey().Key.ShouldBe("SEO-1");
    }
}
