namespace MarketingOps.Application.Tracking;

public interface IOrderSource
{
    Task<IReadOnlyList<OrderDayStats>> GetDailyOrderStatsAsync(string connectionString, string query, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
