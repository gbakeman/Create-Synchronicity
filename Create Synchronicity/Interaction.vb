'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Friend Module Interaction
    Friend InvariantCulture As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
    Friend WithEvents StatusIcon As NotifyIcon = New NotifyIcon() With {.BalloonTipTitle = "Create Synchronicity", .BalloonTipIcon = ToolTipIcon.Info}

    Dim BalloonQueueSize As Integer
    Private StatusIconVisible As Boolean

    Private BalloonTipTarget As String = Nothing
    Private SharedToolTip As ToolTip = New ToolTip() With {.UseFading = False, .UseAnimation = False, .ToolTipIcon = ToolTipIcon.Info}

    Public Sub LoadStatusIcon()
        StatusIcon.Icon = New Drawing.Icon(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("CS.icon-16x16.ico"))
    End Sub

    Public Sub ToggleStatusIcon(ByVal Status As Boolean)
        StatusIcon.Visible = Status And (Not CommandLine.Silent)
        StatusIconVisible = StatusIcon.Visible
    End Sub

    Public Sub ShowBalloonTip(ByVal Msg As String, Optional ByVal File As String = Nothing)
        If CommandLine.Silent Then
            ProgramConfig.LogAppEvent(String.Format("Interaction: Silent: Balloon tip discarded: [{0}].", Msg))
            Exit Sub
        Else
            ProgramConfig.LogAppEvent(String.Format("Interaction: Balloon tip shown: [{0}].", Msg))
        End If

        If Msg = "" Then Exit Sub

        BalloonTipTarget = File
        StatusIcon.BalloonTipText = Msg

        BalloonQueueSize += 1 'Prevents StatusIcon_BalloonTipClosed from hiding the icon if a balloon is closed because another one is being shown.
        StatusIcon.Visible = True
        StatusIcon.ShowBalloonTip(15000)
    End Sub

    Public Sub ShowToolTip(ByVal Ctrl As Control)
        Dim T As TreeView = TryCast(Ctrl, TreeView) 'Exit if Ctrl is a TreeView without checkboxes.
        If T IsNot Nothing AndAlso Not T.CheckBoxes Then Exit Sub

        Dim Offset As Integer = If(TypeOf Ctrl Is RadioButton Or TypeOf Ctrl Is CheckBox, 12, 1)
        Dim Pair As String() = String.Format(CStr(Ctrl.Tag), Ctrl.Text).Split(";".ToCharArray, 2)

        Try
            Dim Pos As New Drawing.Point(0, Ctrl.Height + Offset)
            If Pair.GetLength(0) = 1 Then
                SharedToolTip.ToolTipTitle = ""
                SharedToolTip.Show(Pair(0), Ctrl, Pos)
            ElseIf Pair.GetLength(0) > 1 Then
                SharedToolTip.ToolTipTitle = Pair(0)
                SharedToolTip.Show(Pair(1), Ctrl, Pos)
            End If
        Catch ex As InvalidOperationException
            'See bug #3076129
        End Try
    End Sub

    Public Sub HideToolTip(ByVal sender As Control)
        SharedToolTip.Hide(sender)
    End Sub

    <Diagnostics.Conditional("Debug")>
    Public Sub ShowDebug(ByVal Text As String, Optional ByVal Caption As String = "")
#If DEBUG Then
        ShowMsg(Text, Caption, MessageBoxButtons.OK, MessageBoxIcon.Warning)
#End If
    End Sub

    Public Function ShowMsg(ByVal Text As String, Optional ByVal Caption As String = "", Optional ByVal Buttons As MessageBoxButtons = MessageBoxButtons.OK, Optional ByVal Icon As MessageBoxIcon = MessageBoxIcon.None) As DialogResult
        If CommandLine.Silent Then
            ProgramConfig.LogAppEvent(String.Format("Interaction: Silent: Message Box discarded with default answer: [{0}] - [{1}].", Caption, Text))
            Return DialogResult.OK
        End If

        Dim Result As DialogResult = MessageBox.Show(Text, Caption, Buttons, Icon)
        If CommandLine.Log Then ProgramConfig.LogAppEvent(String.Format("Interaction: Message [{0}] - [{1}] received answer [{2}].", Caption, Text, Result.ToString))

        Return Result
    End Function

    Private Sub BallonClick(ByVal sender As Object, ByVal e As System.EventArgs) Handles StatusIcon.BalloonTipClicked
        If BalloonTipTarget IsNot Nothing Then StartProcess(BalloonTipTarget)
    End Sub

    Private Sub StatusIcon_BalloonTipClosed(sender As Object, e As System.EventArgs) Handles StatusIcon.BalloonTipClosed
        BalloonQueueSize -= 1
        If BalloonQueueSize = 0 Then StatusIcon.Visible = StatusIconVisible
        'BalloonTipTarget = Nothing ' Useless: will be reset by next call to ShowBalloonTip
    End Sub

    Public Sub StartProcess(ByVal Address As String, Optional ByVal Args As String = "")
        Try
            Diagnostics.Process.Start(Address, Args)
        Catch
        End Try
    End Sub

    Function FormatDate(ByVal Value As Date) As String
#If DEBUG Then
        Return Value.ToString("yyyy/MM/dd hh:mm:ss.fff")
#End If
        Return ""
    End Function
End Module

Friend NotInheritable Class SyncingListSorter
    Implements Collections.Generic.IComparer(Of SyncingItem)

    Public Order As SortOrder
    Public SortColumn As Integer

    Public Sub New(ByVal ColumnId As Integer)
        SortColumn = ColumnId
        Order = SortOrder.Ascending
    End Sub

    Public Function Compare(ByVal xs As SyncingItem, ByVal ys As SyncingItem) As Integer Implements Collections.Generic.IComparer(Of SyncingItem).Compare
        Dim Result As Integer
        Select Case SortColumn
            Case 0
                Result = xs.Type.CompareTo(ys.Type)
            Case 1
                Result = If(xs.Action = ys.Action, xs.IsUpdate.CompareTo(ys.IsUpdate), xs.Action.CompareTo(ys.Action))
            Case 2
                Result = xs.Side.CompareTo(ys.Side)
            Case 3
                Result = String.Compare(xs.Path, ys.Path, True)
            Case Else
                Result = xs.RealId.CompareTo(ys.RealId)
        End Select

        Return If(Order = SortOrder.Ascending, 1, -1) * Result
    End Function

    Public Sub RegisterClick(ByVal e As ColumnClickEventArgs)
        If e.Column = SortColumn Then
            Order = If(Order = SortOrder.Ascending, SortOrder.Descending, SortOrder.Ascending)
        Else
            SortColumn = e.Column
            Order = SortOrder.Ascending
        End If
    End Sub
End Class
