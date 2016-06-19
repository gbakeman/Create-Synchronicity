'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Friend Module ProfileSetting
    Public Const Source As String = "Source Directory"
    Public Const Destination As String = "Destination Directory"
    Public Const IncludedTypes As String = "Included Filetypes"
    Public Const ExcludedTypes As String = "Excluded FileTypes"
    Public Const ReplicateEmptyDirectories As String = "Replicate Empty Directories"
    Public Const Method As String = "Synchronization Method"
    Public Const Restrictions As String = "Files restrictions"
    Public Const LeftSubFolders As String = "Source folders to be synchronized"
    Public Const RightSubFolders As String = "Destination folders to be synchronized"
    Public Const MayCreateDestination As String = "Create destination folder"
    Public Const StrictDateComparison As String = "Strict date comparison"
    Public Const PropagateUpdates As String = "Propagate Updates"
    Public Const StrictMirror As String = "Strict mirror"
    Public Const TimeOffset As String = "Time Offset"
    Public Const LastRun As String = "Last run"
    Public Const CatchUpSync As String = "Catch up if missed"
    Public Const CompressionExt As String = "Compress"
    Public Const Group As String = "Group"
    Public Const CheckFileSize As String = "Check file size"
    Public Const FuzzyDstCompensation As String = "Fuzzy DST compensation"
    Public Const Checksum As String = "Checksum"

    'Next settings are hidden, not automatically appended to config files.
    Public Const ExcludedFolders As String = "Excluded folder patterns"
    Public Const WakeupAction As String = "Wakeup action"
    Public Const PostSyncAction As String = "Post-sync action"
    Public Const ExcludeHidden As String = "Exclude hidden entries"
    Public Const DiscardAfter As String = "Discard after"
    Public Const PreviewOnly As String = "Preview only"
    Public Const SyncFolderAttributes As String = "Sync folder attributes"
    Public Const ErrorsLog As String = "Track errors separately"
    Public Const AutoIncludeNewFolders As String = "Auto-include new folders" 'TODO: Not ready for mass use yet.
    Public Const LastModified As String = "Last modified"
    Public Const Decompress As String = "Decompress"
    '</>

    'Disabled: would require keeping a list of modified files to work, since once a source file is deleted in the source, there's no way to tell when it had been last modified, and hence no way to calculate the appropriate deletion date.
    'Public Const Delay As String = "Delay deletions"

    Public Const Scheduling As String = "Scheduling"
    Public Const SchedulingSettingsCount As Integer = 5 'Frequency;WeekDay;MonthDay;Hour;Minute

    Enum SyncMethod
        LRMirror = 0
        LRIncremental = 1
        BiIncremental = 2
    End Enum

    Public Const DefaultMethod As Integer = CInt(SyncMethod.LRIncremental)
End Module

