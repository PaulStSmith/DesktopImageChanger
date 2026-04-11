using System.Drawing;
using WorldMapWallpaper.Shared;
using WorldMapWallpaper.Shared.Models;
using WorldMapWallpaper.Shared.Services;

namespace WorldMapWallpaper.Settings;

/// <summary>
/// Dialog for editing satellite configuration.
/// Allows changing name, color, icon, and visibility settings.
/// </summary>
public partial class SatelliteEditDialog : Form
{
    private readonly ColorScheme _colorScheme;
    private readonly SatelliteConfig _satellite;

    private TextBox _nameTextBox = null!;
    private CheckBox _enabledCheckBox = null!;
    private CheckBox _showOrbitCheckBox = null!;
    private Panel _colorPreviewPanel = null!;
    private Button _colorButton = null!;
    private TextBox _iconPathTextBox = null!;
    private Button _browseIconButton = null!;
    private Button _clearIconButton = null!;
    private Label _infoLabel = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;

    private string _selectedColor;

    /// <summary>
    /// Gets whether the satellite was modified.
    /// </summary>
    public bool WasModified { get; private set; }

    /// <summary>
    /// Initializes a new instance of the SatelliteEditDialog class.
    /// </summary>
    /// <param name="satellite">The satellite configuration to edit.</param>
    public SatelliteEditDialog(SatelliteConfig satellite)
    {
        _satellite = satellite ?? throw new ArgumentNullException(nameof(satellite));
        _selectedColor = satellite.Color ?? "#FFFFFF";
        _colorScheme = ThemeManager.GetCurrentColorScheme();

        InitializeComponent();
        ApplyTheme();
        InitializeControls();
        LoadSatelliteData();
    }

    /// <summary>
    /// Initializes the form component settings.
    /// </summary>
    private void InitializeComponent()
    {
        this.Text = "Edit Satellite";
        this.Size = new Size(420, 400);
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
        var labelWidth = 100;
        var controlLeft = padding + labelWidth + 5;
        var controlWidth = this.ClientSize.Width - controlLeft - padding;

        // Header
        var headerLabel = new Label
        {
            Text = "Edit Satellite Settings",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 28),
            ForeColor = _colorScheme.AccentColor
        };
        this.Controls.Add(headerLabel);
        currentY += 40;

