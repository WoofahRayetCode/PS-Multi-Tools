using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PSMultiTools.PS2.Tools;

public partial class DNASPatcher : Window
{
    public DNASPatcher()
    {
        InitializeComponent();

        BrowseButton.Click += BrowseButton_Click;
        PatchButton.Click += PatchButton_Click;

        if (ModeComboBox.Items.Count > 0 && ModeComboBox.SelectedIndex < 0)
        {
            ModeComboBox.SelectedIndex = 0;
        }
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select PS2 ISO file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "ISO files", Extensions = { "iso", "bin" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            SelectedFolderTextBox.Text = selected[0];
        }
    }

    private static string? FindDnasPatcherBinary()
    {
        string[] searchPaths = [
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "Windows", "DNAS_PATCHER21.EXE"),
            Path.Combine(Environment.CurrentDirectory, "Tools", "Windows", "DNAS_PATCHER21.EXE"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "DNAS_PATCHER21.EXE"),
            Path.Combine(Environment.CurrentDirectory, "Tools", "DNAS_PATCHER21.EXE"),
        ];

        foreach (var path in searchPaths)
        {
            if (File.Exists(path)) return path;
        }

        return null;
    }

    private async void PatchButton_Click(object? sender, RoutedEventArgs e)
    {
        string isoPath = SelectedFolderTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(isoPath) || !File.Exists(isoPath))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a valid PS2 ISO file.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        int mode = ModeComboBox.SelectedIndex + 1;
        if (mode < 1 || mode > 5) mode = 1;

        string? patcherPath = FindDnasPatcherBinary();
        if (patcherPath is null)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "DNAS_PATCHER21.EXE was not found in Tools/Windows/.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        PatchButton.IsEnabled = false;

        await Task.Run(async () =>
        {
            try
            {
                ProcessStartInfo psi;
                if (OperatingSystem.IsWindows())
                {
                    psi = new ProcessStartInfo
                    {
                        FileName = patcherPath,
                        Arguments = $"\"{isoPath}\" {mode}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }
                else
                {
                    // Run with Wine on Linux/macOS
                    psi = new ProcessStartInfo
                    {
                        FileName = "wine",
                        Arguments = $"\"{patcherPath}\" \"{isoPath}\" {mode}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                }

                using var proc = Process.Start(psi);
                string output = "";
                if (proc is not null)
                {
                    output = await proc.StandardOutput.ReadToEndAsync();
                    string error = await proc.StandardError.ReadToEndAsync();
                    proc.WaitForExit();

                    string summary = string.IsNullOrWhiteSpace(output) ? error : output;
                    var resBox = MessageBoxManager.GetMessageBoxStandard("DNAS Patcher Result", $"Patching completed (exit code {proc.ExitCode}).\n{summary}", ButtonEnum.Ok, MsgBoxIcon.Info);
                    await resBox.ShowWindowDialogAsync(this);
                }
            }
            catch (Exception ex)
            {
                var errBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not run DNAS patcher: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
                await errBox.ShowWindowDialogAsync(this);
            }
            finally
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() => PatchButton.IsEnabled = true);
            }
        });
    }
}
