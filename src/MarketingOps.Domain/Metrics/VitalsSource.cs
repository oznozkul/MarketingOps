namespace MarketingOps.Domain.Metrics;

/// <summary>Ölçümün kaynağı: sayfanın kendi gerçek kullanıcı verisi (CrUX), sitenin geneli ya da laboratuvar ölçümü.</summary>
public enum VitalsSource
{
    UrlField,
    OriginField,
    Lab
}
