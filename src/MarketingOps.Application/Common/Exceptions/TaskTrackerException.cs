namespace MarketingOps.Application.Common.Exceptions;

/// <summary>Dış görev sistemi isteği reddetti ya da ulaşılamadı. Mesaj kullanıcıya gösterilir.</summary>
public sealed class TaskTrackerException(string message, Exception? inner = null) : Exception(message, inner);
