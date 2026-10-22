using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketingOps.Infrastructure.Persistence.Configurations;

/// <summary>
/// Kayıt (record) listelerini jsonb kolonuna yazar. Kayıtlar değişmezdir; liste yerinde değiştiği için değişiklik takibi
/// öğe öğe değer eşitliğiyle yapılır. EF'nin owned-JSON eşlemesi kurucu parametreli iç içe kayıtları bağlayamadığı için bu yol seçildi.
/// </summary>
public static class JsonColumn
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static PropertyBuilder<List<T>> HasJsonList<T>(this PropertyBuilder<List<T>> property) =>
        property.HasColumnType("jsonb").HasConversion(
            v => JsonSerializer.Serialize(v, Options),
            v => JsonSerializer.Deserialize<List<T>>(v, Options) ?? new List<T>(),
            new ValueComparer<List<T>>((a, b) => a!.SequenceEqual(b!), v => v.Count, v => v.ToList()));

    /// <summary>Değişmez tek değer; yalnızca yenisiyle değiştirilir, referans eşitliği yeterli.</summary>
    public static PropertyBuilder<T> HasJsonValue<T>(this PropertyBuilder<T> property) where T : class =>
        property.HasColumnType("jsonb").HasConversion(
            v => JsonSerializer.Serialize(v, Options),
            v => JsonSerializer.Deserialize<T>(v, Options)!,
            new ValueComparer<T>((a, b) => ReferenceEquals(a, b), v => v.GetHashCode(), v => v));
}
