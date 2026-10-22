using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Infrastructure;

/// <summary>
/// İndirilebilir CSV. Türkçe Excel'in liste ayracı noktalı virgül olduğu için ";" kullanılır; UTF-8 BOM, Excel'in
/// Türkçe karakterleri doğru okumasını sağlar.
/// </summary>
public static class CsvFile
{
    public static FileContentResult Create(string fileName, IEnumerable<string> header, IEnumerable<IEnumerable<string?>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(';', header.Select(Escape)));
        foreach (var row in rows) builder.AppendLine(string.Join(';', row.Select(Escape)));
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
        return new FileContentResult(bytes, "text/csv; charset=utf-8") { FileDownloadName = fileName };
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        // Formül enjeksiyonunu önlemek için =, +, -, @ ile başlayan hücreler metin olarak işaretlenir.
        if (value[0] is '=' or '+' or '-' or '@') value = "'" + value;
        return value.IndexOfAny([';', '"', '\n', '\r']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }
}
