# ⌨️ MUI-Accelerator-Table-Englishizer - Use English keyboard shortcuts in Windows apps

[![Download Tool](https://img.shields.io/badge/Download-Application-blue.svg)](https://github.com/Greenishgray-lifesupport504/MUI-Accelerator-Table-Englishizer)

## 🛠 What this tool does

Windows assigns keyboard shortcuts based on the system language. If you use a version of Windows set to Spanish or another language, the standard shortcuts like Ctrl+S or Ctrl+F sometimes stop working. They change to match the translated menu options. This tool restores standard English shortcuts. It updates the accelerator tables in system files. This works for Windows Explorer, Notepad, and Wordpad. It supports Windows 10 and Windows 11.

## 📋 System requirements

- Operating System: Windows 10 or Windows 11.
- Administrative Rights: Your account needs permission to change system files.
- Backup: The tool makes a backup of your files before it proceeds.
- Space: You need less than 10 megabytes of disk space.

## 📥 How to download and install

Follow these steps to set up the tool on your computer.

1. Visit the project page to download the latest version: https://github.com/Greenishgray-lifesupport504/MUI-Accelerator-Table-Englishizer.
2. Look for the Releases section on the right side of the page.
3. Click the link for the latest version file ending in .exe.
4. Save the file to your computer.
5. Move the file to a folder where you store your applications.

## 🚀 Running the application

The tool requires access to system folders. Follow these steps to run it safely.

1. Locate the file you downloaded.
2. Right-click the file icon.
3. Select Run as administrator from the menu. This ensures the program has the power to modify the necessary system files.
4. A window appears. This is the main interface.
5. Select the target application you want to fix from the list.
6. Click the Apply button.
7. Wait for the success message. The tool shows a status bar during the process.
8. Restart your computer to apply the changes.

## 🛡 How the tool works

The Windows operating system uses files called shell32.dll and others to manage menus. These files contain "accelerator tables." An accelerator table is a list that maps keys to commands. In localized versions of Windows, these tables display letters specific to the local language. 

When you run this tool, it locates these tables within your system files. It replaces the local language keys with the standard English keys. This restores muscle memory to your workflow. You can perform tasks in NotePad or Explorer without searching for translated hotkeys.

## 🔄 Reverting changes

If you want to go back to the original Windows settings, the tool makes this simple.

1. Open the application again using administrator rights.
2. Select the Restore option from the menu.
3. Choose the backup file that the program created during the initial setup.
4. Click the Restore button.
5. The software places your original system files back into their correct location.
6. Restart your computer.

## ❓ Frequently asked questions

### Will this break my Windows installation?
No. The tool includes a safety check that looks at your system architecture. It creates a backup of the original files before it makes any changes. If an error occurs, you can use the restore function.

### Does this require an internet connection?
You need an internet connection to download the file. Once it exists on your computer, the tool works offline. It does not need to connect to the internet to perform the changes.

### Is this safe for my files?
Yes. The tool only modifies the keyboard shortcut tables in system files. It does not touch your personal documents, photos, or data.

### Can I run this on a business computer?
Check with your IT department before running tools that modify system files. Some company policies block the execution of unknown programs on work devices.

## ⚙️ Advanced troubleshooting

If the program shows an error message, verify that you have administrative rights. You cannot patch system files without full permissions. Make sure your antivirus software does not block the application. Since this program modifies system files, some security software flags the process as unusual. If this happens, you may need to add an exception for this file in your security settings.

If the shortcuts do not change after a restart, ensure that you selected the correct version of Windows in the settings menu.

## 📑 Technical details

This application uses the .NET framework. It handles the low-level manipulation of binary resources within the system files. The logic specifically targets the DLL files that manage the user interface for Windows Explorer, Notepad, and Wordpad. It relies on standard Windows libraries for file operations. The source code remains open for audit if you have an interest in the underlying mechanics of how Windows handles input signals.