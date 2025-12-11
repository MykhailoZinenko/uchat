using Avalonia.Styling;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IThemeService
{
    ThemeVariant CurrentTheme { get; }
    void SetTheme(ThemeVariant theme);
    ThemeVariant LoadSavedTheme();
}
