using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Connectors;

namespace MarketingOps.UnitTests.Domain.Connectors;

public class ConnectorAccountTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Property_connector_waits_for_selection_after_authorization()
    {
        var account = ConnectorAccount.Create(Guid.CreateVersion7(), ConnectorType.SearchConsole, T0);
        account.Status.ShouldBe(ConnectorStatus.NotConnected);
        Should.Throw<DomainException>(() => account.SelectProperty("sc-domain:ornek.com", "x", T0));

        account.Authorize("şifreli", "a@ornek.com", ["scope"], requiresProperty: true, T0);
        account.Status.ShouldBe(ConnectorStatus.PendingApproval);
        account.SelectProperty("sc-domain:ornek.com", "sc-domain:ornek.com", T0);
        account.Status.ShouldBe(ConnectorStatus.Connected);

        // Yeniden yetkilendirmede seçili mülk korunur.
        account.Authorize("yeni", "a@ornek.com", ["scope"], requiresProperty: true, T0.AddDays(1));
        account.Status.ShouldBe(ConnectorStatus.Connected);
        account.ExternalAccountId.ShouldBe("sc-domain:ornek.com");
    }

    [Fact]
    public void Failures_mark_error_or_expired_and_sync_recovers()
    {
        var account = ConnectorAccount.Create(Guid.CreateVersion7(), ConnectorType.Ga4, T0);
        account.Authorize("şifreli", "a@ornek.com", [], requiresProperty: true, T0);
        account.SelectProperty("properties/1", "Hesap › Mülk", T0);

        account.RecordFailure("kota", authorizationExpired: false, T0);
        account.Status.ShouldBe(ConnectorStatus.Error);
        account.RecordSync(T0.AddHours(1));
        account.Status.ShouldBe(ConnectorStatus.Connected);
        account.LastError.ShouldBeNull();

        account.RecordFailure("yetki", authorizationExpired: true, T0);
        account.Status.ShouldBe(ConnectorStatus.Expired);
    }

    [Fact]
    public void Authorizing_with_another_google_account_clears_the_selected_property()
    {
        var account = ConnectorAccount.Create(Guid.CreateVersion7(), ConnectorType.Ga4, T0);
        account.Authorize("şifreli", "yanlis@ornek.com", [], requiresProperty: true, T0);
        account.SelectProperty("properties/1", "Demo", T0);

        account.Authorize("yeni", "dogru@ornek.com", [], requiresProperty: true, T0.AddMinutes(5));

        account.ExternalAccountId.ShouldBeNull();
        account.Status.ShouldBe(ConnectorStatus.PendingApproval);
        account.AccountEmail.ShouldBe("dogru@ornek.com");
    }

    [Fact]
    public void Clearing_the_property_keeps_authorization_and_waits_for_a_new_selection()
    {
        var account = ConnectorAccount.Create(Guid.CreateVersion7(), ConnectorType.Ga4, T0);
        account.Authorize("şifreli", "a@ornek.com", [], requiresProperty: true, T0);
        account.SelectProperty("properties/1", "Demo", T0);

        account.ClearProperty(T0.AddMinutes(1));

        account.ShouldSatisfyAllConditions(
            a => a.ExternalAccountId.ShouldBeNull(), a => a.Status.ShouldBe(ConnectorStatus.PendingApproval), a => a.HasSecret.ShouldBeTrue());
    }

    [Fact]
    public void Disconnect_clears_credentials_and_property()
    {
        var account = ConnectorAccount.Create(Guid.CreateVersion7(), ConnectorType.SearchConsole, T0);
        account.Authorize("şifreli", "a@ornek.com", ["s"], requiresProperty: true, T0);
        account.SelectProperty("sc-domain:ornek.com", "x", T0);
        account.Disconnect(T0);
        account.ShouldSatisfyAllConditions(
            a => a.Status.ShouldBe(ConnectorStatus.NotConnected), a => a.ProtectedSecret.ShouldBeNull(),
            a => a.ExternalAccountId.ShouldBeNull(), a => a.Scopes.ShouldBeEmpty());
    }

    [Theory]
    [InlineData("sc-domain:lastikborsasi.com", true)]
    [InlineData("https://www.lastikborsasi.com/", true)]
    [InlineData("https://lastikborsasi.com/", true)]
    [InlineData("https://blog.lastikborsasi.com/", false)]
    [InlineData("sc-domain:baska.com", false)]
    public void Search_console_property_matches_site_domain(string siteUrl, bool expected) =>
        GoogleConnectors.MatchesSite(siteUrl, "lastikborsasi.com").ShouldBe(expected);
}
