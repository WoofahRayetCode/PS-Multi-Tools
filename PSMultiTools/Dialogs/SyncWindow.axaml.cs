using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace PSMultiTools.Dialogs;

public partial class SyncWindow : Window
{
    public SyncWindow()
    {
        InitializeComponent();
    }

    public void UpdateProgress(int current, int total, string? status = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            LoadProgressBar.IsIndeterminate = false;
            LoadProgressBar.Minimum = 0;
            LoadProgressBar.Maximum = total > 0 ? total : 100;
            LoadProgressBar.Value = current;

            LoadStatusTextBlock.Text = status ?? $"Loading files {current} / {total}";
        });
    }

    public void SetIndeterminate(bool isIndeterminate, string? status = null)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            LoadProgressBar.IsIndeterminate = isIndeterminate;
            if (!string.IsNullOrEmpty(status))
            {
                LoadStatusTextBlock.Text = status;
            }
        });
    }
}
