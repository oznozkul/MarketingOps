namespace MarketingOps.Domain.Common;

/// <summary>Bir domain kuralı ihlal edildiğinde fırlatılır. Mesaj kullanıcıya gösterilir, bu yüzden Türkçedir.</summary>
public class DomainException(string message) : Exception(message);
