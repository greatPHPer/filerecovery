# File Recovery Tool

A C# Windows Forms application for recovering deleted files from a directory.

## Features

- Select source directory where files were deleted (including Recycle Bin location)
- Select target directory to recover files to
- **Multiple Scan Methods:**
  - **Existing Files**: Scan for files still present on disk
  - **USN Journal**: Detect files deleted with Shift+Delete (requires Admin)
  - **Recycle Bin**: Scan and recover files from Windows Recycle Bin
  - **Raw Disk Scan**: Deep scan disk sectors for deleted file signatures
- View list of recoverable files with file sizes
- Files marked by source:
  - `[EXISTING]` - Files still on disk
  - `[USN-DELETED]` - Files deleted with Shift+Delete
  - `[RECYCLE]` - Files in Recycle Bin
  - `[RAW-DISK]` - Files recovered via raw disk scan
- Select specific files to recover (multi-select supported)
- Recover selected files to the target location
- Progress tracking and status updates
- Configurable scan options (enable/disable each method)

## Requirements

- .NET 8.0 SDK or later
- Windows operating system

## Building the Project

```bash
dotnet build FileRecovery.slnx
```

## Running the Application

```bash
dotnet run --project FileRecovery.csproj
```

Or build and run the executable from the output directory:
```
bin\Debug\net8.0-windows\FileRecovery.exe
```

## Usage

1. **Run as Administrator** - Right-click and select "Run as Administrator" for best results (required for USN Journal and Raw Disk Scan)
2. Launch the application
3. Click "Browse..." next to Source Directory to select the directory where files were deleted
4. Click "Browse..." next to Target Directory to select where recovered files should be saved
5. **Configure Scan Options:**
   - ✅ **USN Journal (Shift+Delete)** - Detect recently deleted files via USN Journal
   - ✅ **Recycle Bin** - Scan Windows Recycle Bin for recoverable files
   - ☐ **Raw Disk Scan (Deep)** - Deep scan disk sectors (slower but more thorough)
6. Click "Scan for Files" to scan using selected methods
7. Files will be marked by their source:
   - `[EXISTING]` - Files that still exist on disk
   - `[USN-DELETED]` - Files deleted with Shift+Delete
   - `[RECYCLE]` - Files in Recycle Bin
   - `[RAW-DISK]` - Files found via raw disk scan
8. Select one or more files from the list (use Ctrl+Click for multiple selection)
9. Click "Recover Selected" to recover the selected files to the target directory
10. View the progress and recovery status in the application window

## Important Notes

- This tool can recover files that still exist on disk (files that haven't been overwritten)
- **USN Journal Detection**:
  - Requires Administrator privileges
  - Works best for recently deleted files (journal may be pruned over time)
  - Success depends on whether the data has been overwritten on disk
  - If USN Journal is not available, the tool will continue with other methods
- **Recycle Bin Recovery**:
  - Scans the Windows Recycle Bin on the selected drive
  - Can recover files that were deleted normally (not Shift+Delete)
  - Reads Recycle Bin metadata to determine original file paths
- **Raw Disk Scan**:
  - Deep scan of disk sectors looking for file signatures
  - Can recover files even if file system entries are gone
  - Slower process (scans disk sectors)
  - Success depends on whether data has been overwritten
  - Supports common file types: JPG, PNG, PDF, DOC, ZIP, MP3, MP4, etc.
- **Best Practice**: Use all scan methods for maximum recovery chance
- This is a powerful recovery tool - for complex recovery scenarios, this approach rivals commercial software

## Project Structure

- `FileRecovery.csproj` - Project file
- `Form1.cs` - Main form logic with file recovery implementation
- `Form1.Designer.cs` - Windows Forms designer code
- `Program.cs` - Application entry point
- `FileRecovery.slnx` - Solution file

## Limitations

- This is a basic file recovery tool that copies existing files
- It does not perform low-level disk analysis or raw data recovery
- For Shift+Delete files or permanently deleted files, specialized recovery software is recommended
- Success depends on whether the data has been overwritten on disk
