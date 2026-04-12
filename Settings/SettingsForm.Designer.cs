using WorldMapWallpaper.Shared;

namespace WorldMapWallpaper.Settings
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // Tab Control
        private TabControl _tabControl = null!;
        private TabPage _generalTab = null!;
        private TabPage _satellitesTab = null!;
        private TabPage _aboutTab = null!;

        // General Tab Controls
        private CheckBox _issCheckBox = null!;
        private CheckBox _timeZonesCheckBox = null!;
        private CheckBox _politicalMapCheckBox = null!;
        private ComboBox _updateIntervalCombo = null!;
        private Label _taskStatusLabel = null!;
        private ComboBox _resolutionModeCombo = null!;
        private TextBox _customWidthTextBox = null!;
        private TextBox _customHeightTextBox = null!;
        private Label _detectedResolutionLabel = null!;
        private TextBox _outputFolderTextBox = null!;
        private Button _browseOutputFolderButton = null!;

        // Satellites Tab Controls
        private CheckBox _satelliteTrackingCheckBox = null!;
        private ListBox _satelliteListBox = null!;
        private Button _addSatelliteButton = null!;
        private Button _editSatelliteButton = null!;
        private Button _removeSatelliteButton = null!;
        private Button _moveUpButton = null!;
        private Button _moveDownButton = null!;
        private NumericUpDown _maxVisibleUpDown = null!;
        private Label _satelliteCountLabel = null!;

        // Bottom Buttons
        private Button _previewButton = null!;
        private Button _resetButton = null!;
        private Button _closeButton = null!;
        // Additional controls for proper designer support
        private Label _headerLabel = null!;
        private GroupBox _visualGroup = null!;
        private GroupBox _updateGroup = null!;
        private Label _intervalLabel = null!;
        private GroupBox _resolutionGroup = null!;
        private Label _resModeLabel = null!;
        private Label _customLabel = null!;
        private Label _outputFolderLabel = null!;
        private Label _xLabel = null!;
        private Label _helpLabel = null!;
        private Label _listLabel = null!;
        private Label _maxVisibleLabel = null!;
        private Label _infoLabel = null!;
        private Label _titleLabel = null!;
        private Label _versionLabel = null!;
        private Label _descLabel = null!;
        private Label _featuresLabel = null!;
        private Label _copyrightLabel = null!;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(480, 720);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.ShowIcon = true;
            this.ShowInTaskbar = true;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "World Map Wallpaper Settings";
            this.Font = new Font("Segoe UI", 9F);

            var padding = 15;

            // Header
            _headerLabel = new Label
            {
                Text = "World Map Wallpaper Settings",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Location = new Point(padding, 15),
                Size = new Size(this.ClientSize.Width - 2 * padding, 28),
                ForeColor = Color.Blue,
                BackColor = Color.Transparent
            };
            this.Controls.Add(_headerLabel);

            // Tab Control
            _tabControl = new TabControl
            {
                Location = new Point(padding, 55),
                Size = new Size(this.ClientSize.Width - 2 * padding, 490),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(_tabControl);

            var tabContentWidth = _tabControl.Width - 26;

            // Create tabs
            _generalTab = new TabPage
            {
                Text = "General",
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            _tabControl.TabPages.Add(_generalTab);

            // Visual Elements Group
            _visualGroup = new GroupBox
            {
                Text = "Visual Elements",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(5, 10),
                Size = new Size(tabContentWidth, 105),
                ForeColor = Color.Black,
                BackColor = SystemColors.Control
            };
            _generalTab.Controls.Add(_visualGroup);

            _issCheckBox = new CheckBox
            {
                Text = "Show International Space Station (legacy - see Satellites tab)",
                Location = new Point(12, 22),
                Size = new Size(_visualGroup.Width - 24, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            _visualGroup.Controls.Add(_issCheckBox);

            _timeZonesCheckBox = new CheckBox
            {
                Text = "Show time zone clocks around the world",
                Location = new Point(12, 47),
                Size = new Size(_visualGroup.Width - 24, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _visualGroup.Controls.Add(_timeZonesCheckBox);

            _politicalMapCheckBox = new CheckBox
            {
                Text = "Show political boundaries and country borders",
                Location = new Point(12, 72),
                Size = new Size(_visualGroup.Width - 24, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _visualGroup.Controls.Add(_politicalMapCheckBox);

            // Update Settings Group
            _updateGroup = new GroupBox
            {
                Text = "Update Settings",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(5, 125),
                Size = new Size(tabContentWidth, 90),
                ForeColor = Color.Black,
                BackColor = SystemColors.Control
            };
            _generalTab.Controls.Add(_updateGroup);

            _intervalLabel = new Label
            {
                Text = "Update Frequency:",
                Location = new Point(12, 25),
                Size = new Size(110, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _updateGroup.Controls.Add(_intervalLabel);

            _updateIntervalCombo = new ComboBox
            {
                Location = new Point(125, 22),
                Size = new Size(_updateGroup.Width - 140, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Window,
                ForeColor = Color.Black
            };
            _updateIntervalCombo.Items.Add(new ComboBoxItem("Every 5 minutes", UpdateInterval.Every5Minutes));
            _updateIntervalCombo.Items.Add(new ComboBoxItem("Every 10 minutes", UpdateInterval.Every10Minutes));
            _updateIntervalCombo.Items.Add(new ComboBoxItem("Every 15 minutes", UpdateInterval.Every15Minutes));
            _updateIntervalCombo.Items.Add(new ComboBoxItem("Every 30 minutes", UpdateInterval.Every30Minutes));
            _updateIntervalCombo.Items.Add(new ComboBoxItem("Every hour", UpdateInterval.Hourly));
            _updateIntervalCombo.SelectedIndex = 0;
            _updateGroup.Controls.Add(_updateIntervalCombo);

            _taskStatusLabel = new Label
            {
                Text = "Scheduled task is enabled",
                Location = new Point(12, 55),
                Size = new Size(_updateGroup.Width - 24, 25),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Green,
                BackColor = Color.Transparent
            };
            _updateGroup.Controls.Add(_taskStatusLabel);

            // Resolution Settings Group
            _resolutionGroup = new GroupBox
            {
                Text = "Resolution Settings",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(5, 225),
                Size = new Size(tabContentWidth, 160),
                ForeColor = Color.Black,
                BackColor = SystemColors.Control
            };
            _generalTab.Controls.Add(_resolutionGroup);

            _resModeLabel = new Label
            {
                Text = "Resolution Mode:",
                Location = new Point(12, 22),
                Size = new Size(105, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _resolutionGroup.Controls.Add(_resModeLabel);

            _resolutionModeCombo = new ComboBox
            {
                Location = new Point(120, 19),
                Size = new Size(_resolutionGroup.Width - 135, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Window,
                ForeColor = Color.Black
            };
            _resolutionModeCombo.Items.Add(new ComboBoxItem("Original (1920x1080)", ResolutionMode.None));
            _resolutionModeCombo.Items.Add(new ComboBoxItem("Fit to Screen", ResolutionMode.Fit));
            _resolutionModeCombo.Items.Add(new ComboBoxItem("Stretch to Screen", ResolutionMode.Stretch));
            _resolutionModeCombo.SelectedIndex = 0;
            _resolutionGroup.Controls.Add(_resolutionModeCombo);

            _detectedResolutionLabel = new Label
            {
                Text = "Detected: 1920 x 1080",
                Location = new Point(12, 48),
                Size = new Size(_resolutionGroup.Width - 24, 15),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            _resolutionGroup.Controls.Add(_detectedResolutionLabel);

            _customLabel = new Label
            {
                Text = "Custom (0=auto):",
                Location = new Point(12, 70),
                Size = new Size(105, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _resolutionGroup.Controls.Add(_customLabel);

            _customWidthTextBox = new TextBox
            {
                Location = new Point(120, 67),
                Size = new Size(65, 23),
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Window,
                ForeColor = Color.Black
            };
            _resolutionGroup.Controls.Add(_customWidthTextBox);

            _xLabel = new Label
            {
                Text = "x",
                Location = new Point(190, 70),
                Size = new Size(15, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _resolutionGroup.Controls.Add(_xLabel);

            _customHeightTextBox = new TextBox
            {
                Location = new Point(205, 67),
                Size = new Size(65, 23),
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Window,
                ForeColor = Color.Black
            };
            _resolutionGroup.Controls.Add(_customHeightTextBox);

            _helpLabel = new Label
            {
                Text = "(both 0 = auto-detect)",
                Location = new Point(280, 70),
                Size = new Size(130, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            _resolutionGroup.Controls.Add(_helpLabel);

            _outputFolderLabel = new Label
            {
                Text = "Save image in:",
                Location = new Point(12, 105),
                Size = new Size(105, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _resolutionGroup.Controls.Add(_outputFolderLabel);

            _outputFolderTextBox = new TextBox
            {
                Location = new Point(120, 102),
                Size = new Size(215, 23),
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Window,
                ForeColor = Color.Black
            };
            _resolutionGroup.Controls.Add(_outputFolderTextBox);

            _browseOutputFolderButton = new Button
            {
                Text = "Browse...",
                Location = new Point(340, 101),
                Size = new Size(75, 25),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat
            };
            _browseOutputFolderButton.FlatAppearance.BorderSize = 1;
            _browseOutputFolderButton.FlatAppearance.BorderColor = SystemColors.WindowFrame;
            _resolutionGroup.Controls.Add(_browseOutputFolderButton);

            // Preview Button
            _previewButton = new Button
            {
                Text = "Update Wallpaper Now",
                Location = new Point(padding, 610),
                Size = new Size(this.ClientSize.Width - 2 * padding, 35),
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.Blue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _previewButton.FlatAppearance.BorderSize = 0;
            this.Controls.Add(_previewButton);

            // Bottom buttons
            _resetButton = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(padding, 660),
                Size = new Size(130, 30),
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _resetButton.FlatAppearance.BorderSize = 1;
            _resetButton.FlatAppearance.BorderColor = SystemColors.WindowFrame;
            this.Controls.Add(_resetButton);

            _closeButton = new Button
            {
                Text = "Close",
                Location = new Point(this.ClientSize.Width - padding - 80, 660),
                Size = new Size(80, 30),
                Font = new Font("Segoe UI", 9F),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _closeButton.FlatAppearance.BorderSize = 1;
            _closeButton.FlatAppearance.BorderColor = SystemColors.WindowFrame;
            this.Controls.Add(_closeButton);

            // Satellites Tab
            _satellitesTab = new TabPage
            {
                Text = "Satellites",
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            _tabControl.TabPages.Add(_satellitesTab);

            // Master enable checkbox
            _satelliteTrackingCheckBox = new CheckBox
            {
                Text = "Enable satellite tracking",
                Location = new Point(10, 10),
                Size = new Size(200, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _satellitesTab.Controls.Add(_satelliteTrackingCheckBox);

            // Satellites list
            _listLabel = new Label
            {
                Text = "Configured Satellites (use Move Up/Down to reorder priority):",
                Location = new Point(10, 45),
                Size = new Size(300, 18),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _satellitesTab.Controls.Add(_listLabel);

            _satelliteListBox = new ListBox
            {
                Location = new Point(10, 67),
                Size = new Size(320, 220),
                BackColor = SystemColors.Window,
                ForeColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                SelectionMode = SelectionMode.One
            };
            _satellitesTab.Controls.Add(_satelliteListBox);

            // Side buttons
            _addSatelliteButton = new Button
            {
                Text = "Add...",
                Location = new Point(340, 67),
                Size = new Size(85, 28),
                BackColor = Color.Blue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _addSatelliteButton.FlatAppearance.BorderSize = 0;
            _satellitesTab.Controls.Add(_addSatelliteButton);

            _editSatelliteButton = new Button
            {
                Text = "Edit...",
                Location = new Point(340, 102),
                Size = new Size(85, 28),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _editSatelliteButton.FlatAppearance.BorderSize = 1;
            _editSatelliteButton.FlatAppearance.BorderColor = SystemColors.WindowFrame;
            _satellitesTab.Controls.Add(_editSatelliteButton);

            _removeSatelliteButton = new Button
            {
                Text = "Remove",
                Location = new Point(340, 137),
                Size = new Size(85, 28),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _removeSatelliteButton.FlatAppearance.BorderSize = 1;
            _removeSatelliteButton.FlatAppearance.BorderColor = SystemColors.WindowFrame;
            _satellitesTab.Controls.Add(_removeSatelliteButton);

            _moveUpButton = new Button
            {
                Text = "Move Up",
                Location = new Point(340, 187),
                Size = new Size(85, 28),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _moveUpButton.FlatAppearance.BorderSize = 1;
            _moveUpButton.FlatAppearance.BorderColor = SystemColors.WindowFrame;
            _satellitesTab.Controls.Add(_moveUpButton);

            _moveDownButton = new Button
            {
                Text = "Move Down",
                Location = new Point(340, 222),
                Size = new Size(85, 28),
                BackColor = SystemColors.Control,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            _moveDownButton.FlatAppearance.BorderSize = 1;
            _moveDownButton.FlatAppearance.BorderColor = SystemColors.WindowFrame;
            _satellitesTab.Controls.Add(_moveDownButton);

            // Max visible setting
            _maxVisibleLabel = new Label
            {
                Text = "Max visible satellites:",
                Location = new Point(10, 300),
                Size = new Size(130, 20),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _satellitesTab.Controls.Add(_maxVisibleLabel);

            _maxVisibleUpDown = new NumericUpDown
            {
                Location = new Point(145, 297),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 15,
                Value = 5,
                BackColor = SystemColors.Window,
                ForeColor = Color.Black
            };
            _satellitesTab.Controls.Add(_maxVisibleUpDown);

            _satelliteCountLabel = new Label
            {
                Text = "0 satellites configured",
                Location = new Point(215, 300),
                Size = new Size(200, 20),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            _satellitesTab.Controls.Add(_satelliteCountLabel);

            // Info label
            _infoLabel = new Label
            {
                Text = "Higher priority satellites (top of list) are shown first.\n" +
                       "Double-click a satellite to edit its settings.",
                Location = new Point(10, 332),
                Size = new Size(_satellitesTab.ClientSize.Width - 20, 35),
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            _satellitesTab.Controls.Add(_infoLabel);

            // About Tab
            _aboutTab = new TabPage
            {
                Text = "About",
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            _tabControl.TabPages.Add(_aboutTab);

            _titleLabel = new Label
            {
                Text = "World Map Wallpaper",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Location = new Point(10, 20),
                Size = new Size(400, 30),
                ForeColor = Color.Blue,
                BackColor = Color.Transparent
            };
            _aboutTab.Controls.Add(_titleLabel);

            _versionLabel = new Label
            {
                Text = "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                Font = new Font("Segoe UI", 10F),
                Location = new Point(10, 60),
                Size = new Size(400, 25),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _aboutTab.Controls.Add(_versionLabel);

            _descLabel = new Label
            {
                Text = "Dynamic wallpaper generator featuring real-time day/night cycles,\n" +
                       "time zone clocks, and satellite tracking with SGP4 orbital mechanics.",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(10, 100),
                Size = new Size(400, 45),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _aboutTab.Controls.Add(_descLabel);

            _featuresLabel = new Label
            {
                Text = "Features:\n" +
                       "  - Real-time day/night terminator visualization\n" +
                       "  - Multi-satellite tracking (ISS, Hubble, etc.)\n" +
                       "  - 24 analog clocks showing world time zones\n" +
                       "  - Political boundaries overlay\n" +
                       "  - Resolution scaling for different displays",
                Font = new Font("Segoe UI", 9F),
                Location = new Point(10, 160),
                Size = new Size(400, 120),
                ForeColor = Color.Black,
                BackColor = Color.Transparent
            };
            _aboutTab.Controls.Add(_featuresLabel);

            _copyrightLabel = new Label
            {
                Text = "Copyright 2023-2024. All rights reserved.",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(10, 300),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            _aboutTab.Controls.Add(_copyrightLabel);

            this.ResumeLayout(false);

            _issCheckBox.CheckedChanged += OnSettingChanged;
            _timeZonesCheckBox.CheckedChanged += OnSettingChanged;
            _politicalMapCheckBox.CheckedChanged += OnSettingChanged;
            _updateIntervalCombo.SelectedIndexChanged += OnUpdateIntervalChanged;
            _resolutionModeCombo.SelectedIndexChanged += OnResolutionModeChanged;
            _customWidthTextBox.TextChanged += OnCustomResolutionChanged;
            _customHeightTextBox.TextChanged += OnCustomResolutionChanged;
            _outputFolderTextBox.TextChanged += OnOutputFolderChanged;
            _browseOutputFolderButton.Click += OnBrowseOutputFolderClick;
            _satelliteTrackingCheckBox.CheckedChanged += OnSatelliteTrackingChanged;
            _satelliteListBox.SelectedIndexChanged += OnSatelliteSelectionChanged;
            _satelliteListBox.DoubleClick += OnSatelliteDoubleClick;
            _addSatelliteButton.Click += OnAddSatelliteClick;
            _editSatelliteButton.Click += OnEditSatelliteClick;
            _removeSatelliteButton.Click += OnRemoveSatelliteClick;
            _moveUpButton.Click += OnMoveUpClick;
            _moveDownButton.Click += OnMoveDownClick;
            _maxVisibleUpDown.ValueChanged += OnMaxVisibleChanged;
            _previewButton.Click += OnPreviewClick;
            _resetButton.Click += OnResetClick;
        }

        #endregion
    }
}
