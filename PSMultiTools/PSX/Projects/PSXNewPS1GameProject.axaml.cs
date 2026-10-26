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

public partial class PSXNewPS1GameProject : Window
{
    public PSXNewPS1GameProject()
    {
        InitializeComponent();

        BrowseIMAGE0Button.Click += BrowseIMAGE0Button_Click;
        BrowseIMAGE1Button.Click += (_, _) => BrowseDisc(IMAGE1PathTextBox);
        BrowseIMAGE2Button.Click += (_, _) => BrowseDisc(IMAGE2PathTextBox);
        BrowseIMAGE3Button.Click += (_, _) => BrowseDisc(IMAGE3PathTextBox);

        BrowseProjectPathButton.Click += BrowseProjectPathButton_Click;
        BrowseIconButton.Click += BrowseIconButton_Click;

        MultiDiscCheckBox.IsCheckedChanged += MultiDiscCheckBox_IsCheckedChanged;
        SaveProjectButton.Click += SaveProjectButton_Click;
    }

    private void MultiDiscCheckBox_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        bool isMulti = MultiDiscCheckBox.IsChecked == true;
        IMAGE1PathTextBox.IsEnabled = isMulti;
        BrowseIMAGE1Button.IsEnabled = isMulti;
        IMAGE2PathTextBox.IsEnabled = isMulti;
        BrowseIMAGE2Button.IsEnabled = isMulti;
        IMAGE3PathTextBox.IsEnabled = isMulti;
        BrowseIMAGE3Button.IsEnabled = isMulti;
        UpdateDiscsPreview();
    }

    private void UpdateDiscsPreview()
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(IMAGE0PathTextBox.Text)) sb.AppendLine(Path.GetFileName(IMAGE0PathTextBox.Text));
        if (MultiDiscCheckBox.IsChecked == true)
        {
            if (!string.IsNullOrEmpty(IMAGE1PathTextBox.Text)) sb.AppendLine(Path.GetFileName(IMAGE1PathTextBox.Text));
            if (!string.IsNullOrEmpty(IMAGE2PathTextBox.Text)) sb.AppendLine(Path.GetFileName(IMAGE2PathTextBox.Text));
            if (!string.IsNullOrEmpty(IMAGE3PathTextBox.Text)) sb.AppendLine(Path.GetFileName(IMAGE3PathTextBox.Text));
        }
        DISCSInfoTextBox.Text = sb.ToString();
    }

    private async void BrowseDisc(TextBox target)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select PS1 VCD file",
            AllowMultiple = false,
            Filters = [new FileDialogFilter { Name = "VCD files", Extensions = { "vcd", "VCD" } }]
        };
        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            target.Text = selected[0];
            UpdateDiscsPreview();
        }
    }

    private async void BrowseIMAGE0Button_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select PS1 VCD file (IMAGE0.VCD)",
            AllowMultiple = false,
            Filters = [new FileDialogFilter { Name = "VCD files", Extensions = { "vcd", "VCD" } }]
        };
        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            IMAGE0PathTextBox.Text = selected[0];
            string filename = Path.GetFileNameWithoutExtension(selected[0]);

            if (string.IsNullOrEmpty(ProjectNameTextBox.Text)) ProjectNameTextBox.Text = filename;
            if (string.IsNullOrEmpty(ProjectTitleTextBox.Text)) ProjectTitleTextBox.Text = filename;

            // Try to extract Game ID e.g. SLUS_012.34 or SLES-12345
            var match = Regex.Match(filename, @"([A-Z]{4})[-_](\d{3}\.?\d{2})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                ProjectIDTextBox.Text = match.Value.Replace("-", "_").ToUpperInvariant();
            }
            else if (string.IsNullOrEmpty(ProjectIDTextBox.Text))
            {
                ProjectIDTextBox.Text = $"SLUS_{Math.Abs(filename.GetHashCode()) % 100000:D5}";
            }

            UpdateDiscsPreview();
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
        string vcdPath = IMAGE0PathTextBox.Text?.Trim() ?? "";
        string title = ProjectTitleTextBox.Text?.Trim() ?? "";
        string gameId = ProjectIDTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(projName) || string.IsNullOrEmpty(projDir) || string.IsNullOrEmpty(vcdPath) || !File.Exists(vcdPath))
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Validation Error", "Please provide a valid Project Name, Save Directory, and IMAGE0.VCD file.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
            return;
        }

        try
        {
            string targetProjDir = Path.Combine(projDir, projName);
            string resDir = Path.Combine(targetProjDir, "res");
            Directory.CreateDirectory(resDir);

            // Copy VCD
            File.Copy(vcdPath, Path.Combine(targetProjDir, "IMAGE0.VCD"), true);

            // Multi-disc
            if (MultiDiscCheckBox.IsChecked == true)
            {
                var sbDiscs = new StringBuilder();
                sbDiscs.AppendLine("IMAGE0.VCD");

                if (!string.IsNullOrEmpty(IMAGE1PathTextBox.Text) && File.Exists(IMAGE1PathTextBox.Text))
                {
                    File.Copy(IMAGE1PathTextBox.Text, Path.Combine(targetProjDir, "IMAGE1.VCD"), true);
                    sbDiscs.AppendLine("IMAGE1.VCD");
                }
                if (!string.IsNullOrEmpty(IMAGE2PathTextBox.Text) && File.Exists(IMAGE2PathTextBox.Text))
                {
                    File.Copy(IMAGE2PathTextBox.Text, Path.Combine(targetProjDir, "IMAGE2.VCD"), true);
                    sbDiscs.AppendLine("IMAGE2.VCD");
                }
                if (!string.IsNullOrEmpty(IMAGE3PathTextBox.Text) && File.Exists(IMAGE3PathTextBox.Text))
                {
                    File.Copy(IMAGE3PathTextBox.Text, Path.Combine(targetProjDir, "IMAGE3.VCD"), true);
                    sbDiscs.AppendLine("IMAGE3.VCD");
                }

                await File.WriteAllTextAsync(Path.Combine(targetProjDir, "DISCS.TXT"), sbDiscs.ToString(), Encoding.UTF8);
            }

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
            sb.AppendLine("Type=PS1");
            if (!string.IsNullOrEmpty(ProjectUninstallMsgTextBox.Text)) sb.AppendLine($"UninstallMsg={ProjectUninstallMsgTextBox.Text.Trim()}");

            await File.WriteAllTextAsync(projectFilePath, sb.ToString(), Encoding.UTF8);

            var ok = MessageBoxManager.GetMessageBoxStandard("Project Saved", $"PS1 Project '{projName}' created successfully!\nFile: {projectFilePath}", ButtonEnum.Ok, MsgBoxIcon.Success);
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
