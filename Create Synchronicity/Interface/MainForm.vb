'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Friend Class MainForm
    Dim CurView As Integer
    Dim ProfilesGroups As New List(Of String)
    Dim Views() As View = New View() {View.Tile, View.Details, View.LargeIcon}

#Region " Events "
    Sub New()
        ' This call is required by the designer.
        InitializeComponent()
#If CONFIG = "Linux" Then
        Me.FormBorderStyle = Windows.Forms.FormBorderStyle.Sizable
#End If

        ' Code (largely inspired) by U.N. Owen
        Dim WindowSettings As New List(Of String)(ProgramConfig.GetProgramSetting(Of String)(ProgramSetting.MainFormAttributes, "").Split(","c))
        Try
            Dim Values As New List(Of Integer) : WindowSettings.ForEach(Sub(Elem) Values.Add(CInt(Elem))) 'FIXME: Lambdas
            If Values.Count = 4 AndAlso Values.TrueForAll(Function(Value As Integer) Value > 0 And Value < 5000) Then
                Me.Location = New Drawing.Point(Values(0), Values(1))
                Me.Size = New Drawing.Size(Values(2), Values(3))
                Me.StartPosition = FormStartPosition.Manual
            End If
        Catch
            ' If any string->integer conversion fails (due to invalid syntax), then Me.StartPosition will have remained unchanged.
        End Try

        ReloadNeeded = False

        BuildIcons()
        Me.Icon = ProgramConfig.Icon
        Me.ExitToolStripMenuItem.Image = Me.DeleteToolStripMenuItem.Image
        Me.ExitToolStripMenuItem.Text = Translation.Translate("\CANCEL_CLOSE", ";").Split(";"c)(1)

        Translation.TranslateControl(Me)
        Translation.TranslateControl(Me.ActionsMenu)
        Translation.TranslateControl(Me.StatusIconMenu)

        'Position the "About" label correctly
        Dim PreviousWidth As Integer = AboutLinkLabel.Width
        AboutLinkLabel.AutoSize = True
        Dim NewWidth As Integer = AboutLinkLabel.Width + 16 + 4
        AboutLinkLabel.AutoSize = False

        AboutLinkLabel.Width = NewWidth
        AboutLinkLabel.Location += New Drawing.Size(PreviousWidth - AboutLinkLabel.Width, 0)
    End Sub

    Private Sub MainForm_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        Dim WindowAttributes As String = String.Format("{0},{1},{2},{3}", Me.Location.X, Me.Location.Y, Me.Size.Width, Me.Size.Height)
        ProgramConfig.SetProgramSetting(Of String)(ProgramSetting.MainFormAttributes, WindowAttributes)
        ProgramConfig.SetProgramSetting(Of Integer)(ProgramSetting.MainView, CurView)
        ProgramConfig.SetProgramSetting(Of Single)(ProgramSetting.FontSize, Actions.Font.Size)
    End Sub

    Private Sub MainForm_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        ReloadProfilesList()
        MessageLoop.RedoSchedulerRegistration()
        SetView(ProgramConfig.GetProgramSetting(Of Integer)(ProgramSetting.MainView, 0))
        SetFont(ProgramConfig.GetProgramSetting(Of Integer)(ProgramSetting.FontSize, CInt(Actions.Font.Size)))
    End Sub

    Private Sub MainForm_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Me.KeyDown
        'Requires PreviewKeys to be set to true to work, otherwise the form won't catch the keypress.
        If e.KeyCode = Keys.F1 Then
            Interaction.StartProcess(Branding.Help)
        ElseIf e.KeyCode = Keys.F5 Then
            ReloadProfilesList()
        ElseIf e.Control Then
            Select Case e.KeyCode
                Case Keys.N
                    Actions.LabelEdit = True
                    Actions.Items(0).BeginEdit()
                Case Keys.O
                    Interaction.StartProcess(ProgramConfig.ConfigRootDir)
                Case Keys.E
                    If e.Alt Then
                        Dim NewState As Boolean = Not ProgramConfig.GetProgramSetting(Of Boolean)(ProgramSetting.ExpertMode, False)
                        ProgramConfig.SetProgramSetting(Of Boolean)(ProgramSetting.ExpertMode, NewState)
                        Interaction.ShowMsg("Expert mode " & If(NewState, "enabled", "disabled") & "!")
                    End If
                Case Keys.L
                    SetView(1)
                Case Keys.Add
                    SetFont(Actions.Font.Size + 1)
                Case Keys.Subtract
                    SetFont(Actions.Font.Size - 1)
            End Select
        End If
    End Sub

    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Application.Exit()
    End Sub

    Private Sub ToolStripHeader_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ToolStripHeader.Click
        Diagnostics.Process.Start(Application.ExecutablePath)
    End Sub

    Private Sub Actions_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles Actions.KeyDown
        If Actions.SelectedItems.Count = 0 Then Exit Sub

        If e.KeyCode = Keys.Enter Then
            If Actions.SelectedIndices(0) = 0 Then
                Actions.LabelEdit = True
                Actions.SelectedItems(0).BeginEdit()
            Else
                Dim Bounds As Drawing.Rectangle = Actions.SelectedItems(0).Bounds
                ActionsMenu.Show(Actions, New Drawing.Point(Bounds.Left, Bounds.Top + Bounds.Height))
            End If
        ElseIf e.KeyCode = Keys.F2 And Actions.SelectedIndices(0) <> 0 Then
            Actions.LabelEdit = True
            Actions.SelectedItems(0).BeginEdit()
        End If
    End Sub

    Private Sub Actions_Click(ByVal sender As Object, ByVal e As System.Windows.Forms.MouseEventArgs) Handles Actions.MouseClick
        If Actions.SelectedItems.Count = 0 Then Exit Sub

        If Actions.SelectedIndices(0) = 0 Then
            Actions.LabelEdit = True
            Actions.SelectedItems(0).BeginEdit()
        Else
            ActionsMenu.Show(Actions, e.Location)
        End If
    End Sub

    Private Sub Actions_AfterLabelEdit(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LabelEditEventArgs) Handles Actions.AfterLabelEdit
        Actions.LabelEdit = False
        If e.Label = "" OrElse e.Label.IndexOfAny(IO.Path.GetInvalidFileNameChars) >= 0 Then
            e.CancelEdit = True
            Exit Sub
        End If

        If e.Item = 0 Then
            e.CancelEdit = True
            Dim SettingsForm As New SettingsForm(e.Label, ProfilesGroups)

            'Since this happens while the label is being edited, the normal blinking mecanisms that brings the modal dialog to front when the owner window is clicked doesn't work.
            'Explicitly specifying the owner forces the modal dialog to always be on top of the caller.
            SettingsForm.ShowDialog(Me) 'This is especially important if users minimize CS while editing the label - in this case, returning to CS shows a blocked main windows, and users must loop through all windows (Ctrl+Tab) to reach the settings dialog.
        Else
            If Not Profiles(Actions.Items(e.Item).Text).Rename(e.Label) Then e.CancelEdit = True
        End If

        ' Tracker #3357854: Reloading immediately deletes the item being edited, and the edit is committed on the first item of the list.
        Me.BeginInvoke(New Action(AddressOf ReloadProfilesList))
    End Sub

    Private Sub Actions_SelectedIndexChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Actions.SelectedIndexChanged
        If Actions.SelectedIndices.Count = 0 OrElse Actions.SelectedIndices(0) = 0 Then
            If Actions.SelectedIndices.Count = 0 Then
                LoadDetails("", True)
            ElseIf Actions.SelectedIndices(0) = 0 Then
                LoadDetails(Translation.Translate("\NEW_PROFILE"), True)
            End If

            ActionsMenu.Close()
            Exit Sub
        End If

        LoadDetails(CurrentProfile, False)
    End Sub

    Private Sub AboutLinkLabel_LinkClicked(ByVal sender As System.Object, ByVal e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles AboutLinkLabel.LinkClicked
        Dim About As New AboutForm
        About.ShowDialog()
        If ReloadNeeded Then Me.Close()
    End Sub

    Private Sub ActionsMenu_Opening(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles ActionsMenu.Opening
        Dim FileSize As Integer = If(IO.File.Exists(Profiles(CurrentProfile()).LogPath), CInt(Utilities.GetSize(Profiles(CurrentProfile()).LogPath) / 1000), 0)
        ClearLogMenuItem.Text = String.Format(ClearLogMenuItem.Tag.ToString, FileSize)

        'Forbid syncing without at least one preview, at least in non-expert mode
        SynchronizeMenuItem.Visible = ProgramConfig.GetProgramSetting(Of Boolean)(ProgramSetting.ExpertMode, False) OrElse Profiles(CurrentProfile).GetLastRun() <> ScheduleInfo.DATE_NEVER
    End Sub

    Private Sub PreviewMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PreviewMenuItem.Click
        If Not CheckValidity() Then Exit Sub

        Dim SyncForm As New SynchronizeForm(CurrentProfile, True, False)
        SetVisible(False) : SyncForm.StartSynchronization(True) : SyncForm.ShowDialog() : SetVisible(True)
        SyncForm.Dispose()
    End Sub

    Private Sub SynchronizeMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SynchronizeMenuItem.Click
        If Not CheckValidity() Then Exit Sub

        Dim SyncForm As New SynchronizeForm(CurrentProfile, False, False)
        SetVisible(False) : SyncForm.StartSynchronization(True) : SyncForm.ShowDialog() : SetVisible(True)
        SyncForm.Dispose()
    End Sub

    Private Sub ChangeSettingsMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ChangeSettingsMenuItem.Click
        Dim SettingsForm As New SettingsForm(CurrentProfile, ProfilesGroups)
        SettingsForm.ShowDialog()
        ReloadProfilesList()
    End Sub

    Private Sub DeleteToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DeleteToolStripMenuItem.Click
        If Interaction.ShowMsg(Translation.TranslateFormat("\DELETE_PROFILE", CurrentProfile), Translation.Translate("\CONFIRM_DELETION"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
            Profiles(CurrentProfile).DeleteConfigFile()
            Profiles(CurrentProfile) = Nothing
            Actions.Items.RemoveAt(Actions.SelectedIndices(0))
            TipsLabel.Visible = (Actions.Items.Count = 1)
        End If
    End Sub

    Private Sub RenameMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RenameMenuItem.Click
        Actions.LabelEdit = True
        Actions.SelectedItems(0).BeginEdit()
    End Sub

    Private Sub ViewLogMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ViewLogMenuItem.Click
        If Not IO.File.Exists(Profiles(CurrentProfile).LogPath) Then Exit Sub
        Interaction.StartProcess(Profiles(CurrentProfile).LogPath)
    End Sub

    Private Sub ClearLogMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ClearLogMenuItem.Click
        Profiles(CurrentProfile).DeleteLogFiles()
    End Sub

    Private Sub ScheduleMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ScheduleMenuItem.Click
        Dim SchedForm As New SchedulingForm(CurrentProfile)
        SchedForm.ShowDialog()
        ReloadProfilesList()
        MessageLoop.RedoSchedulerRegistration()
    End Sub

    Private Sub Donate_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Donate.Click
        Interaction.StartProcess(Branding.Contribute)
    End Sub

    Private Sub Donate_MouseEnter(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Donate.MouseEnter
        CType(sender, PictureBox).BackColor = Drawing.Color.LightGray
    End Sub

    Private Sub Donate_MouseLeave(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Donate.MouseLeave
        CType(sender, PictureBox).BackColor = Drawing.Color.White
    End Sub
#End Region

#Region " Functions and Routines "
    Sub SetVisible(ByVal Status As Boolean)
        If Me.IsDisposed Then Exit Sub
        Me.Visible = Status
    End Sub

    Sub SetView(ByVal Offset As Integer)
        CurView = (CurView + Offset) Mod Views.Length
#If CONFIG = "Linux" Then
        If CurView = 0 Then CurView = 1 'Exclude tile view.
#End If

        Actions.View = Views(CurView)

        ReloadProfilesList()
        Actions.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
    End Sub

    Sub SetFont(ByVal Size As Single)
        Size = CSng(Math.Max(6.25, Math.Min(24.25, Size)))
        Actions.Font = New Drawing.Font(Actions.Font.Name, Size)
    End Sub

    Sub ReloadProfilesList()
        If Me.IsDisposed Then Exit Sub
        Dim CreateProfileItem As ListViewItem = Actions.Items(0)

        MessageLoop.ReloadProfiles()
        Actions.Items.Clear()
        Actions.Items.Add(CreateProfileItem).Group = Actions.Groups(0)

        ProfilesGroups.Clear()
        For Each Profile As ProfileHandler In Profiles.Values
            Dim NewItem As ListViewItem = Actions.Items.Add(Profile.ProfileName)

            NewItem.Group = Actions.Groups(1)
            NewItem.ImageIndex = Profile.GetSetting(Of Integer)(ProfileSetting.Method, ProfileSetting.DefaultMethod) + If(Profile.Scheduler.Frequency = ScheduleInfo.Freq.Never, 0, 4)
            NewItem.SubItems.Add(Profile.FormatMethod()).ForeColor = Drawing.Color.DarkGray
            If Actions.View = View.Details Then NewItem.SubItems.Add(Profile.FormatLastRun("D3")) 'Fix #3515300

            Dim GroupName As String = Profile.GetSetting(Of String)(ProfileSetting.Group, "")
            If GroupName <> "" Then
                If Not ProfilesGroups.Contains(GroupName) Then
                    ProfilesGroups.Add(GroupName)
                    Actions.Groups.Add(New ListViewGroup(GroupName, GroupName))
                End If

                NewItem.Group = Actions.Groups.Item(GroupName)
            End If
        Next

        TipsLabel.Visible = (Profiles.Count = 0 And TipsLabel.Text <> "")
        TipsLabel.Text = String.Format(TipsLabel.Text, Translation.Translate("\NEW_PROFILE_LABEL"))
    End Sub

    Sub LoadDetails(ByVal Name As String, ByVal Clear As Boolean)
        ProfileName.Text = Name

        Method.Text = ""
        Source.Text = ""
        Destination.Text = ""
        LimitedCopy.Text = ""
        FileTypes.Text = ""
        Scheduling.Text = ""
        TimeOffset.Text = ""

        If Clear Then Exit Sub

        Method.Text = Profiles(Name).FormatMethod()
        Source.Text = Profiles(Name).GetSetting(Of String)(ProfileSetting.Source)
        Destination.Text = Profiles(Name).GetSetting(Of String)(ProfileSetting.Destination)

        Scheduling.Text = Translation.Translate("\" & Profiles(Name).Scheduler.Frequency.ToString.ToUpper(Interaction.InvariantCulture))

        Select Case Profiles(Name).Scheduler.Frequency
            Case ScheduleInfo.Freq.Weekly
                Dim Day As String = Translation.Translate("\WEEK_DAYS", ";;;;;;").Split(";"c)(Profiles(Name).Scheduler.WeekDay)
                Scheduling.Text &= Day
            Case ScheduleInfo.Freq.Monthly
                Scheduling.Text &= Profiles(Name).Scheduler.MonthDay
        End Select

        If Profiles(Name).Scheduler.Frequency = ScheduleInfo.Freq.Never Then
            Scheduling.Text = ""
        Else
            Scheduling.Text &= ", " & Profiles(Name).Scheduler.Hour.ToString("D2") & Translation.Translate("\H_M_SEP") & Profiles(Name).Scheduler.Minute.ToString("D2")
        End If

        TimeOffset.Text = Profiles(Name).GetSetting(Of Integer)(ProfileSetting.TimeOffset).ToString

        Select Case Profiles(Name).GetSetting(Of Integer)(ProfileSetting.Restrictions, 0)
            Case 0
                LimitedCopy.Text = Translation.Translate("\NO")
            Case 1, 2
                LimitedCopy.Text = Translation.Translate("\YES")
        End Select

        Select Case Profiles(Name).GetSetting(Of Integer)(ProfileSetting.Restrictions, 0)
            Case 1
                FileTypes.Text = Profiles(Name).GetSetting(Of String)(ProfileSetting.IncludedTypes, "")
            Case 2
                FileTypes.Text = "-" & Profiles(Name).GetSetting(Of String)(ProfileSetting.ExcludedTypes, "")
        End Select
    End Sub

    Private Function CheckValidity() As Boolean
        If Not Profiles(CurrentProfile).ValidateConfigFile(True, True) Then
            Interaction.ShowMsg(Translation.Translate("\INVALID_CONFIG"), Translation.Translate("\ERROR"), , MessageBoxIcon.Error)
            Return False
        End If
        Return True
    End Function

    Private Function CurrentProfile() As String
        Return Actions.SelectedItems(0).Text
    End Function

    Private Sub BuildIcons()
        For Id As Integer = 0 To SyncIcons.Images.Count - 2
            Dim NewImg As New Drawing.Bitmap(32, 32)
            Dim Painter As Drawing.Graphics = Drawing.Graphics.FromImage(NewImg)

            Try
                If Id < 2 Then
                    Painter.DrawImage(ScheduleMenuItem.Image, 0, 0, 16, 16) 'Not specifying the destination size makes everything blurry.
                Else
                    Painter.DrawImage(ScheduleMenuItem.Image, 9, 6, 16, 16)
                End If
                Painter.DrawImageUnscaled(SyncIcons.Images(Id), 0, 0)
                SyncIcons.Images.Add(NewImg)
            Finally
                NewImg.Dispose()
                Painter.Dispose()
            End Try
        Next
    End Sub
#End Region

End Class