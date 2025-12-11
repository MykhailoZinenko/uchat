using System.Threading.Tasks;
using Avalonia.Controls;
using uchat_client.Core.Application.Common.Interfaces;

namespace uchat_client.Presentation.Services;

public class DialogService : IDialogService
{
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = await ShowMessageBoxAsync(title, message, MessageBoxButtons.YesNo);
        return result == MessageBoxResult.Yes;
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        await ShowMessageBoxAsync(title, message, MessageBoxButtons.Ok);
    }

    public async Task ShowInformationAsync(string title, string message)
    {
        await ShowMessageBoxAsync(title, message, MessageBoxButtons.Ok);
    }

    public async Task<string?> ShowInputAsync(string title, string prompt, string defaultValue = "")
    {
        // TODO: Implement custom input dialog
        // For now, return null as not implemented
        await Task.CompletedTask;
        return null;
    }

    private async Task<MessageBoxResult> ShowMessageBoxAsync(string title, string message, MessageBoxButtons buttons)
    {
        // Simple implementation using Avalonia's built-in dialogs
        // In production, you'd want custom styled dialogs
        var window = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20)
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Avalonia.Thickness(0, 0, 0, 20)
        });

        var buttonPanel = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        MessageBoxResult result = MessageBoxResult.None;

        if (buttons == MessageBoxButtons.YesNo)
        {
            var yesButton = new Button { Content = "Yes", Width = 80, Margin = new Avalonia.Thickness(0, 0, 10, 0) };
            yesButton.Click += (s, e) => { result = MessageBoxResult.Yes; window.Close(); };

            var noButton = new Button { Content = "No", Width = 80 };
            noButton.Click += (s, e) => { result = MessageBoxResult.No; window.Close(); };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);
        }
        else
        {
            var okButton = new Button { Content = "OK", Width = 80 };
            okButton.Click += (s, e) => { result = MessageBoxResult.Ok; window.Close(); };
            buttonPanel.Children.Add(okButton);
        }

        panel.Children.Add(buttonPanel);
        window.Content = panel;

        await window.ShowDialog(GetMainWindow());
        return result;
    }

    private Window GetMainWindow()
    {
        // Get the main window from the application
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow!;
        }
        throw new System.InvalidOperationException("Could not find main window");
    }

    private enum MessageBoxButtons
    {
        Ok,
        YesNo
    }

    private enum MessageBoxResult
    {
        None,
        Ok,
        Yes,
        No
    }
}
