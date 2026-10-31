using MediatR;

namespace MarketingOps.Application.Crawls.Commands.RenderCrawl;

/// <summary>Biten taramanın örnek sayfalarını tarayıcıda ölçer, ardından kural değerlendirmesini kuyruğa ekler.</summary>
public sealed record RenderCrawlCommand(Guid CrawlId) : IRequest;
