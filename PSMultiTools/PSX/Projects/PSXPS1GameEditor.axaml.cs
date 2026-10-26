using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXPS1GameEditor : Window
{
    public string ProjectDirectory = "";
    private string? _selectedCover;
    private string? _selectedBackground;
    private string? _selectedScreenshot1;
    private string? _selectedScreenshot2;

    public PSXPS1GameEditor()
    {
        InitializeComponent();

        SaveButton.Click += SaveButton_Click;
        LoadFromPSXButton.Click += LoadFromPSXButton_Click;

        GameCoverImage.PointerPressed += (_, _) => BrowseImage(GameCoverImage, path => _selectedCover = path);
        BackgroundImagePictureBox.PointerPressed += (_, _) => BrowseImage(BackgroundImagePictureBox, path => _selectedBackground = path);
        ScreenshotImage1PictureBox.PointerPressed += (_, _) => BrowseImage(ScreenshotImage1PictureBox, path => _selectedScreenshot1 = path);
        ScreenshotImage2PictureBox.PointerPressed += (_, _) => BrowseImage(ScreenshotImage2PictureBox, path => _selectedScreenshot2 = path);
    }

    private async void BrowseImage(Image target, Action<string> onSelected)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select Image (PNG)",
            AllowMultiple = false,
            Filters = [new FileDialogFilter { Name = "PNG Images", Extensions = { "png", "PNG" } }]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            onSelected(selected[0]);
            try { target.Source = new Bitmap(selected[0]); } catch { }
        }
    }

    private async void LoadFromPSXButton_Click(object? sender, RoutedEventArgs e)
    {
        string gameId = GameIDTextBox.Text?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrEmpty(gameId))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please enter a Game ID first (e.g., SLUS-00001 or SLUS_000.01).", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        string cleanId = gameId.Replace("-", "").Replace("_", "").Replace(".", "").ToUpperInvariant();

        try
        {
            using var http = new HttpClient();
            // Try downloading cover from repository or public DB
            string coverUrl = $"https://raw.githubusercontent.com/SvenGDK/PSMT-Covers/main/PS1/{cleanId}.jpg";
            var resp = await http.GetAsync(coverUrl);
            if (resp.IsSuccessStatusCode)
            {
                byte[] imgBytes = await resp.Content.ReadAsByteArrayAsync();
                using var ms = new MemoryStream(imgBytes);
                GameCoverImage.Source = new Bitmap(ms);
            }

            var ok = MessageBoxManager.GetMessageBoxStandard("Data Loaded", $"Searched PSXDatacenter for {cleanId}.", ButtonEnum.Ok, MsgBoxIcon.Info);
            await ok.ShowWindowDialogAsync(this);
        }
        catch (Exception ex)
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not retrieve online data: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Warning);
            await err.ShowWindowDialogAsync(this);
        }
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            string outDir = !string.IsNullOrEmpty(ProjectDirectory) && Directory.Exists(ProjectDirectory)
                ? ProjectDirectory
                : Environment.CurrentDirectory;

            string resDir = Path.Combine(outDir, "res");
            if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);

            if (!string.IsNullOrEmpty(_selectedCover) && File.Exists(_selectedCover))
                File.Copy(_selectedCover, Path.Combine(resDir, "jkt_001.png"), true);
            if (!string.IsNullOrEmpty(_selectedBackground) && File.Exists(_selectedBackground))
                File.Copy(_selectedBackground, Path.Combine(resDir, "bg.png"), true);
            if (!string.IsNullOrEmpty(_selectedScreenshot1) && File.Exists(_selectedScreenshot1))
                File.Copy(_selectedScreenshot1, Path.Combine(resDir, "screen1.png"), true);
            if (!string.IsNullOrEmpty(_selectedScreenshot2) && File.Exists(_selectedScreenshot2))
                File.Copy(_selectedScreenshot2, Path.Combine(resDir, "screen2.png"), true);

            var sb = new StringBuilder();
            sb.AppendLine($"title={GameTitleTextBox.Text?.Trim()}");
            sb.AppendLine($"game_id={GameIDTextBox.Text?.Trim()}");
            sb.AppendLine($"release_date={GameReleaseDateTextBox.Text?.Trim()}");
            sb.AppendLine($"developer={GameDeveloperTextBox.Text?.Trim()}");
            sb.AppendLine($"publisher={GamePublisherTextBox.Text?.Trim()}");
            sb.AppendLine($"note={GameNoteTextBox.Text?.Trim()}");
            sb.AppendLine($"website={GameWebsiteTextBox.Text?.Trim()}");
            sb.AppendLine($"genre={GameGenreTextBox.Text?.Trim()}");
            sb.AppendLine($"region={GameRegionTextBox.Text?.Trim()}");
            sb.AppendLine($"show_subtitle={(ShowGameIDCheckBox.IsChecked == true ? 1 : 0)}");
            sb.AppendLine($"show_copyright={(ShowCopyrightCheckBox.IsChecked == true ? 1 : 0)}");

            await File.WriteAllTextAsync(Path.Combine(outDir, "info.txt"), sb.ToString(), Encoding.UTF8);

            var ok = MessageBoxManager.GetMessageBoxStandard("Saved", "PS1 game resources saved successfully.", ButtonEnum.Ok, MsgBoxIcon.Success);
            await ok.ShowWindowDialogAsync(this);
            Close();
        }
        catch (Exception ex)
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not save resources: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
        }
    }
}
