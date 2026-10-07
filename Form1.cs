using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace FileRecoveryApp;

public partial class Form1 : Form
{
    private List<string> scannedFiles = new List<string>();

    public Form1()
    {
        InitializeComponent();
    }

    private void btnBrowseSource_Click(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "Select the directory where files were deleted";
            dialog.ShowNewFolderButton = false;
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtSourceDir.Text = dialog.SelectedPath;
                lstFiles.Items.Clear();
                scannedFiles.Clear();
                btnRecoverSelected.Enabled = false;
            }
        }
    }

    private void btnBrowseTarget_Click(object sender, EventArgs e)
    {
        using (FolderBrowserDialog dialog = new FolderBrowserDialog())
        {
            dialog.Description = "Select the target directory to recover files to";
            dialog.ShowNewFolderButton = true;
            
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtTargetDir.Text = dialog.SelectedPath;
            }
        }
    }

    private void btnScan_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtSourceDir.Text) || !Directory.Exists(txtSourceDir.Text))
        {
            MessageBox.Show("Please select a valid source directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        lblStatus.Text = "Status: Scanning for recoverable files...";
        lstFiles.Items.Clear();
        scannedFiles.Clear();
        progressBar.Value = 0;
        btnRecoverSelected.Enabled = false;
        Application.DoEvents();

        try
        {
            scannedFiles = ScanForRecoverableFiles(txtSourceDir.Text);
            
            if (scannedFiles.Count == 0)
            {
                lblStatus.Text = "Status: No recoverable files found";
                MessageBox.Show("No recoverable files found in the specified directory.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (string file in scannedFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                string fileSize = FormatFileSize(fileInfo.Length);
                lstFiles.Items.Add($"{fileInfo.Name} ({fileSize})");
            }

            lblStatus.Text = $"Status: Found {scannedFiles.Count} recoverable files. Select files to recover.";
            btnRecoverSelected.Enabled = true;
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Status: Error during scan";
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnRecoverSelected_Click(object sender, EventArgs e)
    {
        if (lstFiles.SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select at least one file to recover.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrEmpty(txtTargetDir.Text))
        {
            MessageBox.Show("Please select a target directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!Directory.Exists(txtTargetDir.Text))
        {
            Directory.CreateDirectory(txtTargetDir.Text);
        }

        lblStatus.Text = "Status: Recovering selected files...";
        progressBar.Value = 0;
        progressBar.Maximum = lstFiles.SelectedItems.Count;
        Application.DoEvents();

        int recoveredCount = 0;
        int failedCount = 0;

        foreach (int selectedIndex in lstFiles.SelectedIndices)
        {
            string sourceFile = scannedFiles[selectedIndex];
            
            try
            {
                string fileName = Path.GetFileName(sourceFile);
                string targetPath = Path.Combine(txtTargetDir.Text, fileName);
                
                if (File.Exists(sourceFile))
                {
                    File.Copy(sourceFile, targetPath, true);
                    recoveredCount++;
                }
                else
                {
                    failedCount++;
                }
            }
            catch
            {
                failedCount++;
            }

            progressBar.Value++;
            Application.DoEvents();
        }

        lblStatus.Text = $"Status: Recovery complete. {recoveredCount} files recovered, {failedCount} failed.";
        MessageBox.Show($"Recovery complete.\n\nRecovered: {recoveredCount}\nFailed: {failedCount}", 
                      "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private List<string> ScanForRecoverableFiles(string directoryPath)
    {
        List<string> recoverableFiles = new List<string>();
        
        try
        {
            string[] allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            foreach (string file in allFiles)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(file);
                    
                    if (fileInfo.Exists && fileInfo.Length > 0)
                    {
                        recoverableFiles.Add(file);
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error scanning directory: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        return recoverableFiles;
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }
}
