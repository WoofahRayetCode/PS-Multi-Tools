using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace PSMultiTools;

public partial class PSXAssetsBrowser : Window
{
    public class AssetItem
    {
        public string AssetFileName { get; set; } = "";
        public string AssetFilePath { get; set; } = "";
        public Bitmap? Icon { get; set; }
    }

    private readonly ObservableCollection<AssetItem> _assets = [];

    public PSXAssetsBrowser()
    {
        InitializeComponent();

        AssetFilesListView.ItemsSource = _assets;

        OpenFolderImage.PointerPressed += OpenFolderImage_PointerPressed;
        AssetFilesListView.SelectionChanged += AssetFilesListView_SelectionChanged;
        OpenImageEditorImage.PointerPressed += OpenImageEditorImage_PointerPressed;
    }

    private async void OpenFolderImage_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select XMB Assets Folder" };
        var selected = await fbd.ShowAsync(this);
        if (string.IsNullOrEmpty(selected) || !Directory.Exists(selected))
            return;

        _assets.Clear();
        var files = Directory.GetFiles(selected, "*.*", SearchOption.TopDirectoryOnly);
        foreach (var f in files)
        {
            _assets.Add(new AssetItem
            {
                AssetFileName = Path.GetFileName(f),
                AssetFilePath = f
            });
        }
    }

    private void AssetFilesListView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (AssetFilesListView.SelectedItem is not AssetItem selected || !File.Exists(selected.AssetFilePath))
            return;

        string ext = Path.GetExtension(selected.AssetFilePath).ToLowerInvariant();
        if (ext is ".png" or ".jpg" or ".jpeg" or ".bmp")
        {
            try
            {
                ImageViewer.Source = new Bitmap(selected.AssetFilePath);
                ImageViewer.IsVisible = true;
                FontPreviewTextBlock.IsVisible = false;
                OpenImageEditorImage.IsVisible = true;
                OpenImageEditorTextBlock.IsVisible = true;
            }
            catch
            {
                ImageViewer.IsVisible = false;
            }
        }
        else if (ext is ".ttf" or ".otf")
        {
            ImageViewer.IsVisible = false;
            FontPreviewTextBlock.IsVisible = true;
            OpenImageEditorImage.IsVisible = false;
            OpenImageEditorTextBlock.IsVisible = false;
        }
        else
        {
            ImageViewer.IsVisible = false;
            FontPreviewTextBlock.IsVisible = false;
            OpenImageEditorImage.IsVisible = false;
            OpenImageEditorTextBlock.IsVisible = false;
        }
    }

    private void OpenImageEditorImage_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var editor = new PSXSimpleGraphicsEditor { ShowActivated = true };
        editor.Show();
    }
}
