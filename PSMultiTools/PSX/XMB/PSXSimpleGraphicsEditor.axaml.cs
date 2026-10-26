using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXSimpleGraphicsEditor : Window
{
    private string? _currentImagePath;

    public PSXSimpleGraphicsEditor()
    {
        InitializeComponent();

        LoadImageMenuItem.Click += LoadImageMenuItem_Click;
        SaveImageMenuItem.Click += SaveImageMenuItem_Click;
    }

    private async void LoadImageMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Load Image",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "Images (PNG, JPG, BMP)", Extensions = { "png", "jpg", "jpeg", "bmp" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            _currentImagePath = selected[0];
            try
            {
                LoadedImage.Source = new Bitmap(_currentImagePath);
                EditMenuItem.IsEnabled = true;
                ImageMenuItem.IsEnabled = true;
                ClipboardMenuItem.IsEnabled = true;
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not load image: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
                await box.ShowWindowDialogAsync(this);
            }
        }
    }

    private async void SaveImageMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentImagePath) || !File.Exists(_currentImagePath))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice", "No image currently loaded to save.", ButtonEnum.Ok, MsgBoxIcon.Info);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Title = "Save Image As",
            DefaultExtension = "png",
            Filters = [
                new FileDialogFilter { Name = "PNG image", Extensions = { "png" } },
                new FileDialogFilter { Name = "JPEG image", Extensions = { "jpg" } }
            ]
        };

        var savePath = await sfd.ShowAsync(this);
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                File.Copy(_currentImagePath, savePath, true);
                var box = MessageBoxManager.GetMessageBoxStandard("Saved", "Image saved successfully.", ButtonEnum.Ok, MsgBoxIcon.Success);
                await box.ShowWindowDialogAsync(this);
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not save image: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
                await box.ShowWindowDialogAsync(this);
            }
        }
    }
}
