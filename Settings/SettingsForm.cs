using WorldMapWallpaper.Shared;
using System.Diagnostics;
using System.Reflection;

namespace WorldMapWallpaper.Settings;

/// <summary>
/// The main settings form for World Map Wallpaper.
/// Provides a modern-looking interface for configuring wallpaper options with system theme support.
/// </summary>
public partial class SettingsForm : Form
{
    /// <summary>
    /// Monitors wallpaper changes to detect when the user switches away from our wallpaper.
    /// </summary>
    private WallpaperMonitor? _wallpaperMonitor;
    
    /// <summary>
    /// The color scheme used for theming the form controls based on the current system theme.
    /// </summary>
    private readonly ColorScheme _colorScheme = null!;
    
    /// <summary>
    /// The system tray icon that provides quick access to settings and wallpaper updates.
    /// </summary>
    private NotifyIcon? _notifyIcon;
    
    /// <summary>
    /// Indicates whether the form should start minimized to the system tray.
    /// </summary>
    private readonly bool _minimizeToTray = false;

    /// <summary>
    /// Checkbox control for enabling/disabling the International Space Station overlay.
    /// </summary>
    private CheckBox _issCheckBox = null!;
    
    /// <summary>
    /// Checkbox control for enabling/disabling the time zones overlay.
    /// </summary>
    private CheckBox _timeZonesCheckBox = null!;
    
    /// <summary>
    /// Checkbox control for enabling/disabling the political map overlay.
    /// </summary>
    private CheckBox _politicalMapCheckBox = null!;
    
    /// <summary>
    /// Combo box for selecting the wallpaper update frequency.
    /// </summary>
    private ComboBox _updateIntervalCombo = null!;
    
    /// <summary>
    /// Button for immediately updating the wallpaper with current settings.
    /// </summary>
    private Button _previewButton = null!;
    
    /// <summary>
    /// Button for resetting all settings to their default values.
    /// </summary>
    private Button _resetButton = null!;
    
    /// <summary>
    /// Button for closing the settings form.
    /// </summary>
    private Button _closeButton = null!;
    
    /// <summary>
    /// Label displaying the current status of the scheduled task.
    /// </summary>
    private Label _taskStatusLabel = null!;

    /// <summary>
    /// Combo box for selecting the resolution scaling mode.
    /// </summary>
    private ComboBox _resolutionModeCombo = null!;

    /// <summary>
    /// Text box for custom resolution width.
    /// </summary>
    private TextBox _customWidthTextBox = null!;

    /// <summary>
    /// Text box for custom resolution height.
    /// </summary>
    private TextBox _customHeightTextBox = null!;

    /// <summary>
    /// Label showing detected screen resolution.
    /// </summary>
    private Label _detectedResolutionLabel = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsForm"/> class.
    /// </summary>
    /// <param name="minimizeToTray">If true, the form starts minimized to the system tray; otherwise, it appears normally.</param>
    public SettingsForm(bool minimizeToTray = false)
    {
        // Get current theme before initializing components
        _colorScheme = ThemeManager.GetCurrentColorScheme();
        
        InitializeComponent();
        InitializeFormSettings();
        ApplyTheme();
        InitializeControls();
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
    /// Applies additional form settings after designer initialization.
    /// </summary>
    private void InitializeFormSettings()
    {
        // Set modern appearance
        this.Font = new Font("Segoe UI", 9F);
    }

    /// <summary>
    /// Applies the current theme colors to the form's background and text.
    /// </summary>
    private void ApplyTheme()
    {
        // Apply theme to the form
        this.BackColor = _colorScheme.BackgroundColor;
        this.ForeColor = _colorScheme.PrimaryTextColor;
    }

    /// <summary>
    /// Creates and configures all the form controls including labels, checkboxes, buttons, and group boxes.
    /// Applies theming and sets up event handlers for user interactions.
    /// </summary>
    private void InitializeControls()
    {
        var padding = 20;
        var currentY = padding;

        // Header
        var headerLabel = new Label
        {
            Text = "World Map Wallpaper Settings",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 32),
            ForeColor = _colorScheme.AccentColor,
            BackColor = Color.Transparent
        };
        this.Controls.Add(headerLabel);
        currentY += 50;

        var subtitleLabel = new Label
        {
            Text = "Configure your dynamic wallpaper preferences",
            Font = new Font("Segoe UI", 9F),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 20),
            ForeColor = _colorScheme.SecondaryTextColor,
            BackColor = Color.Transparent
        };
        this.Controls.Add(subtitleLabel);
        currentY += 40;

