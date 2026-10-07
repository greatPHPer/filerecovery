using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace FileRecoveryApp;

public class RecycleBinScanner
{
    #region Windows API Structures

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHQUERYRBINFO
    {
        public long cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    #endregion

    #region Windows API Imports

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    #endregion

    public class RecycleBinFileInfo
    {
        public string FileName { get; set; }
        public string OriginalPath { get; set; }
        public long FileSize { get; set; }
        public DateTime DeleteDate { get; set; }
        public string RecycleBinPath { get; set; }
    }

    public List<RecycleBinFileInfo> ScanRecycleBin(string driveLetter)
    {
        List<RecycleBinFileInfo> recycleFiles = new List<RecycleBinFileInfo>();

        try
        {
            string recycleBinPath = Path.Combine(driveLetter + @"\", "$Recycle.Bin");
            
            if (!Directory.Exists(recycleBinPath))
            {
                return recycleFiles;
            }

            // Get all user directories in Recycle Bin
            string[] userDirs = Directory.GetDirectories(recycleBinPath);

            foreach (string userDir in userDirs)
            {
                try
                {
                    // Look for $I files (info files) and $R files (actual file data)
                    string[] infoFiles = Directory.GetFiles(userDir, "$I*");
                    
                    foreach (string infoFile in infoFiles)
                    {
                        try
                        {
                            string actualFile = infoFile.Replace("$I", "$R");
                            
                            if (File.Exists(actualFile))
                            {
                                // Read the info file to get original path
                                string originalPath = "";
                                DateTime deleteDate = DateTime.Now;
                                
                                try
                                {
                                    using (BinaryReader reader = new BinaryReader(File.OpenRead(infoFile)))
                                    {
                                        // Skip header (usually 8 bytes)
                                        reader.ReadBytes(8);
                                        
                                        // Read original path (unicode string)
                                        int pathLength = 0;
                                        for (int i = 0; i < 520; i += 2)
                                        {
                                            ushort ch = reader.ReadUInt16();
                                            if (ch == 0) break;
                                            pathLength += 2;
                                        }
                                        
                                        reader.BaseStream.Position = 8;
                                        byte[] pathBytes = reader.ReadBytes(pathLength);
                                        originalPath = System.Text.Encoding.Unicode.GetString(pathBytes);
                                        
                                        // Read file size (at offset 520)
                                        reader.BaseStream.Position = 520;
                                        long fileSize = reader.ReadInt64();
                                        
                                        // Read delete time (at offset 528)
                                        reader.BaseStream.Position = 528;
                                        long fileTime = reader.ReadInt64();
                                        deleteDate = DateTime.FromFileTime(fileTime);
                                    }
                                }
                                catch
                                {
                                    // If we can't read the info file, use the filename
                                    originalPath = Path.GetFileName(actualFile);
                                }

                                FileInfo fileInfo = new FileInfo(actualFile);
                                
                                recycleFiles.Add(new RecycleBinFileInfo
                                {
                                    FileName = Path.GetFileName(originalPath),
                                    OriginalPath = originalPath,
                                    FileSize = fileInfo.Length,
                                    DeleteDate = deleteDate,
                                    RecycleBinPath = actualFile
                                });
                            }
                        }
                        catch
                        {
                            continue;
                        }
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
            throw new Exception($"Recycle Bin scan failed: {ex.Message}", ex);
        }

        return recycleFiles;
    }

    public bool RecoverFromRecycleBin(RecycleBinFileInfo file, string targetPath)
    {
        try
        {
            if (File.Exists(file.RecycleBinPath))
            {
                string destPath = Path.Combine(targetPath, file.FileName);
                File.Copy(file.RecycleBinPath, destPath, true);
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}
