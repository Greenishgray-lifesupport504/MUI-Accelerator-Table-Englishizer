
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

Imports DevCase.Core.Security.DataIntegrity.Checksum

Imports Microsoft.VisualBasic

Imports Microsoft.Win32

#End Region

#Region " Program "

''' <summary>
''' Represents the main application execution context.
''' </summary>
Module Program

#Region " Fields "

    ''' <summary>
    ''' The collection of supported <see cref="LanguageConfiguration"/> instances.
    ''' <para></para>
    ''' This array acts as the registry for all available localizations, enabling 
    ''' the application to validate and patch MUI files for specific cultures.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' To expand support to additional languages, instantiate the new configuration class within this array.
    ''' </remarks>
    Private ReadOnly LangConfigs As LanguageConfiguration() = {
        New LanguageConfiguration_esES()
    }

    ''' <summary>
    ''' The <see cref="CultureInfo"/> instance representing the "en-US" culture.
    ''' </summary>
    Private ReadOnly CultureInfoEnUs As New CultureInfo("en-US")

    ''' <summary>
    ''' Tracks the total count of MUI files successfully processed during the current execution.
    ''' </summary>
    Private muiFilesProcessed As Integer

    ''' <summary>
    ''' Indicates whether any errors occurred during the program execution.
    ''' </summary>
    Private completedWithErrors As Boolean

#End Region