NotInheritable Class ProfileHandler
    Public ProfileName As String
    Public IsNewProfile As Boolean
    Public Scheduler As New ScheduleInfo()

    Public ConfigPath As String
    Public LogPath As String
    Public ErrorsLogPath As String

    Public Configuration As New Dictionary(Of String, String)
    Public LeftCheckedNodes As New Dictionary(Of String, Boolean)
    Public RightCheckedNodes As New Dictionary(Of String, Boolean)

    'NOTE: Only vital settings should be checked for correctness, since the config will be rejected if a mismatch occurs.
    Private Shared ReadOnly RequiredSettings() As String = {ProfileSetting.Source, ProfileSetting.Destination, ProfileSetting.ExcludedTypes, ProfileSetting.IncludedTypes, ProfileSetting.LeftSubFolders, ProfileSetting.RightSubFolders, ProfileSetting.Method, ProfileSetting.Restrictions, ProfileSetting.ReplicateEmptyDirectories}

    Public Sub New(ByVal Name As String)
        ProfileName = Name

        ConfigPath = ProgramConfig.GetConfigPath(Name)
        LogPath = ProgramConfig.GetLogPath(Name)
        ErrorsLogPath = ProgramConfig.GetErrorsLogPath(Name)

        IsNewProfile = Not LoadConfigFile()

        'Never use GetSetting(Of SyncMethod). It searches the config file for a string containing an int (eg "0"), but when failing it calls SetSettings which saves a string containing an enum label (eg. "LRIncremental")
        If GetSetting(Of Integer)(ProfileSetting.Method, ProfileSetting.DefaultMethod) <> ProfileSetting.SyncMethod.LRMirror Then
            'Disable Mirror-Specific settings.
            SetSetting(Of Boolean)(ProfileSetting.StrictMirror, False)
            SetSetting(Of Integer)(ProfileSetting.DiscardAfter, 0)
        End If

        'Post-sync actions require a separate errors log
        If GetSetting(Of String)(ProfileSetting.PostSyncAction) IsNot Nothing Then SetSetting(Of Boolean)(ProfileSetting.ErrorsLog, True)

        'Sanity checks: if no folders were included on the right due to automatic destination creation, select all folders
        If GetSetting(Of Boolean)(ProfileSetting.MayCreateDestination, False) And GetSetting(Of String)(ProfileSetting.RightSubFolders, "") = "" Then SetSetting(Of String)(ProfileSetting.RightSubFolders, "*")
    End Sub

    Function LoadConfigFile() As Boolean
        If Not IO.File.Exists(ConfigPath) Then Return False

        Configuration.Clear()
        Using FileReader As New IO.StreamReader(ConfigPath)
            While Not FileReader.EndOfStream
                Dim ConfigLine As String = ""

                ConfigLine = FileReader.ReadLine()
                Dim Param() As String = ConfigLine.Split(":".ToCharArray, 2)
                If Param.Length < 2 Then
                    Interaction.ShowMsg(Translation.TranslateFormat("\INVALID_SETTING", ConfigLine))
                    ProgramConfig.LogAppEvent("Invalid setting for profile '" & ProfileName & "': " & ConfigLine)
                ElseIf Not Configuration.ContainsKey(Param(0)) Then
                    Configuration.Add(Param(0), Param(1))
                End If
            End While
        End Using

        LoadScheduler()
        LoadSubFoldersList(ProfileSetting.LeftSubFolders, LeftCheckedNodes)
        LoadSubFoldersList(ProfileSetting.RightSubFolders, RightCheckedNodes)

        Return True
    End Function

    Function SaveConfigFile() As Boolean
        Try
            Using FileWriter As New IO.StreamWriter(ConfigPath)
                For Each Setting As KeyValuePair(Of String, String) In Configuration
                    FileWriter.WriteLine(Setting.Key & ":" & Setting.Value)
                Next
            End Using

            Return True
        Catch Ex As Exception
            ProgramConfig.LogAppEvent("Unable to save config file for " & ProfileName & Environment.NewLine & Ex.ToString)
            Return False
        End Try
    End Function

    ' `ReturnString` is used to pass locally generated error messages to caller.
    Function ValidateConfigFile(Optional ByVal WarnUnrootedPaths As Boolean = False, Optional ByVal TryCreateDest As Boolean = False, Optional ByRef FailureMsg As String = Nothing) As Boolean
        Dim IsValid As Boolean = True
        Dim InvalidListing As New List(Of String)

        Dim Dest As String = TranslatePath(GetSetting(Of String)(ProfileSetting.Destination))

        Static NeedsWakeup As Boolean = True 'Static, but not shared.
        Dim Action As String = Me.GetSetting(Of String)(ProfileSetting.WakeupAction)
        If NeedsWakeup And ProgramConfig.GetProgramSetting(Of Boolean)(ProgramSetting.ExpertMode, False) And Action IsNot Nothing Then
            Try
                'Call Wake-up script in a blocking way
                System.Diagnostics.Process.Start(Action, Dest).WaitForExit()
                NeedsWakeup = False
            Catch Ex As Exception
                Interaction.ShowMsg(Translation.Translate("\WAKEUP_FAILED"))
                ProgramConfig.LogAppEvent(Ex.ToString)
                IsValid = False
            End Try
        End If

        If Not IO.Directory.Exists(TranslatePath(GetSetting(Of String)(ProfileSetting.Source))) Then
            InvalidListing.Add(Translation.Translate("\INVALID_SOURCE"))
            IsValid = False
        End If

        'TryCreateDest <=> When this function returns, the folder should exist.
        'MayCreateDest <=> Creating the destination folder is allowed for this folder.
        Dim MayCreateDest As Boolean = GetSetting(Of Boolean)(ProfileSetting.MayCreateDestination, False)
        If MayCreateDest And TryCreateDest Then
            Try
                IO.Directory.CreateDirectory(Dest)
            Catch Ex As Exception
                InvalidListing.Add(Translation.TranslateFormat("\FOLDER_FAILED", Dest, Ex.Message))
            End Try
        End If

        If (Not IO.Directory.Exists(Dest)) And (TryCreateDest Or (Not MayCreateDest)) Then
            InvalidListing.Add(Translation.Translate("\INVALID_DEST"))
            IsValid = False
        End If

        For Each Key As String In RequiredSettings
            If Not Configuration.ContainsKey(Key) Then
                IsValid = False
                InvalidListing.Add(Translation.TranslateFormat("\SETTING_UNSET", Key))
            End If
        Next

        If GetSetting(Of String)(ProfileSetting.CompressionExt, "") <> "" Then
            If Array.IndexOf({".gz", ".bz2"}, Configuration(ProfileSetting.CompressionExt)) < 0 Then
                IsValid = False
                InvalidListing.Add("Unknown compression extension, or missing ""."":" & Configuration(ProfileSetting.CompressionExt))
            End If

            If Not IO.File.Exists(ProgramConfig.CompressionDll) Then
                IsValid = False
                InvalidListing.Add(String.Format("{0} not found!", ProgramConfig.CompressionDll))
            End If
        End If

        If Not IsValid Then
            Dim ErrorsList As String = String.Join(Environment.NewLine, InvalidListing.ToArray)
            Dim ErrMsg As String = String.Format("{0} - {1}{2}{3}", ProfileName, Translation.Translate("\INVALID_CONFIG"), Environment.NewLine, ErrorsList)

            If Not FailureMsg Is Nothing Then FailureMsg = ErrMsg '(FailureMsg is passed ByRef)
            If Not CommandLine.Quiet Then Interaction.ShowMsg(ErrMsg, Translation.Translate("\INVALID_CONFIG"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Return False
        Else
            If WarnUnrootedPaths Then
                If Not IO.Path.IsPathRooted(TranslatePath(GetSetting(Of String)(ProfileSetting.Source))) Then
                    If Interaction.ShowMsg(Translation.TranslateFormat("\LEFT_UNROOTED", IO.Path.GetFullPath(GetSetting(Of String)(ProfileSetting.Source))), , MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then Return False
                End If

                If Not IO.Path.IsPathRooted(TranslatePath(GetSetting(Of String)(ProfileSetting.Destination))) Then
                    If Interaction.ShowMsg(Translation.TranslateFormat("\RIGHT_UNROOTED", IO.Path.GetFullPath(GetSetting(Of String)(ProfileSetting.Destination))), , MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.No Then Return False
                End If
            End If

            Return True
        End If
    End Function

    Function Rename(ByVal NewName As String) As Boolean
        'Don't exit if there's a case change.
        If (Not String.Equals(ProfileName, NewName, StringComparison.OrdinalIgnoreCase)) And (IO.File.Exists(ProgramConfig.GetLogPath(NewName)) Or IO.File.Exists(ProgramConfig.GetErrorsLogPath(NewName)) Or IO.File.Exists(ProgramConfig.GetConfigPath(NewName))) Then Return False

        Try
            If IO.File.Exists(ErrorsLogPath) Then IO.File.Move(ErrorsLogPath, ProgramConfig.GetErrorsLogPath(NewName))
            If IO.File.Exists(LogPath) Then IO.File.Move(LogPath, ProgramConfig.GetLogPath(NewName))
            IO.File.Move(ConfigPath, ProgramConfig.GetConfigPath(NewName))

            ProfileName = NewName 'Not really useful in the current situation : profiles are reloaded just after renaming anyway.
        Catch
            Return False
        End Try
        Return True
    End Function

    Sub DeleteConfigFile()
        IO.File.Delete(ConfigPath)
        DeleteLogFiles()
    End Sub

    Sub DeleteLogFiles()
        IO.File.Delete(LogPath)
        IO.File.Delete(ErrorsLogPath)
    End Sub

    Sub SetSetting(Of T)(ByVal SettingName As String, ByVal Value As T)
        Configuration(SettingName) = Value.ToString 'Dates are serialized in a locale-dependent way.
    End Sub

    Sub CopySetting(Of T)(ByVal Key As String, ByRef Value As T, ByVal Load As Boolean)
        If Load Then
            Value = GetSetting(Of T)(Key, Value) 'Passes the current value as default answer.
        Else
            Configuration(Key) = If(Value IsNot Nothing, Value.ToString, Nothing)
        End If
    End Sub

    Function GetSetting(Of T)(ByVal Key As String, Optional ByVal DefaultVal As T = Nothing) As T
        Dim Val As String = ""
        If Configuration.TryGetValue(Key, Val) AndAlso Not String.IsNullOrEmpty(Val) Then
            Try
                Return CType(CObj(Val), T)
            Catch
                SetSetting(Of T)(Key, DefaultVal) 'Couldn't convert the value to a proper format; resetting.
            End Try
        End If
        Return DefaultVal
    End Function

    Sub LoadScheduler()
        Dim Opts() As String = GetSetting(Of String)(ProfileSetting.Scheduling, "").Split(";".ToCharArray, StringSplitOptions.RemoveEmptyEntries)

        If Opts.GetLength(0) = ProfileSetting.SchedulingSettingsCount Then
            Scheduler = New ScheduleInfo(Opts(0), Opts(1), Opts(2), Opts(3), Opts(4))
        Else
            Scheduler = New ScheduleInfo() With {.Frequency = ScheduleInfo.Freq.Never} 'NOTE: Wrong strings default to never
        End If
    End Sub

    Sub SaveScheduler()
        SetSetting(Of String)(ProfileSetting.Scheduling, String.Join(";", New String() {Scheduler.Frequency.ToString, Scheduler.WeekDay.ToString, Scheduler.MonthDay.ToString, Scheduler.Hour.ToString, Scheduler.Minute.ToString}))
    End Sub

    Sub LoadSubFoldersList(ByVal ConfigLine As String, ByRef Subfolders As Dictionary(Of String, Boolean))
        Subfolders.Clear()
        Dim ConfigCheckedFoldersList As New List(Of String)(GetSetting(Of String)(ConfigLine, "").Split(";"c))
        ConfigCheckedFoldersList.RemoveAt(ConfigCheckedFoldersList.Count - 1) 'Removes the last, empty element
        ' Warning: The trailing comma can't be removed when generating the configuration string.
        ' Using StringSplitOptions.RemoveEmptyEntries would make no difference between ';' (root folder selected, no subfolders) and '' (nothing selected at all)

        For Each Dir As String In ConfigCheckedFoldersList
            Dim Recursive As Boolean = False

            If Dir.EndsWith("*") Then
                Recursive = True
                Dir = Dir.Substring(0, Dir.Length - 1)
            End If

            If Not Subfolders.ContainsKey(Dir) Then Subfolders.Add(Dir, Recursive)
        Next
    End Sub

    Public Shared Function TranslatePath(ByVal Path As String) As String
        If Path Is Nothing Then Return Nothing
        Return TranslatePath_Unsafe(Path).TrimEnd(ProgramSetting.DirSep) 'Careful with Linux root
        'Prevents a very annoying bug, where the presence of a slash at the end of the base directory would confuse the engine (#3052979)
    End Function

    Public Shared Function TranslatePath_Inverse(ByVal Path As String) As String
#If CONFIG <> "Linux" Then
        If Text.RegularExpressions.Regex.IsMatch(Path, "^(?<driveletter>[A-Z]\:)(\\(?<relativepath>.*))?$") Then
            Dim Label As String = ""
            For Each Drive As IO.DriveInfo In IO.DriveInfo.GetDrives
                If Drive.Name(0) = Path(0) Then Label = Drive.VolumeLabel
            Next
            If Label <> "" Then Return String.Format("""{0}""\{1}", Label, Path.Substring(2).Trim(ProgramSetting.DirSep)).TrimEnd(ProgramSetting.DirSep)
        End If
#End If

        Return Path
    End Function

    Private Shared Function TranslatePath_Unsafe(ByVal Path As String) As String
        Dim Translated_Path As String = Path

#If CONFIG <> "Linux" Then
        Dim Label As String, RelativePath As String
        If Path.StartsWith("""") Or Path.StartsWith(":") Then
            Dim ClosingPos As Integer = Path.LastIndexOfAny(""":".ToCharArray)
            If ClosingPos = 0 Then Return "" 'LINUX: Currently returns "" (aka linux root) if no closing op is found.

            Label = Path.Substring(1, ClosingPos - 1)
            RelativePath = Path.Substring(ClosingPos + 1)

            If Path.StartsWith("""") And Not Label = "" Then
                For Each Drive As IO.DriveInfo In IO.DriveInfo.GetDrives
                    'The drive's name ends with a "\". If RelativePath = "", then TrimEnd on the RelativePath won't do anything; that's why you trim after joining
                    If Not Drive.Name(0) = "A"c AndAlso Drive.IsReady AndAlso String.Compare(Drive.VolumeLabel, Label, True) = 0 Then
                        'This is the line why this function is called unsafe: no path should *ever* end with a DirSep, otherwise the system gets confused as to what base and added path sections are.
                        Translated_Path = (Drive.Name & RelativePath.TrimStart(ProgramSetting.DirSep)).TrimEnd(ProgramSetting.DirSep) 'Bug #3052979
                        Exit For
                    End If
                Next
            End If
        End If
#End If

        ' Use a path-friendly version of the DATE constant.
        Environment.SetEnvironmentVariable("MMMYYYY", Date.Today.ToString("MMMyyyy").ToLower(Interaction.InvariantCulture))
        Environment.SetEnvironmentVariable("DATE", Date.Today.ToShortDateString.Replace("/"c, "-"c))
        Environment.SetEnvironmentVariable("DAY", Date.Today.ToString("dd"))
        Environment.SetEnvironmentVariable("MONTH", Date.Today.ToString("MM"))
        Environment.SetEnvironmentVariable("YEAR", Date.Today.ToString("yyyy"))

        Return Environment.ExpandEnvironmentVariables(Translated_Path)
    End Function

    Public Function GetLastRun() As Date
        Try
            Return GetSetting(Of Date)(ProfileSetting.LastRun, ScheduleInfo.DATE_NEVER) 'NOTE: Conversion seems ok, but there might be locale-dependent problems arising.
        Catch
            Return ScheduleInfo.DATE_NEVER
        End Try
    End Function

    Public Sub SetLastRun()
        SetSetting(Of Date)(ProfileSetting.LastRun, Date.Now)
        SaveConfigFile()
    End Sub

    Public Function FormatLastRun(Optional ByVal Format As String = "") As String
        Dim LastRun As Date = GetLastRun()
        Return If(LastRun = ScheduleInfo.DATE_NEVER, "-", Translation.TranslateFormat("\LAST_SYNC", (Date.Now - LastRun).Days.ToString(Format), (Date.Now - LastRun).Hours.ToString(Format), LastRun.ToString))
    End Function

    Public Function FormatMethod() As String
        Select Case GetSetting(Of Integer)(ProfileSetting.Method, ProfileSetting.DefaultMethod)  'Important: (Of Integer)
            Case ProfileSetting.SyncMethod.LRMirror
                Return Translation.Translate("\LR_MIRROR")
            Case ProfileSetting.SyncMethod.BiIncremental
                Return Translation.Translate("\TWOWAYS_INCREMENTAL")
            Case Else
                Return Translation.Translate("\LR_INCREMENTAL")
        End Select
    End Function
End Class

Structure SchedulerEntry
    Dim Name As String
    Dim NextRun As Date
    Dim CatchUp As Boolean
    Dim HasFailed As Boolean

    Sub New(ByVal _Name As String, ByVal _NextRun As Date, ByVal _Catchup As Boolean, ByVal _HasFailed As Boolean)
        Name = _Name
        NextRun = _NextRun
        CatchUp = _Catchup
        HasFailed = _HasFailed
    End Sub
End Structure

Structure ScheduleInfo
    Enum Freq
        Never
        Daily
        Weekly
        Monthly
    End Enum

    Public Frequency As Freq
    Public WeekDay, MonthDay, Hour, Minute As Integer 'Sunday = 0

    Public Shared ReadOnly DATE_NEVER As Date = Date.MaxValue
    Public Shared ReadOnly DATE_CATCHUP As Date = Date.MinValue

    Sub New(ByVal Frq As String, ByVal _WeekDay As String, ByVal _MonthDay As String, ByVal _Hour As String, ByVal _Minute As String)
        Try
            Hour = CInt(_Hour)
            Minute = CInt(_Minute)
            WeekDay = CInt(_WeekDay)
            MonthDay = CInt(_MonthDay)
            Frequency = Str2Freq(Frq)
        Catch Ex As FormatException
        Catch Ex As OverflowException
        End Try
    End Sub

    Private Shared Function Str2Freq(ByVal Str As String) As Freq
        Try
            Return CType([Enum].Parse(GetType(Freq), Str, True), Freq)
        Catch Ex As ArgumentException
            Return Freq.Never
        End Try
    End Function

    Function GetInterval() As TimeSpan
        Dim Interval As TimeSpan
        Select Case Frequency
            Case Freq.Daily
                Interval = New TimeSpan(1, 0, 0, 0)
            Case Freq.Weekly
                Interval = New TimeSpan(7, 0, 0, 0)
            Case Freq.Monthly
                Interval = Date.Today.AddMonths(1) - Date.Today
            Case Freq.Never
                Interval = New TimeSpan(0)
        End Select

        Return Interval
    End Function

    Function NextRun() As Date
        Dim Now As Date = Date.Now
        Dim Today As Date = Date.Today

        Dim RunAt As Date
        Dim Interval As TimeSpan = GetInterval()

        Select Case Frequency
            Case Freq.Daily
                RunAt = Today.AddHours(Hour).AddMinutes(Minute)
            Case Freq.Weekly
                RunAt = Today.AddDays(WeekDay - Today.DayOfWeek).AddHours(Hour).AddMinutes(Minute)
            Case Freq.Monthly
                RunAt = Today.AddDays(MonthDay - Today.Day).AddHours(Hour).AddMinutes(Minute)
            Case Else
                Return DATE_NEVER
        End Select

        '">=" prevents double-syncing. Using ">" could cause the scheduler to queue Date.Now as next run time.
        While Now >= RunAt : RunAt += Interval : End While 'Loop needed (eg when today = jan 1 and schedule = every 1st month day)
        Return RunAt
    End Function
End Structure
