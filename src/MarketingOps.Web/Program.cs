using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using MarketingOps.Application;
using MarketingOps.Infrastructure;
using MarketingOps.Infrastructure.Persistence;
using Hangfire;
using MarketingOps.Web.Filters;
using MarketingOps.Web.Hubs;
using MarketingOps.Web.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.WebEncoders;

var builder = WebApplication.CreateBuilder(args);

// Arayüz genelinde Türkçe kültür: 1.206, %38, 21 Eki.
var tr = CultureInfo.GetCultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = tr;
CultureInfo.DefaultThreadCurrentUICulture = tr;

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Türkçe karakterler HTML'de entity'ye çevrilmesin (ı → &#x131;); kaynak okunur ve daha küçük kalır.
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

builder.Services.AddScoped<SiteContext>();
builder.Services.AddScoped<ISiteContext>(sp => sp.GetRequiredService<SiteContext>());
builder.Services.AddScoped<SiteContextFilter>();

builder.Services
    .AddControllersWithViews(o =>
    {
        o.Filters.AddService<SiteContextFilter>();
        // Doğrulama FluentValidation'da ve Türkçe; MVC'nin örtük [Required] ve İngilizce bağlama mesajları devre dışı.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        var messages = o.ModelBindingMessageProvider;
        messages.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
        messages.SetMissingBindRequiredValueAccessor(_ => "Bu alan zorunludur.");
        messages.SetAttemptedValueIsInvalidAccessor((value, _) => $"'{value}' geçerli bir değer değil.");
        messages.SetValueIsInvalidAccessor(value => $"'{value}' geçerli bir değer değil.");
        messages.SetValueMustBeANumberAccessor(_ => "Bir sayı girin.");
        messages.SetUnknownValueIsInvalidAccessor(_ => "Geçerli bir değer girin.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"'{value}' geçerli bir değer değil.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Geçerli bir değer girin.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "Bir sayı girin.");
        messages.SetMissingKeyOrValueAccessor(() => "Bu alan zorunludur.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "İstek gövdesi boş olamaz.");
    })
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("postgres");
builder.Services.AddOpenApi();
builder.Services.AddSignalR().AddJsonProtocol(o => o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddHostedService<CrawlProgressBroadcaster>();

var app = builder.Build();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(tr),
    SupportedCultures = [tr],
    SupportedUICultures = [tr]
});

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    ExceptionHandlingPath = "/error",
    // GlobalExceptionHandler'ın işlediği beklenen hatalar (404, 409, 400) "unhandled" diye loglanmasın.
    SuppressDiagnosticsCallback = ctx => ctx.ExceptionHandledBy == ExceptionHandledType.ExceptionHandlerService
});
if (!app.Environment.IsDevelopment())
    app.UseHsts();

// API yanıtları ProblemDetails olarak kalır; HTML hata sayfası yalnızca ekranlar için.
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/error/{0}"));

app.UseHttpsRedirection();
app.UseRouting();
app.MapStaticAssets();
app.MapControllers().WithStaticAssets();
app.MapHealthChecks("/health");
app.MapHub<CrawlHub>(CrawlHub.Path);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHangfireDashboard("/jobs", new DashboardOptions { Authorization = [new DevelopmentDashboardAuthorization()] });
    await DatabaseInitializer.MigrateAndSeedAsync(app.Services);
}

app.Run();

public partial class Program;
