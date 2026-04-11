using WorldMapWallpaper.Shared;
using WorldMapWallpaper.Shared.Models;
using WorldMapWallpaper.Shared.Services;
using System.Diagnostics;
using System.Reflection;

namespace WorldMapWallpaper.Settings;

/// <summary>
/// The main settings form for World Map Wallpaper.
/// Provides a modern tabbed interface for configuring wallpaper and satellite options.
/// </summary>
public partial class SettingsForm : Form
{
    /// <summary>
    /// Monitors wallpaper changes to detect when the user switches away from our wallpaper.
    /// </summary>
    private WallpaperMonitor? _wallpaperMonitor;

    /// <summary>
    /// The color scheme used for theming the form controls.
    /// </summary>
    private readonly ColorScheme _colorScheme = null!;

    /// <summary>
    /// The system tray icon for quick access.
    /// </summary>
    private NotifyIcon? _notifyIcon;

    /// <summary>
    /// Indicates whether the form should start minimized to the system tray.
    /// </summary>
    private readonly bool _minimizeToTray = false;

    /// <summary>
    /// Initializes a new instance of the SettingsForm class.
    /// </summary>
    /// <param name="minimizeToTray">If true, the form starts minimized to the system tray.</param>
    public SettingsForm(bool minimizeToTray = false)
    {
        _colorScheme = ThemeManager.GetCurrentColorScheme();
        _minimizeToTray = minimizeToTray;

        InitializeComponent();
        WireEvents();
        ApplyTheme();
        InitializeTrayIcon();
        LoadSettings();
        StartWallpaperMonitoring();

        if (minimizeToTray)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }
    }

    /// <summary>
    /// Wires up event handlers for the form controls.
    /// </summary>
    private void WireEvents()
    {
        _closeButton.Click += (s, e) => MinimizeToTray();
    }

    /// <summary>
    /// Applies the current theme colors to the form.
    /// </summary>
    private void ApplyTheme()
    {
        BackColor = _colorScheme.BackgroundColor;
        ForeColor = _colorScheme.PrimaryTextColor;

        ApplyThemeToControl(this);
        ApplyThemeToButtons();
        ApplyThemeToTabPages();
        UpdateAboutVersionLabel();
    }

    private void ApplyThemeToControl(Control control)
    {
        switch (control)
        {
            case TabPage tabPage:
                tabPage.BackColor = _colorScheme.BackgroundColor;
                tabPage.ForeColor = _colorScheme.PrimaryTextColor;
                break;

            case GroupBox groupBox:
                groupBox.BackColor = _colorScheme.GroupBoxBackColor;
                groupBox.ForeColor = _colorScheme.PrimaryTextColor;
                break;

            case TextBox textBox:
                textBox.BackColor = _colorScheme.SurfaceColor;
                textBox.ForeColor = _colorScheme.PrimaryTextColor;
                break;

            case ListBox listBox:
                listBox.BackColor = _colorScheme.SurfaceColor;
                listBox.ForeColor = _colorScheme.PrimaryTextColor;
                break;

            case ComboBox comboBox:
                comboBox.BackColor = _colorScheme.SurfaceColor;
                comboBox.ForeColor = _colorScheme.PrimaryTextColor;
                break;

            case NumericUpDown numericUpDown:
                numericUpDown.BackColor = _colorScheme.SurfaceColor;
                numericUpDown.ForeColor = _colorScheme.PrimaryTextColor;
                break;

            case CheckBox checkBox:
                checkBox.BackColor = Color.Transparent;
                checkBox.ForeColor = checkBox.Enabled ? _colorScheme.PrimaryTextColor : _colorScheme.SecondaryTextColor;
                break;

            case Label label:
                ApplyThemeToLabel(label);
                break;

            case Button:
                // Buttons are themed in a dedicated pass so primary actions stay accented.
                break;

            default:
                control.BackColor = _colorScheme.BackgroundColor;
                control.ForeColor = _colorScheme.PrimaryTextColor;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyThemeToControl(child);
        }
    }

    private void ApplyThemeToLabel(Label label)
    {
        label.BackColor = Color.Transparent;

        if (ReferenceEquals(label, _headerLabel) || ReferenceEquals(label, _titleLabel))
        {
            label.ForeColor = _colorScheme.AccentColor;
            return;
        }

        if (ReferenceEquals(label, _taskStatusLabel))
        {
            label.ForeColor = TaskManager.IsTaskEnabled() ? _colorScheme.SuccessColor : _colorScheme.WarningColor;
            return;
        }

        if (ReferenceEquals(label, _detectedResolutionLabel) ||
            ReferenceEquals(label, _helpLabel) ||
            ReferenceEquals(label, _satelliteCountLabel) ||
            ReferenceEquals(label, _infoLabel) ||
            ReferenceEquals(label, _copyrightLabel))
        {
            label.ForeColor = _colorScheme.SecondaryTextColor;
            return;
        }

        label.ForeColor = _colorScheme.PrimaryTextColor;
    }

    private void ApplyThemeToButtons()
    {
        ApplyButtonTheme(_previewButton, primary: true);
        ApplyButtonTheme(_addSatelliteButton, primary: true);
        ApplyButtonTheme(_resetButton);
        ApplyButtonTheme(_closeButton);
        ApplyButtonTheme(_editSatelliteButton);
        ApplyButtonTheme(_removeSatelliteButton);
        ApplyButtonTheme(_moveUpButton);
        ApplyButtonTheme(_moveDownButton);
        ApplyButtonTheme(_browseOutputFolderButton);
    }

    private void ApplyButtonTheme(Button? button, bool primary = false)
    {
        if (button == null)
            return;

        button.FlatStyle = FlatStyle.Flat;

        if (primary && button.Enabled)
        {
            button.BackColor = _colorScheme.AccentColor;
            button.ForeColor = Color.White;
            button.FlatAppearance.BorderSize = 0;
        }
        else
        {
            button.BackColor = _colorScheme.ButtonBackColor;
            button.ForeColor = button.Enabled ? _colorScheme.PrimaryTextColor : _colorScheme.SecondaryTextColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        }
    }

    private void ApplyThemeToTabPages()
    {
        if (_tabControl == null)
            return;

        _tabControl.BackColor = _colorScheme.BackgroundColor;
        _tabControl.ForeColor = _colorScheme.PrimaryTextColor;

        foreach (TabPage page in _tabControl.TabPages)
        {
            page.BackColor = _colorScheme.BackgroundColor;
            page.ForeColor = _colorScheme.PrimaryTextColor;
        }
    }

    private void UpdateAboutVersionLabel()
    {
        if (_versionLabel == null)
            return;

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        _versionLabel.Text = version == null
            ? "Version 2.0"
            : $"Version {version.Major}.{version.Minor}";
    }

    /// <summary>
    /// Initializes the system tray icon.
    /// </summary>
    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = LoadEmbeddedIcon(),
            Text = "World Map Wallpaper",
            Visible = true
        };

        var contextMenu = new ContextMenuStrip();

        var showSettingsItem = new ToolStripMenuItem("Settings")
        {
            Font = new Font(contextMenu.Font, FontStyle.Bold)
        };
        showSettingsItem.Click += (s, e) => ShowSettingsWindow();
        contextMenu.Items.Add(showSettingsItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var updateNowItem = new ToolStripMenuItem("Update Wallpaper Now");
        updateNowItem.Click += (s, e) => _ = UpdateWallpaperNow();
        contextMenu.Items.Add(updateNowItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowSettingsWindow();
    }

    /// <summary>
    /// Shows the settings window.
    /// </summary>
    private void ShowSettingsWindow()
    {
        this.Visible = true;
        this.ShowInTaskbar = true;
        this.WindowState = FormWindowState.Normal;
        this.BringToFront();
        this.Activate();
    }

    private void MinimizeToTray()
    {
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;
    }

    /// <summary>
    /// Loads the application icon from embedded resources.
    /// </summary>
    private static Icon LoadEmbeddedIcon()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "WorldMapWallpaper.Settings.Resources.AppIcon.ico";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                return new Icon(stream);
            }
        }
        catch
        {
            // Fall back to system icon
        }

        return SystemIcons.Application;
    }

    /// <summary>
    /// Updates the wallpaper immediately.
    /// </summary>
    private async Task UpdateWallpaperNow()
    {
        try
        {
            _notifyIcon!.ShowBalloonTip(2000, "World Map Wallpaper", "Updating wallpaper...", ToolTipIcon.Info);

            var success = TaskManager.RunTaskNow();
            if (success)
                _notifyIcon.ShowBalloonTip(2000, "World Map Wallpaper", "Wallpaper updated successfully!", ToolTipIcon.Info);
            else
                _notifyIcon.ShowBalloonTip(2000, "World Map Wallpaper", "Failed to update wallpaper", ToolTipIcon.Warning);
        }
        catch (Exception ex)
        {
            _notifyIcon!.ShowBalloonTip(2000, "World Map Wallpaper", $"Error: {ex.Message}", ToolTipIcon.Error);
        }
    }

    /// <summary>
    /// Exits the application.
    /// </summary>
    private void ExitApplication()
    {
        _notifyIcon?.Dispose();
        Application.Exit();
    }

    /// <summary>
    /// Loads settings into form controls.
    /// </summary>
    private void LoadSettings()
    {
        // General tab
        _issCheckBox.Checked = Shared.Settings.ShowISS;
        _timeZonesCheckBox.Checked = Shared.Settings.ShowTimeZones;
        _politicalMapCheckBox.Checked = Shared.Settings.ShowPoliticalMap;
        _taskStatusLabel.Text = GetTaskStatusText();
        _detectedResolutionLabel.Text = GetDetectedResolutionText();

        var currentInterval = Shared.Settings.UpdateInterval;
        for (var i = 0; i < _updateIntervalCombo.Items.Count; i++)
        {
            if (_updateIntervalCombo.Items[i] is ComboBoxItem item && item.Value.Equals(currentInterval))
            {
                _updateIntervalCombo.SelectedIndex = i;
                break;
            }
        }

        var currentResMode = Shared.Settings.ResolutionMode;
        for (var i = 0; i < _resolutionModeCombo.Items.Count; i++)
        {
            if (_resolutionModeCombo.Items[i] is ComboBoxItem item && item.Value.Equals(currentResMode))
            {
                _resolutionModeCombo.SelectedIndex = i;
                break;
            }
        }

        _customWidthTextBox.Text = Shared.Settings.CustomResolutionWidth.ToString();
        _customHeightTextBox.Text = Shared.Settings.CustomResolutionHeight.ToString();
        _outputFolderTextBox.Text = Shared.Settings.WallpaperOutputDirectory;
        UpdateCustomResolutionState();

        // Satellites tab
        _satelliteTrackingCheckBox.Checked = Shared.Settings.SatelliteTrackingEnabled;
        LoadSatelliteList();
        _maxVisibleUpDown.Value = SatelliteConfigManager.Instance.MaxVisibleSatellites;
        UpdateSatelliteUI();
    }

    /// <summary>
    /// Loads the satellite list from configuration.
    /// </summary>
    private void LoadSatelliteList()
    {
        _satelliteListBox.Items.Clear();

        var satellites = SatelliteConfigManager.Instance.Satellites
            .OrderBy(s => s.Priority)
            .ToList();

        foreach (var sat in satellites)
        {
            _satelliteListBox.Items.Add(new SatelliteListItem(sat));
        }

        UpdateSatelliteCountLabel();
    }

    /// <summary>
    /// Updates the satellite count label.
    /// </summary>
    private void UpdateSatelliteCountLabel()
    {
        var total = SatelliteConfigManager.Instance.Satellites.Count;
        var enabled = SatelliteConfigManager.Instance.Satellites.Count(s => s.Enabled);
        _satelliteCountLabel.Text = $"({enabled} enabled / {total} total)";
    }

    /// <summary>
    /// Updates the satellite UI button states.
    /// </summary>
    private void UpdateSatelliteUI()
    {
        var hasSelection = _satelliteListBox.SelectedItem != null;
        var selectedIndex = _satelliteListBox.SelectedIndex;

        _editSatelliteButton.Enabled = hasSelection;
        _removeSatelliteButton.Enabled = hasSelection;
        _moveUpButton.Enabled = hasSelection && selectedIndex > 0;
        _moveDownButton.Enabled = hasSelection && selectedIndex < _satelliteListBox.Items.Count - 1;

        var trackingEnabled = _satelliteTrackingCheckBox.Checked;
        _satelliteListBox.Enabled = trackingEnabled;
        _addSatelliteButton.Enabled = trackingEnabled;
        _maxVisibleUpDown.Enabled = trackingEnabled;

        if (!trackingEnabled)
        {
            _editSatelliteButton.Enabled = false;
            _removeSatelliteButton.Enabled = false;
            _moveUpButton.Enabled = false;
            _moveDownButton.Enabled = false;
        }

        ApplyThemeToButtons();
    }

    /// <summary>
    /// Saves current settings.
    /// </summary>
    private void SaveSettings()
    {
        Shared.Settings.ShowISS = _issCheckBox.Checked;
        Shared.Settings.ShowTimeZones = _timeZonesCheckBox.Checked;
        Shared.Settings.ShowPoliticalMap = _politicalMapCheckBox.Checked;

        if (_updateIntervalCombo.SelectedItem is ComboBoxItem item)
        {
            Shared.Settings.UpdateInterval = (UpdateInterval)item.Value;
            TaskManager.UpdateTaskSchedule((UpdateInterval)item.Value);
        }

        if (_resolutionModeCombo.SelectedItem is ComboBoxItem resItem)
        {
            Shared.Settings.ResolutionMode = (ResolutionMode)resItem.Value;
        }

        if (int.TryParse(_customWidthTextBox.Text, out var width))
        {
            Shared.Settings.CustomResolutionWidth = Math.Max(0, width);
        }

        if (int.TryParse(_customHeightTextBox.Text, out var height))
        {
            Shared.Settings.CustomResolutionHeight = Math.Max(0, height);
        }

        Shared.Settings.WallpaperOutputDirectory = string.IsNullOrWhiteSpace(_outputFolderTextBox.Text)
            ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            : _outputFolderTextBox.Text.Trim();

        _taskStatusLabel.Text = GetTaskStatusText();
        _detectedResolutionLabel.Text = GetDetectedResolutionText();
        ApplyThemeToLabel(_taskStatusLabel);
        ApplyThemeToLabel(_detectedResolutionLabel);
    }

    // Event handlers
    private void OnSettingChanged(object? sender, EventArgs e) => SaveSettings();

    private void OnUpdateIntervalChanged(object? sender, EventArgs e) => SaveSettings();

    private void OnResolutionModeChanged(object? sender, EventArgs e)
    {
        SaveSettings();
        UpdateCustomResolutionState();
    }

    private void OnCustomResolutionChanged(object? sender, EventArgs e) => SaveSettings();

    private void OnOutputFolderChanged(object? sender, EventArgs e) => SaveSettings();

    private void UpdateCustomResolutionState()
    {
        var mode = ResolutionMode.None;
        if (_resolutionModeCombo.SelectedItem is ComboBoxItem item)
        {
            mode = (ResolutionMode)item.Value;
        }

        var enableCustom = mode != ResolutionMode.None;
        _customWidthTextBox.Enabled = enableCustom;
        _customHeightTextBox.Enabled = enableCustom;
    }

    private static string GetDetectedResolutionText()
    {
        try
        {
            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                return $"Detected: {screen.Bounds.Width} x {screen.Bounds.Height}";
            }
        }
        catch { }
        return "Detected: Unable to detect";
    }

    private void OnBrowseOutputFolderClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the folder where generated wallpaper images should be saved.",
            SelectedPath = string.IsNullOrWhiteSpace(_outputFolderTextBox.Text)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                : _outputFolderTextBox.Text,
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void OnSatelliteTrackingChanged(object? sender, EventArgs e)
    {
        Shared.Settings.SatelliteTrackingEnabled = _satelliteTrackingCheckBox.Checked;
        UpdateSatelliteUI();
    }

    private void OnSatelliteSelectionChanged(object? sender, EventArgs e) => UpdateSatelliteUI();

    private void OnSatelliteDoubleClick(object? sender, EventArgs e) => OnEditSatelliteClick(sender, e);

    private void OnAddSatelliteClick(object? sender, EventArgs e)
    {
        using var dialog = new AddSatelliteDialog();
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            LoadSatelliteList();
        }
    }

    private void OnEditSatelliteClick(object? sender, EventArgs e)
    {
        if (_satelliteListBox.SelectedItem is SatelliteListItem item)
        {
            using var dialog = new SatelliteEditDialog(item.Config);
            if (dialog.ShowDialog(this) == DialogResult.OK && dialog.WasModified)
            {
                LoadSatelliteList();
            }
        }
    }

    private void OnRemoveSatelliteClick(object? sender, EventArgs e)
    {
        if (_satelliteListBox.SelectedItem is SatelliteListItem item)
        {
            var result = MessageBox.Show(
                $"Remove {item.Config.Name} from tracking?",
                "Confirm Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SatelliteConfigManager.Instance.RemoveSatellite(item.Config.NoradId);
                LoadSatelliteList();
            }
        }
    }

    private void OnMoveUpClick(object? sender, EventArgs e)
    {
        var index = _satelliteListBox.SelectedIndex;
        if (index > 0)
        {
            ReorderSatellites(index, index - 1);
        }
    }

    private void OnMoveDownClick(object? sender, EventArgs e)
    {
        var index = _satelliteListBox.SelectedIndex;
        if (index < _satelliteListBox.Items.Count - 1)
        {
            ReorderSatellites(index, index + 1);
        }
    }

    private void ReorderSatellites(int fromIndex, int toIndex)
    {
        var items = _satelliteListBox.Items.Cast<SatelliteListItem>().ToList();
        var item = items[fromIndex];
        items.RemoveAt(fromIndex);
        items.Insert(toIndex, item);

        var noradIds = items.Select(i => i.Config.NoradId);
        SatelliteConfigManager.Instance.ReorderSatellites(noradIds);

        LoadSatelliteList();
        _satelliteListBox.SelectedIndex = toIndex;
    }

    private void OnMaxVisibleChanged(object? sender, EventArgs e)
    {
        SatelliteConfigManager.Instance.MaxVisibleSatellites = (int)_maxVisibleUpDown.Value;
    }

    private async void OnPreviewClick(object? sender, EventArgs e)
    {
        _previewButton.Enabled = false;
        _previewButton.Text = "Updating...";

        try
        {
            SaveSettings();

            if (TaskManager.RunTaskNow())
            {
                _previewButton.Text = "Updated!";
                await Task.Delay(2000);
            }
            else
            {
                _previewButton.Text = "Failed to update";
                await Task.Delay(2000);
            }
        }
        finally
        {
            _previewButton.Text = "Update Wallpaper Now";
            _previewButton.Enabled = true;
        }
    }

    private void OnResetClick(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Reset all settings to defaults?\nThis will also reset satellite configuration to ISS only.",
            "Reset Settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            Shared.Settings.ResetToDefaults();
            SatelliteConfigManager.Instance.ResetToDefaults();
            LoadSettings();
        }
    }

    private void StartWallpaperMonitoring()
    {
        _wallpaperMonitor = new WallpaperMonitor();
        _wallpaperMonitor.WallpaperChanged += OnWallpaperChanged;
        _wallpaperMonitor.Start();
    }

    private void OnWallpaperChanged(bool isOurWallpaper)
    {
        if (!isOurWallpaper)
        {
            TaskManager.EnableTask(false);
            Shared.Settings.IsActive = false;

            if (InvokeRequired)
                Invoke(new Action(() => _notifyIcon?.ShowBalloonTip(3000, "World Map Wallpaper",
                    "Automatic updates disabled - you switched to a different wallpaper", ToolTipIcon.Info)));
            else
                _notifyIcon?.ShowBalloonTip(3000, "World Map Wallpaper",
                    "Automatic updates disabled - you switched to a different wallpaper", ToolTipIcon.Info);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            MinimizeToTray();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _wallpaperMonitor?.Stop();
        _wallpaperMonitor?.Dispose();
        _notifyIcon?.Dispose();
        base.OnFormClosed(e);
    }

    private static string GetTaskStatusText()
    {
        if (!TaskManager.TaskExists())
            return "Task not found - reinstall may be required";

        if (!TaskManager.IsTaskEnabled())
            return "Task is disabled";

        var taskInfo = TaskManager.GetTaskInfo();
        if (taskInfo.HasValue)
        {
            var (state, nextRun) = taskInfo.Value;
            var nextRunText = nextRun?.ToString("MMM d, h:mm tt") ?? "Not scheduled";
            var triggers = TaskManager.GetTriggerInfo();
            return $"Task active with {triggers.Count} triggers - Next: {nextRunText}";
        }

        return "Task status unknown";
    }

    /// <summary>
    /// Helper class for ComboBox items.
    /// </summary>
    private class ComboBoxItem(string display, object value)
    {
        public string Display { get; } = display;
        public object Value { get; } = value;
        public override string ToString() => Display;
    }

    /// <summary>
    /// Helper class for satellite list items.
    /// </summary>
    private class SatelliteListItem
    {
        public SatelliteConfig Config { get; }

        public SatelliteListItem(SatelliteConfig config)
        {
            Config = config;
        }

        public override string ToString()
        {
            var status = Config.Enabled ? "" : " [disabled]";
            var lagrange = Config.IsLagrangePoint ? " [L-point]" : "";
            return $"{Config.Name} (#{Config.NoradId}){status}{lagrange}";
        }
    }
}
