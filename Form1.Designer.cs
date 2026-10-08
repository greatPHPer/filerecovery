namespace FileRecoveryApp;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    private Label lblSourceDir;
    private TextBox txtSourceDir;
    private Button btnBrowseSource;
    private Label lblTargetDir;
    private TextBox txtTargetDir;
    private Button btnBrowseTarget;
    private Button btnScan;
    private Button btnRecoverSelected;
    private Label lblStatus;
    private ProgressBar progressBar;
    private ListBox lstFiles;
    private CheckBox chkUSNJournal;
    private CheckBox chkRecycleBin;
    private CheckBox chkRawDisk;
    private CheckBox chkFullDiskScan;
    private CheckBox chkScanEntireDrive;
    private Label lblScanOptions;
    private TextBox txtSearchText;
    private Label lblSearchText;
    private Button btnEnableUSN;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        lblSourceDir = new Label();
        txtSourceDir = new TextBox();
        btnBrowseSource = new Button();
        lblTargetDir = new Label();
        txtTargetDir = new TextBox();
        btnBrowseTarget = new Button();
        lblStatus = new Label();
        progressBar = new ProgressBar();
        lstFiles = new ListBox();
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 600);
        Text = "File Recovery Tool";
        
        // lblSourceDir
        lblSourceDir.AutoSize = true;
        lblSourceDir.Location = new Point(20, 20);
        lblSourceDir.Name = "lblSourceDir";
        lblSourceDir.Size = new Size(150, 20);
        lblSourceDir.Text = "Source Directory:";
        
        // txtSourceDir
        txtSourceDir.Location = new Point(20, 45);
        txtSourceDir.Name = "txtSourceDir";
        txtSourceDir.Size = new Size(550, 27);
        txtSourceDir.Text = "";
        
        // btnBrowseSource
        btnBrowseSource.Location = new Point(580, 45);
        btnBrowseSource.Name = "btnBrowseSource";
        btnBrowseSource.Size = new Size(100, 30);
        btnBrowseSource.Text = "Browse...";
        btnBrowseSource.Click += new EventHandler(btnBrowseSource_Click);
        
        // lblTargetDir
        lblTargetDir.AutoSize = true;
        lblTargetDir.Location = new Point(20, 85);
        lblTargetDir.Name = "lblTargetDir";
        lblTargetDir.Size = new Size(150, 20);
        lblTargetDir.Text = "Target Directory:";
        
        // txtTargetDir
        txtTargetDir.Location = new Point(20, 110);
        txtTargetDir.Name = "txtTargetDir";
        txtTargetDir.Size = new Size(550, 27);
        txtTargetDir.Text = "";
        
        // btnBrowseTarget
        btnBrowseTarget.Location = new Point(580, 110);
        btnBrowseTarget.Name = "btnBrowseTarget";
        btnBrowseTarget.Size = new Size(100, 30);
        btnBrowseTarget.Text = "Browse...";
        btnBrowseTarget.Click += new EventHandler(btnBrowseTarget_Click);
        
        // btnScan
        btnScan = new Button();
        btnScan.Location = new Point(20, 155);
        btnScan.Name = "btnScan";
        btnScan.Size = new Size(150, 40);
        btnScan.Text = "Scan for Files";
        btnScan.Click += new EventHandler(btnScan_Click);
        
        // btnRecoverSelected
        btnRecoverSelected = new Button();
        btnRecoverSelected.Location = new Point(180, 155);
        btnRecoverSelected.Name = "btnRecoverSelected";
        btnRecoverSelected.Size = new Size(150, 40);
        btnRecoverSelected.Text = "Recover Selected";
        btnRecoverSelected.Click += new EventHandler(btnRecoverSelected_Click);
        btnRecoverSelected.Enabled = false;
        
        // lblScanOptions
        lblScanOptions = new Label();
        lblScanOptions.AutoSize = true;
        lblScanOptions.Location = new Point(20, 205);
        lblScanOptions.Name = "lblScanOptions";
        lblScanOptions.Size = new Size(200, 20);
        lblScanOptions.Text = "Scan Options:";
        
        // chkUSNJournal
        chkUSNJournal = new CheckBox();
        chkUSNJournal.AutoSize = true;
        chkUSNJournal.Location = new Point(20, 230);
        chkUSNJournal.Name = "chkUSNJournal";
        chkUSNJournal.Size = new Size(200, 24);
        chkUSNJournal.Text = "USN Journal (Shift+Delete)";
        chkUSNJournal.Checked = true;
        
        // chkRecycleBin
        chkRecycleBin = new CheckBox();
        chkRecycleBin.AutoSize = true;
        chkRecycleBin.Location = new Point(250, 230);
        chkRecycleBin.Name = "chkRecycleBin";
        chkRecycleBin.Size = new Size(150, 24);
        chkRecycleBin.Text = "Recycle Bin";
        chkRecycleBin.Checked = true;
        
        // chkRawDisk
        chkRawDisk = new CheckBox();
        chkRawDisk.AutoSize = true;
        chkRawDisk.Location = new Point(420, 230);
        chkRawDisk.Name = "chkRawDisk";
        chkRawDisk.Size = new Size(200, 24);
        chkRawDisk.Text = "Raw Disk Scan (Deep)";
        chkRawDisk.Checked = true;
        
        // chkFullDiskScan
        chkFullDiskScan = new CheckBox();
        chkFullDiskScan.AutoSize = true;
        chkFullDiskScan.Location = new Point(20, 260);
        chkFullDiskScan.Name = "chkFullDiskScan";
        chkFullDiskScan.Size = new Size(200, 24);
        chkFullDiskScan.Text = "Full Disk Scan (Raw)";
        chkFullDiskScan.Checked = false;
        
        // chkScanEntireDrive
        chkScanEntireDrive = new CheckBox();
        chkScanEntireDrive.AutoSize = true;
        chkScanEntireDrive.Location = new Point(230, 260);
        chkScanEntireDrive.Name = "chkScanEntireDrive";
        chkScanEntireDrive.Size = new Size(200, 24);
        chkScanEntireDrive.Text = "Scan Entire Drive (USN)";
        chkScanEntireDrive.Checked = false;
        
        // lblSearchText
        lblSearchText = new Label();
        lblSearchText.AutoSize = true;
        lblSearchText.Location = new Point(20, 290);
        lblSearchText.Name = "lblSearchText";
        lblSearchText.Size = new Size(200, 20);
        lblSearchText.Text = "Search Text (optional):";
        
        // txtSearchText
        txtSearchText = new TextBox();
        txtSearchText.Location = new Point(20, 315);
        txtSearchText.Name = "txtSearchText";
        txtSearchText.Size = new Size(450, 27);
        txtSearchText.Text = "";
        
        // btnEnableUSN
        btnEnableUSN = new Button();
        btnEnableUSN.Location = new Point(480, 315);
        btnEnableUSN.Name = "btnEnableUSN";
        btnEnableUSN.Size = new Size(120, 30);
        btnEnableUSN.Text = "Enable USN";
        btnEnableUSN.Click += new EventHandler(btnEnableUSN_Click);
        
        // lblStatus
        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(20, 350);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(100, 20);
        lblStatus.Text = "Status: Ready";
        
        // progressBar
        progressBar.Location = new Point(20, 375);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(760, 30);
        
        // lstFiles
        lstFiles.Location = new Point(20, 415);
        lstFiles.Name = "lstFiles";
        lstFiles.Size = new Size(760, 150);
        lstFiles.SelectionMode = SelectionMode.MultiExtended;
        
        Controls.Add(lblSourceDir);
        Controls.Add(txtSourceDir);
        Controls.Add(btnBrowseSource);
        Controls.Add(lblTargetDir);
        Controls.Add(txtTargetDir);
        Controls.Add(btnBrowseTarget);
        Controls.Add(btnScan);
        Controls.Add(btnRecoverSelected);
        Controls.Add(lblScanOptions);
        Controls.Add(chkUSNJournal);
        Controls.Add(chkRecycleBin);
        Controls.Add(chkRawDisk);
        Controls.Add(chkFullDiskScan);
        Controls.Add(chkScanEntireDrive);
        Controls.Add(lblSearchText);
        Controls.Add(txtSearchText);
        Controls.Add(btnEnableUSN);
        Controls.Add(lblStatus);
        Controls.Add(progressBar);
        Controls.Add(lstFiles);
    }

    #endregion
}
