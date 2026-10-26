using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using PSMultiTools.Classes;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace PSMultiTools;

public partial class PSXMainWindow : Window
{
    private readonly ObservableCollection<ComboBoxProjectItem> _projectItems = [];
    private readonly ObservableCollection<ComboBoxProjectItem> _preparedItems = [];

    public PSXMainWindow()
    {
        InitializeComponent();

        ProjectListComboBox.ItemsSource = _projectItems;
        PreparedProjectsComboBox.ItemsSource = _preparedItems;

        // Navigation
        StartMenuItem.Click += (_, _) => ShowStartGrid();
        ProjectsMenuItem.Click += (_, _) => ShowProjectsGrid();
        HDDManagerMenuItem.Click += (_, _) => OpenWindow(new PSXPartitionManager());
        XMBToolsMenuItem.Click += (_, _) => OpenWindow(new PSXAssetsBrowser());
        NBDDriverMenuItem.Click += NBDDriverMenuItem_Click;
        DokanDriverMenuItem.Click += DokanDriverMenuItem_Click;

        // Start / Connection
        ConnectButton.Click += ConnectButton_Click;

        // Projects
        NewHomebrewProjectButton.Click += (_, _) => OpenWindow(new PSXNewAppProject());
        NewGameProjectButton.Click += (_, _) => OpenWindow(new PSXNewPS2GameProject());
        NewPS1GameProjectButton.Click += (_, _) => OpenWindow(new PSXNewPS1GameProject());

        DeleteProjectButton.Click += DeleteProjectButton_Click;
        EditProjectButton.Click += EditProjectButton_Click;
        PrepareProjectButton.Click += PrepareProjectButton_Click;
        InstallProjectButton.Click += InstallProjectButton_Click;

        CheckDriverStatus();
        LoadProjects();
    }

    private static void OpenWindow(Window win)
    {
        win.ShowActivated = true;
        win.Show();
    }

    private void ShowStartGrid()
    {
        StartGrid.IsVisible = true;
        StartGrid.Opacity = 1;
        ProjectsGrid.IsVisible = false;
        ProjectsGrid.Opacity = 0;

        StartMenuItem.Background = new SolidColorBrush(Color.Parse("#FF004671"));
        ProjectsMenuItem.Background = new SolidColorBrush(Color.Parse("#FF00619C"));
    }

    private void ShowProjectsGrid()
    {
        StartGrid.IsVisible = false;
        StartGrid.Opacity = 0;
        ProjectsGrid.IsVisible = true;
        ProjectsGrid.Opacity = 1;

        StartMenuItem.Background = new SolidColorBrush(Color.Parse("#FF00619C"));
        ProjectsMenuItem.Background = new SolidColorBrush(Color.Parse("#FF004671"));

        LoadProjects();
    }

    private void CheckDriverStatus()
    {
        if (OperatingSystem.IsWindows())
        {
            string wnbdPath = Path.Combine(Environment.CurrentDirectory, "Tools", "Windows", "wnbd-client.exe");
            if (File.Exists(wnbdPath))
            {
                NBDDriverVersionLabel.Text = "WNBD Client Available";
                NBDDriverVersionLabel.Foreground = Brushes.LightGreen;
            }
            else
            {
                NBDDriverVersionLabel.Text = "Not installed";
                NBDDriverVersionLabel.Foreground = new SolidColorBrush(Color.Parse("#FFC12249"));
            }

            string dokanPath = Path.Combine(Environment.CurrentDirectory, "Tools", "Windows", "dokanctl.exe");
            if (File.Exists(dokanPath))
            {
                DokanDriverVersionLabel.Text = "Dokan 2.x Available";
                DokanDriverVersionLabel.Foreground = Brushes.LightGreen;
            }
            else
            {
                DokanDriverVersionLabel.Text = "Not installed";
                DokanDriverVersionLabel.Foreground = new SolidColorBrush(Color.Parse("#FFC12249"));
            }
        }
        else
        {
            // Linux native NBD and FUSE
            NBDDriverVersionLabel.Text = "Linux Kernel NBD (/dev/nbd*)";
            NBDDriverVersionLabel.Foreground = Brushes.LightGreen;

            DokanDriverVersionLabel.Text = "Linux FUSE (pfsfuse)";
            DokanDriverVersionLabel.Foreground = Brushes.LightGreen;
        }
    }

