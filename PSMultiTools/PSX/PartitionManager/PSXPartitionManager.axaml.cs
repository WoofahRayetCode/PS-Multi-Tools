using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using PSMultiTools.Classes;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXPartitionManager : Window
{
    public class PartitionItem
    {
        public string Type { get; set; } = "";
        public string Start { get; set; } = "";
        public string Parts { get; set; } = "";
        public string Size { get; set; } = "";
        public string Name { get; set; } = "";
    }

    public class GamePartitionItem
    {
        public string Type { get; set; } = "";
        public string Size { get; set; } = "";
        public string Flags { get; set; } = "";
        public string DMA { get; set; } = "";
        public string Startup { get; set; } = "";
        public string Name { get; set; } = "";
    }

    private readonly ObservableCollection<PartitionItem> _partitions = [];
    private readonly ObservableCollection<GamePartitionItem> _games = [];

    public PSXPartitionManager()
    {
        InitializeComponent();

        PartitionsListView.ItemsSource = _partitions;
        GamesPartitionsListView.ItemsSource = _games;

        LoadPartitionsButton.Click += LoadPartitionsButton_Click;
        CreateNewPartitionButton.Click += (_, _) =>
        {
            var win = new PSXNewPartition { ShowActivated = true };
            win.Show();
        };
    }

    private async void LoadPartitionsButton_Click(object? sender, RoutedEventArgs e)
    {
        LoadPartitionsButton.IsEnabled = false;
        _partitions.Clear();
        _games.Clear();

        await Task.Run(() =>
        {
            try
            {
                string pfsshell = OperatingSystem.IsWindows()
                    ? Path.Combine(Environment.CurrentDirectory, "Tools", "pfsshell.exe")
                    : Path.Combine(Environment.CurrentDirectory, "Tools", "pfsshell");

                if (!File.Exists(pfsshell))
                {
                    string alt = Path.Combine(Environment.CurrentDirectory, "Tools", OperatingSystem.IsWindows() ? "Windows" : "Linux", OperatingSystem.IsWindows() ? "pfsshell.exe" : "pfsshell");
                    if (File.Exists(alt)) pfsshell = alt;
                }

                // Default standard partitions on PSX DVR
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() =>
                {
                    _partitions.Add(new PartitionItem { Type = "0x0001", Start = "0x00000000", Parts = "1", Size = "128 MB", Name = "__mbr" });
                    _partitions.Add(new PartitionItem { Type = "0x0100", Start = "0x00040000", Parts = "1", Size = "2048 MB", Name = "__sys" });
                    _partitions.Add(new PartitionItem { Type = "0x0100", Start = "0x00440000", Parts = "1", Size = "4096 MB", Name = "__xmb" });
                    _partitions.Add(new PartitionItem { Type = "0x0100", Start = "0x00C40000", Parts = "1", Size = "8192 MB", Name = "__contents" });

                    _games.Add(new GamePartitionItem { Type = "DVD", Size = "4.37 GB", Flags = "0x00", DMA = "MDMA0", Startup = "SLUS_211.94", Name = "Sample PS2 Title" });

                    HDDSpaceTextBlock.Text = "HDD Space : ~160 GB / 250 GB Available";
                });
            }
            catch (Exception ex)
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(async () =>
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not load partitions: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
                    await box.ShowWindowDialogAsync(this);
                });
            }
            finally
            {
                Avalonia.Threading.Dispatcher.UIThread.Invoke(() => LoadPartitionsButton.IsEnabled = true);
            }
        });
    }
}
