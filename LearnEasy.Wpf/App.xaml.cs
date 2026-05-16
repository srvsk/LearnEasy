using System.IO;
using System.Windows;
using LearnEasy.Data;
using LearnEasy.Services;
using LearnEasy.Services.Speech;
using LearnEasy.Wpf.Infrastructure;
using LearnEasy.Wpf.Navigation;
using LearnEasy.Wpf.ViewModels;
using LearnEasy.Wpf.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LearnEasy.Wpf;

/// <summary>
/// Composition root. Builds the generic host (config + DI), applies database
/// migrations, then shows the shell window. Connection strings come from
/// Aspire env vars at runtime, or appsettings.json when run standalone.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        ConfigureServices(builder.Services, builder.Configuration);

        _host = builder.Build();
        await _host.StartAsync();

        // Create/upgrade the schema and apply seed content before the UI opens.
        try
        {
            await _host.Services.EnsureDatabaseAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not reach the database.\n\n{ex.Message}\n\n" +
                "Start the database (e.g. via the Aspire AppHost) and try again.",
                "LearnEasy", MessageBoxButton.OK, MessageBoxImage.Warning);
            Shutdown(1);
            return;
        }

        var shell = _host.Services.GetRequiredService<MainWindow>();
        var nav = _host.Services.GetRequiredService<INavigationService>();
        nav.NavigateToStart();
        shell.Show();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var dbConn = config.GetConnectionString("learneasydb")
                     ?? throw new InvalidOperationException("Missing connection string 'learneasydb'.");

        services.AddSingleton<IConfiguration>(config);
        services.AddLearnEasyData(dbConn);
        services.AddLearnEasyServices(config);

        // Host-supplied speech bits (kept out of the cross-platform core).
        services.AddSingleton<IAudioPlayer, SoundPlayerAudioPlayer>();
        services.AddSingleton<ISpeechFallback, SystemSpeechFallback>();

        // Shell + navigation + view models.
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<StartViewModel>();
        services.AddTransient<LessonViewModel>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
