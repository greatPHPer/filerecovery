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
        ClientSize = new Size(800, 500);
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
        
        // lblStatus
        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(20, 205);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(100, 20);
        lblStatus.Text = "Status: Ready";
        
        // progressBar
        progressBar.Location = new Point(20, 230);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(760, 30);
        
        // lstFiles
        lstFiles.Location = new Point(20, 270);
        lstFiles.Name = "lstFiles";
        lstFiles.Size = new Size(760, 200);
        lstFiles.SelectionMode = SelectionMode.MultiExtended;
        
        Controls.Add(lblSourceDir);
        Controls.Add(txtSourceDir);
        Controls.Add(btnBrowseSource);
        Controls.Add(lblTargetDir);
        Controls.Add(txtTargetDir);
        Controls.Add(btnBrowseTarget);
        Controls.Add(btnScan);
        Controls.Add(btnRecoverSelected);
        Controls.Add(lblStatus);
        Controls.Add(progressBar);
        Controls.Add(lstFiles);
    }

    #endregion
}
