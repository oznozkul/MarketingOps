using System.Globalization;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;

namespace MarketingOps.Infrastructure.Google;

/// <summary>GA4 Admin API (mülk listesi) ve Data API (event raporu).</summary>
public sealed class Ga4Client(HttpClient http) : IGa4Client
{
    public async Task<IReadOnlyList<Ga4Property>> ListPropertiesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var properties = new List<Ga4Property>();
        string? pageToken = null;
        do
        {
            var url = "https://analyticsadmin.googleapis.com/v1beta/accountSummaries?pageSize=200" + (pageToken is null ? "" : $"&pageToken={Uri.EscapeDataString(pageToken)}");
            var json = await GoogleHttp.SendAsync(http, HttpMethod.Get, url, accessToken, null, cancellationToken);
            foreach (var account in json.Array("accountSummaries"))
            foreach (var property in account.Array("propertySummaries"))
                properties.Add(new Ga4Property(property.String("property")!, property.String("displayName") ?? "", account.String("displayName") ?? ""));
            pageToken = json.String("nextPageToken");
        } while (!string.IsNullOrEmpty(pageToken));
        return properties;
    }

    public async Task<IReadOnlyList<Ga4EventRow>> RunEventReportAsync(string accessToken, string propertyId, DateOnly start, DateOnly end,
        IReadOnlyList<string> eventNames, CancellationToken cancellationToken)
    {
        var json = await GoogleHttp.SendAsync(http, HttpMethod.Post, $"https://analyticsdata.googleapis.com/v1beta/{propertyId}:runReport", accessToken, new
        {
            dateRanges = new[] { new { startDate = start.ToString("yyyy-MM-dd"), endDate = end.ToString("yyyy-MM-dd") } },
            dimensions = new[] { new { name = "date" }, new { name = "eventName" } },
            metrics = new[] { new { name = "eventCount" }, new { name = "eventValue" } },
            dimensionFilter = new { filter = new { fieldName = "eventName", inListFilter = new { values = eventNames } } },
            limit = 10000
        }, cancellationToken);
        return json.Array("rows").Select(r =>
        {
            var dims = r.GetProperty("dimensionValues");
            var mets = r.GetProperty("metricValues");
            return new Ga4EventRow(DateOnly.ParseExact(dims[0].String("value")!, "yyyyMMdd", CultureInfo.InvariantCulture), dims[1].String("value")!,
                long.Parse(mets[0].String("value") ?? "0", CultureInfo.InvariantCulture),
                decimal.Parse(mets[1].String("value") ?? "0", NumberStyles.Float, CultureInfo.InvariantCulture));
        }).ToList();
    }
}
