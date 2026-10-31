namespace MarketingOps.Domain.Rendering;

public enum RenderStatus
{
    Measured,
    /// <summary>
    /// Ölçüldü ama ekranın büyük kısmını sabit bir katman (çoğunlukla çerez penceresi) kaplıyordu; ilk ekran ve kontrast
    /// sonuçları o katmanı ölçer, sayfayı değil. Bu kontroller bu örneği kullanmaz.
    /// </summary>
    Obstructed,
    Failed
}