        // Visual Elements Group
        var visualGroup = new GroupBox
        {
            Text = "Visual Elements",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 120),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = _colorScheme.GroupBoxBackColor
        };
        this.Controls.Add(visualGroup);

        _issCheckBox = new CheckBox
        {
            Text = "Show International Space Station position and orbit",
            Location = new Point(15, 25),
            Size = new Size(visualGroup.Width - 30, 23),
            Checked = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = Color.Transparent
        };
        _issCheckBox.CheckedChanged += OnSettingChanged;
        visualGroup.Controls.Add(_issCheckBox);

        _timeZonesCheckBox = new CheckBox
        {
            Text = "Show time zone clocks around the world",
            Location = new Point(15, 50),
            Size = new Size(visualGroup.Width - 30, 23),
            Checked = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = Color.Transparent
        };
        _timeZonesCheckBox.CheckedChanged += OnSettingChanged;
        visualGroup.Controls.Add(_timeZonesCheckBox);

        _politicalMapCheckBox = new CheckBox
        {
            Text = "Show political boundaries and country borders",
            Location = new Point(15, 75),
            Size = new Size(visualGroup.Width - 30, 23),
            Checked = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = Color.Transparent
        };
        _politicalMapCheckBox.CheckedChanged += OnSettingChanged;
        visualGroup.Controls.Add(_politicalMapCheckBox);

        currentY += 140;

        // Update Settings Group
        var updateGroup = new GroupBox
        {
            Text = "Update Settings",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 100),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = _colorScheme.GroupBoxBackColor
        };
        this.Controls.Add(updateGroup);

        var intervalLabel = new Label
        {
            Text = "Update Frequency:",
            Location = new Point(15, 30),
            Size = new Size(120, 23),
            Font = new Font("Segoe UI", 9F),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = Color.Transparent
        };
        updateGroup.Controls.Add(intervalLabel);

        _updateIntervalCombo = new ComboBox
        {
            Location = new Point(140, 27),
            Size = new Size(updateGroup.Width - 155, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor
        };

        // Populate combo box
        foreach (var interval in Enum.GetValues<UpdateInterval>())
        {
            _updateIntervalCombo.Items.Add(new ComboBoxItem(interval.ToDisplayString(), interval));
        }
        _updateIntervalCombo.SelectedIndexChanged += OnUpdateIntervalChanged;
        updateGroup.Controls.Add(_updateIntervalCombo);

        var infoLabel = new Label
        {
            Text = "More frequent updates ensure accurate day/night cycles.",
            Location = new Point(15, 60),
            Size = new Size(updateGroup.Width - 30, 15),
            Font = new Font("Segoe UI", 8F),
            ForeColor = _colorScheme.SecondaryTextColor,
            BackColor = Color.Transparent
        };
        updateGroup.Controls.Add(infoLabel);

        // Add task status label
        _taskStatusLabel = new Label
        {
            Text = GetTaskStatusText(),
            Location = new Point(15, 75),
            Size = new Size(updateGroup.Width - 30, 15),
            Font = new Font("Segoe UI", 8F),
            ForeColor = TaskManager.IsTaskEnabled() ? _colorScheme.SuccessColor : _colorScheme.ErrorColor,
            BackColor = Color.Transparent
        };
        updateGroup.Controls.Add(_taskStatusLabel);

        currentY += 120;

        // Resolution Settings Group
        var resolutionGroup = new GroupBox
        {
            Text = "Resolution Settings",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 130),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = _colorScheme.GroupBoxBackColor
        };
        this.Controls.Add(resolutionGroup);

        var resolutionModeLabel = new Label
        {
            Text = "Resolution Mode:",
            Location = new Point(15, 25),
            Size = new Size(110, 23),
            Font = new Font("Segoe UI", 9F),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = Color.Transparent
        };
        resolutionGroup.Controls.Add(resolutionModeLabel);

        _resolutionModeCombo = new ComboBox
        {
            Location = new Point(130, 22),
            Size = new Size(resolutionGroup.Width - 145, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9F),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor
        };

        // Populate resolution mode combo box
        foreach (var mode in Enum.GetValues<ResolutionMode>())
        {
            _resolutionModeCombo.Items.Add(new ComboBoxItem(mode.ToDisplayString(), mode));
        }
        _resolutionModeCombo.SelectedIndexChanged += OnResolutionModeChanged;
        resolutionGroup.Controls.Add(_resolutionModeCombo);

        // Detected resolution display
        _detectedResolutionLabel = new Label
        {
            Text = GetDetectedResolutionText(),
            Location = new Point(15, 52),
            Size = new Size(resolutionGroup.Width - 30, 15),
            Font = new Font("Segoe UI", 8F),
            ForeColor = _colorScheme.SecondaryTextColor,
            BackColor = Color.Transparent
        };
        resolutionGroup.Controls.Add(_detectedResolutionLabel);

        // Custom resolution inputs
        var customResLabel = new Label
        {
            Text = "Custom (0 = auto):",
            Location = new Point(15, 75),
            Size = new Size(110, 23),
            Font = new Font("Segoe UI", 9F),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = Color.Transparent
        };
        resolutionGroup.Controls.Add(customResLabel);

        _customWidthTextBox = new TextBox
        {
            Location = new Point(130, 72),
            Size = new Size(70, 23),
            Font = new Font("Segoe UI", 9F),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            Text = "0"
        };
        _customWidthTextBox.TextChanged += OnCustomResolutionChanged;
        resolutionGroup.Controls.Add(_customWidthTextBox);

        var xLabel = new Label
        {
            Text = "x",
            Location = new Point(205, 75),
            Size = new Size(15, 23),
            Font = new Font("Segoe UI", 9F),
            ForeColor = _colorScheme.PrimaryTextColor,
            BackColor = Color.Transparent
        };
        resolutionGroup.Controls.Add(xLabel);

        _customHeightTextBox = new TextBox
        {
            Location = new Point(220, 72),
            Size = new Size(70, 23),
            Font = new Font("Segoe UI", 9F),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            Text = "0"
        };
        _customHeightTextBox.TextChanged += OnCustomResolutionChanged;
        resolutionGroup.Controls.Add(_customHeightTextBox);

        var customHelpLabel = new Label
        {
            Text = "Set both to 0 to use detected screen resolution",
            Location = new Point(15, 100),
            Size = new Size(resolutionGroup.Width - 30, 15),
            Font = new Font("Segoe UI", 8F),
            ForeColor = _colorScheme.SecondaryTextColor,
            BackColor = Color.Transparent
        };
        resolutionGroup.Controls.Add(customHelpLabel);

        currentY += 150;

        // Preview Button
        _previewButton = new Button
        {
            Text = "Update Wallpaper Now",
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 35),
            Font = new Font("Segoe UI", 9F),
            BackColor = _colorScheme.AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _previewButton.FlatAppearance.BorderSize = 0;
        _previewButton.Click += OnPreviewClick;
        this.Controls.Add(_previewButton);
        currentY += 50;

        // Bottom buttons
        _resetButton = new Button
        {
            Text = "Reset to Defaults",
            Location = new Point(padding, currentY),
            Size = new Size(150, 30),
            Font = new Font("Segoe UI", 9F),
            BackColor = _colorScheme.ButtonBackColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _resetButton.FlatAppearance.BorderSize = 1;
        _resetButton.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        _resetButton.Click += OnResetClick;
        this.Controls.Add(_resetButton);


        _closeButton = new Button
        {
            Text = "Close",
            Location = new Point(this.ClientSize.Width - padding - 80, currentY),
            Size = new Size(80, 30),
            Font = new Font("Segoe UI", 9F),
            BackColor = _colorScheme.ButtonBackColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _closeButton.FlatAppearance.BorderSize = 1;
        _closeButton.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        _closeButton.Click += (s, e) => MinimizeToTray();
        this.Controls.Add(_closeButton);
    }

    /// <summary>
    /// Initializes the system tray icon with a context menu for quick access to application functions.
    /// Sets up menu items for showing settings, updating wallpaper, and exiting the application.
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
    /// Shows the settings window by bringing it to the foreground and restoring it from the system tray.
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
    /// <returns>The application icon, or a system icon as fallback.</returns>
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
            // Fall back to system icon if embedded resource loading fails
        }
        
        return SystemIcons.Application;
    }

    /// <summary>
    /// Asynchronously updates the wallpaper immediately and displays notification balloons to inform the user of the progress and result.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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
    /// Exits the application completely by disposing of the tray icon and calling Application.Exit().
    /// </summary>
    private void ExitApplication()
    {
        _notifyIcon?.Dispose();
        Application.Exit();
    }

    /// <summary>
    /// Loads the current settings from the application configuration and updates the form controls to reflect these values.
    /// </summary>
    private void LoadSettings()
    {
        _issCheckBox.Checked = Shared.Settings.ShowISS;
        _timeZonesCheckBox.Checked = Shared.Settings.ShowTimeZones;
        _politicalMapCheckBox.Checked = Shared.Settings.ShowPoliticalMap;

        var currentInterval = Shared.Settings.UpdateInterval;
        for (var i = 0; i < _updateIntervalCombo.Items.Count; i++)
        {
            if (_updateIntervalCombo.Items[i] is ComboBoxItem item &&
                item.Value.Equals(currentInterval))
            {
                _updateIntervalCombo.SelectedIndex = i;
                break;
            }
        }

        // Load resolution settings
        var currentResMode = Shared.Settings.ResolutionMode;
        for (var i = 0; i < _resolutionModeCombo.Items.Count; i++)
        {
            if (_resolutionModeCombo.Items[i] is ComboBoxItem item &&
                item.Value.Equals(currentResMode))
            {
                _resolutionModeCombo.SelectedIndex = i;
                break;
            }
        }

        _customWidthTextBox.Text = Shared.Settings.CustomResolutionWidth.ToString();
        _customHeightTextBox.Text = Shared.Settings.CustomResolutionHeight.ToString();

        UpdateCustomResolutionState();
    }

    /// <summary>
    /// Saves the current form control values to the application settings and updates the task schedule if necessary.
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

        // Save resolution settings
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
    }

    /// <summary>
    /// Event handler that is triggered when any of the visual element checkboxes are changed.
    /// Automatically saves the new settings.
    /// </summary>
    /// <param name="sender">The checkbox control that triggered the event.</param>
    /// <param name="e">Event arguments containing information about the change.</param>
    private void OnSettingChanged(object? sender, EventArgs e)
    {
        SaveSettings();
    }

    /// <summary>
    /// Event handler that is triggered when the update interval combo box selection changes.
    /// Automatically saves the new settings and updates the task schedule.
    /// </summary>
    /// <param name="sender">The combo box control that triggered the event.</param>
    /// <param name="e">Event arguments containing information about the selection change.</param>
    private void OnUpdateIntervalChanged(object? sender, EventArgs e)
    {
        SaveSettings();
    }

    /// <summary>
    /// Event handler for resolution mode combo box changes.
    /// </summary>
    /// <param name="sender">The combo box control that triggered the event.</param>
    /// <param name="e">Event arguments for the selection change.</param>
    private void OnResolutionModeChanged(object? sender, EventArgs e)
    {
        SaveSettings();
        UpdateCustomResolutionState();
    }

    /// <summary>
    /// Event handler for custom resolution text box changes.
    /// </summary>
    /// <param name="sender">The text box control that triggered the event.</param>
    /// <param name="e">Event arguments for the text change.</param>
    private void OnCustomResolutionChanged(object? sender, EventArgs e)
    {
        SaveSettings();
    }

    /// <summary>
    /// Updates the enabled state of custom resolution inputs based on mode.
    /// Custom resolution inputs are only enabled when resolution mode is not None.
    /// </summary>
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

    /// <summary>
    /// Gets the detected screen resolution as a display string.
    /// </summary>
    /// <returns>A string showing the detected primary screen resolution.</returns>
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
        catch
        {
            // Ignore detection errors
        }
        return "Detected: Unable to detect";
    }

    /// <summary>
    /// Event handler for the preview/update button click. Temporarily disables the button, 
    /// saves current settings, attempts to update the wallpaper, and provides user feedback.
    /// </summary>
    /// <param name="sender">The button control that was clicked.</param>
    /// <param name="e">Event arguments for the click event.</param>
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

    /// <summary>
    /// Event handler for the reset button click. Prompts the user for confirmation 
    /// before resetting all settings to their default values.
    /// </summary>
    /// <param name="sender">The button control that was clicked.</param>
    /// <param name="e">Event arguments for the click event.</param>
    private void OnResetClick(object? sender, EventArgs e)
    {
        var result = MessageBox.Show(
            "Are you sure you want to reset all settings to their default values?",
            "Reset Settings",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            Shared.Settings.ResetToDefaults();
            LoadSettings();
        }
    }


    /// <summary>
    /// Initializes and starts the wallpaper monitoring service to detect when the user changes 
    /// to a different wallpaper provider.
    /// </summary>
    private void StartWallpaperMonitoring()
    {
        _wallpaperMonitor = new WallpaperMonitor();
        _wallpaperMonitor.WallpaperChanged += OnWallpaperChanged;
        _wallpaperMonitor.Start();
    }

    /// <summary>
    /// Event handler that is triggered when the system wallpaper changes. If the user has switched 
    /// to a different wallpaper, disables the automatic update task and shows a notification.
    /// </summary>
    /// <param name="isOurWallpaper">True if the current wallpaper is from this application; false if the user switched to a different wallpaper.</param>
    private void OnWallpaperChanged(bool isOurWallpaper)
    {
        if (!isOurWallpaper)
        {
            // User switched to a different wallpaper - disable our task
            TaskManager.EnableTask(false);
            Shared.Settings.IsActive = false;
            
            // Show notification instead of closing
            if (InvokeRequired)
                Invoke(new Action(() => _notifyIcon?.ShowBalloonTip(3000, "World Map Wallpaper", "Automatic updates disabled - you switched to a different wallpaper", ToolTipIcon.Info)));
            else
                _notifyIcon?.ShowBalloonTip(3000, "World Map Wallpaper", "Automatic updates disabled - you switched to a different wallpaper", ToolTipIcon.Info);
        }
    }

    /// <summary>
    /// Overrides the base SetVisibleCore method to prevent the form from becoming visible 
    /// when it should be minimized to the system tray.
    /// </summary>
    /// <param name="value">The visibility state to set.</param>
    protected override void SetVisibleCore(bool value)
    {
        // Prevent the form from becoming visible at design time or when minimized to tray
        base.SetVisibleCore(!_minimizeToTray && value);
    }

    /// <summary>
    /// Overrides the form closing behavior to minimize to the system tray instead of actually closing 
    /// when the user clicks the close button.
    /// </summary>
    /// <param name="e">Event arguments that can be used to cancel the closing operation.</param>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Minimize to tray instead of closing when user clicks X
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }
        else
        {
            base.OnFormClosing(e);
        }
    }

    /// <summary>
    /// Overrides the form closed event to properly dispose of resources including 
    /// the wallpaper monitor and system tray icon.
    /// </summary>
    /// <param name="e">Event arguments containing information about how the form was closed.</param>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _wallpaperMonitor?.Stop();
        _wallpaperMonitor?.Dispose();
        _notifyIcon?.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// Gets the current task status as a user-friendly string.
    /// </summary>
    /// <returns>A string describing the current task status.</returns>
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
            
            // Get trigger count for additional info
            var triggers = TaskManager.GetTriggerInfo();
            var triggerCount = triggers.Count;
            
            return $"Task active with {triggerCount} triggers - Next: {nextRunText}";
        }

        return "Task status unknown";
    }

    /// <summary>
    /// Helper class for ComboBox items that associates a display string with an underlying value.
    /// </summary>
    /// <param name="display">The text to display in the combo box.</param>
    /// <param name="value">The underlying value associated with this item.</param>
    private class ComboBoxItem(string display, object value)
    {
        /// <summary>
        /// Gets the display text for this combo box item.
        /// </summary>
        public string Display { get; } = display;
        
        /// <summary>
        /// Gets the underlying value associated with this combo box item.
        /// </summary>
        public object Value { get; } = value;

        /// <summary>
        /// Returns the display string for this combo box item.
        /// </summary>
        /// <returns>The display text.</returns>
        public override string ToString() => Display;
    }
}