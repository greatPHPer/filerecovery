using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace FileRecoveryApp;

public class UsnJournalScanner
{
    #region Windows API Structures and Constants

    [StructLayout(LayoutKind.Sequential)]
    private struct USN_RECORD
    {
        public uint RecordLength;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ulong FileReferenceNumber;
        public ulong ParentFileReferenceNumber;
        public UsnRecordExtents Usn;
        public uint Reason;
        public uint FileAttributes;
        public ushort FileNameLength;
        public ushort FileNameOffset;
        public uint FileName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UsnRecordExtents
    {
        public ulong Usn;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MFT_ENUM_DATA
    {
        public long StartFileReferenceNumber;
        public ulong LowUsn;
        public ulong HighUsn;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct USN_JOURNAL_DATA
    {
        public ulong UsnJournalID;
        public ulong FirstUsn;
        public ulong NextUsn;
        public ulong LowestValidUsn;
        public ulong MaxUsn;
        public ulong MaximumSize;
        public ulong AllocationDelta;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CREATE_USN_JOURNAL_DATA
    {
        public ulong MaximumSize;
        public ulong AllocationDelta;
    }

    private const int FSCTL_ENUM_USN_DATA = 0x000900B3;
    private const int FSCTL_QUERY_USN_JOURNAL = 0x000900B4;
    private const int FSCTL_CREATE_USN_JOURNAL = 0x000900B7;
    private const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const int FILE_ATTRIBUTE_HIDDEN = 0x00000002;
    private const int FILE_ATTRIBUTE_SYSTEM = 0x00000004;

    #endregion

    #region Windows API Imports

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
    private static extern bool DeviceIoControl(
        IntPtr hDevice,
        uint dwIoControlCode,
        IntPtr lpInBuffer,
        uint nInBufferSize,
        IntPtr lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

    #endregion

    public class DeletedFileInfo
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime DeleteTime { get; set; }
        public bool IsExisting { get; set; }
        public string OriginalPath { get; set; } = "";
    }

    public List<DeletedFileInfo> ScanDirectoryForDeletedFiles(string directoryPath)
    {
        List<DeletedFileInfo> deletedFiles = new List<DeletedFileInfo>();

        try
        {
            string driveLetter = Path.GetPathRoot(directoryPath).Replace("\\", "");
            string volumePath = $"\\\\.\\{driveLetter}";

            IntPtr hVolume = CreateFile(
                volumePath,
                GENERIC_READ,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (hVolume.ToInt32() == -1)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to open volume. Run as Administrator.");
            }

            try
            {
                // Try to get USN Journal data
                USN_JOURNAL_DATA journalData = new USN_JOURNAL_DATA();
                IntPtr journalDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(journalData));
                Marshal.StructureToPtr(journalData, journalDataPtr, false);

                uint bytesReturned;
                bool result = DeviceIoControl(
                    hVolume,
                    FSCTL_QUERY_USN_JOURNAL,
                    IntPtr.Zero,
                    0,
                    journalDataPtr,
                    (uint)Marshal.SizeOf(journalData),
                    out bytesReturned,
                    IntPtr.Zero);

                if (!result)
                {
                    int error = Marshal.GetLastWin32Error();
                    
                    // If USN Journal doesn't exist, try to create it
                    if (error == 2 || error == 1168) // ERROR_FILE_NOT_FOUND or ERROR_NOT_FOUND
                    {
                        Marshal.FreeHGlobal(journalDataPtr);
                        
                        // Try to create USN Journal
                        CREATE_USN_JOURNAL_DATA createData = new CREATE_USN_JOURNAL_DATA
                        {
                            MaximumSize = 0x10000000, // 256MB
                            AllocationDelta = 0x100000 // 1MB
                        };
                        
                        IntPtr createDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(createData));
                        Marshal.StructureToPtr(createData, createDataPtr, false);
                        
                        result = DeviceIoControl(
                            hVolume,
                            FSCTL_CREATE_USN_JOURNAL,
                            createDataPtr,
                            (uint)Marshal.SizeOf(createData),
                            IntPtr.Zero,
                            0,
                            out bytesReturned,
                            IntPtr.Zero);
                        
                        Marshal.FreeHGlobal(createDataPtr);
                        
                        if (!result)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to create USN Journal. Volume may not support it.");
                        }
                        
                        // Try to query again
                        journalDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(journalData));
                        Marshal.StructureToPtr(journalData, journalDataPtr, false);
                        
                        result = DeviceIoControl(
                            hVolume,
                            FSCTL_QUERY_USN_JOURNAL,
                            IntPtr.Zero,
                            0,
                            journalDataPtr,
                            (uint)Marshal.SizeOf(journalData),
                            out bytesReturned,
                            IntPtr.Zero);
                        
                        if (!result)
                        {
                            Marshal.FreeHGlobal(journalDataPtr);
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to query USN Journal after creation.");
                        }
                    }
                    else
                    {
                        Marshal.FreeHGlobal(journalDataPtr);
                        throw new Win32Exception(error, "Failed to query USN Journal. Volume may not support it.");
                    }
                }

                journalData = (USN_JOURNAL_DATA)Marshal.PtrToStructure(journalDataPtr, typeof(USN_JOURNAL_DATA));
                Marshal.FreeHGlobal(journalDataPtr);

                // Enumerate USN records
                MFT_ENUM_DATA mftData = new MFT_ENUM_DATA
                {
                    StartFileReferenceNumber = 0,
                    LowUsn = 0,
                    HighUsn = journalData.NextUsn
                };

                IntPtr mftDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(mftData));
                Marshal.StructureToPtr(mftData, mftDataPtr, false);

                IntPtr buffer = Marshal.AllocHGlobal(65536);
                int count = 0;

                while (true)
                {
                    result = DeviceIoControl(
                        hVolume,
                        FSCTL_ENUM_USN_DATA,
                        mftDataPtr,
                        (uint)Marshal.SizeOf(mftData),
                        buffer,
                        65536,
                        out bytesReturned,
                        IntPtr.Zero);

                    if (!result || bytesReturned <= 8)
                    {
                        break;
                    }

                    IntPtr pRecord = new IntPtr(buffer.ToInt64() + 8);
                    while (true)
                    {
                        USN_RECORD record = (USN_RECORD)Marshal.PtrToStructure(pRecord, typeof(USN_RECORD));

                        if (record.RecordLength == 0)
                            break;

                        IntPtr pFileName = new IntPtr(pRecord.ToInt64() + record.FileNameOffset);
                        string fileName = Marshal.PtrToStringUni(pFileName, record.FileNameLength / 2);

                        // Check if this is a delete operation
                        if ((record.Reason & 0x00000100) != 0) // USN_REASON_FILE_DELETE
                        {
                            // Filter by directory
                            if (IsInDirectory(fileName, directoryPath, driveLetter))
                            {
                                bool isExisting = File.Exists(fileName) || Directory.Exists(fileName);
                                
                                if (!isExisting)
                                {
                                    deletedFiles.Add(new DeletedFileInfo
                                    {
                                        FileName = Path.GetFileName(fileName),
                                        FilePath = fileName,
                                        OriginalPath = fileName,
                                        FileSize = 0, // USN doesn't store file size
                                        DeleteTime = DateTime.FromFileTime((long)record.Usn.Usn),
                                        IsExisting = false
                                    });
                                }
                            }
                        }

                        pRecord = new IntPtr(pRecord.ToInt64() + record.RecordLength);
                        count++;

                        if (count > 100000) // Safety limit
                            break;
                    }

                    if (count > 100000)
                        break;

                    // Update start file reference number
                    mftData.StartFileReferenceNumber = Marshal.ReadInt64(buffer);
                    Marshal.StructureToPtr(mftData, mftDataPtr, false);
                }

                Marshal.FreeHGlobal(mftDataPtr);
                Marshal.FreeHGlobal(buffer);
            }
            finally
            {
                CloseHandle(hVolume);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"USN Journal scan failed: {ex.Message}", ex);
        }

        return deletedFiles;
    }

    private bool IsInDirectory(string filePath, string targetDirectory, string driveLetter)
    {
        try
        {
            // Normalize paths
            string normalizedTarget = targetDirectory.ToLower().TrimEnd('\\');
            string normalizedFile = filePath.ToLower().TrimEnd('\\');

            // Add drive letter if not present
            if (!normalizedFile.StartsWith(driveLetter.ToLower()))
            {
                normalizedFile = driveLetter.ToLower() + normalizedFile;
            }

            return normalizedFile.StartsWith(normalizedTarget);
        }
        catch
        {
            return false;
        }
    }
}
