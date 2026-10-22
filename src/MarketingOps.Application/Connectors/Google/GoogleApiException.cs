namespace MarketingOps.Application.Connectors.Google;

/// <summary>Google API hatası. <see cref="IsAuthorizationError"/>: yetki geri alındı ya da süresi doldu; yeniden bağlanmak gerekir.</summary>
public sealed class GoogleApiException(string message, bool isAuthorizationError = false, Exception? inner = null) : Exception(message, inner)
{
    public bool IsAuthorizationError { get; } = isAuthorizationError;
}
