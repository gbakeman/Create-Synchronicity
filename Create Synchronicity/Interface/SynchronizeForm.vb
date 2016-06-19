'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Option Strict On

Friend Class SynchronizeForm
    Private Log As LogHandler
    Private Handler As ProfileHandler

    Private ValidFiles As New Dictionary(Of String, Boolean)
    Private SyncingList As New List(Of SyncingItem)
    Private IncludedPatterns As New List(Of FileNamePattern)
    Private ExcludedPatterns As New List(Of FileNamePattern)
    Private ExcludedDirPatterns As New List(Of FileNamePattern)

    Private Labels() As String = Nothing
    Private StatusLabel As String = ""
    Private Lock As New Object()

    Private Catchup As Boolean 'Indicates whether this operation was started due to catchup rules.
    Private Preview As Boolean 'Should show a preview.

    Private Status As StatusData
    Private TitleText As String
    Private Sorter As New SyncingListSorter(3)

    Private FullSyncThread As Threading.Thread
    Private ScanThread As Threading.Thread
    Private SyncThread As Threading.Thread

    Private AutoInclude As Boolean, MDate As Date
    Private LeftRootPath, RightRootPath As String 'Translated path to left and right folders

    Private Delegate Sub StepCompletedCall(ByVal Id As StatusData.SyncStep)
    Private Delegate Sub SetIntCall(ByVal Id As StatusData.SyncStep, ByVal Max As Integer)

    Friend Event SyncFinished(ByVal Name As String, ByVal Completed As Boolean)

    'Not evaluating file size gives better performance (See speed-test.vb for tests):
    'With size evaluation: 1'20, 46'', 36'', 35'', 31''
    'Without:                    41'', 42'', 26'', 29''

#Region " Events "
    Public Sub New(ByVal ConfigName As String, ByVal DisplayPreview As Boolean, ByVal _Catchup As Boolean)
        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Catchup = _Catchup
        Preview = DisplayPreview
        SyncBtn.Enabled = False
        SyncBtn.Visible = Preview

        Status = New StatusData
        Status.CurrentStep = StatusData.SyncStep.Scan
        Status.StartTime = Date.Now ' NOTE: This call should be useless; it however seems that when the messagebox.show method is called when a profile is not found, the syncingtimecounter starts ticking. This is not suitable, but until the cause is found there this call remains, for display consistency.

        Handler = New ProfileHandler(ConfigName)
        Log = New LogHandler(ConfigName, Handler.GetSetting(Of Boolean)(ProfileSetting.ErrorsLog, False))

        MDate = Handler.GetSetting(Of Date)(ProfileSetting.LastModified, IO.File.GetLastWriteTimeUtc(Handler.ConfigPath))
        AutoInclude = Handler.GetSetting(Of Boolean)(ProfileSetting.AutoIncludeNewFolders, False)
        LeftRootPath = ProfileHandler.TranslatePath(Handler.GetSetting(Of String)(ProfileSetting.Source))
        RightRootPath = ProfileHandler.TranslatePath(Handler.GetSetting(Of String)(ProfileSetting.Destination))

        FileNamePattern.LoadPatternsList(IncludedPatterns, Handler.GetSetting(Of String)(ProfileSetting.IncludedTypes, ""), False, ProgramSetting.ExcludedFolderPrefix)
        FileNamePattern.LoadPatternsList(ExcludedPatterns, Handler.GetSetting(Of String)(ProfileSetting.ExcludedTypes, ""), False, ProgramSetting.ExcludedFolderPrefix)
        FileNamePattern.LoadPatternsList(ExcludedDirPatterns, Handler.GetSetting(Of String)(ProfileSetting.ExcludedFolders, ""), True, "")
        FileNamePattern.LoadPatternsList(ExcludedDirPatterns, Handler.GetSetting(Of String)(ProfileSetting.ExcludedTypes, ""), True, ProgramSetting.ExcludedFolderPrefix)

        FullSyncThread = New Threading.Thread(AddressOf FullSync)
        ScanThread = New Threading.Thread(AddressOf Scan)
        SyncThread = New Threading.Thread(AddressOf Sync)

        Me.CreateHandle()
        Translation.TranslateControl(Me)
        Me.Icon = ProgramConfig.Icon
        TitleText = String.Format(Me.Text, Handler.ProfileName, LeftRootPath, RightRootPath)

        Labels = New String() {"", Step1StatusLabel.Text, Step2StatusLabel.Text, Step3StatusLabel.Text}

#If LINUX Then
        Step1ProgressBar.MarqueeAnimationSpeed = 5000
        SyncingTimer.Interval = 1000
#End If
    End Sub

    Sub StartSynchronization(ByVal CalledShowModal As Boolean)
        ProgramConfig.CanGoOn = False

#If DEBUG Then
        Log.LogInfo("Synchronization started.")
        Log.LogInfo("Profile settings:")
        For Each Pair As KeyValuePair(Of String, String) In Handler.Configuration
            Log.LogInfo(String.Format("    {0,-50}: {1}", Pair.Key, Pair.Value))
        Next
        Log.LogInfo("Done.")
