using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FileRecoveryApp;

public partial class Form1 : Form
{
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

    private void btnRecover_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtSourceDir.Text) || !Directory.Exists(txtSourceDir.Text))
        {
            MessageBox.Show("Please select a valid source directory.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        lblStatus.Text = "Status: Scanning for recoverable files...";
        lstFiles.Items.Clear();
        progressBar.Value = 0;
        Application.DoEvents();

        try
        {
            List<string> recoverableFiles = ScanForRecoverableFiles(txtSourceDir.Text);
            
            if (recoverableFiles.Count == 0)
            {
                lblStatus.Text = "Status: No recoverable files found";
                MessageBox.Show("No recoverable files found in the specified directory.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            progressBar.Maximum = recoverableFiles.Count;
            int recoveredCount = 0;

            foreach (string file in recoverableFiles)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    string targetPath = Path.Combine(txtTargetDir.Text, fileName);
                    
                    if (File.Exists(file))
                    {
                        File.Copy(file, targetPath, true);
                        lstFiles.Items.Add($"Recovered: {fileName}");
                        recoveredCount++;
                    }
                }
                catch (Exception ex)
                {
                    lstFiles.Items.Add($"Failed: {Path.GetFileName(file)} - {ex.Message}");
                }

                progressBar.Value++;
                Application.DoEvents();
            }

            lblStatus.Text = $"Status: Recovery complete. {recoveredCount} files recovered.";
            MessageBox.Show($"Recovery complete. {recoveredCount} out of {recoverableFiles.Count} files recovered.", 
                          "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Status: Error during recovery";
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
}
