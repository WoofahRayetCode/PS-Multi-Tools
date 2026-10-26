using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PSMultiTools.PSV.Tools;

public partial class PSVIMGTools : Window
{
    public PSVIMGTools()
    {
        InitializeComponent();

        // Extract PSVIMG
        BrowsePSVIMGButton.Click += BrowsePSVIMGButton_Click;
        BrowseExtractOutputFolderButton.Click += BrowseExtractOutputFolderButton_Click;
        ExtractPSVIMGButton.Click += ExtractPSVIMGButton_Click;

        // Decrypt PSVMD
        BrowsePSVMDButton.Click += BrowsePSVMDButton_Click;
        BrowseDecryptOutputFolderButton.Click += BrowseDecryptOutputFolderButton_Click;
        DecryptPSVMDButton.Click += DecryptPSVMDButton_Click;

        // Create PSVIMG
        BrowseMetadataFileButton.Click += BrowseMetadataFileButton_Click;
        BrowseCreateInputFolderButton.Click += BrowseCreateInputFolderButton_Click;
        BrowseCreateOutputFolderButton.Click += BrowseCreateOutputFolderButton_Click;
        CreatePSVIMGButton.Click += CreatePSVIMGButton_Click;
    }

    private static string? FindPsvimgTool(string binaryName)
    {
        string exeName = OperatingSystem.IsWindows() ? $"{binaryName}.exe" : binaryName;
        string platformSubdir = OperatingSystem.IsWindows() ? "Windows" : (OperatingSystem.IsMacOS() ? "macOS" : "Linux");

        string[] searchPaths = [
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", platformSubdir, exeName),
            Path.Combine(Environment.CurrentDirectory, "Tools", platformSubdir, exeName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", exeName),
            Path.Combine(Environment.CurrentDirectory, "Tools", exeName),
        ];

        foreach (var path in searchPaths)
        {
            if (File.Exists(path)) return path;
        }

        // Check system PATH
        return null;
    }

    private async void BrowsePSVIMGButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select .psvimg file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "PSVIMG files", Extensions = { "psvimg" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            SelectedPSVIMGFileTextBox.Text = selected[0];
        }
    }

