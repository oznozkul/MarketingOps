using System.Text.Json;
using System.Text.Json.Serialization;
using MarketingOps.Application.Crawls.Queries;

namespace MarketingOps.Web.Infrastructure;

/// <summary>Canlı ilerleme bileşeninin (Alpine "crawlLive") başlangıç verisi; SignalR mesajıyla aynı biçimde.</summary>
public static class CrawlLiveJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static string For(CrawlListItemResponse crawl) => JsonSerializer.Serialize(new
    {
        crawlId = crawl.Id,
        number = crawl.Number,
        status = crawl.Status,
        startedAt = crawl.StartedAt,
        pagesDiscovered = crawl.PagesDiscovered,
        pagesCrawled = crawl.PagesCrawled,
        pagesFailed = crawl.PagesFailed
    }, Options);
}
