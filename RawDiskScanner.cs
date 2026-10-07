using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FileRecoveryApp;

public class RawDiskScanner
{
    #region Windows API Structures and Constants

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_NO_BUFFERING = 0x20000000;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(
        IntPtr hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFilePointerEx(
        IntPtr hFile,
        long liDistanceToMove,
        out long lpNewFilePointer,
        uint dwMoveMethod);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint FILE_BEGIN = 0;

    #endregion

    // File signatures (magic bytes)
    private static readonly Dictionary<string, byte[]> FileSignatures = new Dictionary<string, byte[]>
    {
        { ".jpg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        { ".gif", new byte[] { 0x47, 0x49, 0x46, 0x38 } },
        { ".bmp", new byte[] { 0x42, 0x4D } },
        { ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 } },
        { ".zip", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        { ".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        { ".xlsx", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
        { ".doc", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        { ".xls", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 } },
        { ".mp3", new byte[] { 0xFF, 0xFB } },
        { ".mp4", new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 } },
        { ".avi", new byte[] { 0x52, 0x49, 0x46, 0x46 } },
        { ".exe", new byte[] { 0x4D, 0x5A } },
        { ".txt", new byte[] { 0xEF, 0xBB, 0xBF } }, // UTF-8 BOM
    };

    public class RecoveredFileInfo
    {
        public string FileType { get; set; }
        public long StartSector { get; set; }
        public long EstimatedSize { get; set; }
        public string SuggestedName { get; set; }
    }

    public List<RecoveredFileInfo> ScanRawDisk(string driveLetter, long maxSectorsToScan = 100000, string searchText = null)
    {
        List<RecoveredFileInfo> recoveredFiles = new List<RecoveredFileInfo>();

        try
        {
            string volumePath = $"\\\\.\\{driveLetter}";
            
            IntPtr hDisk = CreateFile(
                volumePath,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_NO_BUFFERING,
                IntPtr.Zero);

            if (hDisk.ToInt32() == -1)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Failed to open disk. Run as Administrator.");
            }

            try
            {
                const int sectorSize = 512;
                const int bufferSize = sectorSize * 1024; // Read 512 sectors at a time
                byte[] buffer = new byte[bufferSize];
                long currentSector = 0;
                long sectorsScanned = 0;

                while (sectorsScanned < maxSectorsToScan)
                {
                    uint bytesRead;
                    bool result = ReadFile(hDisk, buffer, (uint)bufferSize, out bytesRead, IntPtr.Zero);

                    if (!result || bytesRead == 0)
                        break;

                    // Scan buffer for file signatures
                    for (int i = 0; i < bytesRead - 16; i++)
                    {
                        foreach (var signature in FileSignatures)
                        {
                            if (MatchSignature(buffer, i, signature.Value))
                            {
                                // Found a potential file
                                recoveredFiles.Add(new RecoveredFileInfo
                                {
                                    FileType = signature.Key,
                                    StartSector = currentSector + (i / sectorSize),
                                    EstimatedSize = EstimateFileSize(buffer, i, signature.Key),
                                    SuggestedName = $"recovered_{signature.Key}_{currentSector}_{i}{signature.Key}"
                                });
                            }
                        }
                    }

                    // Search for specific text if provided
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        string bufferText = Encoding.ASCII.GetString(buffer);
                        int textIndex = bufferText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                        if (textIndex >= 0)
                        {
                            recoveredFiles.Add(new RecoveredFileInfo
                            {
                                FileType = ".txt",
                                StartSector = currentSector + (textIndex / sectorSize),
                                EstimatedSize = 4096, // Default 4KB for text files
                                SuggestedName = $"recovered_text_{currentSector}_{textIndex}.txt"
                            });
                        }
                    }

                    currentSector += (bytesRead / sectorSize);
                    sectorsScanned += (bytesRead / sectorSize);
                }
            }
            finally
            {
                CloseHandle(hDisk);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Raw disk scan failed: {ex.Message}", ex);
        }

        return recoveredFiles;
    }

    private bool MatchSignature(byte[] buffer, int offset, byte[] signature)
    {
        if (offset + signature.Length > buffer.Length)
            return false;

        for (int i = 0; i < signature.Length; i++)
        {
            if (buffer[offset + i] != signature[i])
                return false;
        }

        return true;
    }

    private long EstimateFileSize(byte[] buffer, int offset, string fileType)
    {
        // This is a simplified estimation
        // Real implementation would parse file-specific structures
        
        switch (fileType.ToLower())
        {
            case ".jpg":
            case ".jpeg":
                // JPEG - could be any size, estimate based on common sizes
                return 1024 * 1024; // 1MB default
            case ".png":
                // PNG - read IHDR chunk for exact size
                if (offset + 24 < buffer.Length)
                {
                    int width = BitConverter.ToInt32(buffer, offset + 16);
                    int height = BitConverter.ToInt32(buffer, offset + 20);
                    return width * height * 4 + 100; // Rough estimate
                }
                return 1024 * 512;
            case ".pdf":
                // PDF - scan for %%EOF marker
                return 1024 * 500;
            case ".zip":
            case ".docx":
            case ".xlsx":
                // ZIP-based formats - can read end of central directory
                return 1024 * 1024;
            default:
                return 1024 * 512; // 512KB default
        }
    }

    public bool RecoverRawFile(string driveLetter, RecoveredFileInfo file, string targetPath)
    {
        try
        {
            string volumePath = $"\\\\.\\{driveLetter}";
            
            IntPtr hDisk = CreateFile(
                volumePath,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_NO_BUFFERING,
                IntPtr.Zero);

            if (hDisk.ToInt32() == -1)
                return false;

            try
            {
                const int sectorSize = 512;
                long byteOffset = file.StartSector * sectorSize;
                
                SetFilePointerEx(hDisk, byteOffset, out long _, FILE_BEGIN);

                byte[] buffer = new byte[file.EstimatedSize];
                uint bytesRead;
                
                bool result = ReadFile(hDisk, buffer, (uint)file.EstimatedSize, out bytesRead, IntPtr.Zero);

                if (result && bytesRead > 0)
                {
                    string destPath = Path.Combine(targetPath, file.SuggestedName);
                    File.WriteAllBytes(destPath, buffer);
                    return true;
                }

                return false;
            }
            finally
            {
                CloseHandle(hDisk);
            }
        }
        catch
        {
            return false;
        }
    }
}
