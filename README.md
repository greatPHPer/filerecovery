# File Recovery Tool

A C# Windows Forms application for recovering deleted files from a directory.

## Features

- Select source directory where files were deleted (including Recycle Bin location)
- Select target directory to recover files to
- Scan for recoverable files in the source directory
- Recover files to the target location
- Progress tracking and status updates

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

1. Launch the application
2. Click "Browse..." next to Source Directory to select the directory where files were deleted
3. Click "Browse..." next to Target Directory to select where recovered files should be saved
4. Click "Scan & Recover Files" to begin the recovery process
5. View the progress and list of recovered files in the application window

## Important Notes

- This tool can only recover files that still exist on disk (files that haven't been overwritten)
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
