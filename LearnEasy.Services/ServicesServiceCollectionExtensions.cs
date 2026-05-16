using LearnEasy.Core.Abstractions;
using LearnEasy.Services.Learning;
using LearnEasy.Services.Nlp;
using LearnEasy.Services.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LearnEasy.Services;

public static class ServicesServiceCollectionExtensions
{
    /// <summary>
    /// Registers speech, NLP, adaptive difficulty, gamification and the
    /// teacher orchestration. The host must additionally register an
    /// <see cref="IAudioPlayer"/> (and optionally an <see cref="ISpeechFallback"/>).
    /// </summary>
    public static IServiceCollection AddLearnEasyServices(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PiperOptions>(configuration.GetSection(PiperOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));

        // The Ollama value is either an Aspire structured connection string
        // ("Endpoint=http://host:port;Model=phi4-mini") or a plain URL when run
        // standalone. Parse it and let it win over the appsettings defaults.
        var rawOllama = configuration.GetConnectionString("ollama")
                        ?? configuration[$"{OllamaOptions.SectionName}:Endpoint"]
                        ?? "http://localhost:11434";
        var ollama = OllamaConnectionInfo.Parse(rawOllama);

        services.PostConfigure<OllamaOptions>(o =>
        {
            o.Endpoint = ollama.Endpoint;
            if (!string.IsNullOrWhiteSpace(ollama.Model))
                o.Model = ollama.Model!;
        });

        services.AddHttpClient<INlpService, OllamaNlpService>((sp, client) =>
        {
            var opt = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            client.BaseAddress = new Uri(opt.Endpoint);
        });

        // Piper: prefer the HTTP transport (Aspire injects the "piper" endpoint
        // for the orchestrated piper1-gpl container). With no endpoint we fall
        // back to the on-disk CLI so standalone dev still works without Docker.
        var piperEndpoint = configuration.GetConnectionString("piper")
                            ?? configuration[$"{PiperOptions.SectionName}:Endpoint"];

        if (!string.IsNullOrWhiteSpace(piperEndpoint))
        {
            services.PostConfigure<PiperOptions>(o => o.Endpoint = piperEndpoint!);
            services.AddHttpClient<ITextToSpeechService, HttpPiperTextToSpeechService>((sp, client) =>
            {
                var opt = sp.GetRequiredService<IOptions<PiperOptions>>().Value;
                client.BaseAddress = new Uri(opt.Endpoint);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            services.AddSingleton<ITextToSpeechService, PiperTextToSpeechService>();
        }

        services.AddSingleton<IAdaptiveDifficultySelector, AdaptiveDifficultySelector>();
        services.AddSingleton<IGamificationEngine, GamificationEngine>();
        services.AddSingleton<ISpellingLessonService, SpellingLessonService>();

        return services;
    }
}
