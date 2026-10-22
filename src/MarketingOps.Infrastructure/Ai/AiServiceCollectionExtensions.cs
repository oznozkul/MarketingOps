using Anthropic;
using MarketingOps.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Infrastructure.Ai;

public static class AiServiceCollectionExtensions
{
    /// <summary>
    /// Seçili sağlayıcıyı kaydeder. Sağlayıcı yoksa ya da anahtarı eksikse <see cref="IAiSuggestionService"/> kaydedilmez;
    /// görevler şablon önerisiyle çalışmaya devam eder.
    /// </summary>
    public static IServiceCollection AddAiSuggestions(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(AiOptions.Section).Get<AiOptions>() ?? new AiOptions();
        services.AddSingleton(sp => new KnowledgeBase(
            Path.Combine(sp.GetRequiredService<IHostEnvironment>().ContentRootPath, configuration["Knowledge:Path"] ?? "knowledge"),
            sp.GetRequiredService<ILogger<KnowledgeBase>>()));

        switch (options.Provider)
        {
            case AiProvider.Anthropic when !string.IsNullOrWhiteSpace(options.Anthropic.ApiKey):
                services.AddSingleton(options.Anthropic);
                services.AddSingleton(new AnthropicClient { ApiKey = options.Anthropic.ApiKey });
                services.AddSingleton<IAiSuggestionService, AnthropicSuggestionService>();
                break;
            case AiProvider.DeepSeek when !string.IsNullOrWhiteSpace(options.DeepSeek.ApiKey):
                AddOpenAiCompatible(services, options.DeepSeek);
                break;
            case AiProvider.Ollama when !string.IsNullOrWhiteSpace(options.Ollama.Model):
                AddOpenAiCompatible(services, options.Ollama);
                break;
        }
        return services;
    }

    private static void AddOpenAiCompatible(IServiceCollection services, OpenAiCompatibleOptions options)
    {
        services.AddSingleton(options);
        services.AddHttpClient<IAiSuggestionService, OpenAiCompatibleSuggestionService>(c => c.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds));
    }
}
