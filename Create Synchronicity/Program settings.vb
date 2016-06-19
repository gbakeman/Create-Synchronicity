'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Friend Module ProgramSetting
    'Main program settings
    Public Const Language As String = "Language"
    Public Const DefaultLanguage As String = "english"
    Public Const AutoUpdates As String = "Auto updates"
    Public Const MaxLogEntries As String = "Archived log entries"
    Public Const MainView As String = "Main view"
    Public Const FontSize As String = "Font size"
    Public Const MainFormAttributes As String = "Window size and position"
    Public Const ExpertMode As String = "Expert mode"
    Public Const DiffProgram As String = "Diff program"
    Public Const DiffArguments As String = "Diff arguments"
    Public Const TextLogs As String = "Text logs"
    Public Const Autocomplete As String = "Autocomplete"
    Public Const Forecast As String = "Forecast"
    Public Const Pause As String = "Pause"
    Public Const AutoStartupRegistration As String = "Auto startup registration"

    'Program files
    Public Const ConfigFolderName As String = "config"
    Public Const LogFolderName As String = "log"
    Public Const SettingsFileName As String = "mainconfig.ini"
    Public Const AppLogName As String = "app.log"
    Public Const DllName As String = "compress-decompress.dll"
    'Public CompressionThreshold As Integer = 0 'Better not filter at all

    Public Const ExcludedFolderPrefix As String = "folder" 'Used to parse excluded file types. For example, `folder"Documents"` means that folders named documents should be excluded.
    Public Const GroupPrefix As Char = ":"c
    Public Const EnqueuingSeparator As Char = "|"c
#If CONFIG = "Linux" Then
    Public Const DirSep As Char = "/"c
#Else
    Public Const DirSep As Char = "\"c
#End If

#If DEBUG Then
    Public Const Debug As Boolean = True
    Public Const ForecastDelay As Integer = 0
#Else
    Public Const Debug As Boolean = False
    Public Const ForecastDelay As Integer = 60
#End If

    Public Const AppLogThreshold As Integer = 1 << 23 '8 MB

    Public Const RegistryBootVal As String = "Create Synchronicity - Scheduler"
    Public Const RegistryBootKey As String = "Software\Microsoft\Windows\CurrentVersion\Run"
    Public Const RegistryRootedBootKey As String = "HKEY_CURRENT_USER\" & RegistryBootKey
End Module