#End If

        If CommandLine.Quiet Then
            Me.Visible = False

            Interaction.StatusIcon.ContextMenuStrip = Nothing
            AddHandler Interaction.StatusIcon.Click, AddressOf StatusIcon_Click

            Interaction.StatusIcon.Text = Translation.Translate("\RUNNING")

            Interaction.ToggleStatusIcon(True)
            If Catchup Then
                Interaction.ShowBalloonTip(Translation.TranslateFormat("\CATCHING_UP", Handler.ProfileName, Handler.FormatLastRun()))
            Else
                Interaction.ShowBalloonTip(Translation.TranslateFormat("\RUNNING_TASK", Handler.ProfileName))
            End If
        Else
            If Not CalledShowModal Then Me.Visible = True
        End If

        Status.FailureMsg = ""

        Dim IsValid As Boolean = Handler.ValidateConfigFile(False, True, Status.FailureMsg)
        If Handler.GetSetting(Of Boolean)(PreviewOnly, False) And (Not Preview) Then IsValid = False

        Status.Failed = Not IsValid

        If IsValid Then
            ProgramConfig.IncrementSyncsCount()
            If Preview Then
                ScanThread.Start()
            Else
                FullSyncThread.Start()
            End If
        Else
            EndAll() 'Also saves the log file
        End If
    End Sub

    Private Sub SynchronizeForm_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        If e.Control Then
            If e.KeyCode = Keys.L AndAlso Status.CurrentStep = StatusData.SyncStep.Done Then
                Interaction.StartProcess(Handler.LogPath)
            ElseIf e.KeyCode = Keys.D And PreviewList.SelectedIndices.Count <> 0 Then
                Dim DiffProgram As String = ProgramConfig.GetProgramSetting(Of String)(ProgramSetting.DiffProgram, "")

                Dim NewFile As String = "", OldFile As String = ""
                If Not SetPathFromSelectedItem(NewFile, OldFile) Then Exit Sub

                Try
                    If DiffProgram <> "" AndAlso IO.File.Exists(OldFile) AndAlso IO.File.Exists(NewFile) Then Interaction.StartProcess(DiffProgram, """" & OldFile & """ """ & NewFile & """")
                Catch Ex As Exception
                    Interaction.ShowMsg("Error loading diff: " & Ex.ToString)
                End Try
            End If
        End If
    End Sub

    Private Sub SynchronizeForm_FormClosed(ByVal sender As System.Object, ByVal e As System.Windows.Forms.FormClosedEventArgs) Handles MyBase.FormClosed
        EndAll()
        ProgramConfig.CanGoOn = True
        Interaction.StatusIcon.ContextMenuStrip = MainFormInstance.StatusIconMenu
        RemoveHandler Interaction.StatusIcon.Click, AddressOf StatusIcon_Click

        Interaction.StatusIcon.Text = Translation.Translate("\WAITING")
        RaiseEvent SyncFinished(Handler.ProfileName, Not (Status.Failed Or Status.Cancel)) 'These parameters are not used atm.
    End Sub

    Private Sub CancelBtn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles StopBtn.Click
        Select Case StopBtn.Text
            Case StopBtn.Tag.ToString.Split(";"c)(0)
                EndAll()
            Case StopBtn.Tag.ToString.Split(";"c)(1)
                Me.Close()
        End Select
    End Sub

    Private Sub SyncBtn_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SyncBtn.Click
        PreviewList.Visible = False
        SyncBtn.Visible = False
        StopBtn.Text = StopBtn.Tag.ToString.Split(";"c)(0)

        SyncThread.Start()
    End Sub

    Private Sub StatusIcon_Click(ByVal sender As Object, ByVal e As System.EventArgs) 'Handler dynamically added
        Me.Visible = Not Me.Visible
        Me.WindowState = FormWindowState.Normal
        If Me.Visible Then Me.Activate()
    End Sub

    Private Sub SynchronizeForm_Resize(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Resize
        If Me.WindowState = FormWindowState.Minimized And CommandLine.Quiet Then Me.Visible = False
    End Sub

    Private Sub SyncingTimeCounter_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SyncingTimer.Tick
        UpdateStatuses()
    End Sub

#If 0 Then
    'Works, but not really efficiently, and flickers a lot.
    Private Sub PreviewList_CacheVirtualItems(sender As Object, e As System.Windows.Forms.CacheVirtualItemsEventArgs) Handles PreviewList.CacheVirtualItems
        Static PrevStartIndex As Integer = -1
        Exit Sub
        If PrevStartIndex <> e.StartIndex Then
            PrevStartIndex = e.StartIndex
            For id As Integer = 0 To PreviewList.Columns.Count - 1
                PreviewList.AutoResizeColumn(id, ColumnHeaderAutoResizeStyle.ColumnContent)
            Next
        End If
    End Sub
#End If

    Private Sub PreviewList_RetrieveVirtualItem(sender As System.Object, e As System.Windows.Forms.RetrieveVirtualItemEventArgs) Handles PreviewList.RetrieveVirtualItem
        If Status.ShowingErrors Then
            e.Item = Log.Errors(e.ItemIndex).ToListViewItem
        Else
            e.Item = SyncingList(e.ItemIndex).ToListViewItem
        End If

        'TODO: Auto-resizing would be nice, but:
        '      * AutoResizeColumns raises RetrieveVirtualItem, causing a StackOverflowException
        '      * Checking TopItem to conditionally resize columns doesn't work in virtual mode (it even crashes the debugger).
        '      * Handling the CacheVirtualItems event works, but does flicker a lot.
    End Sub

    Private Sub PreviewList_ColumnClick(ByVal sender As System.Object, ByVal e As System.Windows.Forms.ColumnClickEventArgs) Handles PreviewList.ColumnClick
        If Status.ShowingErrors Then Exit Sub

        Sorter.RegisterClick(e)
        SyncingList.Sort(Sorter)
        PreviewList.Refresh()
    End Sub

    Private Sub PreviewList_DoubleClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PreviewList.DoubleClick
        Dim Source As String = "", Dest As String = ""
        If Not SetPathFromSelectedItem(Source, Dest) Then Exit Sub

        Dim Path As String = If((Control.ModifierKeys And Keys.Alt) <> 0, Dest, Source)
        If (Control.ModifierKeys And Keys.Control) <> 0 Then Path = IO.Path.GetDirectoryName(Path)

        If IO.File.Exists(Path) OrElse IO.Directory.Exists(Path) Then Interaction.StartProcess(Path)
    End Sub

    Private Function SetPathFromSelectedItem(ByRef Source As String, ByRef Dest As String) As Boolean
        'Exit if nothing is selected, or if in error mode
        If PreviewList.SelectedIndices.Count = 0 OrElse Status.ShowingErrors Then Return False

        Dim CurItem As SyncingItem = SyncingList(PreviewList.SelectedIndices(0))

        Dim LeftFile As String, RightFile As String
        LeftFile = LeftRootPath & CurItem.Path
        RightFile = RightRootPath & CurItem.Path

        Select Case CurItem.Side
            Case SideOfSource.Left
                Source = LeftFile : Dest = RightFile
            Case SideOfSource.Right
                Source = RightFile : Dest = LeftFile
        End Select

        Return True
    End Function

    Private Sub UpdateStatuses()
        Static PreviousActionsDone As Long = -1
        Static CanDelete As Boolean = (Handler.GetSetting(Of Integer)(ProfileSetting.Method, ProfileSetting.DefaultMethod) = SyncMethod.LRMirror)

        Status.TimeElapsed = (DateTime.Now - Status.StartTime) + New TimeSpan(1000000) ' ie +0.1s

        Dim EstimateString As String = ""
        Dim Copying As Boolean = Status.CurrentStep = StatusData.SyncStep.LR Or (Not CanDelete And Status.CurrentStep = StatusData.SyncStep.RL)

        If Status.CurrentStep = StatusData.SyncStep.Scan Then
            Speed.Text = Math.Round(Status.FilesScanned / Status.TimeElapsed.TotalSeconds).ToString & " files/s"
        ElseIf CanDelete And Status.CurrentStep = StatusData.SyncStep.RL Then
            Speed.Text = Math.Round(Status.DeletedFiles / Status.TimeElapsed.TotalSeconds).ToString & " files/s"
        ElseIf Copying AndAlso PreviousActionsDone <> Status.ActionsDone Then
            PreviousActionsDone = Status.ActionsDone

            Status.Speed = Status.BytesCopied / Status.TimeElapsed.TotalSeconds
            Speed.Text = Utilities.FormatSize(Status.Speed) & "/s"
        End If


        If Copying AndAlso Status.Speed > (1 << 10) AndAlso Status.TimeElapsed.TotalSeconds > ProgramSetting.ForecastDelay AndAlso ProgramConfig.GetProgramSetting(Of Boolean)(ProgramSetting.Forecast, False) Then
            Dim TotalTime As Integer = CInt(Math.Min(Integer.MaxValue, Status.BytesToCopy / Status.Speed))
            EstimateString = String.Format(" / ~{0}", Utilities.FormatTimespan(New TimeSpan(0, 0, TotalTime)))
        End If

        ElapsedTime.Text = Utilities.FormatTimespan(Status.TimeElapsed) & EstimateString

        Done.Text = Status.ActionsDone & "/" & Status.TotalActionsCount
        FilesDeleted.Text = Status.DeletedFiles & "/" & Status.FilesToDelete
        FilesCreated.Text = Status.CreatedFiles & "/" & Status.FilesToCreate & " (" & Utilities.FormatSize(Status.BytesCopied) & ")"
        FoldersDeleted.Text = Status.DeletedFolders & "/" & Status.FoldersToDelete
        FoldersCreated.Text = Status.CreatedFolders & "/" & Status.FoldersToCreate

        SyncLock Lock
            If Labels IsNot Nothing Then
                Step1StatusLabel.Text = Labels(1)
                Step2StatusLabel.Text = Labels(2)
                Step3StatusLabel.Text = Labels(3)
            End If
            Interaction.StatusIcon.Text = StatusLabel
        End SyncLock

        Dim PercentProgress As Integer
        If Status.CurrentStep = StatusData.SyncStep.Scan Then
            PercentProgress = 0
        ElseIf Status.CurrentStep = StatusData.SyncStep.Done OrElse Status.TotalActionsCount = 0 Then
            PercentProgress = 100
        Else
            PercentProgress = CInt(100 * Status.ActionsDone / Status.TotalActionsCount)
        End If

        'Later: No need to update every time when CurrentStep \in {Scan, Done}
        Me.Text = String.Format("({0}%) ", PercentProgress) & TitleText 'Feature requests #3037548, #3055740
    End Sub
#End Region

#Region " Interface "
    Private Sub UpdateLabel(ByVal Id As StatusData.SyncStep, ByVal Text As String)
        Dim StatusText As String = Text
        If Text.Length > 30 Then
            StatusText = "..." & Text.Substring(Text.Length - 30, 30)
        End If

        Select Case Id
            Case StatusData.SyncStep.Scan
                StatusText = Translation.TranslateFormat("\STEP_1_STATUS", StatusText)
            Case StatusData.SyncStep.LR
                StatusText = Translation.TranslateFormat("\STEP_2_STATUS", Step2ProgressBar.Value, Step2ProgressBar.Maximum, StatusText)
            Case StatusData.SyncStep.RL
                StatusText = Translation.TranslateFormat("\STEP_3_STATUS", Step3ProgressBar.Value, Step3ProgressBar.Maximum, StatusText)
        End Select

        SyncLock Lock
            Labels(Id) = Text
            StatusLabel = StatusText
        End SyncLock
    End Sub

    Private Function GetProgressBar(ByVal Id As StatusData.SyncStep) As ProgressBar
        Select Case Id
            Case StatusData.SyncStep.Scan
                Return Step1ProgressBar
            Case StatusData.SyncStep.LR
                Return Step2ProgressBar
            Case Else
                Return Step3ProgressBar
        End Select
    End Function

    Private Sub Increment(ByVal Id As StatusData.SyncStep, ByVal Progress As Integer)
        Dim CurBar As ProgressBar = GetProgressBar(Id)
        If CurBar.Value + Progress < CurBar.Maximum Then CurBar.Value += Progress
    End Sub

    Private Sub SetMax(ByVal Id As StatusData.SyncStep, ByVal MaxValue As Integer, Optional ByVal Finished As Boolean = False) 'Careful: MaxValue is an Integer.
        Dim CurBar As ProgressBar = GetProgressBar(Id)

        CurBar.Style = ProgressBarStyle.Blocks
        CurBar.Maximum = Math.Max(0, MaxValue)
        CurBar.Value = If(Finished, MaxValue, 0)
    End Sub

    Private Sub StepCompleted(ByVal StepId As StatusData.SyncStep)
        If Not Status.CurrentStep = StepId Then Exit Sub 'Prevents a potentially infinite exit loop.

        SetMax(StepId, 100, True)
        UpdateLabel(StepId, Translation.Translate("\FINISHED"))
        UpdateStatuses()

        Select Case StepId
            Case StatusData.SyncStep.Scan
                SyncingTimer.Stop()
                Status.CurrentStep = StatusData.SyncStep.LR
                If Preview Then
                    ShowPreviewList()
                    StopBtn.Text = StopBtn.Tag.ToString.Split(";"c)(1)
                End If

            Case StatusData.SyncStep.LR
                Status.CurrentStep = StatusData.SyncStep.RL

            Case StatusData.SyncStep.RL
                SyncingTimer.Stop()
                Status.CurrentStep = StatusData.SyncStep.Done

                UpdateStatuses() 'Last update, to remove forecasts.

                If Status.Failed Then
                    Interaction.ShowBalloonTip(Status.FailureMsg)
                ElseIf Log.Errors.Count > 0 Then
                    PreviewList.Visible = True
                    Status.ShowingErrors = True

                    PreviewList.VirtualMode = True 'In case it hadn't been enabled (ie. if there was no preview)
                    PreviewList.VirtualListSize = Log.Errors.Count

                    PreviewList.Columns.Clear()
                    PreviewList.Columns.Add(Translation.Translate("\ERROR"))
                    PreviewList.Columns.Add(Translation.Translate("\ERROR_DETAIL"))
                    PreviewList.Columns.Add(Translation.Translate("\PATH"))

                    PreviewList.Refresh()
                    PreviewList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)

                    If Not Status.Cancel Then Interaction.ShowBalloonTip(Translation.TranslateFormat("\SYNCED_W_ERRORS", Handler.ProfileName), Handler.LogPath)
                Else
                    If Not Status.Cancel Then Interaction.ShowBalloonTip(Translation.TranslateFormat("\SYNCED_OK", Handler.ProfileName), Handler.LogPath)
                End If

                Log.SaveAndDispose(LeftRootPath, RightRootPath, Status)
                If Not (Status.Failed Or Status.Cancel) Then Handler.SetLastRun() 'Required to implement catching up

                RunPostSync()

                If (CommandLine.Quiet And Not Me.Visible) Or CommandLine.NoStop Then
                    Me.Close()
                Else
                    StopBtn.Text = StopBtn.Tag.ToString.Split(";"c)(1)
                End If
        End Select
    End Sub

    Private Sub ShowPreviewList()
        ' This part computes acceptable defaut values for column widths, since using VirtualMode prevents from resizing based on actual values.
        ' This part requires that VirtualMode be set to False.
        Dim i1 As New SyncingItem() With {.Action = TypeOfAction.Copy, .Side = SideOfSource.Left, .Type = TypeOfItem.File, .Path = "".PadLeft(260)}
        Dim i2 As New SyncingItem() With {.Action = TypeOfAction.Copy, .Side = SideOfSource.Right, .Type = TypeOfItem.File, .IsUpdate = True}
        Dim i3 As New SyncingItem() With {.Action = TypeOfAction.Delete, .Side = SideOfSource.Right, .Type = TypeOfItem.Folder}

        PreviewList.Items.Add(i1.ToListViewItem)
        PreviewList.Items.Add(i2.ToListViewItem)
        PreviewList.Items.Add(i3.ToListViewItem)

        PreviewList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
        PreviewList.Items.Clear()

        PreviewList.VirtualMode = True
        PreviewList.Visible = True
        PreviewList.VirtualListSize = SyncingList.Count

        If Not (Status.Cancel Or Handler.GetSetting(Of Boolean)(ProfileSetting.PreviewOnly, False)) Then SyncBtn.Enabled = True
    End Sub

    Sub RunPostSync()
        ' Search for a post-sync action, requiring that Expert mode be enabled.
        Dim PostSyncAction As String = Handler.GetSetting(Of String)(ProfileSetting.PostSyncAction)

        If ProgramConfig.GetProgramSetting(Of Boolean)(ProgramSetting.ExpertMode, False) AndAlso PostSyncAction IsNot Nothing Then
            Try
                Environment.CurrentDirectory = Application.StartupPath
                Interaction.ShowBalloonTip(String.Format(Translation.Translate("\POST_SYNC"), PostSyncAction))
                Diagnostics.Process.Start(PostSyncAction, String.Format("""{0}"" ""{1}"" ""{2}"" ""{3}"" ""{4}"" ""{5}""", Handler.ProfileName, Not (Status.Cancel Or Status.Failed), Log.Errors.Count, LeftRootPath, RightRootPath, Handler.ErrorsLogPath))
            Catch Ex As Exception
                Dim Err As String = Translation.Translate("\POSTSYNC_FAILED") & Environment.NewLine & Ex.Message
                Interaction.ShowBalloonTip(Err)
                ProgramConfig.LogAppEvent(Err)
            End Try
        End If
    End Sub

    Private Sub LaunchTimer()
        Status.BytesCopied = 0
        Status.StartTime = DateTime.Now
        SyncingTimer.Start()
    End Sub

    Private Sub EndAll()
        Status.Cancel = Status.Cancel Or (Status.CurrentStep <> StatusData.SyncStep.Done)
        FullSyncThread.Abort()
        ScanThread.Abort() : SyncThread.Abort()
        StepCompleted(StatusData.SyncStep.Scan) : StepCompleted(StatusData.SyncStep.LR) : StepCompleted(StatusData.SyncStep.RL) 'This call will sleep for 5s after displaying its failure message if the backup failed.
    End Sub
#End Region

#Region " Scanning / IO "
    Private Sub FullSync()
        Scan()
        Sync()
    End Sub

    Private Sub Scan()
        Dim Context As New SyncingContext
        Dim StepCompletedCallback As New StepCompletedCall(AddressOf StepCompleted)

        'Pass 1: Create actions L->R for files/folder copy, and mark dest files that should be kept
        'Pass 2: Create actions R->L for files/folder copy/deletion, based on what was marked as Valid.

        SyncingList.Clear()
        ValidFiles.Clear()

        Me.Invoke(New Action(AddressOf LaunchTimer))
        Context.Source = SideOfSource.Left
        Context.SourceRootPath = LeftRootPath
        Context.DestinationRootPath = RightRootPath
        Init_Synchronization(Handler.LeftCheckedNodes, Context, TypeOfAction.Copy)

        Context.Source = SideOfSource.Right
        Context.SourceRootPath = RightRootPath
        Context.DestinationRootPath = LeftRootPath
        Select Case Handler.GetSetting(Of Integer)(ProfileSetting.Method, ProfileSetting.DefaultMethod) 'Important: (Of Integer)
            Case SyncMethod.LRMirror
                Init_Synchronization(Handler.RightCheckedNodes, Context, TypeOfAction.Delete)
            Case SyncMethod.LRIncremental
                'Pass
            Case SyncMethod.BiIncremental
                Init_Synchronization(Handler.RightCheckedNodes, Context, TypeOfAction.Copy)
        End Select
        Me.Invoke(StepCompletedCallback, StatusData.SyncStep.Scan)
    End Sub

    Private Sub Sync()
        Dim StepCompletedCallback As New StepCompletedCall(AddressOf StepCompleted)
        Dim SetMaxCallback As New SetIntCall(AddressOf SetMax)

        'Restore original order before syncing.
        Sorter.SortColumn = -1 ' Sorts according to initial index.
        Sorter.Order = SortOrder.Ascending
        SyncingList.Sort(Sorter)

        Me.Invoke(New Action(AddressOf LaunchTimer))
        Me.Invoke(SetMaxCallback, New Object() {StatusData.SyncStep.LR, Status.LeftActionsCount})
        Do_Tasks(SideOfSource.Left, StatusData.SyncStep.LR)
        Me.Invoke(StepCompletedCallback, StatusData.SyncStep.LR)

        Me.Invoke(SetMaxCallback, New Object() {StatusData.SyncStep.RL, Status.RightActionsCount})
        Do_Tasks(SideOfSource.Right, StatusData.SyncStep.RL)
        Me.Invoke(StepCompletedCallback, StatusData.SyncStep.RL)
    End Sub

    '"Source" is "current side", with the corresponding side stored in "Side"
    Private Sub Do_Tasks(ByVal Side As SideOfSource, ByVal CurrentStep As StatusData.SyncStep)
        Dim IncrementCallback As New SetIntCall(AddressOf Increment)

        Dim Source As String = If(Side = SideOfSource.Left, LeftRootPath, RightRootPath)
        Dim Destination As String = If(Side = SideOfSource.Left, RightRootPath, LeftRootPath)

        For Each Entry As SyncingItem In SyncingList
            If Entry.Side <> Side Then Continue For

            Dim SourcePath As String = Source & Entry.Path
            Dim DestPath As String = Destination & Entry.Path

            Try
                UpdateLabel(CurrentStep, If(Entry.Action = TypeOfAction.Delete, SourcePath, Entry.Path))

                Select Case Entry.Type
                    Case TypeOfItem.File
                        Select Case Entry.Action
                            Case TypeOfAction.Copy 'FIXME: File attributes are never updated
                                CopyFile(SourcePath, DestPath)
                            Case TypeOfAction.Delete
                                IO.File.SetAttributes(SourcePath, IO.FileAttributes.Normal)
                                IO.File.Delete(SourcePath)
                                Status.DeletedFiles += 1
                        End Select

                    Case TypeOfItem.Folder
                        Select Case Entry.Action
                            Case TypeOfAction.Copy
                                IO.Directory.CreateDirectory(DestPath)

                                'FIXME: Folder attributes sometimes don't apply well.
                                IO.File.SetAttributes(DestPath, IO.File.GetAttributes(SourcePath))

                                'When a file is updated, so is its parent folder's last-write time.
                                'LATER: Remove this line: manual copying doesn't preserve creation time.
                                IO.Directory.SetCreationTimeUtc(DestPath, IO.Directory.GetCreationTimeUtc(SourcePath).AddHours(Handler.GetSetting(Of Integer)(ProfileSetting.TimeOffset, 0)))

                                Status.CreatedFolders += 1
                            Case TypeOfAction.Delete
#If DEBUG Then
                                Dim RemainingFiles As String() = IO.Directory.GetFiles(SourcePath)
                                Dim RemainingFolders As String() = IO.Directory.GetDirectories(SourcePath)
                                If RemainingFiles.Length > 0 Or RemainingFolders.Length > 0 Then Log.LogInfo(String.Format("Do_Tasks: Removing non-empty folder {0} ({1}) ({2})", SourcePath, String.Join(", ", RemainingFiles), String.Join(", ", RemainingFolders)))
#End If
                                Try
                                    IO.Directory.Delete(SourcePath, True)
                                Catch ex As Exception
                                    Dim DirInfo As New IO.DirectoryInfo(SourcePath)
                                    DirInfo.Attributes = IO.FileAttributes.Directory 'Using "DirInfo.Attributes = IO.FileAttributes.Normal" does just the same, and actually sets DirInfo.Attributes to "IO.FileAttributes.Directory"
                                    DirInfo.Delete()
                                End Try
                                Status.DeletedFolders += 1
                        End Select
                End Select
                Status.ActionsDone += 1
                Log.LogAction(Entry, Side, True)

            Catch StopEx As Threading.ThreadAbortException
                Exit Sub

            Catch ex As Exception
                Log.HandleError(ex, SourcePath)
                Log.LogAction(Entry, Side, False) 'Side parameter is only used for logging purposes.
            End Try

            If Not Status.Cancel Then Me.Invoke(IncrementCallback, New Object() {CurrentStep, 1})
        Next
    End Sub

    Private Sub Init_Synchronization(ByRef FoldersList As Dictionary(Of String, Boolean), ByVal Context As SyncingContext, ByVal Action As TypeOfAction)
        For Each Folder As String In FoldersList.Keys
            Log.LogInfo(String.Format("=> Scanning ""{0}"" top level folders: ""{1}""", Context.SourceRootPath, Folder))
            If IO.Directory.Exists(CombinePathes(Context.SourceRootPath, Folder)) Then
                If Action = TypeOfAction.Copy Then
                    'FIXED-BUG: Every ancestor of this folder should be added too.
                    'Careful with this, for it's a performance issue. Ancestors should only be added /once/.
                    'How to do that? Well, if ancestors of a folder have not been scanned, it means that this folder wasn't reached by a recursive call, but by a initial call.
                    'Therefore, only the folders in the sync config file should be added.
                    AddValidAncestors(Folder)
                    SearchForChanges(Folder, FoldersList(Folder), Context)
                ElseIf Action = TypeOfAction.Delete Then
                    SearchForCrap(Folder, FoldersList(Folder), Context)
                End If
            End If
        Next
    End Sub

    Private Sub AddToSyncingList(ByVal Path As String, ByVal Type As TypeOfItem, ByVal Side As SideOfSource, ByVal Action As TypeOfAction, ByVal IsUpdate As Boolean)
        Dim Entry As New SyncingItem With {.Path = Path, .Type = Type, .Side = Side, .Action = Action, .IsUpdate = IsUpdate, .RealId = SyncingList.Count}

        SyncingList.Add(Entry)
        If Entry.Action <> TypeOfAction.Delete Then AddValidFile(If(Type = TypeOfItem.Folder, Entry.Path, GetCompressedName(Entry.Path)))

        Select Case Entry.Action
            Case TypeOfAction.Copy
                If Entry.Type = TypeOfItem.Folder Then
                    Status.FoldersToCreate += 1
                ElseIf Entry.Type = TypeOfItem.File Then
                    Status.FilesToCreate += 1
                End If
            Case TypeOfAction.Delete
                If Entry.Type = TypeOfItem.Folder Then
                    Status.FoldersToDelete += 1
                ElseIf Entry.Type = TypeOfItem.File Then
                    Status.FilesToDelete += 1
                End If
        End Select
        Select Case Entry.Side
            Case SideOfSource.Left
                Status.LeftActionsCount += 1
            Case SideOfSource.Right
                Status.RightActionsCount += 1
        End Select
        Status.TotalActionsCount += 1
    End Sub

    Private Sub AddValidFile(ByVal File As String)
        If Not IsValidFile(File) Then ValidFiles.Add(File.ToLower(Interaction.InvariantCulture), Nothing)
    End Sub

    Private Sub AddValidAncestors(ByVal Folder As String)
        Log.LogInfo(String.Format("AddValidAncestors: Folder ""{0}"" is a top level folder, adding it's ancestors.", Folder))
        Dim CurrentAncestor As New System.Text.StringBuilder
        Dim Ancestors As New List(Of String)(Folder.Split(New Char() {ProgramSetting.DirSep}, StringSplitOptions.RemoveEmptyEntries))

        For Depth As Integer = 0 To (Ancestors.Count - 1) - 1 'The last ancestor is the folder itself, and will be added in SearchForChanges.
            CurrentAncestor.Append(ProgramSetting.DirSep).Append(Ancestors(Depth))
            AddValidFile(CurrentAncestor.ToString)
            Log.LogInfo(String.Format("AddValidAncestors: [Valid folder] ""{0}""", CurrentAncestor.ToString))
        Next
    End Sub

    Private Sub RemoveValidFile(ByVal File As String)
        If IsValidFile(File) Then ValidFiles.Remove(File.ToLower(Interaction.InvariantCulture))
    End Sub

    Private Function IsValidFile(ByVal File As String) As Boolean
        Return ValidFiles.ContainsKey(File.ToLower(Interaction.InvariantCulture))
    End Function

    Private Sub PopSyncingList(ByVal Side As SideOfSource)
        ValidFiles.Remove(SyncingList(SyncingList.Count - 1).Path)
        SyncingList.RemoveAt(SyncingList.Count - 1)

        Status.TotalActionsCount -= 1
        Select Case Side
            Case SideOfSource.Left
                Status.LeftActionsCount -= 1
            Case SideOfSource.Right
                Status.RightActionsCount -= 1
        End Select
    End Sub


    ' This procedure searches for changes in the source directory.
    Private Sub SearchForChanges(ByVal Folder As String, ByVal Recursive As Boolean, ByVal Context As SyncingContext)
        Dim SourceFolder As String = CombinePathes(Context.SourceRootPath, Folder)
        Dim DestinationFolder As String = CombinePathes(Context.DestinationRootPath, Folder)

        'Exit on excluded folders (and optionally on hidden ones).
        If Not HasAcceptedDirname(Folder) OrElse IsExcludedSinceHidden(SourceFolder) OrElse IsSymLink(SourceFolder) Then Exit Sub

        UpdateLabel(StatusData.SyncStep.Scan, SourceFolder)
        Log.LogInfo(String.Format("=> Scanning folder ""{0}"" for new or updated files.", Folder))

        'LATER: Factor out.
        Dim IsNewFolder As Boolean = Not IO.Directory.Exists(DestinationFolder)
        Dim ShouldUpdateFolder As Boolean = IsNewFolder OrElse AttributesChanged(SourceFolder, DestinationFolder)
        If ShouldUpdateFolder Then
            AddToSyncingList(Folder, TypeOfItem.Folder, Context.Source, TypeOfAction.Copy, Not IsNewFolder)
            Log.LogInfo(String.Format("SearchForChanges: {0} ""{1}"" ""{2}"" ({3})", If(IsNewFolder, "[New folder]", "[Updated folder]"), SourceFolder, DestinationFolder, Folder))
        Else
            AddValidFile(Folder)
            Log.LogInfo(String.Format("SearchForChanges: [Valid folder] ""{0}"" ""{1}"" ({2})", SourceFolder, DestinationFolder, Folder))
        End If

        Dim InitialValidFilesCount As Integer = ValidFiles.Count
        Try
            For Each SourceFile As String In IO.Directory.GetFiles(SourceFolder)
                Log.LogInfo("Scanning " & SourceFile)
                Dim DestinationFile As String = GetCompressedName(CombinePathes(DestinationFolder, IO.Path.GetFileName(SourceFile)))

                Try
                    If IsIncludedInSync(SourceFile) Then
                        Dim IsNewFile As Boolean = Not IO.File.Exists(DestinationFile)
                        Dim RelativeFilePath As String = SourceFile.Substring(Context.SourceRootPath.Length)

                        If IsNewFile OrElse SourceIsMoreRecent(SourceFile, DestinationFile) Then
                            AddToSyncingList(RelativeFilePath, TypeOfItem.File, Context.Source, TypeOfAction.Copy, Not IsNewFile)
                            Log.LogInfo(String.Format("SearchForChanges: {0} ""{1}"" ""{2}"" ({3}).", If(IsNewFile, "[New File]", "[Updated file]"), SourceFile, DestinationFile, RelativeFilePath))

                            If ProgramConfig.GetProgramSetting(Of Boolean)(ProgramSetting.Forecast, False) Then Status.BytesToCopy += Utilities.GetSize(SourceFile) 'Degrades performance.
                        Else
                            'Adds an entry to not delete this when cleaning up the other side.
                            AddValidFile(GetCompressedName(RelativeFilePath))
                            Log.LogInfo(String.Format("SearchForChanges: [Valid] ""{0}"" ""{1}"" ({2})", SourceFile, DestinationFile, RelativeFilePath))
                        End If
                    Else
                        Log.LogInfo(String.Format("SearchForChanges: [Excluded file] ""{0}""", SourceFile))
                    End If

                Catch Ex As Exception
                    Log.HandleError(Ex, SourceFile)
                End Try

                Status.FilesScanned += 1
            Next
        Catch Ex As Exception
            Log.HandleSilentError(Ex)
            'Error with entering the folder || Thread aborted.
        End Try

        If Recursive Or AutoInclude Then
            Try
                For Each SubFolder As String In IO.Directory.GetDirectories(SourceFolder)
                    If Recursive OrElse (AutoInclude AndAlso IO.Directory.GetCreationTimeUtc(SubFolder) > MDate) Then
                        SearchForChanges(SubFolder.Substring(Context.SourceRootPath.Length), True, Context)
                    End If
                Next
            Catch Ex As Exception
                Log.HandleSilentError(Ex)
            End Try
        End If

        If InitialValidFilesCount = ValidFiles.Count Then
            If Not Handler.GetSetting(Of Boolean)(ProfileSetting.ReplicateEmptyDirectories, True) Then
                If ShouldUpdateFolder Then
                    'Don't create/update this folder.
                    Status.FoldersToCreate -= 1
                    PopSyncingList(Context.Source)
                End If

                RemoveValidFile(Folder) 'Folders added for creation are marked as valid in AddToSyncingList. Removing this mark is vital to ensuring that the ReplicateEmptyDirectories setting works correctly (otherwise the count increases.

                'Problem: What if ancestors of a folder have been marked valid, and the folder is empty?
                'If the folder didn't exist, it's ancestors won't be created, since only the folder itself is added.
                'Yet if ancestors exist, should they be removed? Let's say NO for now.
            End If
        End If
    End Sub

    Private Sub SearchForCrap(ByVal Folder As String, ByVal Recursive As Boolean, ByVal Context As SyncingContext)
        'Here, Source is set to be the right folder, and dest to be the left folder
        Dim SourceFolder As String = CombinePathes(Context.SourceRootPath, Folder)
        Dim DestinationFolder As String = CombinePathes(Context.DestinationRootPath, Folder)

        ' Folder exclusion doesn't work exactly the same as file exclusion: if "Source\a" is excluded, "Dest\a" doesn't get deleted. That way one can safely exclude "Source\System Volume Information" and the like.
        If Not HasAcceptedDirname(Folder) OrElse IsExcludedSinceHidden(SourceFolder) OrElse IsSymLink(SourceFolder) Then Exit Sub

        UpdateLabel(StatusData.SyncStep.Scan, SourceFolder)
        Log.LogInfo(String.Format("=> Scanning folder ""{0}"" for files to delete.", Folder))
        Try
            For Each File As String In IO.Directory.GetFiles(SourceFolder)
                Dim RelativeFName As String = File.Substring(Context.SourceRootPath.Length)

                Try
                    If Not IsValidFile(RelativeFName) Then
                        AddToSyncingList(RelativeFName, TypeOfItem.File, Context.Source, TypeOfAction.Delete, False)
                        Log.LogInfo(String.Format("Cleanup: [Delete] ""{0}"" ({1})", File, RelativeFName))
                    Else
                        Log.LogInfo(String.Format("Cleanup: [Keep] ""{0}"" ({1})", File, RelativeFName))
                    End If

                Catch Ex As Exception
                    Log.HandleError(Ex)
                End Try

                Status.FilesScanned += 1
            Next
        Catch Ex As Exception
            Log.HandleSilentError(Ex)
        End Try

        If Recursive Or AutoInclude Then
            Try
                For Each SubFolder As String In IO.Directory.GetDirectories(SourceFolder)
                    If Recursive OrElse (AutoInclude AndAlso IO.Directory.GetCreationTimeUtc(SubFolder) > MDate) Then
                        SearchForCrap(SubFolder.Substring(Context.SourceRootPath.Length), True, Context)
                    End If
                Next
            Catch Ex As Exception
                Log.HandleSilentError(Ex)
            End Try
        End If

        ' Folder.Length = 0 <=> This is the root folder, not to be deleted.
        If Folder.Length <> 0 AndAlso Not IsValidFile(Folder) Then
            Log.LogInfo(String.Format("Cleanup: [Delete folder] ""{0}"" ({1}).", DestinationFolder, Folder))
            AddToSyncingList(Folder, TypeOfItem.Folder, Context.Source, TypeOfAction.Delete, False)
        End If
    End Sub

    Private Sub SyncFileAttributes(SourceFile As String, DestFile As String)
        If Handler.GetSetting(Of Integer)(ProfileSetting.TimeOffset, 0) <> 0 Then 'Updating attributes is needed.
            Log.LogInfo("SyncFileAttributes: DST: Setting attributes to normal; current attributes: " & IO.File.GetAttributes(DestFile))
            IO.File.SetAttributes(DestFile, IO.FileAttributes.Normal) 'Tracker #2999436
            Log.LogInfo("SyncFileAttributes: DST: Setting last write time")
            'Must use IO.File.GetLastWriteTimeUtc(**DestFile**), because it might differ from IO.File.GetLastWriteTimeUtc(**SourceFile**) (rounding, DST settings, ...)
            IO.File.SetLastWriteTimeUtc(DestFile, IO.File.GetLastWriteTimeUtc(DestFile).AddHours(Handler.GetSetting(Of Integer)(ProfileSetting.TimeOffset, 0)))
            Log.LogInfo("SyncFileAttributes: DST: Last write time set to " & IO.File.GetLastWriteTimeUtc(DestFile))
        End If

        Log.LogInfo("SyncFileAttributes: Setting attributes to " & IO.File.GetAttributes(SourceFile))
        IO.File.SetAttributes(DestFile, IO.File.GetAttributes(SourceFile))
        Log.LogInfo("SyncFileAttributes: Attributes set to " & IO.File.GetAttributes(DestFile))
    End Sub

    Private Shared Sub SafeCopy(SourceFile As String, DestFile As String)
        Dim TempDest, DestBack As String
        Do
            TempDest = DestFile & "-" & IO.Path.GetRandomFileName()
            DestBack = DestFile & "-" & IO.Path.GetRandomFileName()
        Loop While IO.File.Exists(TempDest) Or IO.File.Exists(DestBack)

        IO.File.Copy(SourceFile, TempDest, False)
        IO.File.Move(DestFile, DestBack)
        IO.File.Move(TempDest, DestFile)
        IO.File.Delete(DestBack)
    End Sub

    Private Sub CopyFile(ByVal SourceFile As String, ByVal DestFile As String)
        Dim CompressedFile As String = GetCompressedName(DestFile)

        Log.LogInfo(String.Format("CopyFile: Source: {0}, Destination: {1}", SourceFile, DestFile))

        If IO.File.Exists(DestFile) Then
            IO.File.SetAttributes(DestFile, IO.FileAttributes.Normal)
        End If

        Dim Compression As Boolean = CompressedFile <> DestFile
        DestFile = CompressedFile

        If Compression Then
            Static GZipCompressor As Compressor = LoadCompressionDll()
            Static Decompress As Boolean = Handler.GetSetting(Of Boolean)(ProfileSetting.Decompress, False)
            GZipCompressor.CompressFile(SourceFile, CompressedFile, Decompress, Sub(Progress As Long) Status.BytesCopied += Progress) ', ByRef ContinueRunning As Boolean) 'ContinueRunning = Not [STOP]
        Else
            If IO.File.Exists(DestFile) Then
                Try
                    Using TestForAccess As New IO.FileStream(SourceFile, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.None) : End Using 'Checks whether the file can be accessed before trying to copy it. This line was added because if the file is only partially locked, CopyFileEx starts copying it, then fails on the way, and deletes the destination.
                    IO.File.Copy(SourceFile, DestFile, True)
                Catch Ex As IO.IOException
                    Log.LogInfo(String.Format("Copy failed with message ""{0}"": Retrying in safe mode", Ex.Message))
                    SafeCopy(SourceFile, DestFile)
                End Try
            Else
                IO.File.Copy(SourceFile, DestFile)
            End If
        End If

        Try
            SyncFileAttributes(SourceFile, DestFile)
        Catch Ex As UnauthorizedAccessException
            'This section addresses a subtle bug on NAS drives: if you reconfigure your NAS and change user settings, some files may cause access denied exceptions when trying to update their attributes. Resetting the file is the only solution I've found.
            Log.LogInfo("CopyFile: Syncing file attributes failed. Retrying")
            SafeCopy(SourceFile, DestFile)
            SyncFileAttributes(SourceFile, DestFile)
        End Try

        Status.CreatedFiles += 1
        If Not Compression Then Status.BytesCopied += Utilities.GetSize(SourceFile)
        If Handler.GetSetting(Of Boolean)(ProfileSetting.Checksum, False) AndAlso Md5(SourceFile) <> Md5(DestFile) Then Throw New System.Security.Cryptography.CryptographicException("MD5 validation: failed.")
    End Sub
#End Region

#Region " Functions "
    Private Function IsExcludedSinceHidden(ByVal Path As String) As Boolean
        'File.GetAttributes works for folders ; see http://stackoverflow.com/questions/8110646/
        Return Handler.GetSetting(Of Boolean)(ProfileSetting.ExcludeHidden, False) AndAlso (IO.File.GetAttributes(Path) And IO.FileAttributes.Hidden) <> 0
    End Function

    Private Function IsTooOld(ByVal Path As String) As Boolean
        Dim Days As Integer = Handler.GetSetting(Of Integer)(ProfileSetting.DiscardAfter, 0)
        Return ((Days > 0) AndAlso (Date.UtcNow - IO.File.GetLastWriteTimeUtc(Path)).TotalDays > Days)
    End Function

    Private Function IsIncludedInSync(ByVal FullPath As String) As Boolean
        If IsExcludedSinceHidden(FullPath) OrElse IsTooOld(FullPath) Then Return False

        Try
            Select Case Handler.GetSetting(Of Integer)(ProfileSetting.Restrictions)
                Case 1
                    Return FileNamePattern.MatchesPattern(GetFileOrFolderName(FullPath), IncludedPatterns)
                Case 2
                    Return Not FileNamePattern.MatchesPattern(GetFileOrFolderName(FullPath), ExcludedPatterns)
            End Select
        Catch Ex As Exception 'TODO: When?
            Log.HandleSilentError(Ex)
        End Try

        Return True
    End Function

    Private Function HasAcceptedDirname(ByVal Path As String) As Boolean
        Return Not FileNamePattern.MatchesPattern(Path, ExcludedDirPatterns)
    End Function

    Private Function GetCompressedName(ByVal OriginalName As String) As String
        Static Extension As String = Handler.GetSetting(Of String)(ProfileSetting.CompressionExt, "")

        If Extension <> "" AndAlso Handler.GetSetting(Of Boolean)(ProfileSetting.Decompress, False) Then
            Return If(OriginalName.EndsWith(Extension), OriginalName.Substring(0, OriginalName.LastIndexOf(Extension)), OriginalName)
        Else
            Return OriginalName + Extension
        End If
        'AndAlso Utilities.GetSize(File) > ConfigOptions.CompressionThreshold
    End Function

    Private Function AttributesChanged(ByVal AbsSource As String, ByVal AbsDest As String) As Boolean
        Const AttributesMask As IO.FileAttributes = IO.FileAttributes.Hidden Or IO.FileAttributes.System Or IO.FileAttributes.Encrypted

        ' Disabled by default, and in two-ways mode.
        ' TODO: Enable by default. It's currently disabled because some network drives do not update attributes correctly.
        If Not Handler.GetSetting(Of Boolean)(ProfileSetting.SyncFolderAttributes, False) Then Return False
        If Handler.GetSetting(Of Integer)(ProfileSetting.Method, ProfileSetting.DefaultMethod) = ProfileSetting.SyncMethod.BiIncremental Then Return False

        Try
            Return ((IO.File.GetAttributes(AbsSource) And AttributesMask) <> (IO.File.GetAttributes(AbsDest) And AttributesMask))
        Catch Ex As Exception
            Return False
        End Try
    End Function

    'Error catching for this function is done in the calling section
    Private Function SourceIsMoreRecent(ByVal AbsSource As String, ByVal AbsDest As String) As Boolean 'Assumes Source and Destination exist.
        If (Not Handler.GetSetting(Of Boolean)(ProfileSetting.PropagateUpdates, True)) Then Return False 'LATER: Require expert mode?

        Log.LogInfo(String.Format("SourceIsMoreRecent: {0}, {1}", AbsSource, AbsDest))

        Dim SourceFATTime As Date = NTFSToFATTime(IO.File.GetLastWriteTimeUtc(AbsSource)).AddHours(Handler.GetSetting(Of Integer)(ProfileSetting.TimeOffset, 0))
        Dim DestFATTime As Date = NTFSToFATTime(IO.File.GetLastWriteTimeUtc(AbsDest))
        Log.LogInfo(String.Format("SourceIsMoreRecent: S:({0}, {1}); D:({2}, {3})", Interaction.FormatDate(IO.File.GetLastWriteTimeUtc(AbsSource)), Interaction.FormatDate(SourceFATTime), Interaction.FormatDate(IO.File.GetLastWriteTimeUtc(AbsDest)), Interaction.FormatDate(DestFATTime)))

        If Handler.GetSetting(Of Boolean)(ProfileSetting.FuzzyDstCompensation, False) Then
            Dim HoursDiff As Integer = CInt((SourceFATTime - DestFATTime).TotalHours)
            If Math.Abs(HoursDiff) = 1 Then DestFATTime = DestFATTime.AddHours(HoursDiff)
        End If

        'StrictMirror is disabled in constructor if Method != LRMirror
        If SourceFATTime < DestFATTime AndAlso (Not Handler.GetSetting(Of Boolean)(ProfileSetting.StrictMirror, False)) Then Return False

        'User-enabled checks
        If Handler.GetSetting(Of Boolean)(ProfileSetting.Checksum, False) AndAlso Md5(AbsSource) <> Md5(AbsDest) Then Return True
        If Handler.GetSetting(Of Boolean)(ProfileSetting.CheckFileSize, False) AndAlso Utilities.GetSize(AbsSource) <> Utilities.GetSize(AbsDest) Then Return True

        If Handler.GetSetting(Of Boolean)(ProfileSetting.StrictDateComparison, True) Then
            If SourceFATTime = DestFATTime Then Return False
        Else
            If Math.Abs((SourceFATTime - DestFATTime).TotalSeconds) <= 4 Then Return False 'Note: NTFSToFATTime introduces additional fuzziness (justifies the <= ('=')).
        End If
        Log.LogInfo("SourceIsMoreRecent: Filetimes differ")

        Return True
    End Function

    Private Function IsSymLink(ByVal SubFolder As String) As Boolean
#If LINUX Then
        If (IO.File.GetAttributes(SubFolder) And IO.FileAttributes.ReparsePoint) <> 0 Then
            Log.LogInfo(String.Format("Symlink detected: {0}; not following.", SubFolder))
            Return True
        End If
#End If
        Return False
    End Function
#End Region

#Region " Shared functions "
    Private Shared Function CombinePathes(ByVal Dir As String, ByVal File As String) As String 'LATER: Should be optimized; IO.Path?
        Return Dir.TrimEnd(ProgramSetting.DirSep) & ProgramSetting.DirSep & File.TrimStart(ProgramSetting.DirSep)
    End Function

    Private Shared Function LoadCompressionDll() As Compressor
        Dim DLL As Reflection.Assembly = Reflection.Assembly.LoadFrom(ProgramConfig.CompressionDll)

        For Each SubType As Type In DLL.GetTypes
            If GetType(Compressor).IsAssignableFrom(SubType) Then Return CType(Activator.CreateInstance(SubType), Compressor)
        Next

        Throw New ArgumentException("Invalid DLL: " & ProgramConfig.CompressionDll)
    End Function

    Private Shared Function Md5(ByVal Path As String) As String
        Using DataStream As New IO.StreamReader(Path), CryptObject As New System.Security.Cryptography.MD5CryptoServiceProvider()
            Return Convert.ToBase64String(CryptObject.ComputeHash(DataStream.BaseStream))
        End Using
    End Function

    Private Shared Function NTFSToFATTime(ByVal NTFSTime As Date) As Date
        Return (New Date(NTFSTime.Year, NTFSTime.Month, NTFSTime.Day, NTFSTime.Hour, NTFSTime.Minute, NTFSTime.Second).AddSeconds(If(NTFSTime.Millisecond = 0, NTFSTime.Second Mod 2, 2 - (NTFSTime.Second Mod 2))))
    End Function
#End Region

#Region " Tests "
#If DEBUG Then
    Structure DatePair
        Dim Ntfs, FAT As Date

        <Diagnostics.DebuggerStepThrough()>
        Sub New(ByVal NtfsTime As Date, ByVal FatTime As Date)
            Ntfs = NtfsTime
            FAT = FatTime
        End Sub
    End Structure

    Public Shared Sub Check_NTFSToFATTime()
        Check_StaticFATTimes()
        Check_HardwareFATTimes()
    End Sub

    'Note: This could be a useful function for NAS drives known to round NTFS timestamps, but currently only DLink does, and they do it incorrectly (there's a bug in their drivers)
    Private Shared Function RoundToSecond(ByVal NTFSTime As Date) As Date
        Return (New Date(NTFSTime.Year, NTFSTime.Month, NTFSTime.Day, NTFSTime.Hour, NTFSTime.Minute, NTFSTime.Second).AddSeconds(If(NTFSTime.Millisecond > 500, 1, 0)))
    End Function

    Public Shared Sub Check_StaticFATTimes()
        System.Diagnostics.Debug.WriteLine("Starting hardcoded NTFS -> FAT tests")
        Dim Tests As New List(Of DatePair) From {New DatePair(#7:31:00 AM#, #7:31:00 AM#), New DatePair(#7:31:00 AM#.AddMilliseconds(1), #7:31:02 AM#), New DatePair(#7:31:01 AM#, #7:31:02 AM#), New DatePair(#7:31:01 AM#.AddMilliseconds(999), #7:31:02 AM#)}
        For Each Test As DatePair In Tests
            Dim Actual As Date = NTFSToFATTime(Test.Ntfs)
            Dim Result As String = String.Format("Check_NTFSToFATTime: {0} -> {1} ({2} expected) --> {3}", Test.Ntfs, Actual, Test.FAT, If(Actual = Test.FAT, "Ok", "Failed"))
            System.Diagnostics.Debug.WriteLine(Result)
        Next
        System.Diagnostics.Debug.WriteLine("Done!")
    End Sub

    Public Shared Sub Check_HardwareFATTimes()
        Using LogWriter As New IO.StreamWriter("C:\FatTimes.txt", False)
            LogWriter.WriteLine("Starting dynamic NTFS -> FAT tests")
            Dim Source As String = "C:\NtfsTests", Destination As String = "Z:\NtfsTests"
            If IO.Directory.Exists(Source) Then IO.Directory.Delete(Source, True)
            If IO.Directory.Exists(Destination) Then IO.Directory.Delete(Destination, True)

            IO.Directory.CreateDirectory(Source)
            IO.Directory.CreateDirectory(Destination)

            Dim BaseDate As Date = Date.Today.AddHours(8)
            Dim FormatString As String = "{0,-15}{1,-15}{2,-15}{3,-15}{4,-15}{5,-15}{6,-15}"

            LogWriter.WriteLine(String.Format(FormatString, "Input", "Source", "Dest (Created)", "Dest (Copied)", "ForecastedDate", "Rounded", "Equal?"))

            For ms As Integer = 0 To 61000 Step 71
                Dim InputDate As Date = BaseDate.AddMilliseconds(ms)
                Dim SourcePath As String = IO.Path.Combine(Source, ms.ToString)
                Dim DestPath_Created As String = IO.Path.Combine(Destination, ms.ToString & "-created")
                Dim DestPath_Copied As String = IO.Path.Combine(Destination, ms.ToString & "-copied")
                IO.File.Create(SourcePath).Close()
                IO.File.Create(DestPath_Created).Close()

                IO.File.SetLastWriteTime(SourcePath, InputDate)
                IO.File.SetLastWriteTime(DestPath_Created, InputDate)
                IO.File.Copy(SourcePath, DestPath_Copied)

                Dim SourceDate As Date = IO.File.GetLastWriteTime(SourcePath)
                Dim DestCreatedDate As Date = IO.File.GetLastWriteTime(DestPath_Created)
                Dim DestCopiedDate As Date = IO.File.GetLastWriteTime(DestPath_Copied)
                Dim ForecastedDate As Date = NTFSToFATTime(InputDate)
                Dim RoundedDate As Date = RoundToSecond(InputDate)
                Dim Equal As Boolean = InputDate = SourceDate And DestCreatedDate = DestCopiedDate And DestCopiedDate = ForecastedDate

                IO.File.Delete(SourcePath)
                IO.File.Delete(DestPath_Copied)
                IO.File.Delete(DestPath_Created)

                LogWriter.WriteLine(FormatString, FormatDate(InputDate), FormatDate(SourceDate), FormatDate(DestCreatedDate), FormatDate(DestCopiedDate), FormatDate(ForecastedDate), FormatDate(RoundedDate), Equal)
            Next

            IO.Directory.Delete(Source, True)
            IO.Directory.Delete(Destination, True)

            LogWriter.WriteLine("Done!")
        End Using
    End Sub
#End If
#End Region
End Class