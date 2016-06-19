'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Structure StatusData
    Enum SyncStep
        Done
        Scan
        LR
        RL
    End Enum

    Dim StartTime As Date

    Dim BytesCopied As Long
    Dim BytesToCopy As Long

    Dim FilesScanned As Long

    Dim CreatedFiles As Long
    Dim CreatedFolders As Long
    Dim FilesToCreate As Long
    Dim FoldersToCreate As Long

    Dim DeletedFiles As Long
    Dim DeletedFolders As Long
    Dim FilesToDelete As Long
    Dim FoldersToDelete As Long

    Dim ActionsDone As Long
    Dim TotalActionsCount As Long ' == SyncingList.Count
    Dim LeftActionsCount As Integer ' Used to set a ProgressBar's maximum => Integer
    Dim RightActionsCount As Integer

    Dim CurrentStep As SyncStep
    Dim TimeElapsed As TimeSpan
    Dim Speed As Double

    Dim Cancel As Boolean '= False
    Dim Failed As Boolean '= False
    Dim ShowingErrors As Boolean '= False

    Dim FailureMsg As String
End Structure

Public Enum TypeOfItem As Integer
    File = 0
    Folder = 1
End Enum

Public Enum TypeOfAction As Integer
    Delete = -1
    None = 0
    Copy = 1
End Enum

Public Enum SideOfSource As Integer
    Left = 0
    Right = 1
End Enum

Structure SyncingContext
    Public Source As SideOfSource
    Public SourceRootPath As String
    Public DestinationRootPath As String
End Structure

Class SyncingItem
    Public Path As String
    Public Type As TypeOfItem
    Public Side As SideOfSource

    Public IsUpdate As Boolean
    Public Action As TypeOfAction

    Public RealId As Integer ' Keeps track of the order in which items where inserted in the syncing list, hence making it possible to recover this insertion order even after sorting the list on other criterias

    Function FormatType() As String
        Select Case Type
            Case TypeOfItem.File
                Return Translation.Translate("\FILE")
            Case Else
                Return Translation.Translate("\FOLDER")
        End Select
    End Function

    Function FormatAction() As String
        Select Case Action
            Case TypeOfAction.Copy
                Return If(IsUpdate, Translation.Translate("\UPDATE"), Translation.Translate("\CREATE"))
            Case TypeOfAction.Delete
                Return Translation.Translate("\DELETE")
            Case Else
                Return Translation.Translate("\NONE")
        End Select
    End Function

    Function FormatDirection() As String
        Select Case Side
            Case SideOfSource.Left
                Return If(Action = TypeOfAction.Copy, Translation.Translate("\LR"), Translation.Translate("\LEFT"))
            Case SideOfSource.Right
                Return If(Action = TypeOfAction.Copy, Translation.Translate("\RL"), Translation.Translate("\RIGHT"))
            Case Else
                Return ""
        End Select
    End Function

    Function ToListViewItem() As ListViewItem
        Dim ListItem As New ListViewItem(New String() {FormatType(), FormatAction(), FormatDirection(), Path})

        Dim Delta As Integer = If(IsUpdate, 1, 0)
        Select Case Action
            Case TypeOfAction.Copy
                If Type = TypeOfItem.Folder Then
                    ListItem.ImageIndex = 5 + Delta
                ElseIf Type = TypeOfItem.File Then
                    Select Case Side
                        Case SideOfSource.Left
                            ListItem.ImageIndex = 0 + Delta
                        Case SideOfSource.Right
                            ListItem.ImageIndex = 2 + Delta
                    End Select
                End If
            Case TypeOfAction.Delete
                If Type = TypeOfItem.Folder Then
                    ListItem.ImageIndex = 7
                ElseIf Type = TypeOfItem.File Then
                    ListItem.ImageIndex = 4
                End If
        End Select

        Return ListItem
    End Function
End Class

