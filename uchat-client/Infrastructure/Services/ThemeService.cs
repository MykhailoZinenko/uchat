using System;
using System.IO;
using Avalonia;
using Avalonia.Styling;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Infrastructure.Services;

public class ThemeService : IThemeService
{
    private const string ThemeFileName = "theme.txt";
    private readonly string _themeFilePath;
    private ThemeVariant _currentTheme;

    public ThemeVariant CurrentTheme => _currentTheme;

    public ThemeService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".uchat"
        );

        Directory.CreateDirectory(appDataPath);
        _themeFilePath = Path.Combine(appDataPath, ThemeFileName);
        _currentTheme = LoadSavedTheme();
    }

    public void SetTheme(ThemeVariant theme)
    {
        _currentTheme = theme;

        // Apply theme to application
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = theme;
        }

        // Persist theme choice
        SaveTheme(theme);
    }

    public ThemeVariant LoadSavedTheme()
    {
        try
        {
            if (File.Exists(_themeFilePath))
            {
                var savedTheme = File.ReadAllText(_themeFilePath).Trim();
                return savedTheme.ToLowerInvariant() switch
                {
                    "dark" => ThemeVariant.Dark,
                    "light" => ThemeVariant.Light,
                    _ => ThemeVariant.Default
                };
            }
        }
        catch
        {
            // If there's any error loading the theme, fall back to default
        }

        return ThemeVariant.Default;
    }

    private void SaveTheme(ThemeVariant theme)
    {
        try
        {
            var themeString = theme == ThemeVariant.Dark ? "dark" :
                              theme == ThemeVariant.Light ? "light" : "default";
            File.WriteAllText(_themeFilePath, themeString);
        }
        catch
        {
            // Silently fail if we can't save the theme
        }
    }
}
