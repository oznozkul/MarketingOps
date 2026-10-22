namespace MarketingOps.Domain.WorkTasks;

public enum VerificationType
{
    /// <summary>Sistem işaretler: bağlı bulgu sonraki taramada çözülünce sağlanır.</summary>
    Automatic,

    /// <summary>Ekip işaretler: taramayla doğrulanamayan iş (ör. CMS şablonunun güncellenmesi).</summary>
    TeamConfirmation
}
