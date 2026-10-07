# File Recovery Tool

A C# Windows Forms application for recovering deleted files from a directory.

## Features

- Select source directory where files were deleted (including Recycle Bin location)
- Select target directory to recover files to
- Scan for recoverable files in the source directory
- **NEW: Detect files deleted with Shift+Delete using USN Journal**
- View list of recoverable files with file sizes
- Files marked as [EXISTING] or [DELETED] for easy identification
- Select specific files to recover (multi-select supported)
- Recover selected files to the target location
- Progress tracking and status updates
- **Requires Administrator privileges for Shift+Delete detection**

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

1. **Run as Administrator** - Right-click and select "Run as Administrator" for Shift+Delete detection
2. Launch the application
3. Click "Browse..." next to Source Directory to select the directory where files were deleted
4. Click "Browse..." next to Target Directory to select where recovered files should be saved
5. Click "Scan for Files" to scan the source directory for recoverable files
6. Files will be marked as:
   - `[EXISTING]` - Files that still exist on disk
   - `[DELETED]` - Files deleted with Shift+Delete (detected via USN Journal)
7. Select one or more files from the list (use Ctrl+Click for multiple selection)
8. Click "Recover Selected" to recover the selected files to the target directory
9. View the progress and recovery status in the application window

## Important Notes

- This tool can recover files that still exist on disk (files that haven't been overwritten)
- **Shift+Delete Detection**: Uses Windows USN Journal to detect recently deleted files
  - Requires Administrator privileges
  - Works best for recently deleted files (journal may be pruned)
  - Success depends on whether the data has been overwritten on disk
  - Creates placeholder files for deleted files that cannot be recovered via simple methods
- For files deleted from Recycle Bin, you may need to select the Recycle Bin location (typically `$Recycle.Bin` folder)
- The tool scans for all existing files in the directory and subdirectories
- Files recovered will be copied to the target directory
- This is a basic recovery tool - for complex recovery scenarios, consider using specialized data recovery software

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
