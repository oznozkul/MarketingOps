using FluentValidation;
using MarketingOps.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddScoped<Rules.RuleEvaluator>();
        services.AddScoped<Tasks.WorkTaskFactory>();
        services.AddScoped<Rules.ExternalDataLoader>();
        services.AddScoped<Scores.ScoreReader>();
        services.AddScoped<Notifications.SummaryReportFactory>();
        services.AddSingleton<Connectors.ConnectorSecretProtector>();
        services.AddScoped<Connectors.GoogleTokenProvider>();
        return services;
    }
}
