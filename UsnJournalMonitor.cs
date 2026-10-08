using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FileRecoveryApp;

public class UsnJournalMonitor
{
    private const uint FsctlQueryUsnJournal = 0x000900F4;
    private const uint FsctlReadUsnJournal = 0x000900BB;

    private const uint GenericRead = 0x80000000;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint FileShareDelete = 0x00000004;
    private const uint OpenExisting = 3;
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileReadAttributes = 0x00000080;
    private const int ErrorJournalEntryDeleted = 1177;

    private const uint UsnReasonFileDelete = 0x00000200;
    private const int UsnRecordV2MinimumLength = 60;
    private const uint FileAttributeDirectory = 0x00000010;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<ulong, string>> _parentPathCaches =
        new(StringComparer.OrdinalIgnoreCase);

    public class DeletedFileInfo
    {
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string DirectoryPath { get; set; } = "";
        public DateTime DeletedAtUtc { get; set; }
        public long? FileSizeBytes { get; set; }
        public ulong FileReferenceNumber { get; set; }
        public ulong ParentFileReferenceNumber { get; set; }
    }

    public class UsnDeletedFileRecord
    {
        public string FullPath { get; set; } = "";
        public ulong FileReferenceNumber { get; set; }
        public ulong ParentFileReferenceNumber { get; set; }
        public string FileName { get; set; } = "";
        public string Directory { get; set; } = "";
        public DateTime DeletedAtUtc { get; set; }

        public UsnDeletedFileRecord(
            string fullPath,
            ulong fileReferenceNumber,
            ulong parentFileReferenceNumber,
            string fileName,
            string directory,
            DateTime deletedAtUtc)
        {
            FullPath = fullPath;
            FileReferenceNumber = fileReferenceNumber;
            ParentFileReferenceNumber = parentFileReferenceNumber;
            FileName = fileName;
            Directory = directory;
            DeletedAtUtc = deletedAtUtc;
        }
    }

    public static class RecoveryMonitoringExclusions
    {
        public static bool IsExcludedPath(string path)
        {
            // Stub - can be expanded later
            return false;
        }
    }

    public List<DeletedFileInfo> ScanDeletedDirectoryFromUsnJournal(
        string root,
        string targetDirectory,
        bool includeSubdirectories,
        Action<string>? onProgress = null)
    {
        var volumeKey = root.TrimEnd(Path.DirectorySeparatorChar);

        using var volumeHandle = CreateFile(
            $@"\\.\\{volumeKey[..2]}",
            GenericRead,
            FileShareRead | FileShareWrite | FileShareDelete,
            IntPtr.Zero,
            OpenExisting,
            FileFlagBackupSemantics,
            IntPtr.Zero);

        if (volumeHandle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        if (!TryQueryJournal(volumeHandle, out var journal, out var queryError))
        {
            throw new Win32Exception(
                queryError,
                $"Could not query the USN journal for {volumeKey}.");
        }

        var normalizedDirectory = NormalizePath(targetDirectory)
            .TrimEnd(Path.DirectorySeparatorChar);

        var cache = _parentPathCaches.GetOrAdd(
            volumeKey,
            _ => new ConcurrentDictionary<ulong, string>());

        // Keep only the newest historical deletion for each resolved full path
        var latestResultByPath =
            new Dictionary<string, UsnDeletedFileRecord>(
                StringComparer.OrdinalIgnoreCase);

        var nextUsn = journal.FirstUsn;
        var batchesRead = 0L;
        var deleteRecordsSeen = 0L;
        var parentResolutions = 0L;
        var directoryMatches = 0L;
        var duplicatePathCollapses = 0L;

        onProgress?.Invoke($"USN scan started: scanning {targetDirectory}...");

        while (nextUsn < journal.NextUsn)
        {
            var records = ReadRecords(
                volumeHandle,
                journal.JournalId,
                nextUsn,
                out var returnedNextUsn);

            batchesRead++;

            if (returnedNextUsn <= nextUsn)
            {
                break;
            }

            foreach (var record in records)
            {
                if ((record.Reason & UsnReasonFileDelete) == 0 ||
                    (record.FileAttributes & FileAttributeDirectory) != 0 ||
                    string.IsNullOrWhiteSpace(record.FileName))
                {
                    continue;
                }

                deleteRecordsSeen++;

                var directory = cache.TryGetValue(
                    record.ParentFileReferenceNumber,
                    out var knownDirectory)
                    ? knownDirectory
                    : null;

                if (string.IsNullOrWhiteSpace(directory))
                {
                    directory = ResolveParentDirectory(
                        volumeHandle,
                        record.ParentFileReferenceNumber);

                    parentResolutions++;

                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        cache[record.ParentFileReferenceNumber] = directory;
                    }
                }

                if (string.IsNullOrWhiteSpace(directory) ||
                    !MatchesDirectory(
                        directory,
                        normalizedDirectory,
                        includeSubdirectories))
                {
                    continue;
                }

                directoryMatches++;

                var fullPath = NormalizePath(
                    Path.Combine(directory, record.FileName));

                if (RecoveryMonitoringExclusions.IsExcludedPath(fullPath))
                {
                    continue;
                }

                var candidate = new UsnDeletedFileRecord(
                    fullPath,
                    record.FileReferenceNumber,
                    record.ParentFileReferenceNumber,
                    record.FileName,
                    directory,
                    record.TimestampUtc);

                if (latestResultByPath.TryGetValue(
                        fullPath,
                        out var existing))
                {
                    if (candidate.DeletedAtUtc <= existing.DeletedAtUtc)
                    {
                        duplicatePathCollapses++;
                        continue;
                    }

                    duplicatePathCollapses++;
                }

                latestResultByPath[fullPath] = candidate;
            }

            nextUsn = returnedNextUsn;

            if (batchesRead == 1 || batchesRead % 16 == 0)
            {
                onProgress?.Invoke(
                    $"USN scan progress: batches={batchesRead:N0}, " +
                    $"matches={directoryMatches:N0}, " +
                    $"unique files={latestResultByPath.Count:N0}");
            }

            if (records.Count == 0)
            {
                break;
            }
        }

        var results = latestResultByPath.Values
            .OrderByDescending(record => record.DeletedAtUtc)
            .Select(r => new DeletedFileInfo
            {
                FileName = r.FileName,
                FullPath = r.FullPath,
                DirectoryPath = r.Directory,
                DeletedAtUtc = r.DeletedAtUtc,
                FileSizeBytes = null,
                FileReferenceNumber = r.FileReferenceNumber,
                ParentFileReferenceNumber = r.ParentFileReferenceNumber
            })
            .ToList();

        onProgress?.Invoke(
            $"USN scan complete: found {results.Count} deleted files in {targetDirectory}");

        return results;
    }

