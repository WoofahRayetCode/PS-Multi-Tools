using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXNewAppProject : Window
{
    public PSXNewAppProject()
    {
        InitializeComponent();

        BrowseProjectPathButton.Click += BrowseProjectPathButton_Click;
        BrowseELFButton.Click += BrowseELFButton_Click;
        BrowseIconButton.Click += BrowseIconButton_Click;
        SaveProjectButton.Click += SaveProjectButton_Click;
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

    private async void BrowseELFButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select Homebrew ELF file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "ELF files", Extensions = { "elf", "ELF" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            ProjectELFFileTextBox.Text = selected[0];
            if (string.IsNullOrEmpty(ProjectNameTextBox.Text))
            {
                ProjectNameTextBox.Text = Path.GetFileNameWithoutExtension(selected[0]);
            }
            if (string.IsNullOrEmpty(ProjectTitleTextBox.Text))
            {
                ProjectTitleTextBox.Text = Path.GetFileNameWithoutExtension(selected[0]);
            }
        }
    }

    private async void BrowseIconButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select Custom Icon (PNG)",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "PNG images", Extensions = { "png", "PNG" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
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
        string elfPath = ProjectELFFileTextBox.Text?.Trim() ?? "";
        string title = ProjectTitleTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(projName) || string.IsNullOrEmpty(projDir) || string.IsNullOrEmpty(elfPath) || !File.Exists(elfPath))
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Validation Error", "Please provide a valid Project Name, Save Directory, and Homebrew ELF file.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
            return;
        }

        try
        {
            string targetProjDir = Path.Combine(projDir, projName);
            string resDir = Path.Combine(targetProjDir, "res");
            Directory.CreateDirectory(resDir);

            // Copy ELF
            string targetElf = Path.Combine(targetProjDir, Path.GetFileName(elfPath));
            File.Copy(elfPath, targetElf, true);

            // Copy Icon if provided
            string iconPath = ProjectIconPathTextBox.Text?.Trim() ?? "";
            if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
            {
                File.Copy(iconPath, Path.Combine(resDir, "jkt_001.png"), true);
            }

            // Generate App ID
            string appId = $"PSX{Math.Abs(projName.GetHashCode()) % 100000:D5}";

            // Global Projects directory
            string globalProjectsDir = Path.Combine(Environment.CurrentDirectory, "Projects");
            Directory.CreateDirectory(globalProjectsDir);

            string projectFilePath = Path.Combine(globalProjectsDir, $"{projName}.project");
            var sb = new StringBuilder();
            sb.AppendLine($"Title={title}");
            sb.AppendLine($"ID={appId}");
            sb.AppendLine($"Directory={targetProjDir}");
            sb.AppendLine("Type=App");
            if (!string.IsNullOrEmpty(ProjectSubTitleTextBox.Text)) sb.AppendLine($"SubTitle={ProjectSubTitleTextBox.Text.Trim()}");
            if (!string.IsNullOrEmpty(ProjectUninstallMsgTextBox.Text)) sb.AppendLine($"UninstallMsg={ProjectUninstallMsgTextBox.Text.Trim()}");

            await File.WriteAllTextAsync(projectFilePath, sb.ToString(), Encoding.UTF8);

            var ok = MessageBoxManager.GetMessageBoxStandard("Project Saved", $"PSX App Project '{projName}' created successfully!\nFile: {projectFilePath}", ButtonEnum.Ok, MsgBoxIcon.Success);
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
