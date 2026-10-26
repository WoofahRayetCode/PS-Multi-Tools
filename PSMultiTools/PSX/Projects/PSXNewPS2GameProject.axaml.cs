using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXNewPS2GameProject : Window
{
    public PSXNewPS2GameProject()
    {
        InitializeComponent();

        BrowseGameISOButton.Click += BrowseGameISOButton_Click;
        BrowseProjectPathButton.Click += BrowseProjectPathButton_Click;
        BrowseIconButton.Click += BrowseIconButton_Click;
        SaveProjectButton.Click += SaveProjectButton_Click;
    }

    private async void BrowseGameISOButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select PS2 ISO or CUE file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "PS2 Disc Images", Extensions = { "iso", "bin", "cue" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            ProjectISOFileTextBox.Text = selected[0];
            string filename = Path.GetFileNameWithoutExtension(selected[0]);

            if (string.IsNullOrEmpty(ProjectNameTextBox.Text)) ProjectNameTextBox.Text = filename;
            if (string.IsNullOrEmpty(ProjectTitleTextBox.Text)) ProjectTitleTextBox.Text = filename;

            // Try to extract Game ID e.g. SLUS-21194 or SLES_543.21
            var match = Regex.Match(filename, @"([A-Z]{4})[-_](\d{3}\.?\d{2}|\d{5})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                ProjectIDTextBox.Text = match.Value.Replace("-", "_").ToUpperInvariant();
            }
            else if (string.IsNullOrEmpty(ProjectIDTextBox.Text))
            {
                ProjectIDTextBox.Text = $"SLUS_{Math.Abs(filename.GetHashCode()) % 100000:D5}";
            }
        }
    }

    private async void BrowseProjectPathButton_Click(object? sender, RoutedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select project save directory" };
        var selected = await fbd.ShowAsync(this);
        if (!string.IsNullOrEmpty(selected))
        {
            ProjectDirectoryTextBox.Text = selected;
        }
    }

    private async void BrowseIconButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select Custom Icon (PNG)",
            AllowMultiple = false,
            Filters = [new FileDialogFilter { Name = "PNG images", Extensions = { "png", "PNG" } }]
        };
        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            ProjectIconPathTextBox.Text = selected[0];
        }
    }

    private async void SaveProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        string projName = ProjectNameTextBox.Text?.Trim() ?? "";
        string projDir = ProjectDirectoryTextBox.Text?.Trim() ?? "";
        string isoPath = ProjectISOFileTextBox.Text?.Trim() ?? "";
        string title = ProjectTitleTextBox.Text?.Trim() ?? "";
        string gameId = ProjectIDTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(projName) || string.IsNullOrEmpty(projDir) || string.IsNullOrEmpty(isoPath) || !File.Exists(isoPath))
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Validation Error", "Please provide a valid Project Name, Save Directory, and Game ISO file.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
            return;
        }

        try
        {
            string targetProjDir = Path.Combine(projDir, projName);
            string resDir = Path.Combine(targetProjDir, "res");
            Directory.CreateDirectory(resDir);

            // Copy or record game image
            string ext = Path.GetExtension(isoPath);
            File.Copy(isoPath, Path.Combine(targetProjDir, $"image{ext}"), true);

            // Copy Icon
            string iconPath = ProjectIconPathTextBox.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                File.Copy(iconPath, Path.Combine(resDir, "jkt_001.png"), true);
            }

            // Global Projects directory
            string globalProjectsDir = Path.Combine(Environment.CurrentDirectory, "Projects");
            Directory.CreateDirectory(globalProjectsDir);

            string projectFilePath = Path.Combine(globalProjectsDir, $"{projName}.project");
            var sb = new StringBuilder();
            sb.AppendLine($"Title={title}");
            sb.AppendLine($"ID={gameId}");
            sb.AppendLine($"Directory={targetProjDir}");
            sb.AppendLine("Type=PS2");
            if (!string.IsNullOrEmpty(ProjectUninstallMsgTextBox.Text)) sb.AppendLine($"UninstallMsg={ProjectUninstallMsgTextBox.Text.Trim()}");

            await File.WriteAllTextAsync(projectFilePath, sb.ToString(), Encoding.UTF8);

            var ok = MessageBoxManager.GetMessageBoxStandard("Project Saved", $"PS2 Project '{projName}' created successfully!\nFile: {projectFilePath}", ButtonEnum.Ok, MsgBoxIcon.Success);
            await ok.ShowWindowDialogAsync(this);
            Close();
        }
        catch (Exception ex)
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not save project: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
        }
    }
}
