using MediatR;

namespace MarketingOps.Application.Findings.Commands.RequestCrawlEvaluation;

/// <summary>
/// Bitmiş ama kurallarla değerlendirilmemiş taramayı (ör. kural motorundan önce yapılmış ya da değerlendirme işi hata almış)
/// değerlendirme kuyruğuna ekler. Site yeniden taranmaz; mevcut snapshot'lar kullanılır.
/// </summary>
public sealed record RequestCrawlEvaluationCommand(Guid SiteId, Guid CrawlId) : IRequest;
