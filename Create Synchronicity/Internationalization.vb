'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Friend NotInheritable Class LanguageHandler
    Private Shared Singleton As LanguageHandler

    Structure LangInfo
        Dim CodeNames As List(Of String)
        Dim NativeName As String
    End Structure

    'Renames : non-english file name -> english file name
    Private Shared Renames As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {{"francais", "french"}, {"deutsch", "german"}}

    Private Shared Function NewFilename(ByVal OldLanguageName As String) As String
        Return If(Renames.ContainsKey(OldLanguageName), Renames(OldLanguageName), OldLanguageName)
    End Function

    Private Shared Function GetLanguageFilePath(ByVal LanguageName As String) As String
        Return ProgramConfig.LanguageRootDir & ProgramSetting.DirSep & NewFilename(LanguageName) & ".lng"
    End Function

    Private Sub New()
        ProgramConfig.LoadProgramSettings()
        IO.Directory.CreateDirectory(ProgramConfig.LanguageRootDir)

        Strings = New Dictionary(Of String, String)

        Dim DictFile As String = GetLanguageFilePath(ProgramConfig.GetProgramSetting(Of String)(ProgramSetting.Language, ProgramSetting.DefaultLanguage))

        If Not IO.File.Exists(DictFile) Then
            DictFile = GetLanguageFilePath(ProgramSetting.DefaultLanguage)
        End If

        If Not IO.File.Exists(DictFile) Then
            Interaction.ShowMsg("No language file found!")
        Else
            Using Reader As New IO.StreamReader(DictFile, Text.Encoding.UTF8)
                While Not Reader.EndOfStream()
                    Dim Line As String = Reader.ReadLine()
                    If Line.StartsWith("#") OrElse (Not Line.Contains("=")) Then Continue While

                    Dim Pair() As String = Line.Split("=".ToCharArray(), 2)
                    Try
                        If Pair(0).StartsWith("->") Then Pair(0) = Pair(0).Remove(0, 2)
                        Strings.Add("\" & Pair(0), Pair(1).Replace("\n", Environment.NewLine))
                    Catch Ex As ArgumentException
                        Interaction.ShowDebug("Duplicate line in translation: " & Line)
                    End Try
                End While
            End Using
        End If
    End Sub

    Public Shared Function GetSingleton(Optional ByVal Reload As Boolean = False) As LanguageHandler
        If Reload Or (Singleton Is Nothing) Then Singleton = New LanguageHandler()
        Return Singleton
    End Function

    Dim Strings As Dictionary(Of String, String)

    Public Function Translate(ByVal Code As String, Optional ByVal DefaultValue As String = "") As String
        If Code = Nothing OrElse Code = "" Then Return DefaultValue
        Return If(Strings.ContainsKey(Code), Strings(Code), If(DefaultValue = "", Code, DefaultValue))
    End Function

#Region "TranslateFormat"
    'ParamArray requires building objects array, and adds binsize.
    Public Function TranslateFormat(ByVal Code As String, ByVal Arg0 As Object) As String
        Return String.Format(Translate(Code), Arg0)
    End Function
    Public Function TranslateFormat(ByVal Code As String, ByVal Arg0 As Object, ByVal Arg1 As Object) As String
        Return String.Format(Translate(Code), Arg0, Arg1)
    End Function
    Public Function TranslateFormat(ByVal Code As String, ByVal Arg0 As Object, ByVal Arg1 As Object, ByVal Arg2 As Object) As String
        Return String.Format(Translate(Code), Arg0, Arg1, Arg2)
    End Function
#End Region

    Public Sub TranslateControl(ByVal Ctrl As Control)
        If Ctrl Is Nothing Then Exit Sub

        'Add ; in tags so as to avoid errors when tag properties are split.
        Ctrl.Text = Translate(Ctrl.Text)
        TranslateControl(Ctrl.ContextMenuStrip)

        If TypeOf Ctrl Is ListView Then
            Dim List As ListView = DirectCast(Ctrl, ListView)

            For Each Group As ListViewGroup In List.Groups
                Group.Header = Translate(Group.Header)
            Next

            For Each Column As ColumnHeader In List.Columns
                Column.Text = Translate(Column.Text)
            Next

            If Not List.VirtualMode Then
                For Each Item As ListViewItem In List.Items
                    For Each SubItem As ListViewItem.ListViewSubItem In Item.SubItems
                        SubItem.Text = Translate(SubItem.Text)
                        SubItem.Tag = Translate(CStr(SubItem.Tag), ";")
                    Next
                Next
            End If
        End If

        If TypeOf Ctrl Is ContextMenuStrip Then
            Dim ContextMenu As ContextMenuStrip = DirectCast(Ctrl, ContextMenuStrip)
            For Each Item As ToolStripItem In ContextMenu.Items
                Item.Text = Translate(Item.Text)
                Item.Tag = Translate(CStr(Item.Tag), ";")
            Next
        End If

        Ctrl.Tag = Translate(CStr(Ctrl.Tag), ";")
        For Each ChildCtrl As Control In Ctrl.Controls
            TranslateControl(ChildCtrl)
        Next
    End Sub

    Public Shared Sub FillLanguagesComboBox(ByVal LanguagesComboBox As ComboBox)
        Dim LanguagesInfo As New Dictionary(Of String, LangInfo)

        If IO.File.Exists(ProgramConfig.LocalNamesFile) Then
            Using PropsReader As New IO.StreamReader(ProgramConfig.LocalNamesFile)
                While Not PropsReader.EndOfStream
                    Dim CurLanguage() As String = PropsReader.ReadLine.Split(";".ToCharArray)

                    If CurLanguage.Length <> 3 Then Continue While
                    LanguagesInfo.Add(CurLanguage(0), New LangInfo With {.NativeName = CurLanguage(1), .CodeNames = New List(Of String)(CurLanguage(2).ToLower(Interaction.InvariantCulture).Split("/"c))})
                End While
            End Using
        End If

        ' Use strings to allow for some sorting.
        Dim SystemLanguageItem As String = Nothing
        Dim ProgramLanguageItem As String = Nothing
        Dim DefaultLanguageItem As String = Nothing

        Dim CurrentCulture As Globalization.CultureInfo = Globalization.CultureInfo.InstalledUICulture

        ProgramConfig.LoadProgramSettings()
        Dim CurProgramLanguage As String = NewFilename(ProgramConfig.GetProgramSetting(Of String)(ProgramSetting.Language, ""))

        'Merge fr-CA, fr-FR, and so on to fr, and distinguish zh-Hans and zh-Hant.
        If Not CurrentCulture.IsNeutralCulture Then CurrentCulture = CurrentCulture.Parent

        LanguagesComboBox.Items.Clear()
        For Each File As String In IO.Directory.GetFiles(ProgramConfig.LanguageRootDir, "*.lng")
            Dim EnglishName As String = IO.Path.GetFileNameWithoutExtension(File)
            Dim NewItemText As String = EnglishName

            If LanguagesInfo.ContainsKey(EnglishName) Then
                Dim Info As LanguageHandler.LangInfo = LanguagesInfo(EnglishName)
                NewItemText = String.Format("{0} - {1} ({2})", EnglishName, Info.NativeName, Info.CodeNames(0))

                If Info.CodeNames.Contains(CurrentCulture.Name.ToLower(Interaction.InvariantCulture)) Then SystemLanguageItem = NewItemText
            End If

            LanguagesComboBox.Items.Add(NewItemText)
            If String.Compare(EnglishName, CurProgramLanguage, True) = 0 Then ProgramLanguageItem = NewItemText
            If String.Compare(EnglishName, ProgramSetting.DefaultLanguage, True) = 0 Then DefaultLanguageItem = NewItemText
        Next

        LanguagesComboBox.Sorted = True

        If ProgramLanguageItem IsNot Nothing Then
            LanguagesComboBox.SelectedItem = ProgramLanguageItem
        ElseIf SystemLanguageItem IsNot Nothing Then
            LanguagesComboBox.SelectedItem = SystemLanguageItem
        ElseIf DefaultLanguageItem IsNot Nothing Then
            LanguagesComboBox.SelectedItem = DefaultLanguageItem
        ElseIf LanguagesComboBox.Items.Count > 0 Then
            LanguagesComboBox.SelectedIndex = 0
        End If
    End Sub

#If Debug And 0 Then
    Public Shared Sub EnumerateCultures()
        Dim Builder As New Text.StringBuilder
        For Each Culture As Globalization.CultureInfo In Globalization.CultureInfo.GetCultures(Globalization.CultureTypes.AllCultures)
            Builder.AppendLine(String.Join(Microsoft.VisualBasic.ControlChars.Tab, New String() {Culture.Name, Culture.Parent.Name, Culture.IsNeutralCulture.ToString, Culture.DisplayName, Culture.NativeName, Culture.EnglishName, Culture.TwoLetterISOLanguageName, Culture.ThreeLetterISOLanguageName, Culture.ThreeLetterWindowsLanguageName})) ', LangInfo.LocalName, LangInfo.IsoLanguageName, LangInfo.WindowsCode
        Next

        MessageBox.Show(Builder.ToString)
    End Sub
#End If
End Class