NotInheritable Class ConfigHandler
    Private Shared Singleton As ConfigHandler

    Public LogRootDir As String
    Public ConfigRootDir As String
    Public LanguageRootDir As String

    Public CompressionDll As String
    Public LocalNamesFile As String
    Public MainConfigFile As String
    Public AppLogFile As String
    Public StatsFile As String

    Public CanGoOn As Boolean = True 'To check whether a synchronization is already running (in scheduler mode only, queuing uses callbacks).

    Friend Icon As Drawing.Icon
    Private SettingsLoaded As Boolean = False
    Private Settings As New Dictionary(Of String, String)

    Private Sub New()
        LogRootDir = GetUserFilesRootDir() & ProgramSetting.LogFolderName
        ConfigRootDir = GetUserFilesRootDir() & ProgramSetting.ConfigFolderName
        LanguageRootDir = Application.StartupPath & ProgramSetting.DirSep & "languages"

        StatsFile = ConfigRootDir & ProgramSetting.DirSep & "syncs-count.txt"
        LocalNamesFile = LanguageRootDir & ProgramSetting.DirSep & "local-names.txt"
        MainConfigFile = ConfigRootDir & ProgramSetting.DirSep & ProgramSetting.SettingsFileName
        CompressionDll = Application.StartupPath & ProgramSetting.DirSep & ProgramSetting.DllName
        AppLogFile = GetUserFilesRootDir() & ProgramSetting.AppLogName

        Try
            Icon = Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch Ex As ArgumentException
            Icon = Drawing.Icon.FromHandle((New Drawing.Bitmap(32, 32)).GetHicon)
        End Try

        TrimAppLog() 'Prevents app log from getting too large.
    End Sub

    Public Shared Function GetSingleton() As ConfigHandler
        If Singleton Is Nothing Then Singleton = New ConfigHandler()
        Return Singleton
    End Function

    ' Useful for renaming logs, or in cases where a ProfileHandler isn't available.
    Public Function GetConfigPath(ByVal Name As String) As String
        Return ConfigRootDir & ProgramSetting.DirSep & Name & ".sync"
    End Function

    Public Function GetLogPath(ByVal Name As String) As String
        Return LogRootDir & ProgramSetting.DirSep & Name & ".log" & If(ProgramSetting.Debug Or GetProgramSetting(Of Boolean)(ProgramSetting.TextLogs, False), "", ".html")
    End Function

    Public Function GetErrorsLogPath(ByVal Name As String) As String
        Return LogRootDir & ProgramSetting.DirSep & Name & ".errors.log"
    End Function

    Public Function GetUserFilesRootDir() As String 'Return the place were config files are stored
        Static UserFilesRootDir As String = ""
        If Not UserFilesRootDir = "" Then Return UserFilesRootDir

        Dim UserPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) & ProgramSetting.DirSep & Branding.Brand & ProgramSetting.DirSep & Branding.Name & ProgramSetting.DirSep

        'To change folder attributes: http://support.microsoft.com/default.aspx?scid=kb;EN-US;326549
        Dim WriteNeededFolders As New List(Of String) From {Application.StartupPath, Application.StartupPath & ProgramSetting.DirSep & LogFolderName, Application.StartupPath & ProgramSetting.DirSep & ConfigFolderName}
        Dim ProgramPathExists As Boolean = IO.Directory.Exists(Application.StartupPath & ProgramSetting.DirSep & ConfigFolderName)
        Dim ToDelete As New List(Of String)

        Try
            For Each Folder As String In WriteNeededFolders
                If Not IO.Directory.Exists(Folder) Then Continue For

                Dim TestPath As String = Folder & ProgramSetting.DirSep & "write-permissions." & IO.Path.GetRandomFileName()
                IO.File.Create(TestPath).Close()
                ToDelete.Add(TestPath)

                If Folder = Application.StartupPath Then Continue For
                For Each File As String In IO.Directory.GetFiles(Folder)
                    If (IO.File.GetAttributes(File) And IO.FileAttributes.ReadOnly) = IO.FileAttributes.ReadOnly Then Throw New IO.IOException(File)
                Next
            Next

            For Each TestFile As String In ToDelete
                Try
                    IO.File.Delete(TestFile)
                Catch Ex As IO.IOException
                    ' Silently fail when the file can't be found or is being used by another process 
                End Try
            Next
        Catch Ex As Exception When TypeOf Ex Is System.UnauthorizedAccessException Or TypeOf Ex Is IO.IOException
            If ProgramPathExists Then Interaction.ShowMsg("Create Synchronicity cannot write to your installation directory, although it contains configuration files. Your Application Data folder will therefore be used instead." & Environment.NewLine & Ex.Message, "Information", , MessageBoxIcon.Information)
            Return UserPath
        End Try

        ' When a user folder exists, and no config folder exists in the install dir, use the user's folder.
        Return If(ProgramPathExists Or Not IO.Directory.Exists(UserPath), Application.StartupPath & ProgramSetting.DirSep, UserPath)
    End Function

    Public Function GetProgramSetting(Of T)(ByVal Key As String, ByVal DefaultVal As T) As T
        Dim Val As String = ""
        If Settings.TryGetValue(Key, Val) AndAlso Not String.IsNullOrEmpty(Val) Then
            Try
                Return CType(CObj(Val), T)
            Catch
                SetProgramSetting(Of T)(Key, DefaultVal) 'Couldn't convert the value to a proper format; resetting.
            End Try
        End If
        Return DefaultVal
    End Function

    Public Sub SetProgramSetting(Of T)(ByVal Key As String, ByVal Value As T)
        Settings(Key) = Value.ToString
    End Sub

    Public Sub LoadProgramSettings()
        If SettingsLoaded Then Exit Sub

        IO.Directory.CreateDirectory(ConfigRootDir)
        If Not IO.File.Exists(MainConfigFile) Then
            IO.File.Create(MainConfigFile).Close()
            Exit Sub
        End If

        Dim ConfigString As String
        Try
            ConfigString = IO.File.ReadAllText(MainConfigFile)
        Catch Ex As IO.IOException
            System.Threading.Thread.Sleep(200) 'Freeze for 1/5 of a second to allow for the other user to release the file.
            ConfigString = IO.File.ReadAllText(MainConfigFile)
        End Try

        For Each Setting As String In ConfigString.Split(";"c)
            Dim Pair As String() = Setting.Split(":".ToCharArray, 2)
            If Pair.Length() < 2 Then Continue For
            If Settings.ContainsKey(Pair(0)) Then Settings.Remove(Pair(0))
            Settings.Add(Pair(0).Trim, Pair(1).Trim)
        Next

        SettingsLoaded = True
    End Sub

    Public Sub SaveProgramSettings() 'LATER: Unify the 'mainconfig.ini' and 'profile.sync' formats.
        Dim ConfigStrB As New Text.StringBuilder
        For Each Setting As KeyValuePair(Of String, String) In Settings
            ConfigStrB.AppendFormat("{0}:{1};", Setting.Key, Setting.Value)
        Next

        Try
            IO.File.WriteAllText(MainConfigFile, ConfigStrB.ToString) 'IO.File.WriteAllText overwrites the file.
        Catch
            Interaction.ShowDebug("Unable to save main config file.")
        End Try
    End Sub

    Public Function ProgramSettingsSet(ByVal Setting As String) As Boolean
        Return Settings.ContainsKey(Setting)
    End Function

    <Diagnostics.Conditional("Debug")>
    Public Sub LogDebugEvent(ByVal EventData As String)
#If DEBUG Then
        LogAppEvent(EventData)
