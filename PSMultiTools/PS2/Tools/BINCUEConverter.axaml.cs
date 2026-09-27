using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using PSMultiTools.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSMultiTools.PS1.Tools;

public partial class BINCUEConverter : Window
{
    public bool ConvertForPS1 = false;
    private string BINFile = "";

    public BINCUEConverter()
    {
        InitializeComponent();
        Loaded += BINCUEConverter_Loaded;
    }

    private void BINCUEConverter_Loaded(object? sender, RoutedEventArgs e)
    {
        if (ConvertForPS1)
        {
            IsForPSXCheckBox.IsChecked = true;
        }
    }

    private async void BrowseCueButton_Click(object? sender, RoutedEventArgs e)
    {
        var cueFileFilter = new FileDialogFilter { Name = "CUE File", Extensions = ["cue"] };
        var dialog = new OpenFileDialog { Filters = { cueFileFilter }, AllowMultiple = true };
        var selectedFiles = await dialog.ShowAsync(this);

        if (selectedFiles is null)
        {
            return;
        }

        foreach (var cueFile in selectedFiles.Where(File.Exists))
        {
            AddCueFile(cueFile);
        }
    }

    private async void BrowseCueFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select a folder containing CUE files" };
        var selectedFolder = await dialog.ShowAsync(this);
        if (string.IsNullOrWhiteSpace(selectedFolder))
        {
            return;
        }

