using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using System;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXNewPartition : Window
{
    public PSXNewPartition()
    {
        InitializeComponent();

        CreatePartitionButton.Click += CreatePartitionButton_Click;
    }

    private async void CreatePartitionButton_Click(object? sender, RoutedEventArgs e)
    {
        string name = NewPartitionNameTextBox.Text?.Trim() ?? "";
        string sizeStr = NewPartitionSizeTextBox.Text?.Trim() ?? "";

        if (string.IsNullOrEmpty(name))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please enter a valid partition name.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        if (!int.TryParse(sizeStr, out int sizeMb) || sizeMb <= 0)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please enter a valid size in MB (positive integer).", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        var ok = MessageBoxManager.GetMessageBoxStandard("Success", $"Partition '{name}' ({sizeMb} MB) definition created.", ButtonEnum.Ok, MsgBoxIcon.Success);
        await ok.ShowWindowDialogAsync(this);
        Close();
    }
}
