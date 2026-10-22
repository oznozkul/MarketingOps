using FluentValidation;
using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Infrastructure;

/// <summary>
/// /api isteklerindeki hataları RFC 7807 ProblemDetails'e çevirir. Ekran isteklerinde false döner;
/// böylece UseExceptionHandler /error sayfasını gösterir.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var details = exception switch
        {
            ValidationException validation => new ValidationProblemDetails(
                validation.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Girdi doğrulanamadı."
            },
            DomainException => new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = exception.Message },
            NotFoundException => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = exception.Message },
            ConflictException => new ProblemDetails { Status = StatusCodes.Status409Conflict, Title = exception.Message },
            _ => null
        };

        if (details is null)
        {
            // Beklenmeyen hata: logla, ayrıntıyı dışarı sızdırma.
            logger.LogError(exception, "Beklenmeyen hata: {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            details = new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Beklenmeyen bir hata oluştu." };
        }

        // ExceptionHandlerMiddleware, handler'ları çağırmadan önce Request.Path'i "/error" yapar; asıl yol feature'dadır.
        var originalPath = httpContext.Features.Get<IExceptionHandlerFeature>()?.Path ?? httpContext.Request.Path.Value ?? "";
        if (!originalPath.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)) return false;

        // IProblemDetailsService burada kullanılmaz: hata anında endpoint bilgisi temizlendiği ve Accept başlığı
        // müzakere edildiği için yazmayı reddedip isteği HTML hata sayfasına düşürebiliyor.
        details.Instance = originalPath;
        details.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = details.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(details, details.GetType(), options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}
