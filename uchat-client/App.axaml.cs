using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using uchat_client.Core.Application.Common.Interfaces;
using uchat_client.Core.Application.Features.Shell.ViewModels;
using uchat_client.DependencyInjection;
using uchat_client.Presentation.Views.Shell;

namespace uchat_client;

public class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/uchat-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("Application starting up");

            // Build DI container
            var services = new ServiceCollection();
            var serverUrl = $"http://{Program.ServerIp}:{Program.ServerPort}";
            services.AddUChatClient(serverUrl, Program.ClientId);

            _serviceProvider = services.BuildServiceProvider();

            // Load and apply saved theme
            var themeService = _serviceProvider.GetRequiredService<IThemeService>();
            var savedTheme = themeService.LoadSavedTheme();
            themeService.SetTheme(savedTheme);

            // Create MainWindow with MainViewModel
            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
            };

            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnMainWindowClose;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
