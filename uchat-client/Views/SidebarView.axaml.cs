using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using uchat_client.ViewModels;
namespace uchat_client.Views;

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
            // Get the ChatViewModel
            if (DataContext is SidebarViewModel viewModel)
            {
                viewModel.PerformSearch();
            }

            e.Handled = true;
        }
    }

    private async void ProfilePictureButton_Click(object? sender, RoutedEventArgs e)
    {
        // Get the top level (window)
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Configure file picker options
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Profile Picture",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg" },
                    MimeTypes = new[] { "image/png", "image/jpeg" }
                }
            }
        });

        // If a file was selected
        if (files.Count > 0 && DataContext is SidebarViewModel viewModel)
        {
            var file = files[0];
            var filePath = file.Path.LocalPath;

            // Check file size
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxProfilePictureSize)
            {
                // TODO: Show error message to user
                // File too large (> 5 MB)
                return;
            }

            try
            {
                // Load the image
                using var stream = File.OpenRead(filePath);
                var bitmap = new Bitmap(stream);

                // Store the path
                viewModel.ProfilePicturePath = filePath;

                // TODO: Display the bitmap in the UI
                // You'll need to add an Image control and bind it to a property
            }
            catch (Exception ex)
            {
                // TODO: Handle error (invalid image file)
                Console.WriteLine($"Error loading profile picture: {ex.Message}");
            }
        }
    }
}