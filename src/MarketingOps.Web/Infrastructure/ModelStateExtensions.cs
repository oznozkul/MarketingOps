using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MarketingOps.Web.Infrastructure;

public static class ModelStateExtensions
{
    /// <summary>
    /// Alanın ilk hata mesajı. "ExcludedPathPatterns[2]" gibi liste elemanı hataları da üst alana bağlanır,
    /// çünkü formda liste tek bir metin alanıyla düzenlenir.
    /// </summary>
    public static string? FirstError(this ModelStateDictionary modelState, string field) =>
        modelState
            .Where(kv => kv.Key == field || kv.Key.StartsWith(field + "[", StringComparison.Ordinal) || kv.Key.StartsWith(field + ".", StringComparison.Ordinal))
            .SelectMany(kv => kv.Value!.Errors)
            .Select(e => e.ErrorMessage)
            .FirstOrDefault();
}