    private static bool MatchesDirectory(
        string directory,
        string targetDirectory,
        bool includeSubdirectories)
    {
        var normalizedDir = NormalizePath(directory)
            .TrimEnd(Path.DirectorySeparatorChar);

        if (string.Equals(normalizedDir, targetDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (includeSubdirectories)
        {
            return normalizedDir.StartsWith(
                targetDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        path = path.Replace('/', Path.DirectorySeparatorChar);

        if (path.StartsWith(@"\\?\\UNC\\", StringComparison.OrdinalIgnoreCase))
        {
            return @"\\" + path[8..];
        }

        if (path.StartsWith(@"\\?\\", StringComparison.OrdinalIgnoreCase))
        {
            return path[4..];
        }

        return path;
    }

    private static bool TryQueryJournal(SafeFileHandle volumeHandle, out JournalInfo journal, out int error)
    {
        journal = default;
        error = 0;
        var output = new byte[64];

        if (!DeviceIoControl(
                volumeHandle,
                FsctlQueryUsnJournal,
                null,
                0,
                output,
                (uint)output.Length,
                out var bytesReturned,
                IntPtr.Zero))
        {
            error = Marshal.GetLastWin32Error();
            if (error == 2 || error == 1178)
            {
                return false;
            }

            throw new Win32Exception(error);
        }

        if (bytesReturned < 60)
        {
            return false;
        }

        journal = new JournalInfo(
            BinaryPrimitives.ReadUInt64LittleEndian(output.AsSpan(0, 8)),
            BinaryPrimitives.ReadInt64LittleEndian(output.AsSpan(8, 8)),
            BinaryPrimitives.ReadInt64LittleEndian(output.AsSpan(16, 8)),
            BinaryPrimitives.ReadInt64LittleEndian(output.AsSpan(24, 8)));

        return true;
    }

    private static List<UsnRecord> ReadRecords(
        SafeFileHandle volumeHandle,
        ulong journalId,
        long startUsn,
        out long nextUsn)
    {
        var request = new ReadUsnJournalRequest
        {
            StartUsn = startUsn,
            ReasonMask = 0xFFFFFFFF,
            ReturnOnlyOnClose = 0,
            Timeout = 1,
            BytesToWaitFor = 1,
            UsnJournalId = journalId,
            MinMajorVersion = 2,
            MaxMajorVersion = 2
        };

        var input = StructureToBytes(request);
        var output = new byte[1024 * 1024];
        nextUsn = startUsn;

        if (!DeviceIoControl(
                volumeHandle,
                FsctlReadUsnJournal,
                input,
                (uint)input.Length,
                output,
                (uint)output.Length,
                out var bytesReturned,
                IntPtr.Zero))
        {
            var error = Marshal.GetLastWin32Error();

            if (error == ErrorJournalEntryDeleted)
            {
                return [];
            }

            throw new Win32Exception(error);
        }

        if (bytesReturned < sizeof(long))
        {
            return [];
        }

        nextUsn = BinaryPrimitives.ReadInt64LittleEndian(output.AsSpan(0, 8));

        var records = new List<UsnRecord>();
        var offset = 8;

        while (offset + 4 <= bytesReturned)
        {
            var recordLength = BinaryPrimitives.ReadUInt32LittleEndian(output.AsSpan(offset, 4));
            if (recordLength < UsnRecordV2MinimumLength ||
                recordLength > bytesReturned - offset)
            {
                break;
            }

            var recordSpan = output.AsSpan(offset, checked((int)recordLength));
            var majorVersion = BinaryPrimitives.ReadUInt16LittleEndian(recordSpan.Slice(4, 2));
            if (majorVersion != 2)
            {
                offset += checked((int)recordLength);
                continue;
            }

            var fileReference = BinaryPrimitives.ReadUInt64LittleEndian(recordSpan.Slice(8, 8));
            var parentReference = BinaryPrimitives.ReadUInt64LittleEndian(recordSpan.Slice(16, 8));
            var timestampFileTime = BinaryPrimitives.ReadInt64LittleEndian(recordSpan.Slice(32, 8));
            var reason = BinaryPrimitives.ReadUInt32LittleEndian(recordSpan.Slice(40, 4));
            var fileAttributes = BinaryPrimitives.ReadUInt32LittleEndian(recordSpan.Slice(52, 4));
            var nameLength = BinaryPrimitives.ReadUInt16LittleEndian(recordSpan.Slice(56, 2));
            var nameOffset = BinaryPrimitives.ReadUInt16LittleEndian(recordSpan.Slice(58, 2));

            if (nameOffset + nameLength <= recordSpan.Length)
            {
                var name = System.Text.Encoding.Unicode.GetString(
                    recordSpan.Slice(nameOffset, nameLength));

                DateTime timestampUtc;
                try
                {
                    timestampUtc = DateTime.FromFileTimeUtc(timestampFileTime);
                }
                catch
                {
                    timestampUtc = DateTime.UtcNow;
                }

                records.Add(new UsnRecord(
                    fileReference,
                    parentReference,
                    reason,
                    fileAttributes,
                    name,
                    timestampUtc));
            }

            offset += checked((int)recordLength);
        }

        return records;
    }

    private static string? ResolveParentDirectory(SafeFileHandle volumeHandle, ulong parentFileReference)
    {
        var descriptor = new FileIdDescriptor
        {
            Size = (uint)Marshal.SizeOf<FileIdDescriptor>(),
            Type = 0,
            FileId = unchecked((long)parentFileReference)
        };

        var directoryHandle = OpenFileById(
            volumeHandle,
            ref descriptor,
            FileReadAttributes,
            FileShareRead | FileShareWrite | FileShareDelete,
            IntPtr.Zero,
            FileFlagBackupSemantics);

        if (directoryHandle == IntPtr.Zero ||
            directoryHandle == new IntPtr(-1))
        {
            return null;
        }

        try
        {
            var builder = new System.Text.StringBuilder(1024);
            var length = GetFinalPathNameByHandle(
                directoryHandle,
                builder,
                (uint)builder.Capacity,
                0);

            if (length == 0)
            {
                return null;
            }

            if (length >= builder.Capacity)
            {
                builder = new System.Text.StringBuilder((int)length + 1);
                length = GetFinalPathNameByHandle(
                    directoryHandle,
                    builder,
                    (uint)builder.Capacity,
                    0);
            }

            if (length == 0)
            {
                return null;
            }

            return NormalizeFinalPath(builder.ToString());
        }
        finally
        {
            CloseHandle(directoryHandle);
        }
    }

    private static string NormalizeFinalPath(string path)
    {
        if (path.StartsWith(@"\\?\\UNC\\", StringComparison.OrdinalIgnoreCase))
        {
            return @"\\" + path[8..];
        }

        if (path.StartsWith(@"\\?\\", StringComparison.OrdinalIgnoreCase))
        {
            return path[4..];
        }

        return path;
    }

    private static byte[] StructureToBytes<T>(T value) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        var bytes = new byte[size];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

        try
        {
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), fDeleteOld: false);
            return bytes;
        }
        finally
        {
            handle.Free();
        }
    }

    private readonly record struct JournalInfo(
        ulong JournalId,
        long FirstUsn,
        long NextUsn,
        long LowestValidUsn);

    private readonly record struct UsnRecord(
        ulong FileReferenceNumber,
        ulong ParentFileReferenceNumber,
        uint Reason,
        uint FileAttributes,
        string FileName,
        DateTime TimestampUtc);

    [StructLayout(LayoutKind.Sequential)]
    private struct ReadUsnJournalRequest
    {
        public long StartUsn;
        public uint ReasonMask;
        public uint ReturnOnlyOnClose;
        public ulong Timeout;
        public ulong BytesToWaitFor;
        public ulong UsnJournalId;
        public ushort MinMajorVersion;
        public ushort MaxMajorVersion;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct FileIdDescriptor
    {
        [FieldOffset(0)]
        public uint Size;

        [FieldOffset(4)]
        public int Type;

        [FieldOffset(8)]
        public long FileId;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        byte[]? lpInBuffer,
        uint nInBufferSize,
        byte[]? lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenFileById(
        SafeFileHandle hVolumeHint,
        ref FileIdDescriptor lpFileId,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwFlagsAndAttributes);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(
        IntPtr hFile,
        System.Text.StringBuilder lpszFilePath,
        uint cchFilePath,
        uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
