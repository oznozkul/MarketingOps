namespace MarketingOps.Application.Tracking;

/// <summary>Meta token'ı reddetti. Denetim diğer kurallara devam eder; Meta kuralları bu turda çözülmez.</summary>
public sealed class MetaGraphAuthException(string message) : Exception(message);
