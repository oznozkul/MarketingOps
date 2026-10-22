namespace MarketingOps.Web.ViewModels.Design;

public sealed record DesignOrderDay(DateOnly Date, int Orders, int Purchases)
{
    public double DifferencePercent => Orders == 0 ? 0 : (Purchases - Orders) * 100.0 / Orders;
}
