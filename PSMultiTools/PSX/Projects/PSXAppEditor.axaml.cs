using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXAppEditor : Window
{
    public string ProjectDirectory = "";
    private string? _selectedCoverPath;

    public PSXAppEditor()
    {
        InitializeComponent();

        SaveButton.Click += SaveButton_Click;
        CoverPictureBox.PointerPressed += CoverPictureBox_PointerPressed;
    }

    private async void CoverPictureBox_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select New Cover Image (PNG)",
            AllowMultiple = false,
            Filters = [new FileDialogFilter { Name = "PNG Images", Extensions = { "png", "PNG" } }]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            _selectedCoverPath = selected[0];
            try
            {
                CoverPictureBox.Source = new Bitmap(_selectedCoverPath);
            }
            catch { }
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

            if (!string.IsNullOrEmpty(_selectedCoverPath) && File.Exists(_selectedCoverPath))
            {
                File.Copy(_selectedCoverPath, Path.Combine(resDir, "jkt_001.png"), true);
            }

            var sb = new StringBuilder();
            sb.AppendLine($"title={HomebrewTitleTextBox.Text?.Trim()}");
            sb.AppendLine($"subtitle={HomebrewSubtitleTextBox.Text?.Trim()}");
            sb.AppendLine($"release_date={HomebrewReleaseDateTextBox.Text?.Trim()}");
            sb.AppendLine($"developer={HomebrewDeveloperTextBox.Text?.Trim()}");
            sb.AppendLine($"publisher={PublisherTextBox.Text?.Trim()}");
            sb.AppendLine($"note={HomebrewNoteTextBox.Text?.Trim()}");
            sb.AppendLine($"website={HomebrewWebsiteTextBox.Text?.Trim()}");
            sb.AppendLine($"genre={HomebrewGenreTextBox.Text?.Trim()}");
            sb.AppendLine($"region={RegionTextBox.Text?.Trim()}");
            sb.AppendLine($"show_subtitle={(ShowGameIDCheckBox.IsChecked == true ? 1 : 0)}");
            sb.AppendLine($"show_copyright={(ShowCopyrightCheckBox.IsChecked == true ? 1 : 0)}");

            await File.WriteAllTextAsync(Path.Combine(outDir, "info.txt"), sb.ToString(), Encoding.UTF8);

            var ok = MessageBoxManager.GetMessageBoxStandard("Saved", "Homebrew resources saved successfully.", ButtonEnum.Ok, MsgBoxIcon.Success);
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
