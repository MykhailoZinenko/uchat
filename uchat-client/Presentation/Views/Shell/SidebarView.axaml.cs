using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using uchat_client.Core.Application.Features.Shell.ViewModels;

namespace uchat_client.Presentation.Views.Shell;

public partial class SidebarView : UserControl
{
    private const long MaxProfilePictureSize = 5 * 1024 * 1024; // 5 MB
    public SidebarView()
    {
        InitializeComponent();
    }

    private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // Check if Enter key was pressed
        if (e.Key == Key.Enter)
        {
            // Get the SidebarViewModel
            if (DataContext is SidebarViewModel viewModel)
            {
                if (viewModel.SearchCommand.CanExecute(null))
                {
                    viewModel.SearchCommand.Execute(null);
                }
            }

            e.Handled = true;
        }
    }

    private async void ProfilePictureButton_Click(object? sender, RoutedEventArgs e)
    {
        // Profile picture handling removed (no backing property)
        await System.Threading.Tasks.Task.CompletedTask;
    }
}