#End If
    End Sub

    Private Sub TrimAppLog()
        If IO.File.Exists(AppLogFile) AndAlso Utilities.GetSize(AppLogFile) > ProgramSetting.AppLogThreshold Then
            Dim AppLogBackup As String = AppLogFile & ".old"

            If IO.File.Exists(AppLogBackup) Then IO.File.Delete(AppLogBackup)
            IO.File.Move(AppLogFile, AppLogBackup)

            LogAppEvent("Moved " & AppLogFile & " to " & AppLogBackup)
        End If
    End Sub

    Public Sub LogAppEvent(ByVal EventData As String)
        If ProgramSetting.Debug Or CommandLine.Silent Or CommandLine.Log Then
            Static UniqueID As String = Guid.NewGuid().ToString

            Try
                Using AppLog As New IO.StreamWriter(AppLogFile, True)
                    AppLog.WriteLine(String.Format("[{0}][{1}] {2}", UniqueID, Date.Now.ToString(), EventData.Replace(Environment.NewLine, " // ")))
                End Using
            Catch Ex As IO.IOException
                ' File in use.
            End Try
        End If
    End Sub

    Public Sub RegisterBoot()
        If ProgramConfig.GetProgramSetting(Of Boolean)(ProgramSetting.AutoStartupRegistration, True) Then
            If Microsoft.Win32.Registry.GetValue(ProgramSetting.RegistryRootedBootKey, ProgramSetting.RegistryBootVal, Nothing) Is Nothing Then
                LogAppEvent("Registering program in startup list")
                Microsoft.Win32.Registry.SetValue(ProgramSetting.RegistryRootedBootKey, ProgramSetting.RegistryBootVal, String.Format("""{0}"" /scheduler", Application.ExecutablePath))
            End If
        End If
    End Sub

    Public Sub IncrementSyncsCount()
        Try
            Dim Count As Integer
            If IO.File.Exists(StatsFile) AndAlso Integer.TryParse(IO.File.ReadAllText(StatsFile), Count) Then IO.File.WriteAllText(StatsFile, (Count + 1).ToString)
        Catch
        End Try
    End Sub
End Class

Structure CommandLine
    Enum RunMode
        Normal
        Scheduler
        Queue
#If DEBUG Then
        Scanner
#End If
    End Enum

    Shared Help As Boolean '= False
    Shared Quiet As Boolean '= False
    Shared TasksToRun As String = ""
    Shared RunAll As Boolean '= False
    Shared ShowPreview As Boolean '= False
    Shared RunAs As RunMode '= RunMode.Normal
    Shared Silent As Boolean '= False
    Shared Log As Boolean '= False
    Shared NoUpdates As Boolean '= False
    Shared NoStop As Boolean '= False
#If DEBUG Then
    Shared ScanPath As String = ""
#End If

    Shared Sub ReadArgs(ByVal ArgsList As List(Of String))
#If DEBUG Then
        ProgramConfig.LogDebugEvent("Parsing command line settings")
        For Each Param As String In ArgsList
            ProgramConfig.LogDebugEvent("  Got: " + Param)
        Next
        ProgramConfig.LogDebugEvent("Done.")
#End If

        If ArgsList.Count > 1 Then
            CommandLine.Help = ArgsList.Contains("/help")
            CommandLine.Quiet = ArgsList.Contains("/quiet")
            CommandLine.ShowPreview = ArgsList.Contains("/preview")
            CommandLine.Silent = ArgsList.Contains("/silent")
            CommandLine.Log = ArgsList.Contains("/log")
            CommandLine.NoUpdates = ArgsList.Contains("/noupdates")
            CommandLine.NoStop = ArgsList.Contains("/nostop")
            CommandLine.RunAll = ArgsList.Contains("/all")

            Dim RunArgIndex As Integer = ArgsList.IndexOf("/run")
            If (Not CommandLine.RunAll) AndAlso RunArgIndex <> -1 AndAlso RunArgIndex + 1 < ArgsList.Count Then
                CommandLine.TasksToRun = ArgsList(RunArgIndex + 1)
            End If

#If DEBUG Then
            Dim ScanArgIndex As Integer = ArgsList.IndexOf("/scan")
            If ScanArgIndex <> -1 AndAlso ScanArgIndex + 1 < ArgsList.Count Then
                CommandLine.ScanPath = ArgsList(ScanArgIndex + 1)
            End If
#End If
        End If

        If CommandLine.RunAll Or CommandLine.TasksToRun <> "" Then
            CommandLine.RunAs = CommandLine.RunMode.Queue
        ElseIf ArgsList.Contains("/scheduler") Then
            CommandLine.RunAs = CommandLine.RunMode.Scheduler
#If DEBUG Then
        ElseIf CommandLine.ScanPath <> "" Then
            CommandLine.RunAs = CommandLine.RunMode.Scanner
#End If
        End If

        CommandLine.Quiet = CommandLine.Quiet Or CommandLine.RunAs = RunMode.Scheduler Or CommandLine.Silent
    End Sub
End Structure
