'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Option Strict On

Imports System
Imports System.Collections.Generic

Module Update_Languages
    Function ReadList(Prompt As String) As List(Of String)
        Console.WriteLine(Prompt)
        Return New List(Of String)(Console.ReadLine().Split(" ".ToCharArray, StringSplitOptions.RemoveEmptyEntries))
    End Function

    Structure Translation
        Dim Text As String
        Dim Updated As Boolean
    End Structure

    Const COMMENT As String = "#"
    Const UPDATE As String = "->"

    Sub Main()
        Dim Languages As New List(Of String)

        Languages.AddRange(IO.Directory.GetFiles(Environment.CurrentDirectory, "*.lng"))

        Dim Updated As List(Of String) = ReadList("Please input a space-separated list of all updated strings")
        Dim Created As List(Of String) = ReadList("Please input a space-separated list of all added strings")
        Dim Deleted As List(Of String) = ReadList("Please input a space-separated list of all removed strings")

        Using TODOList As New IO.StreamWriter(IO.Path.Combine(Environment.CurrentDirectory, "TODO.txt"))
            For Each FilePath As String In Languages
                Console.WriteLine("Updating " & IO.Path.GetFileNameWithoutExtension(FilePath))
                Try
                    Dim Todo As Integer = 0
                    Dim Comments As New List(Of String)
                    Dim Translations As New Dictionary(Of String, Translation)

                    Using Reader As New IO.StreamReader(FilePath, System.Text.Encoding.UTF8)
                        While Not Reader.EndOfStream
                            Dim Line As String = Reader.ReadLine

                            If Line.StartsWith(COMMENT) Then
                                Comments.Add(Line)
                            Else
                                Try
                                    Dim Tab As String() = Line.Split("=".ToCharArray, 2)
                                    Dim Key As String = Tab(0)
                                    Dim Value As New Translation With {.Text = Tab(1)}

                                    If Key.StartsWith(UPDATE) Then
                                        Value.Updated = True
                                        Key = Key.Substring(UPDATE.Length)
                                    End If

                                    If Updated.Contains(Key) Then Value.Updated = True

                                    If Deleted.Contains(Key) Then
                                        Console.WriteLine("Key removed: " & Key)
                                        Continue While
                                    End If

									If Value.Updated Then Todo += 1
									Translations.Add(Key, Value)
                                Catch Ex As Exception
                                    Console.WriteLine(Ex.ToString)
                                End Try
                            End If
                        End While
                    End Using

                    For Each NewKey As String In Created
                        If Not Translations.ContainsKey(NewKey) Then
                            Translations.Add(NewKey, New Translation With {.Updated = True, .Text = ""})
                            Todo += 1
                        End If
                    Next

                    Using Writer As New IO.StreamWriter(FilePath, False, System.Text.Encoding.UTF8)
                        For Each CommentString As String In Comments
                            Writer.WriteLine(CommentString)
                        Next

                        For Each Pair As KeyValuePair(Of String, Translation) In Translations
                            Writer.WriteLine("{0}{1}={2}", If(Pair.Value.Updated, UPDATE, ""), Pair.Key, Pair.Value.Text)
                        Next
                    End Using

                    Dim LanguageName As String = IO.Path.GetFileNameWithoutExtension(FilePath)
                    TODOList.WriteLine(LanguageName & ":" & Todo)

                    Console.WriteLine("Updated " & FilePath)

                Catch Ex As Exception
                    Console.WriteLine(Ex.ToString())
                End Try
            Next
        End Using

        Console.WriteLine("Done!")
        Console.ReadLine()
    End Sub
End Module