#Region " Entry Point "

    ''' <summary>
    ''' The main entry point of the application.
    ''' </summary>
    <DebuggerStepperBoundary>
    Public Sub Main()

        Thread.CurrentThread.CurrentCulture = Program.CultureInfoEnUs
        Thread.CurrentThread.CurrentUICulture = Program.CultureInfoEnUs

        Console.OutputEncoding = New UTF8Encoding()
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White

        Dim consoleTitle As String = $"{My.Application.Info.Title} {My.Application.Info.Version.ToString(fieldCount:=3)} — by ElektroStudios"
#If Debug Then
        Console.Title = consoleTitle
#End If
        Program.WriteColoredLine(" " & consoleTitle, ConsoleColor.Cyan)
        Console.WriteLine("╭──────────────────────────────────────────────────────────────────────────────────╮")
        Console.WriteLine("│ Purpose:                                                                         │")
        Console.WriteLine("│   This application restores English keyboard accelerator tables for Explorer,    │")
        Console.WriteLine("│   Notepad and Wordpad on Windows 10 and 11, replacing localized versions.        │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│ Execution:                                                                       │")
        Console.WriteLine("│   It automatically locates and identifies supported MUI files, makes a temporary │")
        Console.WriteLine("│   copy on a safe directory, overwrites the resource accelerators table, and      │")
        Console.WriteLine("│   schedules the original MUI file replacements upon the next system reboot.      │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│ Scope:                                                                           │")
        Console.WriteLine("│   Currently, only Spanish (Spain) 'es-ES' MUI files are supported.               │")
        Console.WriteLine("│                                                                                  │")
        Console.WriteLine("│ [!] Disclaimer:                                                                  │")
        Console.WriteLine("│   This program is provided 'as-is', without warranty of any kind.                │")
        Console.WriteLine("│   Use it at your own risk, and ensure you have a full system backup before usage.│")
        Console.WriteLine("╰──────────────────────────────────────────────────────────────────────────────────╯")
        Console.WriteLine()

        Program.WriteColoredLine("Press 'Y' key to begin the accelerator tables restoration, or 'Escape' key to exit...", ConsoleColor.Yellow)
        Do
            Dim keyInfo As ConsoleKeyInfo = Console.ReadKey(intercept:=True)
            If keyInfo.Key = ConsoleKey.Y Then
                Exit Do
            ElseIf keyInfo.Key = ConsoleKey.Escape Then
                Environment.Exit(0)
            End If
        Loop
        Console.WriteLine()

        For Each langconfig As LanguageConfiguration In Program.LangConfigs

            Dim ciName As String = langconfig.CultureInfo.Name
            Dim tempMuiDirectoryPath As String = Path.Combine(AppGlobals.BaseTempMuiDirectoryPath, ciName)

            Try
                Program.WriteColoredLine(" CONFIGURATION", ConsoleColor.Magenta)
                Program.WriteColoredLine("================================================================================", ConsoleColor.DarkGray)
                Console.WriteLine()
                Program.WriteColoredLine($" Target MUI Culture Name: {ciName}", ConsoleColor.DarkYellow)
                Program.WriteColoredLine($" Temp path for pending files: {tempMuiDirectoryPath}", ConsoleColor.DarkYellow)
                Console.WriteLine()

                Program.WriteColoredLine(" MUI FILES RETRIEVAL", ConsoleColor.Magenta)
                Program.WriteColoredLine("================================================================================", ConsoleColor.DarkGray)
                Console.WriteLine()

                Program.WriteColoredLine(" Locating directories and taking ownership...", ConsoleColor.White)
                Console.WriteLine()
                Dim resolvedDirectories As ReadOnlyCollection(Of String) = Program.LocateDirectories(langconfig)
                For i As Integer = 0 To resolvedDirectories.Count - 1
                    Program.WriteColoredLine($"   {i + 1:N0}: {resolvedDirectories(i)}", ConsoleColor.DarkCyan)
                Next i
                Console.WriteLine()
                Program.WriteColoredLine(" Completed locating directories.", ConsoleColor.Green)
                Console.WriteLine()

                Program.WriteColoredLine(" Locating MUI files and taking ownership...", ConsoleColor.White)
                Console.WriteLine()
                Dim resolvedMuiFiles As Dictionary(Of String, SortedSet(Of String)) = Program.LocateMuiFiles(resolvedDirectories, langconfig)
                For Each kvp As KeyValuePair(Of String, SortedSet(Of String)) In resolvedMuiFiles
                    Program.WriteColoredLine($"   Group {Path.GetFileName(kvp.Value.First())} [CRC-32: {kvp.Key}]:", ConsoleColor.Cyan)

                    Dim printIndex As Integer = 1
                    For Each matchedFilePath As String In kvp.Value
                        Program.WriteColoredLine($"     {printIndex:N0}: {matchedFilePath}", ConsoleColor.DarkCyan)
                        printIndex += 1
                    Next matchedFilePath
                    Console.WriteLine()
                Next kvp
                Program.WriteColoredLine(" Completed locating MUI files.", ConsoleColor.Green)
                Console.WriteLine()

                If (resolvedMuiFiles IsNot Nothing) AndAlso (resolvedMuiFiles.Count > 0) Then
                    Program.WriteColoredLine(" MUI RESOURCE PROCESSING", ConsoleColor.Magenta)
                    Program.WriteColoredLine("================================================================================", ConsoleColor.DarkGray)
                    Console.WriteLine()

                    Program.WriteColoredLine(" Accepting Movefile EULA...", ConsoleColor.White)
                    Dim successAcceptMovefileEula As Boolean = Program.AcceptMovefileEula()
                    If Not successAcceptMovefileEula Then
                        Program.ExitWithMessage(Nothing, exitCode:=1, Console.ForegroundColor)
                    End If
                    Program.WriteColoredLine(" Completed accepting Movefile EULA.", ConsoleColor.Green)
                    Console.WriteLine()

                    Program.WriteColoredLine($" Processing MUI file groups...", ConsoleColor.White)
                    Console.WriteLine()
                    For Each kvp As KeyValuePair(Of String, SortedSet(Of String)) In resolvedMuiFiles

                        Dim currentChecksum As String = kvp.Key
                        Program.WriteColoredLine($"   Group {Path.GetFileName(kvp.Value.First())} [CRC-32: {currentChecksum}]:", ConsoleColor.Cyan)
                        Console.WriteLine()

                        Dim matchingMuiDesc As MuiDescriptor = Nothing
                        Dim foundMuiDesc As Boolean = False

                        For Each muiDesc As MuiDescriptor In langconfig.MuiDescriptors
                            If muiDesc.Checksum.Equals(currentChecksum, StringComparison.OrdinalIgnoreCase) Then
                                matchingMuiDesc = muiDesc
                                foundMuiDesc = True
                                Exit For
                            End If
                        Next muiDesc

                        If Not foundMuiDesc Then
                            Program.WriteColoredLine($"     [WARN] No MUI descriptor definitions found for checksum: {currentChecksum}. Skipping group.", ConsoleColor.Red)
                            Console.WriteLine()
                            ' Program.completedWithErrors = True
                            Continue For
                        End If

                        Dim muiFileName As String = matchingMuiDesc.FileName

                        Dim tempRcFilePath As String = Path.Combine(Path.GetTempPath(), $"{muiFileName}.rc")
                        Dim tempResFilePath As String = Path.Combine(Path.GetTempPath(), $"{muiFileName}.res")

                        Dim accTable As String = matchingMuiDesc.AcceleratorTable

                        Program.WriteColoredLine($"     Writing Accelerators table resource to: {tempRcFilePath}...", ConsoleColor.Gray)
                        If File.Exists(tempRcFilePath) Then
                            ' Silently try to delete any previous .rc file for safety.
                            Try
                                File.Delete(tempRcFilePath)
                            Catch
                            End Try
                        End If
                        Try
                            File.WriteAllText(tempRcFilePath, accTable, encoding:=Encoding.Unicode)
                        Catch ex As Exception
                            Program.WriteColoredLine($"     An error occurred: {ex.Message}", ConsoleColor.Red)
                            Console.WriteLine()
                            Program.completedWithErrors = True
                            Continue For
                        End Try
                        Program.WriteColoredLine("     Completed writing Accelerators table resource.", ConsoleColor.Green)
                        Console.WriteLine()

                        Dim compileArgs As String =
                            $"-log    ""{AppGlobals.RESOURCE_HACKER_LOGFILE_PATH}"" " &
                            $"-open   ""{tempRcFilePath}"" " &
                            $"-save   ""{tempResFilePath}"" " &
                             "-action   compile"

                        Program.WriteColoredLine($"     Compiling Accelerators table resource to: {tempResFilePath}...", ConsoleColor.Gray)
                        If File.Exists(tempResFilePath) Then
                            ' Silently try to delete any previous .res file for safety.
                            Try
                                File.Delete(tempResFilePath)
                            Catch
                            End Try
                        End If
                        Dim successCompile As Boolean = Program.ExecuteResourceHacker(compileArgs)
                        If Not successCompile Then
                            Console.WriteLine()
                            Program.completedWithErrors = True
                            Continue For
                        End If
                        Program.WriteColoredLine("     Completed compiling Accelerators table resource.", ConsoleColor.Green)
                        Console.WriteLine()

                        For i As Integer = 0 To kvp.Value.Count - 1

                            Dim sourceMuiFilePath As String = kvp.Value(i)
                            Dim tempMuiFilePathPending As String = Path.Combine(tempMuiDirectoryPath, $"{muiFileName}.[{matchingMuiDesc.Checksum}].({i + 1:N0}).{AppGlobals.MuiFilePendingSuffix}")
                            Dim tempMuiFilePathFailed As String = Path.Combine(tempMuiDirectoryPath, $"{muiFileName}.[{matchingMuiDesc.Checksum}].({i + 1:N0}).{AppGlobals.MuiFileFailedSuffix}")

                            Program.WriteColoredLine($"     {i + 1:N0}: {sourceMuiFilePath}", ConsoleColor.DarkCyan)

                            Program.WriteColoredLine($"        Copying to temp file: {tempMuiFilePathPending}...", ConsoleColor.Gray)

                            If Not Directory.Exists(tempMuiDirectoryPath) Then
                                Try
                                    Directory.CreateDirectory(tempMuiDirectoryPath)
                                Catch ex As Exception
                                    Program.WriteColoredLine($"        Cannot create target directory: {ex.Message}", ConsoleColor.Red)
                                    Console.WriteLine()
                                    Program.completedWithErrors = True
                                    Continue For
                                End Try
                            End If

                            Try
                                File.Copy(sourceMuiFilePath, tempMuiFilePathPending, overwrite:=True)
                            Catch ex As Exception
                                Program.WriteColoredLine($"        An error occurred: {ex.Message}", ConsoleColor.Red)
                                Console.WriteLine()
                                Program.completedWithErrors = True
                                Continue For
                            End Try

                            Program.WriteColoredLine($"        Clearing Read-Only attribute in temp file...", ConsoleColor.Gray)
                            Try
                                Dim attributes As FileAttributes = File.GetAttributes(tempMuiFilePathPending)
                                If (attributes And FileAttributes.ReadOnly) = FileAttributes.ReadOnly Then
                                    File.SetAttributes(tempMuiFilePathPending, attributes And Not FileAttributes.ReadOnly)
                                End If
                            Catch ex As Exception
                                Program.WriteColoredLine($"        An error occurred: {ex.Message}", ConsoleColor.Red)
                                Console.WriteLine()
                                Program.completedWithErrors = True
                                Continue For
                            End Try

                            Program.WriteColoredLine("        Overwriting Accelerators table in temp file...", ConsoleColor.Gray)

                            Dim overwriteArgs As String =
                                $"-log      ""{AppGlobals.RESOURCE_HACKER_LOGFILE_PATH}"" " &
                                $"-open     ""{tempMuiFilePathPending}"" " &
                                $"-save     ""{tempMuiFilePathPending}"" " &
                                $"-resource ""{tempResFilePath}"" " &
                                "-action      addoverwrite " &
                                $"-mask     ""ACCELERATORS,,{langconfig.CultureInfo.LCID}"""

                            Dim successOverwrite As Boolean = Program.ExecuteResourceHacker(overwriteArgs)
                            If Not successOverwrite Then
                                Console.WriteLine()
                                Program.completedWithErrors = True
                                Continue For
                            End If

                            Program.WriteColoredLine("        Scheduling replacement for source MUI file...", ConsoleColor.Gray)
                            Dim bakFilePath As String = $"{sourceMuiFilePath}.{AppGlobals.MuiFileBackupSuffix}"
                            If Not File.Exists(bakFilePath) Then
                                ' Note: If file "name.mui.bak" exists, the post-reboot operation will silently fail as expected.
                                ' We don't want to delete an original backup file and unnecessarily replace an already modified MUI file.
                                Dim createBakFileArgs As String = $" ""{sourceMuiFilePath}"" ""{bakFilePath}"" "
                                Dim successCreateBakFile As Boolean = Program.ExecuteMoveFile(createBakFileArgs)
                                If Not successCreateBakFile Then
                                    Console.WriteLine()
                                    Program.completedWithErrors = True
                                    Continue For
                                End If
                            End If

                            Dim replaceMuiFileArgs As String = $" ""{tempMuiFilePathPending}"" ""{sourceMuiFilePath}"" "
                            Dim successMuiReplaceFile As Boolean = Program.ExecuteMoveFile(replaceMuiFileArgs)
                            If Not successMuiReplaceFile Then
                                If Not File.Exists(bakFilePath) Then
                                    ' Best effort to revert .bak file rename.
                                    Dim revertBakFileArgs As String = $" ""{bakFilePath}"" ""{sourceMuiFilePath}"" "
                                    Dim successRevertBakFile As Boolean = Program.ExecuteMoveFile(revertBakFileArgs)
                                    If Not successRevertBakFile Then
                                        ' Delete "PendingFileRenameOperations" value only if very critical MUI file:
                                        If muiFileName.Equals("shell32.dll.mui", StringComparison.OrdinalIgnoreCase) Then
                                            Const registryKeyPath As String = "SYSTEM\CurrentControlSet\Control\Session Manager"
                                            Const valueName As String = "PendingFileRenameOperations"
                                            Try
                                                Using sessionManagerKey As RegistryKey = Registry.LocalMachine.OpenSubKey(registryKeyPath, writable:=True)
                                                    ' Deletes the entire multi-string value to clear all scheduled operations and secure the next boot.
                                                    sessionManagerKey?.DeleteValue(valueName, throwOnMissingValue:=False)
                                                End Using

                                            Catch ' Ignore errors. Really, really best effort to revert .bak file rename.
                                            End Try
                                        End If
                                    End If
                                End If

                                Console.WriteLine()
                                Program.completedWithErrors = True
                                Continue For
                            End If

                            ' Delete any failed temp MUI file from previous reboots.
                            Dim deleteFailedTempMuiArgs As String = $" ""{tempMuiFilePathFailed}"" """" "
                            Dim successdeleteFailedTempMui As Boolean = Program.ExecuteMoveFile(deleteFailedTempMuiArgs)
                            If Not successdeleteFailedTempMui Then
                                ' Ignore errors as this is not critical.
                            End If

                            ' If the temp MUI file still exists on directory, mark it as failed (rename it).
                            Dim markTempMuiAsFailedArgs As String = $" ""{tempMuiFilePathPending}"" ""{tempMuiFilePathFailed}"" "
                            Dim successMarkTempMuiAsFailed As Boolean = Program.ExecuteMoveFile(markTempMuiAsFailedArgs)
                            If Not successMarkTempMuiAsFailed Then
                                ' Ignore errors as this is not critical.
                            End If

                            Program.WriteColoredLine($"        Completed processing MUI file.", ConsoleColor.Green)
                            Console.WriteLine()
                            Program.muiFilesProcessed += 1
                        Next i

                    Next kvp
                    Program.WriteColoredLine(" Completed processing MUI file groups.", ConsoleColor.Green)
                    Console.WriteLine()

                End If

            Catch ex As Exception
                Console.WriteLine()
                Dim errMsg As String = If(ex.InnerException IsNot Nothing, ex.InnerException.Message, ex.Message)
                Program.ExitWithMessage($"FATAL ERROR 0x{ex.HResult:X8}: {errMsg}", exitCode:=ex.HResult, ConsoleColor.Red)

            End Try

        Next langconfig

        Program.WriteColoredLine(" FINALIZATION", ConsoleColor.Magenta)
        Program.WriteColoredLine("================================================================================", ConsoleColor.DarkGray)
        Console.WriteLine()

        If Not Program.completedWithErrors AndAlso (Program.muiFilesProcessed = 0) Then
            Program.WriteColoredLine(" [!] No MUI files were processed because no matching files were found on the current system.", ConsoleColor.Yellow)
            Console.WriteLine()
            Program.WriteColoredLine(" The application will exit now without making any changes to your system.", Console.ForegroundColor)
            Console.WriteLine()

        ElseIf Not Program.completedWithErrors Then
            Program.PrintScheduledFileOperations()
            Program.WriteColoredLine(" Operations completed successfully!", ConsoleColor.Green)
            Console.WriteLine()
            Program.WriteColoredLine(" All pending file operations have been scheduled for the next system restart.", ConsoleColor.Yellow)
            Program.WriteColoredLine(" [!] Please reboot your system to apply the changes.", ConsoleColor.Yellow)
            Console.WriteLine()

        Else
            Program.PrintScheduledFileOperations()
            Program.WriteColoredLine(" Operations completed with errors.", ConsoleColor.DarkRed)
            Console.WriteLine()
            Program.WriteColoredLine(" [X] Some MUI resources may not have been updated or some file replacement operations may not have been scheduled. Please review any error messages above.", ConsoleColor.Red)
            Console.WriteLine()

        End If

        Program.ExitWithMessage(Nothing, exitCode:=0, Console.ForegroundColor)
    End Sub

#End Region

#Region " Private Methods "

    ''' <summary>
    ''' Builds the collection of search path patterns where to find MUI files based on the specified language configuration.
    ''' </summary>
    ''' 
    ''' <param name="langConfig">
    ''' The language configuration environment containing the culture info. 
    ''' </param>
    ''' 
    ''' <returns>
    ''' A <see cref="SortedSet(Of String)"/> containing the paths of all successfully resolved directories.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function BuildSearchPathPatterns(langConfig As LanguageConfiguration) As SortedSet(Of String)

        Dim searchPathPatterns As New SortedSet(Of String)(StringComparer.OrdinalIgnoreCase) From
            {   ' 1. C:\Windows\xx-XX
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), langConfig.CultureInfo.Name) _
              , ' 2. C:\Windows\System32\xx-XX
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), langConfig.CultureInfo.Name) _
              , ' 3. C:\Windows\SysWOW64\xx-XX
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), langConfig.CultureInfo.Name) _
              , ' 4. C:\Program Files\WindowsApps\Microsoft.LanguageExperiencePackxx-XX_<Version>_<Architecture>_<ResourceId>_<PublisherId>\Windows\System32\xx-XX
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                  "WindowsApps", $"Microsoft.LanguageExperiencePack{langConfig.CultureInfo.Name}_*") _
              , ' 5. C:\Program Files (x86)\WindowsApps\Microsoft.LanguageExperiencePackxx-XX_<Version>_<Architecture>_<ResourceId>_<PublisherId>\Windows\System32\xx-XX
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                  "WindowsApps", $"Microsoft.LanguageExperiencePack{langConfig.CultureInfo.Name}_*") _
              , ' 6. C:\Program Files\Windows NT\Accessories\xx-XX
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                  $"Windows NT\Accessories\{langConfig.CultureInfo.Name}") _
              , ' 7. C:\Program Files (x86)\Windows NT\Accessories\xx-XX
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                                  $"Windows NT\Accessories\{langConfig.CultureInfo.Name}") _
              , ' 8. C:\Program Files\Wordpad\xx-XX ⟶ (https://winaero.com/wordpad-for-windows-11/)
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                  $"Wordpad\{langConfig.CultureInfo.Name}")
        }

        Return searchPathPatterns
    End Function

    ''' <summary>
    ''' Resolves and locates the target directory paths to search for MUI files based on defined search patterns.
    ''' </summary>
    ''' 
    ''' <param name="langConfig">
    ''' The language configuration environment containing the localized character used to confirm ownership via the TAKEOWN command line tool. 
    ''' </param>
    ''' 
    ''' <returns>
    ''' A <see cref="ReadOnlyCollection(Of String)"/> containing the paths of all successfully resolved directories.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function LocateDirectories(langConfig As LanguageConfiguration) As ReadOnlyCollection(Of String)

        Dim searchPathPatterns As SortedSet(Of String) = Program.BuildSearchPathPatterns(langConfig)

        Dim resolvedDirectories As New SortedSet(Of String)(StringComparer.OrdinalIgnoreCase)

        For Each item As String In searchPathPatterns
            Dim directoryName As String = Path.GetDirectoryName(item)
            Dim searchPattern As String = Path.GetFileName(item)

            If Directory.Exists(directoryName) Then
                ' Program.ClaimDirectoryAccess(directoryName, langConfig)

                If searchPattern.Contains("*") Then
                    Try
                        For Each matchedDir As String In Directory.EnumerateDirectories(directoryName, searchPattern, SearchOption.TopDirectoryOnly)
                            Program.ClaimDirectoryAccess(matchedDir)
                            resolvedDirectories.Add(matchedDir)
                        Next matchedDir

                    Catch ex As UnauthorizedAccessException
                        Program.WriteColoredLine($"Read access denied for directory '{directoryName}'. Unlocking...", ConsoleColor.Yellow)
                            Program.ClaimDirectoryAccess(directoryName)

                            Try
                                For Each matchedDir As String In Directory.EnumerateDirectories(directoryName, searchPattern, SearchOption.TopDirectoryOnly)
                                    resolvedDirectories.Add(matchedDir)
                                Next matchedDir

                            Catch subEx As UnauthorizedAccessException
                                Program.WriteColoredLine($"Unable to grant permissions to directory. Skipping...", ConsoleColor.Red)
                                Program.completedWithErrors = True

                            End Try

                    Catch ex As Exception

                    End Try
                Else
                    Dim fullPath As String = Path.Combine(directoryName, searchPattern)
                    If Directory.Exists(fullPath) Then
                        Program.ClaimDirectoryAccess(fullPath)
                        resolvedDirectories.Add(fullPath)
                    End If

                End If
            End If
        Next item

        Return resolvedDirectories.ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Locates all valid MUI files within the specified directories and groups them by their file checksum (CRC-32).
    ''' </summary>
    ''' 
    ''' <param name="resolvedDirectories">
    ''' The list of directory paths to search for valid MUI files.
    ''' </param>
    ''' 
    ''' <param name="langConfig">
    ''' The language configuration environment containing targeted MUI file definitions 
    ''' and the localized character used to confirm ownership via the TAKEOWN command line tool. 
    ''' </param>
    ''' 
    ''' <returns>
    ''' A <see cref="Dictionary(Of String, HashSet(Of String))"/> where each key is a file checksum 
    ''' and the value is the collection of matching MUI file paths.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function LocateMuiFiles(resolvedDirectories As IList(Of String),
                                    langConfig As LanguageConfiguration) As Dictionary(Of String, SortedSet(Of String))

        Dim groupedMuiFiles As New Dictionary(Of String, SortedSet(Of String))(StringComparer.OrdinalIgnoreCase)

        For Each muiDesc As MuiDescriptor In langConfig.MuiDescriptors

            Dim fileName As String = muiDesc.FileName
            Dim expectedChecksum As String = muiDesc.Checksum
            Dim currentMatches As New SortedSet(Of String)(StringComparer.OrdinalIgnoreCase)

            For Each targetDir As String In resolvedDirectories
                Dim matches As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

                Program.FindFiles(targetDir, fileName, matches)

                For Each matchedFile As String In matches
                    Dim fileChecksum As String = UtilChecksum.ComputeCRC32OfFile(matchedFile)

                    If fileChecksum.Equals(expectedChecksum, StringComparison.OrdinalIgnoreCase) Then
                        Program.ClaimFileAccess(matchedFile)
                        currentMatches.Add(matchedFile)
                    End If
                Next matchedFile
            Next targetDir

            If currentMatches.Count > 0 Then
                If Not groupedMuiFiles.ContainsKey(expectedChecksum) Then
                    groupedMuiFiles.Add(expectedChecksum, currentMatches)
                Else
                    For Each verifiedPath As String In currentMatches
                        groupedMuiFiles(expectedChecksum).Add(verifiedPath)
                    Next verifiedPath
                End If
            End If
        Next muiDesc

        Return groupedMuiFiles
    End Function

    ''' <summary>
    ''' Recursively searches a directory tree for files matching a specific file name or search pattern.
    ''' </summary>
    ''' 
    ''' <param name="rootPath">
    ''' The root directory path to begin searching.
    ''' </param>
    ''' 
    ''' <param name="searchPattern">
    ''' The file name or pattern to match.
    ''' </param>
    ''' 
    ''' <param name="refMatches">
    ''' When this method returns, contains the discovered file paths that matches the <paramref name="searchPattern"/>.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub FindFiles(rootPath As String, searchPattern As String,
                    ByRef refMatches As HashSet(Of String))

        If refMatches Is Nothing Then
            refMatches = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        End If

        Dim directoryStack As New Stack(Of String)()
        directoryStack.Push(rootPath)

        While directoryStack.Count > 0
            Dim currentDir As String = directoryStack.Pop()

            Try
                For Each fileMatch As String In Directory.EnumerateFiles(currentDir, searchPattern, SearchOption.TopDirectoryOnly)
                    refMatches.Add(fileMatch)
                Next fileMatch

                For Each subDir As String In Directory.EnumerateDirectories(currentDir)
                    directoryStack.Push(subDir)
                Next subDir

            Catch ex As UnauthorizedAccessException
                Program.WriteColoredLine($"Read access denied for directory '{currentDir}'. Unlocking...", ConsoleColor.Yellow)
                    Program.ClaimDirectoryAccess(currentDir)

                    Try
                        For Each fileMatch As String In Directory.EnumerateFiles(currentDir, searchPattern, SearchOption.TopDirectoryOnly)
                            refMatches.Add(fileMatch)
                        Next fileMatch

                        For Each subDir As String In Directory.EnumerateDirectories(currentDir)
                            directoryStack.Push(subDir)
                        Next subDir

                    Catch subEx As UnauthorizedAccessException
                        Program.WriteColoredLine($"Unable to grant permissions to directory. Skipping...", ConsoleColor.Red)
                        Program.completedWithErrors = True

                    End Try

            Catch ex As Exception

            End Try
        End While
    End Sub

    ''' <summary>
    ''' Attempts to take ownership to the current user and grant full access for a specific directory path, recursively.
    ''' </summary>
    ''' 
    ''' <param name="dirPath">
    ''' The directory path for which to take ownership and grant access permissions.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub ClaimDirectoryAccess(dirPath As String)

        Try
            ' Take directory ownership to the current user.
            Dim takeOwnCommandArgs As String = $"/F ""{dirPath}"""
            Using takeownProcess As Process =
                Process.Start(New ProcessStartInfo("TAKEOWN.exe", takeOwnCommandArgs) With {
                .UseShellExecute = True,
                .CreateNoWindow = True,
                .Verb = "runas",
                .WindowStyle = ProcessWindowStyle.Hidden
            })
                takeownProcess.WaitForExit()
                Dim exitCode As Integer = takeownProcess.ExitCode
            End Using
#If DEBUG Then
            Program.WriteColoredLine($"   TAKEOWN.exe {takeOwnCommandArgs}", ConsoleColor.DarkGray)
#End If

            ' Grant current user full Control to directory.
            Dim icaclsUserCommandArgs As String = $"    ""{dirPath}"" /Grant ""{Environment.UserName}:(F)"""
            Using icaclsUserProcess As Process =
                Process.Start(New ProcessStartInfo("ICACLS.exe", icaclsUserCommandArgs) With {
                .UseShellExecute = True,
                .CreateNoWindow = True,
                .Verb = "runas",
                .WindowStyle = ProcessWindowStyle.Hidden
            })
                icaclsUserProcess.WaitForExit()
                Dim exitCode As Integer = icaclsUserProcess.ExitCode
            End Using
#If DEBUG Then
            Program.WriteColoredLine($"   ICACLS.exe {icaclsUserCommandArgs}", ConsoleColor.DarkGray)
#End If

            ' Grant SYSTEM full Control to directory.
            Dim icaclsSystemCommandArgs As String = $"    ""{dirPath}"" /Grant ""SYSTEM:(F)"""
            Using icaclsSystemProcess As Process =
                Process.Start(New ProcessStartInfo("ICACLS.exe", icaclsSystemCommandArgs) With {
                .UseShellExecute = True,
                .CreateNoWindow = True,
                .Verb = "runas",
                .WindowStyle = ProcessWindowStyle.Hidden
            })
                icaclsSystemProcess.WaitForExit()
                Dim exitCode As Integer = icaclsSystemProcess.ExitCode
            End Using
#If DEBUG Then
            Program.WriteColoredLine($"   ICACLS.exe {icaclsSystemCommandArgs}", ConsoleColor.DarkGray)
#End If

#If DEBUG Then
            Console.WriteLine()
#End If
        Catch
            ' Silently handle exceptions; movefile will simply fail later if permissions were not successfully granted.

        End Try
    End Sub

    ''' <summary>
    ''' Attempts to take ownership to the current user and grant full access for a specific file.
    ''' </summary>
    ''' 
    ''' <param name="filePath">
    ''' The file path for which to take ownership and grant access permissions.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub ClaimFileAccess(filePath As String)

        Try
            ' Take file ownership to the current user.
            Dim takeOwnCommandArgs As String = $"/F ""{filePath}"""
            Using takeownProcess As Process =
                Process.Start(New ProcessStartInfo("TAKEOWN.exe", takeOwnCommandArgs) With {
                .UseShellExecute = True,
                .CreateNoWindow = True,
                .Verb = "runas",
                .WindowStyle = ProcessWindowStyle.Hidden
            })
                takeownProcess.WaitForExit()
                Dim exitCode As Integer = takeownProcess.ExitCode
            End Using
#If DEBUG Then
            Program.WriteColoredLine($"   TAKEOWN.exe {takeOwnCommandArgs}", ConsoleColor.DarkGray)
#End If

            ' Grant current user full Control to file.
            Dim icaclsUserCommandArgs As String = $"    ""{filePath}"" /Grant ""{Environment.UserName}:(F)"""
            Using icaclsUserProcess As Process =
                Process.Start(New ProcessStartInfo("ICACLS.exe", icaclsUserCommandArgs) With {
                .UseShellExecute = True,
                .CreateNoWindow = True,
                .Verb = "runas",
                .WindowStyle = ProcessWindowStyle.Hidden
            })
                icaclsUserProcess.WaitForExit()
                Dim exitCode As Integer = icaclsUserProcess.ExitCode
            End Using
#If DEBUG Then
            Program.WriteColoredLine($"   ICACLS.exe {icaclsUserCommandArgs}", ConsoleColor.DarkGray)
#End If

            ' Grant SYSTEM full Control to file.
            Dim icaclsSystemCommandArgs As String = $"    ""{filePath}"" /Grant ""SYSTEM:(F)"""
            Using icaclsSystemProcess As Process =
                Process.Start(New ProcessStartInfo("ICACLS.exe", icaclsSystemCommandArgs) With {
                .UseShellExecute = True,
                .CreateNoWindow = True,
                .Verb = "runas",
                .WindowStyle = ProcessWindowStyle.Hidden
            })
                icaclsSystemProcess.WaitForExit()
                Dim exitCode As Integer = icaclsSystemProcess.ExitCode
            End Using
#If DEBUG Then
            Program.WriteColoredLine($"   ICACLS.exe {icaclsSystemCommandArgs}", ConsoleColor.DarkGray)
#End If

#If DEBUG Then
            Console.WriteLine()
#End If
        Catch
            ' Silently handle exceptions; movefile will simply fail later if permissions were not successfully granted.

        End Try
    End Sub

    ''' <summary>
    ''' Executes Resource Hacker with the specified command-line arguments and validates the operation via the log file.
    ''' </summary>
    ''' 
    ''' <param name="arguments">
    ''' The command-line arguments to pass to Resource Hacker.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the operation was successful according to the log; otherwise, <see langword="False"/>.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function ExecuteResourceHacker(arguments As String) As Boolean

        If File.Exists(AppGlobals.RESOURCE_HACKER_LOGFILE_PATH) Then
            Try
                File.Delete(AppGlobals.RESOURCE_HACKER_LOGFILE_PATH)
            Catch
            End Try
        End If

        Try
            Dim reshackerProcessInfo As New ProcessStartInfo(AppGlobals.RESOURCE_HACKER_EXEC_PATH, arguments) With {
                .UseShellExecute = False,
                .CreateNoWindow = True
            }

            Using reshackerProcess As Process = Process.Start(reshackerProcessInfo)
                reshackerProcess.WaitForExit()
            End Using

        Catch ex As Exception
            Program.WriteColoredLine($"        Error executing Resource Hacker: {ex.Message}", ConsoleColor.Red)
            Return False

        End Try

        If File.Exists(AppGlobals.RESOURCE_HACKER_LOGFILE_PATH) Then
            Try
                Dim logContent As String = File.ReadAllText(AppGlobals.RESOURCE_HACKER_LOGFILE_PATH, Encoding.Unicode)

                If logContent.IndexOf("Success!", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Return True
                Else
                    Program.WriteColoredLine("        Resource Hacker execution has failed. Log file content:", ConsoleColor.Red)
                    Console.WriteLine()
                    Program.WriteColoredLine(logContent, ConsoleColor.DarkRed)
                    Return False
                End If

            Catch ex As Exception
                Program.WriteColoredLine($"        Error reading Resource Hacker log file: {ex.Message}", ConsoleColor.Red)
                Return False

            Finally
                Try
                    File.Delete(AppGlobals.RESOURCE_HACKER_LOGFILE_PATH)
                Catch
                End Try
            End Try
        Else
            Program.WriteColoredLine($"        Resource Hacker execution has failed.", ConsoleColor.Red)
            Console.WriteLine()
            Program.WriteColoredLine($"        Full command: ""{AppGlobals.RESOURCE_HACKER_EXEC_PATH}"" {arguments}", ConsoleColor.DarkRed)
            Return False

        End If
    End Function

    ''' <summary>
    ''' Executes the Sysinternals <c>MoveFile</c> with the specified command-line arguments and validates the execution operation.
    ''' </summary>
    ''' 
    ''' <param name="arguments">
    ''' The target arguments containing file operations.
    ''' </param>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if <c>MoveFile</c> process exited with zero; otherwise, <see langword="False"/>.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function ExecuteMoveFile(arguments As String) As Boolean

        Try
            Dim movefileProcessInfo As New ProcessStartInfo(AppGlobals.MOVEFILE_EXEC_PATH, arguments) With {
                .UseShellExecute = False,
                .CreateNoWindow = True
            }

            Using movefileProcess As Process = Process.Start(movefileProcessInfo)
                movefileProcess.WaitForExit()

                If movefileProcess.ExitCode <> 0 Then
                    Program.WriteColoredLine($"        Error executing MoveFile. ExitCode: {movefileProcess.ExitCode}", ConsoleColor.Red)
                    Console.WriteLine()
                    Program.WriteColoredLine($"        Full command: ""{AppGlobals.MOVEFILE_EXEC_PATH}"" {arguments}", ConsoleColor.DarkRed)
                    Return False
                End If
            End Using

            Return True

        Catch ex As Exception
            Program.WriteColoredLine($"        Error executing MoveFile: {ex.Message}", ConsoleColor.Red)
            Return False

        End Try
    End Function

    ''' <summary>
    ''' Programmatically registers the Sysinternals <c>Movefile</c> EULA acceptance into the current user registry hive.
    ''' </summary>
    ''' 
    ''' <returns>
    ''' <see langword="True"/> if the value was successfully written; otherwise, <see langword="False"/>.
    ''' </returns>
    <DebuggerStepThrough>
    Private Function AcceptMovefileEula() As Boolean

        Const subKeyPath As String = "SOFTWARE\Sysinternals\Movefile"
        Const valueName As String = "EulaAccepted"
        Const dwordValue As Integer = 1

        Try
            Using sysinternalsKey As RegistryKey = Registry.CurrentUser.CreateSubKey(subKeyPath, writable:=True)
                If sysinternalsKey IsNot Nothing Then
                    sysinternalsKey.SetValue(valueName, dwordValue, RegistryValueKind.DWord)
                Else
                    Console.WriteLine($" Failed to create or open the registry subkey: {Registry.CurrentUser}\{subKeyPath}")
                End If
            End Using
            Return True

        Catch ex As Exception
            Console.WriteLine($" An error occurred while writing to registry key: {ex.Message}")

        End Try

        Return False
    End Function

    ''' <summary>
    ''' Reads the Windows registry to retrieve and print pending file rename or deletion operations scheduled for the next system reboot.
    ''' <para></para>
    ''' Only operations involving MUI files ('.mui' extension) are displayed.
    ''' </summary>
    <DebuggerStepThrough>
    Private Sub PrintScheduledFileOperations()

        Const subKeyPath As String = "SYSTEM\CurrentControlSet\Control\Session Manager"
        Const valueName As String = "PendingFileRenameOperations"

        Try
            Using sessionManagerKey As RegistryKey = Registry.LocalMachine.OpenSubKey(subKeyPath, writable:=False)
                If sessionManagerKey IsNot Nothing Then
                    Dim pendingOperations As Object = sessionManagerKey.GetValue(valueName)

                    If pendingOperations IsNot Nothing AndAlso TypeOf pendingOperations Is String() Then
                        Dim operationsArray As String() = DirectCast(pendingOperations, String())

                        If operationsArray.Length > 0 Then
                            Program.WriteColoredLine(" Scheduled File Operations on Reboot:", ConsoleColor.Cyan)
                            Console.WriteLine()
                            Dim index As Integer = 0
                            ' Loop increments by 2 because operations are stored in Source/Destination pairs
                            Do While index < operationsArray.Length
                                Dim sourceFile As String = operationsArray(index)
                                Dim destinationFile As String = String.Empty

                                If (index + 1) < operationsArray.Length Then
                                    destinationFile = operationsArray(index + 1)
                                End If

                                If Not String.IsNullOrWhiteSpace(sourceFile) AndAlso sourceFile.IndexOf(".mui") > 0 Then
                                    ' Clean up the Win32 native path prefix "\??\" for clean console output

                                    Dim cleanSource As String = sourceFile.Replace("\??\", "").Replace("*1", "").Replace("*2", "")

                                    Dim cleanDestination As String =
                                        destinationFile.Replace("\??\", "").Replace("*1", "").Replace("*2", "")

                                    If String.IsNullOrWhiteSpace(cleanDestination) Then
                                        Console.WriteLine($"    [-] DELETE  : {cleanSource}")
                                    Else
                                        Console.WriteLine($"    [*] MOVE    : {cleanSource}")
                                        Console.WriteLine($"        TO      : {cleanDestination}")
                                    End If
                                End If

                                If destinationFile.EndsWith($".{AppGlobals.MuiFileFailedSuffix}", StringComparison.OrdinalIgnoreCase) Then
                                    Console.WriteLine()
                                End If

                                index += 2
                            Loop
                            '  Console.WriteLine()
                        End If
                    End If
                End If
            End Using
        Catch ex As Exception
            Program.WriteColoredLine($" [X] Could not read pending operations from registry: {ex.Message}", ConsoleColor.Red)
            Console.WriteLine()

        End Try
    End Sub

    ''' <summary>
    ''' Writes a colored message to the console.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The text message to write.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The foreground color to apply to the text.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub WriteColoredLine(message As String, foreColor As ConsoleColor)

        Dim previousColor As ConsoleColor = Console.ForegroundColor
        Console.ForegroundColor = foreColor
        Console.WriteLine(message)
        Console.ForegroundColor = previousColor
    End Sub

    ''' <summary>
    ''' Displays a message to the console and exits the application with the specified exit code.
    ''' </summary>
    ''' 
    ''' <param name="message">
    ''' The message to display before exiting. If empty or null, no message is displayed.
    ''' </param>
    ''' 
    ''' <param name="exitCode">
    ''' The exit code to return to the operating system. Typically 0 for success, non-zero for errors.
    ''' </param>
    ''' 
    ''' <param name="foreColor">
    ''' The console foreground color to use when displaying the message. 
    ''' <para></para>
    ''' After writing the message, the console color is reset to its original value.
    ''' </param>
    <DebuggerStepThrough>
    Private Sub ExitWithMessage(message As String, exitCode As Integer, foreColor As ConsoleColor)

        If Not String.IsNullOrEmpty(message) Then
            Program.WriteColoredLine(message, foreColor)
            Console.WriteLine()
        End If

        If exitCode <> 0 Then
            Console.WriteLine($"Exiting application with exit code: {exitCode} (0x{exitCode:X8}) ...")
            Console.WriteLine()
        End If

        Console.WriteLine("Press any key to exit...")
        Console.ReadKey(intercept:=True)

        Environment.Exit(exitCode)
    End Sub

#End Region

End Module

#End Region
