using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSMultiTools.PS2.Tools;

public partial class PNACHConverter : Window
{
    private static readonly Regex PnachPatchRegex = new(
        @"^patch\s*=\s*\d+\s*,\s*EE\s*,\s*([0-9a-fA-F]{8})\s*,\s*\w+\s*,\s*([0-9a-fA-F]{8})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PNACHConverter()
    {
        InitializeComponent();

        BrowseButton.Click += BrowseButton_Click;
        ConvertButton.Click += ConvertButton_Click;
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select PCSX2 PNACH cheat file",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "PNACH files", Extensions = { "pnach" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            SelectedPNACHFileTextBox.Text = selected[0];
        }
    }

    private async void ConvertButton_Click(object? sender, RoutedEventArgs e)
    {
        string pnachPath = SelectedPNACHFileTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(pnachPath) || !File.Exists(pnachPath))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please select a valid PNACH file.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(pnachPath);
            var convertedLines = new List<string>();

            bool skipGameTitle = SkipGameTitleEntryCheckBox.IsChecked == true;
            bool skipBlankLines = SkipBlankLinesCheckBox.IsChecked == true;
            bool makeLower = MakeCheatsLowercaseCheckBox.IsChecked == true;
            bool makeUpper = !makeLower && MakeCheatsUppercaseCheckBox.IsChecked == true;

            int convertedCount = 0;

            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line))
                {
                    if (!skipBlankLines)
                        convertedLines.Add("");
                    continue;
                }

                if (line.StartsWith("gametitle=", StringComparison.OrdinalIgnoreCase))
                {
                    if (!skipGameTitle)
                    {
                        string title = line.Substring("gametitle=".Length).Trim();
                        convertedLines.Add($"// Title: {title}");
                    }
                    continue;
                }

                if (line.StartsWith("comment=", StringComparison.OrdinalIgnoreCase))
                {
                    string comment = line.Substring("comment=".Length).Trim();
                    convertedLines.Add($"// {comment}");
                    continue;
                }

                if (line.StartsWith("//") || line.StartsWith("#"))
                {
                    convertedLines.Add(line);
                    continue;
                }

                var match = PnachPatchRegex.Match(line);
                if (match.Success)
                {
                    string addr = match.Groups[1].Value;
                    string val = match.Groups[2].Value;

                    if (makeLower)
                    {
                        addr = addr.ToLowerInvariant();
                        val = val.ToLowerInvariant();
                    }
                    else if (makeUpper)
                    {
                        addr = addr.ToUpperInvariant();
                        val = val.ToUpperInvariant();
                    }

                    convertedLines.Add($"{addr} {val}");
                    convertedCount++;
                }
                else
                {
                    // Pass-through other lines
                    convertedLines.Add(line);
                }
            }

            string baseName = Path.GetFileNameWithoutExtension(pnachPath);
            var sfd = new SaveFileDialog
            {
                Title = "Save Converted Cheats",
                DefaultExtension = "raw",
                InitialFileName = $"{baseName}.raw",
                Filters = [
                    new FileDialogFilter { Name = "Raw cheat files (.raw)", Extensions = { "raw" } },
                    new FileDialogFilter { Name = "Cheat files (.cht)", Extensions = { "cht" } },
                    new FileDialogFilter { Name = "Text files (.txt)", Extensions = { "txt" } }
                ]
            };

            var savePath = await sfd.ShowAsync(this);
            if (string.IsNullOrEmpty(savePath))
                return;

            await File.WriteAllLinesAsync(savePath, convertedLines, Encoding.UTF8);

            var okBox = MessageBoxManager.GetMessageBoxStandard(
                "Conversion Complete",
                $"Successfully converted {convertedCount} cheat code(s) to:\n{savePath}",
                ButtonEnum.Ok,
                MsgBoxIcon.Success);
            await okBox.ShowWindowDialogAsync(this);
        }
        catch (Exception ex)
        {
            var errBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Conversion failed: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await errBox.ShowWindowDialogAsync(this);
        }
    }
}