        foreach (var cueFile in Directory.EnumerateFiles(selectedFolder, "*.cue", SearchOption.AllDirectories)
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            AddCueFile(cueFile);
        }
    }

    private void AddCueFile(string cueFile)
    {
        if (!CueFilesListBox.Items.Cast<object>().Any(item =>
                string.Equals(item as string, cueFile, StringComparison.OrdinalIgnoreCase)))
        {
            CueFilesListBox.Items.Add(cueFile);
        }
    }

    private void RemoveCueButton_Click(object? sender, RoutedEventArgs e)
    {
        if (CueFilesListBox.SelectedItem is string selectedCue)
        {
            CueFilesListBox.Items.Remove(selectedCue);
        }
    }

    private async void BrowseBinButton_Click(object? sender, RoutedEventArgs e)
    {
        var binFileFilter = new FileDialogFilter { Name = "BIN File", Extensions = ["bin"] };
        var dialog = new OpenFileDialog { Filters = { binFileFilter }, AllowMultiple = false };
        var selectedFiles = await dialog.ShowAsync(this);

        if (selectedFiles is not null && selectedFiles.Length > 0)
        {
            SelectedBinTextBox.Text = selectedFiles[0];
            BINFile = selectedFiles[0];
        }
    }

    private async void ConvertButton_Click(object? sender, RoutedEventArgs e)
    {
        var cueFiles = CueFilesListBox.Items.Cast<object>()
            .OfType<string>()
            .Where(File.Exists)
            .ToList();

        if (cueFiles.Count == 0)
        {
            return;
        }

        ConvertButton.IsEnabled = false;
        BrowseCueButton.IsEnabled = false;
        BrowseCueFolderButton.IsEnabled = false;
        BrowseBinButton.IsEnabled = false;
        ClearExistingIsoCheckBox.IsEnabled = false;
        LogTextBox.Clear();

        var outputDirectory = Path.Combine(Environment.CurrentDirectory, "Converted", "ISO");
        Directory.CreateDirectory(outputDirectory);
        var converted = 0;
        var skipped = 0;

        try
        {
            if (ClearExistingIsoCheckBox.IsChecked == true)
            {
                var existingIsoFiles = Directory.GetFiles(outputDirectory, "*.iso");
                if (existingIsoFiles.Length > 0)
                {
                    var wipeWarning = MessageBoxManager.GetMessageBoxStandard(
                        "Start fresh",
                        $"This will permanently delete {existingIsoFiles.Length} existing ISO file(s) from the output folder. Continue?",
                        ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning);
                    if (await wipeWarning.ShowWindowDialogAsync(this) != ButtonResult.Yes)
                    {
                        return;
                    }

                    foreach (var isoFile in existingIsoFiles)
                    {
                        File.Delete(isoFile);
                    }

                    AppendLog($"Deleted {existingIsoFiles.Length} existing ISO file(s).");
                }
            }

            foreach (var cueFile in cueFiles)
            {
                var removedDuplicates = RemoveDuplicateOutputs(outputDirectory, Path.GetFileNameWithoutExtension(cueFile));
                if (removedDuplicates > 0)
                {
                    AppendLog($"[{Path.GetFileName(cueFile)}] Removed {removedDuplicates} duplicate ISO file(s).");
                }
            }

            var existingConversions = cueFiles
                .Select(cueFile => new
                {
                    CueFile = cueFile,
                    ExistingFiles = GetExistingOutputFiles(outputDirectory, Path.GetFileNameWithoutExtension(cueFile))
                })
                .Where(item => item.ExistingFiles.Count > 0)
                .ToList();

            if (existingConversions.Count > 0)
            {
                var warning = MessageBoxManager.GetMessageBoxStandard(
                    "Existing conversions found",
                    $"{existingConversions.Count} of {cueFiles.Count} CUE file(s) already have ISO output. They will be skipped. Continue with the remaining conversions?",
                    ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Warning);
                if (await warning.ShowWindowDialogAsync(this) != ButtonResult.Yes)
                {
                    return;
                }
            }

            var queuedBaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cueFile in cueFiles)
            {
                var binFile = FindBinFile(cueFile, cueFiles.Count == 1 ? SelectedBinTextBox.Text ?? BINFile : "");
                if (binFile is null)
                {
                    AppendLog($"[{Path.GetFileName(cueFile)}] Could not find the BIN referenced by the CUE file. Skipped.");
                    continue;
                }

                var requestedName = Path.GetFileNameWithoutExtension(cueFile);
                var baseName = CleanIsoName(requestedName);
                if (GetExistingOutputFiles(outputDirectory, requestedName).Count > 0 ||
                    !queuedBaseNames.Add(baseName))
                {
                    skipped++;
                    AppendLog($"[{Path.GetFileName(cueFile)}] Existing or duplicate ISO output found. Skipped.");
                    continue;
                }

                AppendLog($"[{Path.GetFileName(cueFile)}] Converting...");
                var exitCode = await ConvertCueAsync(cueFile, binFile, outputDirectory, baseName);
                if (exitCode == 0)
                {
                    converted++;
                    AppendLog($"[{Path.GetFileName(cueFile)}] Conversion complete.");
                }
                else
                {
                    AppendLog($"[{Path.GetFileName(cueFile)}] Conversion failed with exit code {exitCode}.");
                }
            }

            var box = MessageBoxManager.GetMessageBoxStandard(
                "Completed", $"Converted {converted} of {cueFiles.Count} CUE file(s); skipped {skipped}. Open the output folder?",
                ButtonEnum.YesNo, MsBox.Avalonia.Enums.Icon.Question);
            if (await box.ShowWindowDialogAsync(this) == ButtonResult.Yes)
            {
                Utils.OpenFolder(outputDirectory);
            }
        }
        finally
        {
            ConvertButton.IsEnabled = true;
            BrowseCueButton.IsEnabled = true;
            BrowseCueFolderButton.IsEnabled = true;
            BrowseBinButton.IsEnabled = true;
            ClearExistingIsoCheckBox.IsEnabled = true;
        }
    }

    private async Task<int> ConvertCueAsync(string cueFile, string binFile, string outputDirectory, string baseName)
    {
        using var process = new Process();
        process.StartInfo.FileName = OperatingSystem.IsWindows()
            ? Path.Combine(Environment.CurrentDirectory, "Tools", "bchunk.exe")
            : Path.Combine(Environment.CurrentDirectory, "Tools", "bchunk");
        process.StartInfo.WorkingDirectory = outputDirectory;
        process.StartInfo.Arguments = IsForPSXCheckBox.IsChecked == true
            ? $"-p \"{binFile}\" \"{cueFile}\" \"{baseName}\""
            : $"\"{binFile}\" \"{cueFile}\" \"{baseName}\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        AppendLog((await outputTask).TrimEnd());
        AppendLog((await errorTask).TrimEnd());
        NormalizeSingleTrackIsoName(outputDirectory, baseName);
        MoveGeneratedCdrFiles(outputDirectory, baseName);
        return process.ExitCode;
    }

    private static void NormalizeSingleTrackIsoName(string outputDirectory, string baseName)
    {
        var trackOneName = baseName + "01.iso";
        var trackOnePath = Path.Combine(outputDirectory, trackOneName);
        var otherTracks = Directory.GetFiles(outputDirectory, "*.iso")
            .Where(file => Path.GetFileNameWithoutExtension(file).StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
            .Where(file => !string.Equals(file, trackOnePath, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var cleanPath = Path.Combine(outputDirectory, baseName + ".iso");

        if (File.Exists(trackOnePath) && otherTracks.Count == 0 && !File.Exists(cleanPath))
        {
            File.Move(trackOnePath, cleanPath);
        }
    }

    private void MoveGeneratedCdrFiles(string outputDirectory, string baseName)
    {
        var cdrDirectory = Path.Combine(Environment.CurrentDirectory, "Converted", "BIN", "CDR");
        var cdrFiles = Directory.GetFiles(outputDirectory, $"{baseName}*.cdr");
        if (cdrFiles.Length == 0)
        {
            return;
        }

        Directory.CreateDirectory(cdrDirectory);
        foreach (var cdrFile in cdrFiles)
        {
            File.Move(cdrFile, Path.Combine(cdrDirectory, Path.GetFileName(cdrFile)), true);
        }
    }

    private string? FindBinFile(string cueFile, string preferredBinFile)
    {
        if (!string.IsNullOrWhiteSpace(preferredBinFile) && File.Exists(preferredBinFile))
        {
            return preferredBinFile;
        }

        var cueDirectory = Path.GetDirectoryName(cueFile) ?? Environment.CurrentDirectory;
        var fileLinePattern = new Regex(@"^\s*FILE\s+(?:""(?<quoted>[^""]+)""|(?<unquoted>\S+))", RegexOptions.IgnoreCase);
        foreach (var line in File.ReadLines(cueFile))
        {
            var match = fileLinePattern.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var referencedFile = match.Groups["quoted"].Success ? match.Groups["quoted"].Value : match.Groups["unquoted"].Value;
            var candidate = Path.GetFullPath(Path.Combine(cueDirectory, referencedFile));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var defaultBin = Path.Combine(cueDirectory, Path.GetFileNameWithoutExtension(cueFile) + ".bin");
        return File.Exists(defaultBin) ? defaultBin : null;
    }

    private static List<string> GetExistingOutputFiles(string outputDirectory, string requestedName)
    {
        var baseNames = new[] { requestedName, CleanIsoName(requestedName) }
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return Directory.GetFiles(outputDirectory, "*.iso")
            .Where(file => baseNames.Any(baseName => IsOutputForBaseName(file, baseName)))
            .ToList();
    }

    private static int RemoveDuplicateOutputs(string outputDirectory, string requestedName)
    {
        var files = GetExistingOutputFiles(outputDirectory, requestedName);
        var baseNames = new[] { requestedName, CleanIsoName(requestedName) }
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var removed = 0;

        foreach (var baseName in baseNames)
        {
            var canonicalFiles = files
                .Where(file => GetTrackSuffix(file, baseName) is not null && !IsDuplicateOutput(file, baseName))
                .ToList();

            foreach (var duplicateFile in files.Where(file => IsDuplicateOutput(file, baseName)).ToList())
            {
                var duplicateTrack = GetTrackSuffix(duplicateFile, baseName);
                if (duplicateTrack is not null && canonicalFiles.Any(file => GetTrackSuffix(file, baseName) == duplicateTrack))
                {
                    File.Delete(duplicateFile);
                    removed++;
                    files.Remove(duplicateFile);
                }
            }
        }

        return removed;
    }

    private static bool IsDuplicateOutput(string filePath, string baseName)
    {
        var stem = Path.GetFileNameWithoutExtension(filePath);
        return Regex.IsMatch(stem, $"^{Regex.Escape(baseName)} \\(\\d+\\)\\d+$", RegexOptions.IgnoreCase);
    }

    private static string? GetTrackSuffix(string filePath, string baseName)
    {
        var stem = Path.GetFileNameWithoutExtension(filePath);
        if (string.Equals(stem, baseName, StringComparison.OrdinalIgnoreCase))
        {
            return "01";
        }

        if (!stem.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var suffix = stem[baseName.Length..];
        var match = Regex.Match(suffix, @"^(?: \(\d+\))?(\d+)$");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static bool IsOutputForBaseName(string filePath, string baseName)
    {
        var stem = Path.GetFileNameWithoutExtension(filePath);
        if (!stem.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var suffix = stem[baseName.Length..];
        return Regex.IsMatch(suffix, @"^( \(\d+\))?\d+$");
    }

    private static string CleanIsoName(string name)
    {
        name = Regex.Replace(name, @"[\s_-]*merged$", "", RegexOptions.IgnoreCase);
        name = name.Replace('_', ' ');
        name = Regex.Replace(name, @"[<>:""/\\|?*]", "");
        name = Regex.Replace(name, @"\s{2,}", " ").Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(name) ? "Converted" : name;
    }

    private void AppendLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            LogTextBox.Text += message + Environment.NewLine;
            LogTextBox.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault()?.ScrollToEnd();
        });
    }
}