Friend NotInheritable Class FileNamePattern
    Public Enum PatternType
        FileExt
        FileName
        FolderName
        Regex
    End Enum

    Public Type As PatternType
    Public Pattern As String

    Sub New(ByVal _Type As PatternType, ByVal _Pattern As String)
        Type = _Type
        Pattern = _Pattern
    End Sub

    Private Shared Function IsBoxed(ByVal Frame As Char, ByVal Str As String) As Boolean
        Return (Str.StartsWith(Frame) And Str.EndsWith(Frame) And Str.Length > 2)
    End Function

    Private Shared Function Unbox(ByVal Str As String) As String
        Return Str.Substring(1, Str.Length - 2).ToLower(Interaction.InvariantCulture) ' ToLower: Careful on linux ; No need to check that length > 2 here: IsBoxed already has.
    End Function

    Shared Function GetPattern(ByVal Str As String, Optional ByVal IsFolder As Boolean = False) As FileNamePattern
        If IsBoxed(""""c, Str) Then 'Filename
            Return New FileNamePattern(If(IsFolder, PatternType.FolderName, PatternType.FileName), Unbox(Str))
        ElseIf IsBoxed("/"c, Str) Then 'Regex
            Return New FileNamePattern(PatternType.Regex, Unbox(Str))
        Else
            Return New FileNamePattern(PatternType.FileExt, Str.ToLower(Interaction.InvariantCulture))
        End If
    End Function

    Private Shared Function SharpInclude(ByVal FileName As String) As String
        Dim Path As String = ProgramConfig.ConfigRootDir & ProgramSetting.DirSep & FileName
        Return If(IO.File.Exists(Path), IO.File.ReadAllText(Path), FileName)
    End Function

    Friend Shared Sub LoadPatternsList(ByRef PatternsList As List(Of FileNamePattern), ByVal PatternsStr As String, ByVal IsFolder As Boolean, Optional ByVal FolderPrefix As String = "")
        Dim Patterns As New List(Of String)(PatternsStr.Split(";".ToCharArray, StringSplitOptions.RemoveEmptyEntries))

        While Patterns.Count > 0 And Patterns.Count < 1024 'Prevent circular references
            Dim CurPattern As String = Patterns(0)

            If IsFolder = CurPattern.StartsWith(FolderPrefix) Then
                If IsFolder Then CurPattern = CurPattern.Substring(FolderPrefix.Length)

                If IsBoxed(":"c, CurPattern) Then 'Load patterns from file
                    Dim SubPatterns As String = SharpInclude(Unbox(CurPattern))
                    Patterns.AddRange(SubPatterns.Split(";".ToCharArray, StringSplitOptions.RemoveEmptyEntries))
                Else
                    PatternsList.Add(GetPattern(CurPattern, IsFolder))
                End If
            End If

            Patterns.RemoveAt(0)
        End While
    End Sub

    Private Shared Function GetExtension(ByVal File As String) As String
        Return File.Substring(File.LastIndexOf("."c) + 1) 'Not used when dealing with a folder.
    End Function

    Friend Shared Function MatchesPattern(ByVal PathOrFileName As String, ByRef Patterns As List(Of FileNamePattern)) As Boolean
        Dim Extension As String = GetExtension(PathOrFileName)

        For Each Pattern As FileNamePattern In Patterns 'LINUX: Problem with IgnoreCase
            Select Case Pattern.Type
                Case FileNamePattern.PatternType.FileExt
                    If String.Compare(Extension, Pattern.Pattern, True) = 0 Then Return True
                Case FileNamePattern.PatternType.FileName
                    If String.Compare(PathOrFileName, Pattern.Pattern, True) = 0 Then Return True
                Case FileNamePattern.PatternType.FolderName
                    If PathOrFileName.EndsWith(Pattern.Pattern, StringComparison.CurrentCultureIgnoreCase) Then Return True
                Case FileNamePattern.PatternType.Regex
                    If System.Text.RegularExpressions.Regex.IsMatch(PathOrFileName, Pattern.Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase) Then Return True
            End Select
        Next

        Return False
    End Function
End Class

Module FileHandling
    Friend Function GetFileOrFolderName(ByVal Path As String) As String
        Return Path.Substring(Path.LastIndexOf(ProgramSetting.DirSep) + 1) 'IO.Path.* -> Bad because of separate file/folder handling.
    End Function
End Module