        // Satellite info (read-only)
        _infoLabel = new Label
        {
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 35),
            ForeColor = _colorScheme.SecondaryTextColor,
            Font = new Font("Segoe UI", 8F)
        };
        this.Controls.Add(_infoLabel);
        currentY += 45;

        // Name
        var nameLabel = new Label
        {
            Text = "Display Name:",
            Location = new Point(padding, currentY + 3),
            Size = new Size(labelWidth, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        this.Controls.Add(nameLabel);

        _nameTextBox = new TextBox
        {
            Location = new Point(controlLeft, currentY),
            Size = new Size(controlWidth, 23),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor
        };
        this.Controls.Add(_nameTextBox);
        currentY += 35;

        // Enabled checkbox
        _enabledCheckBox = new CheckBox
        {
            Text = "Enable tracking for this satellite",
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        this.Controls.Add(_enabledCheckBox);
        currentY += 30;

        // Show orbit checkbox
        _showOrbitCheckBox = new CheckBox
        {
            Text = "Show orbital path on map",
            Location = new Point(padding, currentY),
            Size = new Size(this.ClientSize.Width - 2 * padding, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        this.Controls.Add(_showOrbitCheckBox);
        currentY += 40;

        // Color selection
        var colorLabel = new Label
        {
            Text = "Trail Color:",
            Location = new Point(padding, currentY + 3),
            Size = new Size(labelWidth, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        this.Controls.Add(colorLabel);

        _colorPreviewPanel = new Panel
        {
            Location = new Point(controlLeft, currentY),
            Size = new Size(30, 25),
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(_colorPreviewPanel);

        _colorButton = new Button
        {
            Text = "Change...",
            Location = new Point(controlLeft + 40, currentY),
            Size = new Size(80, 25),
            BackColor = _colorScheme.ButtonBackColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            FlatStyle = FlatStyle.Flat
        };
        _colorButton.FlatAppearance.BorderSize = 1;
        _colorButton.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        _colorButton.Click += OnColorButtonClick;
        this.Controls.Add(_colorButton);

        // Quick color buttons
        var quickColorX = controlLeft + 130;
        foreach (var hexColor in SatellitePreset.DefaultColorPalette.Take(6))
        {
            var quickColorBtn = new Panel
            {
                Location = new Point(quickColorX, currentY + 2),
                Size = new Size(20, 20),
                BackColor = ParseHexColor(hexColor),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.Hand
            };
            var colorValue = hexColor; // Capture for closure
            quickColorBtn.Click += (s, e) => SelectColor(colorValue);
            this.Controls.Add(quickColorBtn);
            quickColorX += 25;
        }

        currentY += 40;

        // Icon path
        var iconLabel = new Label
        {
            Text = "Custom Icon:",
            Location = new Point(padding, currentY + 3),
            Size = new Size(labelWidth, 23),
            ForeColor = _colorScheme.PrimaryTextColor
        };
        this.Controls.Add(iconLabel);

        _iconPathTextBox = new TextBox
        {
            Location = new Point(controlLeft, currentY),
            Size = new Size(controlWidth - 130, 23),
            BackColor = _colorScheme.SurfaceColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            ReadOnly = true
        };
        this.Controls.Add(_iconPathTextBox);

        _browseIconButton = new Button
        {
            Text = "Browse",
            Location = new Point(this.ClientSize.Width - padding - 120, currentY),
            Size = new Size(60, 25),
            BackColor = _colorScheme.ButtonBackColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            FlatStyle = FlatStyle.Flat
        };
        _browseIconButton.FlatAppearance.BorderSize = 1;
        _browseIconButton.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        _browseIconButton.Click += OnBrowseIconClick;
        this.Controls.Add(_browseIconButton);

        _clearIconButton = new Button
        {
            Text = "Clear",
            Location = new Point(this.ClientSize.Width - padding - 55, currentY),
            Size = new Size(55, 25),
            BackColor = _colorScheme.ButtonBackColor,
            ForeColor = _colorScheme.PrimaryTextColor,
            FlatStyle = FlatStyle.Flat
        };
        _clearIconButton.FlatAppearance.BorderSize = 1;
        _clearIconButton.FlatAppearance.BorderColor = _colorScheme.BorderColor;
        _clearIconButton.Click += (s, e) => { _iconPathTextBox.Text = ""; };
        this.Controls.Add(_clearIconButton);

        currentY += 35;

        var iconHelpLabel = new Label
        {
            Text = "Leave empty to use default satellite marker",
            Location = new Point(controlLeft, currentY),
            Size = new Size(controlWidth, 15),
            ForeColor = _colorScheme.SecondaryTextColor,
            Font = new Font("Segoe UI", 8F)
        };
        this.Controls.Add(iconHelpLabel);

        // Buttons at bottom
        currentY = this.ClientSize.Height - 50;

        _saveButton = new Button
        {
            Text = "Save Changes",
            Location = new Point(this.ClientSize.Width - padding - 190, currentY),
            Size = new Size(95, 30),
            BackColor = _colorScheme.AccentColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += OnSaveClick;
        this.Controls.Add(_saveButton);

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(this.ClientSize.Width - padding - 85, currentY),
            Size = new Size(70, 30),
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
    /// Loads the satellite data into the form controls.
    /// </summary>
    private void LoadSatelliteData()
    {
        _nameTextBox.Text = _satellite.Name;
        _enabledCheckBox.Checked = _satellite.Enabled;
        _showOrbitCheckBox.Checked = _satellite.ShowOrbit;
        _iconPathTextBox.Text = _satellite.IconPath ?? "";

        _infoLabel.Text = $"NORAD ID: {_satellite.NoradId}  |  Category: {_satellite.Category.ToDisplayString()}" +
                          (_satellite.IsLagrangePoint ? "\nWarning: This satellite is at a Lagrange point" : "");

        if (_satellite.IsLagrangePoint)
        {
            _infoLabel.ForeColor = _colorScheme.WarningColor;
        }

        SelectColor(_selectedColor);
    }

    /// <summary>
    /// Selects a color and updates the preview.
    /// </summary>
    private void SelectColor(string hexColor)
    {
        _selectedColor = hexColor;
        _colorPreviewPanel.BackColor = ParseHexColor(hexColor);
    }

    /// <summary>
    /// Handler for color button click.
    /// </summary>
    private void OnColorButtonClick(object? sender, EventArgs e)
    {
        using var colorDialog = new ColorDialog
        {
            Color = _colorPreviewPanel.BackColor,
            FullOpen = true
        };

        if (colorDialog.ShowDialog() == DialogResult.OK)
        {
            var color = colorDialog.Color;
            _selectedColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            _colorPreviewPanel.BackColor = color;
        }
    }

    /// <summary>
    /// Handler for browse icon button click.
    /// </summary>
    private void OnBrowseIconClick(object? sender, EventArgs e)
    {
        using var openDialog = new OpenFileDialog
        {
            Title = "Select Satellite Icon",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*",
            FilterIndex = 1
        };

        if (openDialog.ShowDialog() == DialogResult.OK)
        {
            _iconPathTextBox.Text = openDialog.FileName;
        }
    }

    /// <summary>
    /// Handler for save button click.
    /// </summary>
    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            MessageBox.Show("Please enter a display name for the satellite.",
                "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _satellite.Name = _nameTextBox.Text.Trim();
        _satellite.Enabled = _enabledCheckBox.Checked;
        _satellite.ShowOrbit = _showOrbitCheckBox.Checked;
        _satellite.Color = _selectedColor;
        _satellite.IconPath = string.IsNullOrWhiteSpace(_iconPathTextBox.Text) ? null : _iconPathTextBox.Text;

        var configManager = SatelliteConfigManager.Instance;
        if (configManager.UpdateSatellite(_satellite))
        {
            WasModified = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        else
        {
            MessageBox.Show("Failed to save satellite configuration.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Parses a hex color string to a Color value.
    /// </summary>
    private static Color ParseHexColor(string hexColor)
    {
        try
        {
            if (string.IsNullOrEmpty(hexColor))
                return Color.White;

            var hex = hexColor.TrimStart('#');

            if (hex.Length == 3)
            {
                hex = $"{hex[0]}{hex[0]}{hex[1]}{hex[1]}{hex[2]}{hex[2]}";
            }

            if (hex.Length != 6)
                return Color.White;

            var r = Convert.ToInt32(hex.Substring(0, 2), 16);
            var g = Convert.ToInt32(hex.Substring(2, 2), 16);
            var b = Convert.ToInt32(hex.Substring(4, 2), 16);

            return Color.FromArgb(r, g, b);
        }
        catch
        {
            return Color.White;
        }
    }
}
