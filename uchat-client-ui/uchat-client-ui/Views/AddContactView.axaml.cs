// AddContactView.axaml.cs

using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using uchat_client.ViewModels;

namespace uchat_client.Views;

public partial class AddContactView : UserControl
{
    private const long MaxGroupPictureSize = 5 * 1024 * 1024; // 5 MB
    private const int MaxImageDimension = 800;

    public AddContactView()
    {
        InitializeComponent();
    }

    private async void GroupImageButton_Click(object? sender, RoutedEventArgs e)
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
                Console.WriteLine("File too large (> 5 MB)");
                return;
            }

            try
            {
                var compressedPath = await CompressImageAsync(filePath);
                viewModel.SetGroupPicturePath(compressedPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading group picture: {ex.Message}");
            }
        }
    }

    private async System.Threading.Tasks.Task<string> CompressImageAsync(string originalPath)
    {
        using var stream = File.OpenRead(originalPath);
        var originalBitmap = new Bitmap(stream);

        if (originalBitmap.PixelSize.Width <= MaxImageDimension &&
            originalBitmap.PixelSize.Height <= MaxImageDimension)
        {
            return originalPath;
        }

        double scale = Math.Min(
            (double)MaxImageDimension / originalBitmap.PixelSize.Width,
            (double)MaxImageDimension / originalBitmap.PixelSize.Height
        );

        int newWidth = (int)(originalBitmap.PixelSize.Width * scale);
        int newHeight = (int)(originalBitmap.PixelSize.Height * scale);

        var tempPath = Path.Combine(Path.GetTempPath(), $"compressed_group_{Guid.NewGuid()}.png");

        var resizedBitmap = originalBitmap.CreateScaledBitmap(
            new Avalonia.PixelSize(newWidth, newHeight),
            Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality
        );

        using (var saveStream = File.Create(tempPath))
        {
            resizedBitmap.Save(saveStream);
        }

        resizedBitmap.Dispose();
        originalBitmap.Dispose();

        return tempPath;
    }
}