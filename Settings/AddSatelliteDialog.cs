using WorldMapWallpaper.Shared;
using WorldMapWallpaper.Shared.Models;
using WorldMapWallpaper.Shared.Services;

namespace WorldMapWallpaper.Settings;

/// <summary>
/// Dialog for adding a new satellite to track.
/// Supports both preset selection and custom NORAD ID entry.
/// </summary>
public partial class AddSatelliteDialog : Form
{
    private readonly ColorScheme _colorScheme;
    private readonly SatelliteConfigManager _configManager;

    private TabControl _tabControl = null!;
    private TabPage _presetsTab = null!;
    private TabPage _customTab = null!;

    // Preset tab controls
    private ComboBox _categoryCombo = null!;
    private ListBox _presetListBox = null!;
    private Label _presetInfoLabel = null!;

    // Custom tab controls
    private TextBox _noradIdTextBox = null!;
    private TextBox _nameTextBox = null!;
    private Label _validationLabel = null!;
    private Button _validateButton = null!;

    // Common controls
    private Button _addButton = null!;
    private Button _cancelButton = null!;
    private Label _lagrangeWarningLabel = null!;

    /// <summary>
    /// Gets the satellite that was added, or null if cancelled.
    /// </summary>
    public SatelliteConfig? AddedSatellite { get; private set; }

    /// <summary>
    /// Initializes a new instance of the AddSatelliteDialog class.
    /// </summary>
    public AddSatelliteDialog()
    {
        _colorScheme = ThemeManager.GetCurrentColorScheme();
        _configManager = SatelliteConfigManager.Instance;

        InitializeComponent();
        ApplyTheme();
        InitializeControls();
        PopulatePresets();
    }

    /// <summary>
    /// Initializes the form component settings.
    /// </summary>
    private void InitializeComponent()
    {
        this.Text = "Add Satellite";
        this.Size = new Size(450, 500);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Font = new Font("Segoe UI", 9F);
    }

    /// <summary>
    /// Applies the current theme colors.
    /// </summary>
    private void ApplyTheme()
    {
        this.BackColor = _colorScheme.BackgroundColor;
        this.ForeColor = _colorScheme.PrimaryTextColor;
    }

    /// <summary>
    /// Creates and configures all form controls.
    /// </summary>
    private void InitializeControls()
    {
        var padding = 15;
        var currentY = padding;

        // Header
        var headerLabel = new Label
        {
            Text = "Add Satellite to Track",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 28),
            ForeColor = _colorScheme.AccentColor
        };
        this.Controls.Add(headerLabel);
        currentY += 40;