    private async void BrowseExtractOutputFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select output folder" };
        var selected = await fbd.ShowAsync(this);
        if (!string.IsNullOrEmpty(selected))
        {
            ExtractOutputFolderTextBox.Text = selected;
        }
    }

    private async void ExtractPSVIMGButton_Click(object? sender, RoutedEventArgs e)
    {
        string key = BackupKeyTextBox.Text?.Trim() ?? "";
        string file = SelectedPSVIMGFileTextBox.Text?.Trim() ?? "";
        string outDir = ExtractOutputFolderTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(key))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please provide a valid CMA backup key.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a valid .psvimg file.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (string.IsNullOrEmpty(outDir))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select an output folder.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        string? toolPath = FindPsvimgTool("psvimg-extract");
        if (toolPath is null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Missing Tool", "psvimg-extract binary was not found in the Tools directory.\nPlease download psvimgtools and place psvimg-extract in Tools/.", ButtonEnum.Ok, MsgBoxIcon.Warning);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        LogTextBox.Text = $"Extracting {file} to {outDir}...\r\n";
        ExtractPSVIMGButton.IsEnabled = false;

        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = $"-k {key} -i \"{file}\" \"{outDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc is not null)
                {
                    proc.WaitForExit();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LogTextBox.Text += $"Extraction completed with exit code {proc.ExitCode}.\r\n";
                        ExtractPSVIMGButton.IsEnabled = true;
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LogTextBox.Text += $"Error: {ex.Message}\r\n";
                    ExtractPSVIMGButton.IsEnabled = true;
                });
            }
        });
    }

    private async void BrowsePSVMDButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select .psvmd file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "PSVMD files", Extensions = { "psvmd" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            SelectedPSVMDDecryptFileTextBox.Text = selected[0];
        }
    }

    private async void BrowseDecryptOutputFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select output folder" };
        var selected = await fbd.ShowAsync(this);
        if (!string.IsNullOrEmpty(selected))
        {
            DecryptOutputFolderTextBox.Text = selected;
        }
    }

    private async void DecryptPSVMDButton_Click(object? sender, RoutedEventArgs e)
    {
        string key = BackupKeyTextBox.Text?.Trim() ?? "";
        string file = SelectedPSVMDDecryptFileTextBox.Text?.Trim() ?? "";
        string outDir = DecryptOutputFolderTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(key))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please provide a valid CMA backup key.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (string.IsNullOrEmpty(file) || !File.Exists(file))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a valid .psvmd file.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (string.IsNullOrEmpty(outDir))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select an output folder.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        string? toolPath = FindPsvimgTool("psvmd-decrypt");
        if (toolPath is null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Missing Tool", "psvmd-decrypt binary was not found in the Tools directory.\nPlease download psvimgtools and place psvmd-decrypt in Tools/.", ButtonEnum.Ok, MsgBoxIcon.Warning);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        LogTextBox.Text = $"Decrypting {file} to {outDir}...\r\n";
        DecryptPSVMDButton.IsEnabled = false;

        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = $"-k {key} -i \"{file}\" \"{outDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc is not null)
                {
                    proc.WaitForExit();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LogTextBox.Text += $"Decryption completed with exit code {proc.ExitCode}.\r\n";
                        DecryptPSVMDButton.IsEnabled = true;
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LogTextBox.Text += $"Error: {ex.Message}\r\n";
                    DecryptPSVMDButton.IsEnabled = true;
                });
            }
        });
    }

    private async void BrowseMetadataFileButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select decrypted metadata file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "PSVMD files", Extensions = { "psvmd" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            SelectedMetadataFileTextBox.Text = selected[0];
        }
    }

    private async void BrowseCreateInputFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select input folder" };
        var selected = await fbd.ShowAsync(this);
        if (!string.IsNullOrEmpty(selected))
        {
            CreateInputFolderTextBox.Text = selected;
        }
    }

    private async void BrowseCreateOutputFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select output folder" };
        var selected = await fbd.ShowAsync(this);
        if (!string.IsNullOrEmpty(selected))
        {
            CreateOutputFolderTextBox.Text = selected;
        }
    }

    private async void CreatePSVIMGButton_Click(object? sender, RoutedEventArgs e)
    {
        string key = BackupKeyTextBox.Text?.Trim() ?? "";
        string inDir = CreateInputFolderTextBox.Text?.Trim() ?? "";
        string outDir = CreateOutputFolderTextBox.Text?.Trim() ?? "";
        string metaFile = SelectedMetadataFileTextBox.Text?.Trim() ?? "";
        string titleName = SelectedNameTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(key))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please provide a valid CMA backup key.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (string.IsNullOrEmpty(inDir) || !Directory.Exists(inDir))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a valid input folder.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (string.IsNullOrEmpty(outDir))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select an output folder.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        string? toolPath = FindPsvimgTool("psvimg-create");
        if (toolPath is null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Missing Tool", "psvimg-create binary was not found in the Tools directory.\nPlease download psvimgtools and place psvimg-create in Tools/.", ButtonEnum.Ok, MsgBoxIcon.Warning);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        string args = $"-k {key}";
        if (!string.IsNullOrEmpty(metaFile) && File.Exists(metaFile))
        {
            args += $" -m \"{metaFile}\"";
        }
        if (!string.IsNullOrEmpty(titleName))
        {
            args += $" -n \"{titleName}\"";
        }
        args += $" -i \"{inDir}\" \"{outDir}\"";

        LogTextBox.Text = $"Creating PSVIMG from {inDir}...\r\n";
        CreatePSVIMGButton.IsEnabled = false;

        await Task.Run(() =>
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc is not null)
                {
                    proc.WaitForExit();
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        LogTextBox.Text += $"Creation completed with exit code {proc.ExitCode}.\r\n";
                        CreatePSVIMGButton.IsEnabled = true;
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LogTextBox.Text += $"Error: {ex.Message}\r\n";
                    CreatePSVIMGButton.IsEnabled = true;
                });
            }
        });
    }
}
