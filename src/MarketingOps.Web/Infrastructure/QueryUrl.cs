using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;

namespace MarketingOps.Web.Infrastructure;

/// <summary>Mevcut isteğin query string'ini koruyarak bazı anahtarları değiştiren URL üretir (filtre, sıralama, sayfalama).</summary>
public static class QueryUrl
{
    public static string With(this HttpRequest request, params (string Key, string? Value)[] changes)
    {
        var query = request.Query.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in changes)
        {
            if (string.IsNullOrEmpty(value)) query.Remove(key);
            else query[key] = value;
        }
        return Build(request.Path, query);
    }

    /// <summary>Çok değerli bir anahtarı (ör. severity=Critical&amp;severity=High) verilen değerlerle değiştirir; sayfa başa döner.</summary>
    public static string WithList(this HttpRequest request, string key, IEnumerable<string> values)
    {
        var query = request.Query.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
        query.Remove("page");
        var list = values.ToArray();
        if (list.Length == 0) query.Remove(key);
        else query[key] = new StringValues(list);
        return Build(request.Path, query);
    }

    private static string Build(PathString path, Dictionary<string, StringValues> query)
    {
        var qb = new QueryBuilder();
        foreach (var (key, values) in query)
            foreach (var v in values.Where(v => v is not null))
                qb.Add(key, v!);
        return path + qb.ToQueryString().Value;
    }
}
