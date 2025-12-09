using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using uchat_client.ViewModels;

namespace uchat_client.Views;

public partial class AddContactView : UserControl
{
    private const long MaxGroupPictureSize = 5 * 1024 * 1024; // 5 MB

    public AddContactView()
    {
        InitializeComponent();
    }

    private void Background_Click(object? sender, PointerPressedEventArgs e)
    {
        // Close the window when clicking on the dark background
        if (DataContext is AddContactViewModel viewModel)
        {
            viewModel.CloseCommand.Execute(null);
        }
    }

    private void Content_Click(object? sender, PointerPressedEventArgs e)
    {
        // Prevent the click from propagating to the background
        e.Handled = true;
    }

    private async void UploadGroupPicture_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Group Picture",
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

        if (files.Count > 0 && DataContext is AddContactViewModel viewModel)
        {
            var file = files[0];
            var filePath = file.Path.LocalPath;

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MaxGroupPictureSize)
            {
                viewModel.ErrorMessage = "Image too large (max 5 MB)";
                return;
            }

            try
            {
                viewModel.SetGroupPicturePath(filePath);
                viewModel.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                viewModel.ErrorMessage = "Invalid image file";
                Console.WriteLine($"Error loading group picture: {ex.Message}");
            }
        }
    }
}