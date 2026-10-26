using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit.Highlighting;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXAdvancedEditor : Window
{
    private string? _currentFilePath;

    public PSXAdvancedEditor()
    {
        InitializeComponent();

        SelectedSyntaxComboBox.ItemsSource = new string[] { "None", "XML", "C#", "C++", "JavaScript", "HTML", "PHP", "Java" };
        SelectedSyntaxComboBox.SelectedIndex = 0;
        SelectedSyntaxComboBox.SelectionChanged += SelectedSyntaxComboBox_SelectionChanged;

        SelectedLanguageComboBox.ItemsSource = new string[] { "English", "Japanese", "Spanish", "French", "German", "Italian" };
        SelectedLanguageComboBox.SelectedIndex = 0;

        ShowLineNumbersCheckBox.IsCheckedChanged += (_, _) =>
        {
            CodeBox.ShowLineNumbers = ShowLineNumbersCheckBox.IsChecked == true;
        };

        OpenFileButton.Click += OpenFileButton_Click;
        SaveButton.Click += SaveButton_Click;
        SaveNewButton.Click += SaveNewButton_Click;
        TranslateButton.Click += TranslateButton_Click;
    }

    private void SelectedSyntaxComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        string? syntax = SelectedSyntaxComboBox.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(syntax) || syntax == "None")
        {
            CodeBox.SyntaxHighlighting = null;
            return;
        }

        try
        {
            CodeBox.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(syntax);
        }
        catch
        {
            CodeBox.SyntaxHighlighting = null;
        }
    }

    private async void OpenFileButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Open File",
            AllowMultiple = false,
            Filters = [
                new FileDialogFilter { Name = "All text files", Extensions = { "txt", "xml", "json", "ini", "cfg", "lua", "c", "cpp", "h" } },
                new FileDialogFilter { Name = "All files", Extensions = { "*" } }
            ]
        };

        var selected = await ofd.ShowAsync(this);
        if (selected is not null && selected.Length > 0 && !string.IsNullOrEmpty(selected[0]))
        {
            _currentFilePath = selected[0];
            try
            {
                CodeBox.Text = await File.ReadAllTextAsync(_currentFilePath);

                // Auto-detect syntax
                string ext = Path.GetExtension(_currentFilePath).ToLowerInvariant();
                string syntaxName = ext switch
                {
                    ".xml" or ".xaml" => "XML",
                    ".cs" => "C#",
                    ".cpp" or ".c" or ".h" => "C++",
                    ".js" or ".json" => "JavaScript",
                    ".html" => "HTML",
                    ".php" => "PHP",
                    ".java" => "Java",
                    _ => "None"
                };

                for (int i = 0; i < SelectedSyntaxComboBox.Items.Count; i++)
                {
                    if (SelectedSyntaxComboBox.Items[i]?.ToString() == syntaxName)
                    {
                        SelectedSyntaxComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not open file: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
                await box.ShowWindowDialogAsync(this);
            }
        }
    }

    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            SaveNewButton_Click(sender, e);
            return;
        }

        try
        {
            await File.WriteAllTextAsync(_currentFilePath, CodeBox.Text ?? "");
            var box = MessageBoxManager.GetMessageBoxStandard("Saved", "File saved successfully.", ButtonEnum.Ok, MsgBoxIcon.Success);
            await box.ShowWindowDialogAsync(this);
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not save file: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
        }
    }

    private async void SaveNewButton_Click(object? sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Save File As",
            InitialFileName = !string.IsNullOrEmpty(_currentFilePath) ? Path.GetFileName(_currentFilePath) : "untitled.txt"
        };

        var savePath = await sfd.ShowAsync(this);
        if (!string.IsNullOrEmpty(savePath))
        {
            _currentFilePath = savePath;
            await File.WriteAllTextAsync(_currentFilePath, CodeBox.Text ?? "");
            var box = MessageBoxManager.GetMessageBoxStandard("Saved", "File saved successfully.", ButtonEnum.Ok, MsgBoxIcon.Success);
            await box.ShowWindowDialogAsync(this);
        }
    }

    private async void TranslateButton_Click(object? sender, RoutedEventArgs e)
    {
        string? targetLang = SelectedLanguageComboBox.SelectedItem?.ToString();
        var box = MessageBoxManager.GetMessageBoxStandard("Translation", $"Translation feature targeted to: {targetLang}.\nConnect an offline translation model or API key in settings to enable real-time translation.", ButtonEnum.Ok, MsgBoxIcon.Info);
        await box.ShowWindowDialogAsync(this);
    }
}
