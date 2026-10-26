using Avalonia.Controls;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsgBoxIcon = MsBox.Avalonia.Enums.Icon;
using PSMultiTools.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSMultiTools.MultiPlatformTools;

public partial class SFOEditor : Window
{
    private readonly Dictionary<string, (ushort Format, object Value)> _loadedEntries = new(StringComparer.Ordinal);
    private string? _currentFilePath;

    public SFOEditor()
    {
        InitializeComponent();

        LoadSFOButton.Click += LoadSFOButton_Click;
        SaveSFOButton.Click += SaveSFOButton_Click;
        NewSFOButton.Click += NewSFOButton_Click;

        PS3ParamHelpButton.Click += (_, _) => OpenWikiUrl("https://www.psdevwiki.com/ps3/PARAM.SFO");
        PS4ParamHelpButton.Click += (_, _) => OpenWikiUrl("https://www.psdevwiki.com/ps4/Param.sfo");
        PSVParamHelpButton.Click += (_, _) => OpenWikiUrl("https://www.psdevwiki.com/vita/PARAM.SFO");
        PSPParamHelpButton.Click += (_, _) => OpenWikiUrl("https://www.psdevwiki.com/psp/PARAM.SFO");
    }

    private static void OpenWikiUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open wiki URL: {ex.Message}");
        }
    }

    private async void LoadSFOButton_Click(object? sender, RoutedEventArgs e)
    {
        var ofd = new OpenFileDialog
        {
            Title = "Select a PARAM.SFO file",
            AllowMultiple = false,
            Filters = [new FileDialogFilter { Name = "SFO files", Extensions = { "sfo", "SFO" } }]
        };

        var selectedFiles = await ofd.ShowAsync(this);
        if (selectedFiles is null || selectedFiles.Length == 0 || string.IsNullOrEmpty(selectedFiles[0]))
            return;

        _currentFilePath = selectedFiles[0];
        try
        {
            byte[] fileBytes = await File.ReadAllBytesAsync(_currentFilePath);
            ParseSfoBytes(fileBytes);
            PopulateUiFromEntries();
        }
        catch (Exception ex)
        {
            var errBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not load PARAM.SFO: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await errBox.ShowWindowDialogAsync(this);
        }
    }

    private void ParseSfoBytes(byte[] data)
    {
        _loadedEntries.Clear();

        if (data.Length < 20)
            throw new InvalidDataException("File is too small to be a valid SFO.");

        // PSF magic: 0x00, 'P', 'S', 'F'
        if (data[0] != 0x00 || data[1] != 0x50 || data[2] != 0x53 || data[3] != 0x46)
            throw new InvalidDataException("Invalid SFO magic header (expected \\0PSF).");

        uint keyTableStart = BitConverter.ToUInt32(data, 8);
        uint dataTableStart = BitConverter.ToUInt32(data, 12);
        uint entryCount = BitConverter.ToUInt32(data, 16);

        int entryHeaderOffset = 20;
        for (int i = 0; i < entryCount; i++)
        {
            int offset = entryHeaderOffset + (i * 16);
            if (offset + 16 > data.Length)
                break;

            ushort keyOffset = BitConverter.ToUInt16(data, offset);
            ushort paramFmt = BitConverter.ToUInt16(data, offset + 2);
            uint paramLen = BitConverter.ToUInt32(data, offset + 4);
            uint dataOffset = BitConverter.ToUInt32(data, offset + 12);

            int fullKeyOffset = (int)(keyTableStart + keyOffset);
            int keyEnd = fullKeyOffset;
            while (keyEnd < data.Length && data[keyEnd] != 0)
                keyEnd++;

            string key = Encoding.UTF8.GetString(data, fullKeyOffset, keyEnd - fullKeyOffset);
            int fullDataOffset = (int)(dataTableStart + dataOffset);

            if (paramFmt == 0x0404) // uint32
            {
                uint intVal = fullDataOffset + 4 <= data.Length ? BitConverter.ToUInt32(data, fullDataOffset) : 0;
                _loadedEntries[key] = (paramFmt, intVal);
            }
            else // utf8 string (0x0204) or utf8 special (0x0004)
            {
                int len = (int)paramLen;
                if (fullDataOffset + len > data.Length)
                    len = Math.Max(0, data.Length - fullDataOffset);

                // Strip trailing null bytes
                while (len > 0 && data[fullDataOffset + len - 1] == 0)
                    len--;

                string strVal = Encoding.UTF8.GetString(data, fullDataOffset, len);
                _loadedEntries[key] = (paramFmt, strVal);
            }
        }
    }

    private void PopulateUiFromEntries()
    {
        string GetString(string key, string fallback = "") =>
            _loadedEntries.TryGetValue(key, out var entry) ? entry.Value?.ToString() ?? fallback : fallback;

        uint GetUInt(string key, uint fallback = 0)
        {
            if (_loadedEntries.TryGetValue(key, out var entry))
            {
                if (entry.Value is uint u) return u;
                if (entry.Value is int i) return (uint)i;
                if (uint.TryParse(entry.Value?.ToString(), out var parsed)) return parsed;
            }
            return fallback;
        }

        IDTextBox.Text = GetString("TITLE_ID");
        TitleTextBox.Text = GetString("TITLE");
        VerTextBox.Text = GetString("VERSION", "01.00");
        AppVerTextBox.Text = GetString("APP_VER", "01.00");
        CategoryTextBox.Text = GetString("CATEGORY", "gd");
        ContentIDTextBox.Text = GetString("CONTENT_ID");

        uint parental = GetUInt("PARENTAL_LEVEL");
        ParentalComboBox.SelectedIndex = (int)Math.Clamp(parental, 0, 11);

        string sysVer = GetString("SYSTEM_VER");
        if (string.IsNullOrEmpty(sysVer))
            sysVer = GetString("PS3_SYSTEM_VER");
        SystemVersionTextBox.Text = sysVer;

        // PS3 Parameters
        uint bootable = GetUInt("BOOTABLE", 1);
        PS3BootableCheckBox.IsChecked = bootable == 1;

        uint ps3Attr = GetUInt("ATTRIBUTE");
        PS3BackgroundMusicCheckBox.IsChecked = (ps3Attr & 0x02) != 0;
        PS3MoveSupportCheckBox.IsChecked = (ps3Attr & 0x04) != 0;

        // PS4 Parameters
        uint appType = GetUInt("APP_TYPE");
        PS4AppTypeComboBox.SelectedIndex = (int)Math.Clamp(appType, 0, 4);
        PS4PubToolInfoTextBox.Text = GetString("PUBTOOLINFO");
        PS4PubToolVersionTextBox.Text = GetString("PUBTOOLVER");

        uint ps4Attr = GetUInt("ATTRIBUTE");
        PS4AppSupportsVRCheckBox.IsChecked = (ps4Attr & 0x10) != 0;
        PS4AppSupportsHDRCheckBox.IsChecked = (ps4Attr & 0x20) != 0;
        PS4SupportsNEOModeCheckBox.IsChecked = (ps4Attr & 0x40) != 0;

        // PSP Parameters
        PSPBootableCheckBox.IsChecked = bootable == 1;

        // PSV Parameters
        AppShotTitleTextBox.Text = GetString("SHORT_TITLE");
        ShareAppTitleTextBox.Text = GetString("SHARE_TITLE_ID");
        VitaNPComIDTextBox.Text = GetString("COMM_ID");
        uint quota = GetUInt("SAVEDATA_MAX_SIZE");
        VitaSaveDataQuotaTextBox.Text = quota > 0 ? quota.ToString() : "";
    }

    private async void SaveSFOButton_Click(object? sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            Title = "Save PARAM.SFO",
            DefaultExtension = "sfo",
            InitialFileName = "PARAM.SFO",
            Filters = [new FileDialogFilter { Name = "SFO files", Extensions = { "sfo", "SFO" } }]
        };

        var savePath = await sfd.ShowAsync(this);
        if (string.IsNullOrEmpty(savePath))
            return;

        try
        {
            // Collect UI fields into entries
            void SetStr(string key, string? val, ushort fmt = 0x0204)
            {
                if (!string.IsNullOrEmpty(val))
                    _loadedEntries[key] = (fmt, val);
                else
                    _loadedEntries.Remove(key);
            }

            void SetUInt(string key, uint val)
            {
                _loadedEntries[key] = (0x0404, val);
            }

            SetStr("TITLE_ID", IDTextBox.Text?.Trim());
            SetStr("TITLE", TitleTextBox.Text?.Trim());
            SetStr("VERSION", VerTextBox.Text?.Trim());
            SetStr("APP_VER", AppVerTextBox.Text?.Trim());
            SetStr("CATEGORY", CategoryTextBox.Text?.Trim());
            SetStr("CONTENT_ID", ContentIDTextBox.Text?.Trim());

            if (ParentalComboBox.SelectedIndex >= 0)
                SetUInt("PARENTAL_LEVEL", (uint)ParentalComboBox.SelectedIndex);

            if (!string.IsNullOrWhiteSpace(SystemVersionTextBox.Text))
            {
                SetStr("SYSTEM_VER", SystemVersionTextBox.Text.Trim(), 0x0004);
            }

            if (PS3BootableCheckBox.IsChecked == true || PSPBootableCheckBox.IsChecked == true)
                SetUInt("BOOTABLE", 1);

            if (PS4AppTypeComboBox.SelectedIndex > 0)
                SetUInt("APP_TYPE", (uint)PS4AppTypeComboBox.SelectedIndex);

            SetStr("PUBTOOLINFO", PS4PubToolInfoTextBox.Text?.Trim());
            SetStr("PUBTOOLVER", PS4PubToolVersionTextBox.Text?.Trim());

            SetStr("SHORT_TITLE", AppShotTitleTextBox.Text?.Trim());
            SetStr("SHARE_TITLE_ID", ShareAppTitleTextBox.Text?.Trim());
            SetStr("COMM_ID", VitaNPComIDTextBox.Text?.Trim());

            if (uint.TryParse(VitaSaveDataQuotaTextBox.Text?.Trim(), out var quotaVal))
                SetUInt("SAVEDATA_MAX_SIZE", quotaVal);

            byte[] sfoData = SerializeSfo(_loadedEntries);
            await File.WriteAllBytesAsync(savePath, sfoData);

            var okBox = MessageBoxManager.GetMessageBoxStandard("Saved", "PARAM.SFO saved successfully.", ButtonEnum.Ok, MsgBoxIcon.Success);
            await okBox.ShowWindowDialogAsync(this);
        }
        catch (Exception ex)
        {
            var errBox = MessageBoxManager.GetMessageBoxStandard("Error", $"Could not save PARAM.SFO: {ex.Message}", ButtonEnum.Ok, MsgBoxIcon.Error);
            await errBox.ShowWindowDialogAsync(this);
        }
    }

    private static byte[] SerializeSfo(Dictionary<string, (ushort Format, object Value)> entries)
    {
        var sortedKeys = entries.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        int numEntries = sortedKeys.Count;

        // Key Table
        using var keyTableStream = new MemoryStream();
        var keyOffsets = new Dictionary<string, ushort>(StringComparer.Ordinal);
        foreach (var k in sortedKeys)
        {
            keyOffsets[k] = (ushort)keyTableStream.Position;
            byte[] keyBytes = Encoding.UTF8.GetBytes(k);
            keyTableStream.Write(keyBytes, 0, keyBytes.Length);
            keyTableStream.WriteByte(0); // null terminator
        }
        while (keyTableStream.Position % 4 != 0)
            keyTableStream.WriteByte(0);

        byte[] keyTable = keyTableStream.ToArray();

        // Data Table & Entry Headers
        using var dataTableStream = new MemoryStream();
        var entryHeaders = new List<(ushort KeyOffset, ushort Format, uint Length, uint MaxLength, uint DataOffset)>();

        foreach (var k in sortedKeys)
        {
            var (fmt, val) = entries[k];
            uint dOffset = (uint)dataTableStream.Position;

            if (fmt == 0x0404) // uint32
            {
                uint intVal = val is uint u ? u : (val is int i ? (uint)i : Convert.ToUInt32(val));
                byte[] intBytes = BitConverter.GetBytes(intVal);
                dataTableStream.Write(intBytes, 0, 4);
                entryHeaders.Add((keyOffsets[k], fmt, 4, 4, dOffset));
            }
            else // string (0x0204) or special (0x0004)
            {
                string sVal = val?.ToString() ?? "";
                byte[] strBytes = Encoding.UTF8.GetBytes(sVal);
                byte[] nullTermBytes = new byte[strBytes.Length + 1];
                Array.Copy(strBytes, nullTermBytes, strBytes.Length);
                nullTermBytes[^1] = 0;

                int paddedLen = (nullTermBytes.Length + 3) & ~3;
                dataTableStream.Write(nullTermBytes, 0, nullTermBytes.Length);
                while (dataTableStream.Position % 4 != 0)
                    dataTableStream.WriteByte(0);

                entryHeaders.Add((keyOffsets[k], fmt, (uint)nullTermBytes.Length, (uint)paddedLen, dOffset));
            }
        }

        byte[] dataTable = dataTableStream.ToArray();

        // Header
        uint headerLen = 20;
        uint indexTableLen = (uint)(numEntries * 16);
        uint keyTableStart = headerLen + indexTableLen;
        uint dataTableStart = keyTableStart + (uint)keyTable.Length;

        using var finalStream = new MemoryStream();
        using var writer = new BinaryWriter(finalStream);

        // Magic \0PSF
        writer.Write((byte)0x00);
        writer.Write((byte)'P');
        writer.Write((byte)'S');
        writer.Write((byte)'F');

        // Version 1.1
        writer.Write((uint)0x00000101);
        writer.Write(keyTableStart);
        writer.Write(dataTableStart);
        writer.Write((uint)numEntries);

        // Index Table Entries
        foreach (var h in entryHeaders)
        {
            writer.Write(h.KeyOffset);
            writer.Write(h.Format);
            writer.Write(h.Length);
            writer.Write(h.MaxLength);
            writer.Write(h.DataOffset);
        }

        // Key Table
        writer.Write(keyTable);

        // Data Table
        writer.Write(dataTable);

        return finalStream.ToArray();
    }

    private void NewSFOButton_Click(object? sender, RoutedEventArgs e)
    {
        _loadedEntries.Clear();
        _currentFilePath = null;

        IDTextBox.Text = "CUSA00001";
        TitleTextBox.Text = "New Application";
        VerTextBox.Text = "01.00";
        AppVerTextBox.Text = "01.00";
        CategoryTextBox.Text = "gd";
        ContentIDTextBox.Text = "UP0001-CUSA00001_00-0000000000000000";
        ParentalComboBox.SelectedIndex = 0;
        SystemVersionTextBox.Text = "0x00000000";

        PS3BootableCheckBox.IsChecked = true;
        PSPBootableCheckBox.IsChecked = true;
        PS4AppTypeComboBox.SelectedIndex = 1;
        PS4PubToolInfoTextBox.Text = "";
        PS4PubToolVersionTextBox.Text = "";
        AppShotTitleTextBox.Text = "";
        ShareAppTitleTextBox.Text = "";
        VitaNPComIDTextBox.Text = "";
        VitaSaveDataQuotaTextBox.Text = "1024";
    }
}
