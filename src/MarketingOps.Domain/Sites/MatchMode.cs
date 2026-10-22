namespace MarketingOps.Domain.Sites;

/// <summary>Sayfa tipinin koşullarının nasıl birleştirileceği.</summary>
public enum MatchMode
{
    /// <summary>Tüm koşullar sağlanmalı.</summary>
    All,

    /// <summary>Koşullardan biri yeterli.</summary>
    Any
}