        // Tab Control
        _tabControl = new TabControl
        {
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 320),
            Font = new Font("Segoe UI", 9F)
        };
        this.Controls.Add(_tabControl);

        // Create tabs
        CreatePresetsTab();
        CreateCustomTab();

        currentY += 335;

        // Lagrange point warning (hidden by default)
        _lagrangeWarningLabel = new Label
        {
            Text = "",
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 40),
            ForeColor = _colorScheme.WarningColor,
            Font = new Font("Segoe UI", 8F),
            Visible = false
        };
        this.Controls.Add(_lagrangeWarningLabel);
        currentY += 45;

        // Buttons
        _addButton = new Button
        {
            Text = "Add Satellite",
            Location = new Point(this.ClientSize.Width - padding - 200, currentY),
            Size = new Size(95, 30),
            BackColor = _colorScheme.AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false
        };
        _addButton.FlatAppearance.BorderSize = 0;
        _addButton.Click += OnAddClick;
        this.Controls.Add(_addButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(this.ClientSize.Width - padding - 95, currentY),
            Size = new Size(80, 30),
            BackColor = _colorScheme.ButtonBackColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            FlatStyle = FlatStyle.Flat
        };
        _cancelButton.FlatAppearance.BorderSize = 1;
        _cancelButton.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        _cancelButton.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
        this.Controls.Add(_cancelButton);
    }

    /// <summary>
    /// Creates the presets tab.
    /// </summary>
    private void CreatePresetsTab()
    {
        _presetsTab = new TabPage
        {
            Text = "Preset Satellites",
            BackColor = _colorScheme.GroupBoxBackColor,
            Padding = new Padding(10)
        };
        _tabControl.TabPages.Add(_presetsTab);

        var categoryLabel = new Label
        {
            Text = "Category:",
            Location = new Point(10, 15),
            Size = new Size(70, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        _presetsTab.Controls.Add(categoryLabel);

        _categoryCombo = new ComboBox
        {
            Location = new Point(85, 12),
            Size = new Size(300, 23),
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor
        };
        _categoryCombo.Items.Add("All Categories");
        foreach (var category in Enum.GetValues<SatelliteCategory>())
        {
            _categoryCombo.Items.Add(category.ToDisplayString());
        }
        _categoryCombo.SelectedIndex = 0;
        _categoryCombo.SelectedIndexChanged += OnCategoryChanged;
        _presetsTab.Controls.Add(_categoryCombo);

        _presetListBox = new ListBox
        {
            Location = new Point(10, 45),
            Size = new Size(375, 180),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            BorderStyle = BorderStyle.FixedSingle
        };
        _presetListBox.SelectedIndexChanged += OnPresetSelected;
        _presetsTab.Controls.Add(_presetListBox);

        _presetInfoLabel = new Label
        {
            Text = "Select a satellite to see details",
            Location = new Point(10, 230),
            Size = new Size(375, 50),
            ForeColor = _colorScheme.SecondaryTextColor,
            Font = new Font("Segoe UI", 8F)
        };
        _presetsTab.Controls.Add(_presetInfoLabel);
    }

    /// <summary>
    /// Creates the custom satellite tab.
    /// </summary>
    private void CreateCustomTab()
    {
        _customTab = new TabPage
        {
            Text = "Custom Satellite",
            BackColor = _colorScheme.GroupBoxBackColor,
            Padding = new Padding(10)
        };
        _tabControl.TabPages.Add(_customTab);

        var noradLabel = new Label
        {
            Text = "NORAD ID:",
            Location = new Point(10, 20),
            Size = new Size(80, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        _customTab.Controls.Add(noradLabel);

        _noradIdTextBox = new TextBox
        {
            Location = new Point(95, 17),
            Size = new Size(150, 23),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor
        };
        _noradIdTextBox.TextChanged += OnNoradIdChanged;
        _customTab.Controls.Add(_noradIdTextBox);

        _validateButton = new Button
        {
            Text = "Validate",
            Location = new Point(255, 16),
            Size = new Size(80, 25),
            BackColor = _colorScheme.ButtonBackColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            FlatStyle = FlatStyle.Flat
        };
        _validateButton.FlatAppearance.BorderSize = 1;
        _validateButton.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        _validateButton.Click += OnValidateClick;
        _customTab.Controls.Add(_validateButton);

        var nameLabel = new Label
        {
            Text = "Name:",
            Location = new Point(10, 55),
            Size = new Size(80, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        _customTab.Controls.Add(nameLabel);

        _nameTextBox = new TextBox
        {
            Location = new Point(95, 52),
            Size = new Size(290, 23),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor
        };
        _nameTextBox.TextChanged += OnNameChanged;
        _customTab.Controls.Add(_nameTextBox);

        _validationLabel = new Label
        {
            Text = "Enter a NORAD catalog number and click Validate",
            Location = new Point(10, 90),
            Size = new Size(375, 80),
            ForeColor = _colorScheme.SecondaryTextColor,
            Font = new Font("Segoe UI", 8F)
        };
        _customTab.Controls.Add(_validationLabel);

        var helpLabel = new Label
        {
            Text = "Tip: Find NORAD IDs at celestrak.org or n2yo.com",
            Location = new Point(10, 180),
            Size = new Size(375, 20),
            ForeColor = _colorScheme.SecondaryTextColor,
            Font = new Font("Segoe UI", 8F, FontStyle.Italic)
        };
        _customTab.Controls.Add(helpLabel);
    }

    /// <summary>
    /// Populates the preset list based on selected category.
    /// </summary>
    private void PopulatePresets()
    {
        _presetListBox.Items.Clear();

        IEnumerable<SatellitePreset> presets = _categoryCombo.SelectedIndex == 0
            ? SatellitePreset.All
            : SatellitePreset.ByCategory((SatelliteCategory)(_categoryCombo.SelectedIndex - 1));

        foreach (var preset in presets)
        {
            // Skip if already configured
            if (_configManager.ContainsSatellite(preset.NoradId))
                continue;

            _presetListBox.Items.Add(new PresetItem(preset));
        }

        _addButton.Enabled = false;
        _presetInfoLabel.Text = "Select a satellite to see details";
        _lagrangeWarningLabel.Visible = false;
    }

    /// <summary>
    /// Handler for category selection change.
    /// </summary>
    private void OnCategoryChanged(object? sender, EventArgs e)
    {
        PopulatePresets();
    }

    /// <summary>
    /// Handler for preset selection change.
    /// </summary>
    private void OnPresetSelected(object? sender, EventArgs e)
    {
        if (_presetListBox.SelectedItem is PresetItem item)
        {
            var preset = item.Preset;
            _presetInfoLabel.Text = $"NORAD ID: {preset.NoradId}\n" +
                                    $"Category: {preset.Category.ToDisplayString()}\n" +
                                    $"Orbit: {preset.OrbitType}";

            // Check for Lagrange point
            if (preset.IsLagrangePoint)
            {
                _lagrangeWarningLabel.Text = $"Warning: {preset.Name} is at Lagrange Point {preset.LagrangePointLocation}.\n" +
                                             "Position shown on the map will not be accurate.";
                _lagrangeWarningLabel.Visible = true;
            }
            else
            {
                _lagrangeWarningLabel.Visible = false;
            }

            _addButton.Enabled = true;
        }
        else
        {
            _addButton.Enabled = false;
            _lagrangeWarningLabel.Visible = false;
        }
    }

    /// <summary>
    /// Handler for NORAD ID text change.
    /// </summary>
    private void OnNoradIdChanged(object? sender, EventArgs e)
    {
        _addButton.Enabled = false;
        _validationLabel.Text = "Enter a NORAD catalog number and click Validate";
        _validationLabel.ForeColor = _colorScheme.SecondaryTextColor;
        _lagrangeWarningLabel.Visible = false;
    }

    /// <summary>
    /// Handler for name text change.
    /// </summary>
    private void OnNameChanged(object? sender, EventArgs e)
    {
        // Re-enable add button if validation passed and name is provided
        if (_validationLabel.ForeColor == _colorScheme.SuccessColor &&
            !string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            _addButton.Enabled = true;
        }
    }

    /// <summary>
    /// Handler for validate button click.
    /// </summary>
    private async void OnValidateClick(object? sender, EventArgs e)
    {
        if (!int.TryParse(_noradIdTextBox.Text, out var noradId) || noradId <= 0)
        {
            _validationLabel.Text = "Please enter a valid NORAD catalog number (positive integer)";
            _validationLabel.ForeColor = _colorScheme.ErrorColor;
            return;
        }

        // Check if already configured
        if (_configManager.ContainsSatellite(noradId))
        {
            _validationLabel.Text = "This satellite is already in your configuration";
            _validationLabel.ForeColor = _colorScheme.ErrorColor;
            return;
        }

        // Check for Lagrange point
        var lagrangeResult = LagrangePointDetector.DetectLagrangePoint(noradId);
        if (lagrangeResult.IsLagrangePoint)
        {
            _lagrangeWarningLabel.Text = LagrangePointDetector.GetWarningMessage(lagrangeResult);
            _lagrangeWarningLabel.Visible = true;

            if (!string.IsNullOrEmpty(lagrangeResult.SatelliteName))
            {
                _nameTextBox.Text = lagrangeResult.SatelliteName;
            }
        }
        else
        {
            _lagrangeWarningLabel.Visible = false;
        }

        _validateButton.Enabled = false;
        _validationLabel.Text = "Validating...";
        _validationLabel.ForeColor = _colorScheme.SecondaryTextColor;

        try
        {
            // Try to fetch TLE data to validate the satellite exists
            var tleService = new BatchTleService();
            var tle = await tleService.FetchSingleTleAsync(noradId);

            if (tle != null)
            {
                _validationLabel.Text = $"Satellite found: {tle.SatelliteName}\n" +
                                        $"NORAD ID: {tle.CatalogNumber}\n" +
                                        $"TLE Epoch: {tle.EpochDate:yyyy-MM-dd HH:mm} UTC";
                _validationLabel.ForeColor = _colorScheme.SuccessColor;

                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    _nameTextBox.Text = tle.SatelliteName;
                }

                // Check for Lagrange point based on TLE mean motion
                if (!lagrangeResult.IsLagrangePoint)
                {
                    // Extract mean motion from TLE and check
                    var meanMotion = ExtractMeanMotionFromTle(tle);
                    if (meanMotion.HasValue)
                    {
                        var tleBasedResult = LagrangePointDetector.DetectLagrangePoint(noradId, meanMotion);
                        if (tleBasedResult.IsLagrangePoint)
                        {
                            _lagrangeWarningLabel.Text = LagrangePointDetector.GetWarningMessage(tleBasedResult);
                            _lagrangeWarningLabel.Visible = true;
                        }
                    }
                }

                _addButton.Enabled = !string.IsNullOrWhiteSpace(_nameTextBox.Text);
            }
            else
            {
                _validationLabel.Text = "Satellite not found. Please check the NORAD ID.";
                _validationLabel.ForeColor = _colorScheme.ErrorColor;
            }
        }
        catch (Exception ex)
        {
            _validationLabel.Text = $"Validation failed: {ex.Message}";
            _validationLabel.ForeColor = _colorScheme.ErrorColor;
        }
        finally
        {
            _validateButton.Enabled = true;
        }
    }

    /// <summary>
    /// Extracts mean motion from TLE data.
    /// </summary>
    private static double? ExtractMeanMotionFromTle(TleData tle)
    {
        try
        {
            if (tle.Line2.Length >= 63)
            {
                var meanMotionStr = tle.Line2.Substring(52, 11);
                if (double.TryParse(meanMotionStr, out var meanMotion))
                {
                    return meanMotion;
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }
        return null;
    }

    /// <summary>
    /// Handler for add button click.
    /// </summary>
    private void OnAddClick(object? sender, EventArgs e)
    {
        if (_tabControl.SelectedTab == _presetsTab)
        {
            // Add from preset
            if (_presetListBox.SelectedItem is PresetItem item)
            {
                if (_configManager.AddFromPreset(item.Preset))
                {
                    AddedSatellite = _configManager.GetSatellite(item.Preset.NoradId);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to add satellite. It may already exist.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        else
        {
            // Add custom satellite
            if (int.TryParse(_noradIdTextBox.Text, out var noradId) &&
                !string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                var config = new SatelliteConfig
                {
                    Name = _nameTextBox.Text.Trim(),
                    NoradId = noradId,
                    Enabled = true,
                    ShowOrbit = true,
                    Category = SatelliteCategory.Default
                };

                // Check for Lagrange point
                var lagrangeResult = LagrangePointDetector.DetectLagrangePoint(noradId);
                config.IsLagrangePoint = lagrangeResult.IsLagrangePoint;

                if (_configManager.AddSatellite(config))
                {
                    AddedSatellite = config;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to add satellite. It may already exist.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    /// <summary>
    /// Helper class for preset list items.
    /// </summary>
    private class PresetItem
    {
        public SatellitePreset Preset { get; }

        public PresetItem(SatellitePreset preset)
        {
            Preset = preset;
        }

        public override string ToString()
        {
            var suffix = Preset.IsLagrangePoint ? $" ({Preset.LagrangePointLocation})" : $" ({Preset.OrbitType})";
            return Preset.Name + suffix;
        }
    }
}
