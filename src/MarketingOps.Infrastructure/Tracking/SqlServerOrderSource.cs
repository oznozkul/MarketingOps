using System.Data;
using MarketingOps.Application.Tracking;
using Microsoft.Data.SqlClient;

namespace MarketingOps.Infrastructure.Tracking;

/// <summary>Parametreli SELECT'i gün gün çalıştırır. Zaman aşımı sorgu başına 15 saniyedir.</summary>
public sealed class SqlServerOrderSource : IOrderSource
{
    private const int TimeoutSeconds = 15;

    public async Task<IReadOnlyList<OrderDayStats>> GetDailyOrderStatsAsync(string connectionString, string query, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var days = new List<OrderDayStats>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            await using var command = connection.CreateCommand();
            command.CommandText = query;
            command.CommandTimeout = TimeoutSeconds;
            command.Parameters.Add(new SqlParameter("@date", SqlDbType.Date) { Value = date.ToDateTime(TimeOnly.MinValue) });
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                days.Add(new OrderDayStats(date, 0, 0));
                continue;
            }

            var countOrdinal = Ordinal(reader, "order_count", 0);
            var revenueOrdinal = Ordinal(reader, "order_revenue", 1);
            var count = countOrdinal < 0 || reader.IsDBNull(countOrdinal) ? 0 : Convert.ToInt32(reader.GetValue(countOrdinal), System.Globalization.CultureInfo.InvariantCulture);
            var revenue = revenueOrdinal < 0 || reader.IsDBNull(revenueOrdinal) ? 0m : Convert.ToDecimal(reader.GetValue(revenueOrdinal), System.Globalization.CultureInfo.InvariantCulture);
            days.Add(new OrderDayStats(date, count, revenue));
        }
        return days;
    }

    private static int Ordinal(SqlDataReader reader, string name, int fallback)
    {
        for (var i = 0; i < reader.FieldCount; i++)
            if (reader.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase)) return i;
        return reader.FieldCount > fallback ? fallback : -1;
    }
}
