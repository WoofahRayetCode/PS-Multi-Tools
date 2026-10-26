using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using PSMultiTools.Classes;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PSMultiTools.PSV.Tools;

public partial class PSVPFSTools : Window
{
    private Process? _pfsProcess;

    public PSVPFSTools()
    {
        InitializeComponent();

        BrowseFolderButton.Click += BrowseFolderButton_Click;
        BrowseOutputFolderButton.Click += BrowseOutputFolderButton_Click;
        GetzRIFKeyButton.Click += GetzRIFKeyButton_Click;
        ExtractButton.Click += ExtractButton_Click;
    }

    private async void BrowseFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select encrypted game/app folder" };
        var selectedPath = await fbd.ShowAsync(this);
        if (string.IsNullOrEmpty(selectedPath))
            return;

        SelectedFolderTextBox.Text = selectedPath;

        // Auto-check for work.bin in the selected folder or standard subdirectories
        string[] candidates = [
            Path.Combine(selectedPath, "work.bin"),
            Path.Combine(selectedPath, "sce_sys", "package", "work.bin"),
            Path.Combine(selectedPath, "sce_sys", "work.bin")
        ];

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                LogTextBox.Text += $"[+] Found work.bin at: {candidate}\r\n";
                TryExtractZrifFromWorkBin(candidate);
                break;
            }
        }
    }

    private async void BrowseOutputFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var fbd = new OpenFolderDialog { Title = "Select output folder" };
        var selectedPath = await fbd.ShowAsync(this);
        if (!string.IsNullOrEmpty(selectedPath))
        {
            OutputFolderTextBox.Text = selectedPath;
        }
    }

    private async void GetzRIFKeyButton_Click(object? sender, RoutedEventArgs e)
    {
        // If selected folder already has work.bin, read it
        string inputDir = SelectedFolderTextBox.Text?.Trim() ?? "";
        if (!string.IsNullOrEmpty(inputDir) && Directory.Exists(inputDir))
        {
            string[] candidates = [
                Path.Combine(inputDir, "work.bin"),
                Path.Combine(inputDir, "sce_sys", "package", "work.bin"),
                Path.Combine(inputDir, "sce_sys", "work.bin")
            ];

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    TryExtractZrifFromWorkBin(candidate);
                    return;
                }
            }
        }

        // Otherwise open file dialog for work.bin
        var ofd = new OpenFileDialog
        {
            Title = "Select work.bin license file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "License files", Extensions = { "bin", "rif" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selectedFiles = await ofd.ShowAsync(this);
        if (selectedFiles is not null && selectedFiles.Length > 0 && !string.IsNullOrEmpty(selectedFiles[0]))
        {
            TryExtractZrifFromWorkBin(selectedFiles[0]);
        }
    }

    private void TryExtractZrifFromWorkBin(string workBinPath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(workBinPath);
            if (bytes.Length >= 512)
            {
                // Sony RIF format: Klicensee is typically at offset 0x50 (16 bytes)
                byte[] klicensee = new byte[16];
                Array.Copy(bytes, 0x50, klicensee, 0, 16);
                string hexKey = BitConverter.ToString(klicensee).Replace("-", "");
                zRIFTextBox.Text = hexKey;
                LogTextBox.Text += $"[+] Extracted klicensee key: {hexKey}\r\n";
            }
            else
            {
                LogTextBox.Text += $"[*] work.bin file size ({bytes.Length} bytes) is unexpected for standard RIF.\r\n";
            }
        }
        catch (Exception ex)
        {
            LogTextBox.Text += $"[-] Error reading work.bin: {ex.Message}\r\n";
        }
    }

    private static string FindToolBinary(string toolName)
    {
        string exeName = OperatingSystem.IsWindows() ? $"{toolName}.exe" : toolName;
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

        return exeName;
    }

    private async void ExtractButton_Click(object? sender, RoutedEventArgs e)
    {
        string inputDir = SelectedFolderTextBox.Text?.Trim() ?? "";
        string outputDir = OutputFolderTextBox.Text?.Trim() ?? "";
        string zrif = zRIFTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(inputDir) || !Directory.Exists(inputDir))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a valid encrypted game/app folder.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (string.IsNullOrEmpty(outputDir))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select an output folder.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (!Directory.Exists(outputDir))
        {
            try { Directory.CreateDirectory(outputDir); } catch { }
        }

        string toolPath = FindToolBinary("psvpfsparser");
        LogTextBox.Text += $"[+] Starting PFS decryption...\r\n";
        LogTextBox.Text += $"[+] Tool: {toolPath}\r\n";
        LogTextBox.Text += $"[+] Input: {inputDir}\r\n";
        LogTextBox.Text += $"[+] Output: {outputDir}\r\n";

        ExtractButton.IsEnabled = false;

        string args;
        if (!string.IsNullOrEmpty(zrif))
        {
            args = $"-i \"{inputDir}\" -o \"{outputDir}\" -z {zrif} -f";
        }
        else
        {
            args = $"-i \"{inputDir}\" -o \"{outputDir}\" -f";
        }

        await Task.Run(() =>
        {
            try
            {
                _pfsProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = toolPath,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    },
                    EnableRaisingEvents = true
                };

                _pfsProcess.OutputDataReceived += (_, ev) =>
                {
                    if (!string.IsNullOrEmpty(ev.Data))
                    {
                        Dispatcher.UIThread.Invoke(() => LogTextBox.Text += $"{ev.Data}\r\n");
                    }
                };

                _pfsProcess.ErrorDataReceived += (_, ev) =>
                {
                    if (!string.IsNullOrEmpty(ev.Data))
                    {
                        Dispatcher.UIThread.Invoke(() => LogTextBox.Text += $"[ERR] {ev.Data}\r\n");
                    }
                };

                _pfsProcess.Start();
                _pfsProcess.BeginOutputReadLine();
                _pfsProcess.BeginErrorReadLine();
                _pfsProcess.WaitForExit();

                int exitCode = _pfsProcess.ExitCode;
                Dispatcher.UIThread.Invoke(() =>
                {
                    LogTextBox.Text += $"[✓] Decryption process exited with code {exitCode}.\r\n";
                    ExtractButton.IsEnabled = true;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    LogTextBox.Text += $"[-] Exception running psvpfsparser: {ex.Message}\r\n";
                    ExtractButton.IsEnabled = true;
                });
            }
        });
    }
}
