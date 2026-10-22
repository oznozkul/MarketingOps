using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Metrics;

/// <summary>Bir günün sipariş veritabanı ile GA4 purchase karşılaştırması. (SiteId, Date) benzersizdir.</summary>
public class OrderComparisonDaily : Entity
{
    public Guid SiteId { get; private set; }
    public DateOnly Date { get; private set; }
    public int? OrderCount { get; private set; }
    public decimal? OrderRevenue { get; private set; }
    public long? Ga4PurchaseCount { get; private set; }
    public decimal? Ga4PurchaseValue { get; private set; }

    /// <summary>Sipariş sayısına göre mutlak fark yüzdesi. İki taraf da yoksa ya da sipariş sayısı yoksa boştur.</summary>
    public decimal? GapPercent { get; private set; }

    private OrderComparisonDaily() { }

    public static OrderComparisonDaily Create(Guid siteId, DateOnly date, int? orderCount, decimal? orderRevenue, long? ga4Count, decimal? ga4Value) 
    {
        var row = new OrderComparisonDaily { SiteId = siteId, Date = date };
        row.Update(orderCount, orderRevenue, ga4Count, ga4Value);
        return row;
    }

    public void Update(int? orderCount, decimal? orderRevenue, long? ga4Count, decimal? ga4Value)
    {
        if (orderCount < 0) throw new DomainException("Sipariş sayısı negatif olamaz.");
        OrderCount = orderCount;
        OrderRevenue = orderRevenue;
        Ga4PurchaseCount = ga4Count;
        Ga4PurchaseValue = ga4Value;
        GapPercent = Gap(orderCount, ga4Count);
    }

    /// <summary>Sıfıra bölünmez. Sipariş yok ve purchase varsa fark tamdır; ikisi de sıfırsa fark yoktur.</summary>
    public static decimal? Gap(int? orders, long? purchases)
    {
        if (orders is null || purchases is null) return null;
        if (orders == 0) return purchases == 0 ? 0 : 100;
        return Math.Abs(orders.Value - purchases.Value) / (decimal)orders.Value * 100m;
    }
}
