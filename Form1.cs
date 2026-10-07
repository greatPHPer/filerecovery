using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace FileRecoveryApp;

public partial class Form1 : Form
{
    private List<string> scannedFiles = new List<string>();
    private List<UsnJournalScanner.DeletedFileInfo> deletedFiles = new List<UsnJournalScanner.DeletedFileInfo>();

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
        deletedFiles.Clear();
        progressBar.Value = 0;
        btnRecoverSelected.Enabled = false;
        Application.DoEvents();

        try
        {
            // Scan for existing files
            scannedFiles = ScanForRecoverableFiles(txtSourceDir.Text);
            
            // Scan for deleted files using USN Journal
            lblStatus.Text = "Status: Scanning for deleted files (requires Admin)...";
            Application.DoEvents();
            
            try
            {
                UsnJournalScanner scanner = new UsnJournalScanner();
                deletedFiles = scanner.ScanDirectoryForDeletedFiles(txtSourceDir.Text);
            }
            catch (Exception ex)
            {
                // Non-fatal error - continue with existing files only
                lblStatus.Text = "Status: USN Journal scan failed - scanning existing files only";
                Application.DoEvents();
                // Don't show error dialog - just log it
                System.Diagnostics.Debug.WriteLine($"USN Journal scan failed: {ex.Message}");
            }
            
            int totalFiles = scannedFiles.Count + deletedFiles.Count;
            
            if (totalFiles == 0)
            {
                lblStatus.Text = "Status: No recoverable files found";
                MessageBox.Show("No recoverable files found in the specified directory.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Add existing files
            foreach (string file in scannedFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                string fileSize = FormatFileSize(fileInfo.Length);
                lstFiles.Items.Add($"[EXISTING] {fileInfo.Name} ({fileSize})");
            }

            // Add deleted files
            foreach (var deletedFile in deletedFiles)
            {
                lstFiles.Items.Add($"[DELETED] {deletedFile.FileName} (Shift+Delete)");
            }

            if (deletedFiles.Count > 0)
            {
                lblStatus.Text = $"Status: Found {scannedFiles.Count} existing files, {deletedFiles.Count} deleted files. Select files to recover.";
            }
            else
            {
                lblStatus.Text = $"Status: Found {scannedFiles.Count} existing files. No deleted files detected (USN Journal may not be available or no recent deletions).";
            }
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
        int deletedSkipped = 0;

        foreach (int selectedIndex in lstFiles.SelectedIndices)
        {
            string itemText = lstFiles.Items[selectedIndex].ToString();
            
            try
            {
                if (itemText.StartsWith("[EXISTING]"))
                {
                    // Recover existing file
                    int existingIndex = GetExistingFileIndex(selectedIndex);
                    if (existingIndex >= 0 && existingIndex < scannedFiles.Count)
                    {
                        string sourceFile = scannedFiles[existingIndex];
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
                }
                else if (itemText.StartsWith("[DELETED]"))
                {
                    // Try to recover deleted file using raw sector scan
                    int deletedIndex = GetDeletedFileIndex(selectedIndex);
                    if (deletedIndex >= 0 && deletedIndex < deletedFiles.Count)
                    {
                        var deletedFile = deletedFiles[deletedIndex];
                        bool recovered = AttemptRecoverDeletedFile(deletedFile, txtTargetDir.Text);
                        
                        if (recovered)
                        {
                            recoveredCount++;
                        }
                        else
                        {
                            deletedSkipped++;
                        }
                    }
                }
            }
            catch
            {
                failedCount++;
            }

            progressBar.Value++;
            Application.DoEvents();
        }

        string message = $"Recovery complete.\n\nRecovered: {recoveredCount}\nFailed: {failedCount}";
        if (deletedSkipped > 0)
        {
            message += $"\nDeleted files (Shift+Delete): {deletedSkipped} - may require raw disk scan";
        }
        
        lblStatus.Text = $"Status: Recovery complete. {recoveredCount} files recovered.";
        MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private int GetExistingFileIndex(int listIndex)
    {
        int count = 0;
        for (int i = 0; i < listIndex; i++)
        {
            if (lstFiles.Items[i]?.ToString()?.StartsWith("[EXISTING]") ?? false)
                count++;
        }
        return count;
    }

    private int GetDeletedFileIndex(int listIndex)
    {
        int count = 0;
        for (int i = 0; i < listIndex; i++)
        {
            if (lstFiles.Items[i]?.ToString()?.StartsWith("[DELETED]") ?? false)
                count++;
        }
        return count;
    }

    private bool AttemptRecoverDeletedFile(UsnJournalScanner.DeletedFileInfo deletedFile, string targetDir)
    {
        try
        {
            // This is a placeholder for raw disk recovery
            // For now, we'll create a stub file with the original name
            // Real implementation would require raw disk sector scanning
            
            string targetPath = Path.Combine(targetDir, deletedFile.FileName);
            
            // Try to find the file in common locations first
            // This is a basic heuristic - real recovery needs raw disk access
            string dirName = Path.GetDirectoryName(deletedFile.OriginalPath) ?? "";
            string[] possiblePaths = {
                deletedFile.OriginalPath,
                Path.Combine(dirName, $"~${deletedFile.FileName}"),
                Path.Combine(dirName, deletedFile.FileName)
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    File.Copy(path, targetPath, true);
                    return true;
                }
            }

            // If file data still exists in temporary locations
            string tempPath = Path.GetTempPath();
            string[] tempFiles = Directory.GetFiles(tempPath, $"*{Path.GetFileNameWithoutExtension(deletedFile.FileName)}*", SearchOption.AllDirectories);
            
            foreach (string tempFile in tempFiles)
            {
                try
                {
                    if (new FileInfo(tempFile).Length > 0)
                    {
                        File.Copy(tempFile, targetPath, true);
                        return true;
                    }
                }
                catch
                {
                    continue;
                }
            }

            // Create a placeholder file indicating recovery not possible without raw disk access
            using (StreamWriter writer = File.CreateText(targetPath))
            {
                writer.WriteLine($"This file was deleted with Shift+Delete or force-delete.");
                writer.WriteLine($"Original path: {deletedFile.OriginalPath}");
                writer.WriteLine($"Delete time: {deletedFile.DeleteTime}");
                writer.WriteLine();
                writer.WriteLine("To recover the actual file data, raw disk sector scanning is required.");
                writer.WriteLine("This would require Administrator privileges and complex NTFS parsing.");
            }

            return false;
        }
        catch
        {
            return false;
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

    private string FormatFileSize(long bytes)
    {
        if (bytes == 0)
            return "Unknown size";
            
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