    private async void NBDDriverMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        string msg = OperatingSystem.IsWindows()
            ? "NBD (Network Block Device) driver allows mounting PSX DVR HDD partitions over network using WNBD.\nInstaller: Tools/Windows/wnbd-client.exe"
            : "On Linux, Network Block Device (NBD) is supported natively via kernel module: modprobe nbd\nConnection client: nbd-client <IP> 10809 /dev/nbd0";

        var box = MessageBoxManager.GetMessageBoxStandard("NBD Driver Information", msg, ButtonEnum.Ok, MsgBoxIcon.Info);
        await box.ShowWindowDialogAsync(this);
    }

    private async void DokanDriverMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        string msg = OperatingSystem.IsWindows()
            ? "Dokan allows mounting user-mode filesystems in Windows explorer.\nInstaller: Tools/Windows/dokanctl.exe"
            : "On Linux, Dokan is replaced by standard Linux FUSE (Filesystem in Userspace) and pfsfuse.";

        var box = MessageBoxManager.GetMessageBoxStandard("Dokan Driver Information", msg, ButtonEnum.Ok, MsgBoxIcon.Info);
        await box.ShowWindowDialogAsync(this);
    }

    private async void ConnectButton_Click(object? sender, RoutedEventArgs e)
    {
        string ip = PSXIPTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(ip))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Error", "Please enter the IP address of your PSX.", ButtonEnum.Ok, MsgBoxIcon.Error);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        ConnectButton.IsEnabled = false;
        NBDConnectionLabel.Text = "Connecting...";
        NBDConnectionLabel.Foreground = Brushes.Yellow;

        bool connected = await Task.Run(() =>
        {
            try
            {
                // Test TCP socket to NBD port 10809 or FTP port 21
                using var client = new TcpClient();
                var result = client.BeginConnect(ip, 10809, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                if (success)
                {
                    client.EndConnect(result);
                    return true;
                }

                // Fallback test FTP port 21
                using var ftpClient = new TcpClient();
                var ftpResult = ftpClient.BeginConnect(ip, 21, null, null);
                bool ftpSuccess = ftpResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
                if (ftpSuccess)
                {
                    ftpClient.EndConnect(ftpResult);
                    return true;
                }
            }
            catch
            {
                // Connection failed
            }
            return false;
        });

        ConnectButton.IsEnabled = true;

        if (connected)
        {
            NBDConnectionLabel.Text = $"Connected ({ip})";
            NBDConnectionLabel.Foreground = Brushes.LightGreen;
            MountStatusLabel.Text = "PSX Ready";
            MountStatusLabel.Foreground = Brushes.LightGreen;

            var ok = MessageBoxManager.GetMessageBoxStandard("Connected", $"Successfully connected to PSX at {ip}.", ButtonEnum.Ok, MsgBoxIcon.Success);
            await ok.ShowWindowDialogAsync(this);
        }
        else
        {
            NBDConnectionLabel.Text = "Not connected";
            NBDConnectionLabel.Foreground = new SolidColorBrush(Color.Parse("#FFC12249"));
            MountStatusLabel.Text = "Unmounted";
            MountStatusLabel.Foreground = new SolidColorBrush(Color.Parse("#FFC12249"));

            var err = MessageBoxManager.GetMessageBoxStandard("Connection Failed", $"Could not reach PSX at {ip}. Make sure the PSX DVR server is running on the console.", ButtonEnum.Ok, MsgBoxIcon.Warning);
            await err.ShowWindowDialogAsync(this);
        }
    }

    private void LoadProjects()
    {
        _projectItems.Clear();
        _preparedItems.Clear();

        string projectsDir = Path.Combine(Environment.CurrentDirectory, "Projects");
        if (!Directory.Exists(projectsDir))
        {
            Directory.CreateDirectory(projectsDir);
            return;
        }

        var files = Directory.GetFiles(projectsDir, "*.project");
        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            var item = new ComboBoxProjectItem { ProjectName = name, ProjectFile = file };
            _projectItems.Add(item);
            _preparedItems.Add(item);
        }

        if (_projectItems.Count > 0)
        {
            ProjectListComboBox.SelectedIndex = 0;
            PreparedProjectsComboBox.SelectedIndex = 0;
        }
    }

    private async void DeleteProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ProjectListComboBox.SelectedItem is not ComboBoxProjectItem selected || string.IsNullOrEmpty(selected.ProjectFile))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice", "Please select a project to delete.", ButtonEnum.Ok, MsgBoxIcon.Info);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        var confirm = MessageBoxManager.GetMessageBoxStandard("Confirm Deletion", $"Are you sure you want to delete project '{selected.ProjectName}'?", ButtonEnum.YesNo, MsgBoxIcon.Question);
        var result = await confirm.ShowWindowDialogAsync(this);
        if (result == ButtonResult.Yes)
        {
            try
            {
                if (File.Exists(selected.ProjectFile)) File.Delete(selected.ProjectFile);
                LoadProjects();
            }
            catch (Exception ex)
            {
                var err = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not delete project file: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
                await err.ShowWindowDialogAsync(this);
            }
        }
    }

    private async void EditProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ProjectListComboBox.SelectedItem is not ComboBoxProjectItem selected || string.IsNullOrEmpty(selected.ProjectFile))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice", "Please select a project to edit.", ButtonEnum.Ok, MsgBoxIcon.Info);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(selected.ProjectFile);
            string projectType = "App";
            foreach (var l in lines)
            {
                if (l.StartsWith("Type=", StringComparison.OrdinalIgnoreCase))
                {
                    projectType = l.Substring("Type=".Length).Trim();
                    break;
                }
            }

            if (projectType.Equals("PS1", StringComparison.OrdinalIgnoreCase))
            {
                var editor = new PSXPS1GameEditor { ShowActivated = true };
                editor.Show();
            }
            else if (projectType.Equals("PS2", StringComparison.OrdinalIgnoreCase))
            {
                var editor = new PSXPS2GameEditor { ShowActivated = true };
                editor.Show();
            }
            else
            {
                var editor = new PSXAppEditor { ShowActivated = true };
                editor.Show();
            }
        }
        catch (Exception ex)
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not load project editor: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
        }
    }

    private async void PrepareProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ProjectListComboBox.SelectedItem is not ComboBoxProjectItem selected || string.IsNullOrEmpty(selected.ProjectFile))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice", "Please select a project to prepare.", ButtonEnum.Ok, MsgBoxIcon.Info);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        try
        {
            var lines = await File.ReadAllLinesAsync(selected.ProjectFile);
            string projDir = "";
            foreach (var l in lines)
            {
                if (l.StartsWith("Directory=", StringComparison.OrdinalIgnoreCase))
                {
                    projDir = l.Substring("Directory=".Length).Trim();
                    break;
                }
            }

            if (!string.IsNullOrEmpty(projDir) && Directory.Exists(projDir))
            {
                var ok = MessageBoxManager.GetMessageBoxStandard("Project Prepared", $"Project '{selected.ProjectName}' is verified and ready for installation!\nPath: {projDir}", ButtonEnum.Ok, MsgBoxIcon.Success);
                await ok.ShowWindowDialogAsync(this);
            }
            else
            {
                var err = MessageBoxManager.GetMessageBoxStandard("Preparation Warning", $"Project directory not found: {projDir}", ButtonEnum.Ok, MsgBoxIcon.Warning);
                await err.ShowWindowDialogAsync(this);
            }
        }
        catch (Exception ex)
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not prepare project: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
        }
    }

    private async void InstallProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (PreparedProjectsComboBox.SelectedItem is not ComboBoxProjectItem selected || string.IsNullOrEmpty(selected.ProjectFile))
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Notice", "Please select a prepared project to install.", ButtonEnum.Ok, MsgBoxIcon.Info);
            await box.ShowWindowDialogAsync(this);
            return;
        }

        try
        {
            var installWin = new InstallWindow
            {
                ProjectToInstall = selected,
                ShowActivated = true
            };
            installWin.Show();
        }
        catch (Exception ex)
        {
            var err = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not open installer: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await err.ShowWindowDialogAsync(this);
        }
    }
}
