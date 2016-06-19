Module Utilities
    Friend Function FormatTimespan(ByVal T As TimeSpan) As String
        Dim Hours As Integer = CInt(Math.Truncate(T.TotalHours))
        Dim Blocks As New List(Of String)
        If Hours <> 0 Then Blocks.Add(Hours & " h")
        If T.Minutes <> 0 Then Blocks.Add(T.Minutes.ToString & " m")
        If T.Seconds <> 0 Or Blocks.Count = 0 Then Blocks.Add(T.Seconds.ToString & " s")
        Return String.Join(", ", Blocks.ToArray())
    End Function

    Friend Function GetSize(ByVal File As String) As Long
        Return (New System.IO.FileInfo(File)).Length 'Faster than My.Computer.FileSystem.GetFileInfo().Length (See FileLen_Speed_Test.vb)
    End Function

    Friend Function FormatSize(ByVal Size As Double, Optional ByVal Digits As Integer = 2) As String
        Select Case Size
            Case Is >= (1 << 30)
                Return Math.Round(Size / (1 << 30), Digits).ToString & " GB"
            Case Is >= (1 << 20)
                Return Math.Round(Size / (1 << 20), Digits).ToString & " MB"
            Case Is >= (1 << 10)
                Return Math.Round(Size / (1 << 10), Digits).ToString & " kB"
            Case Else
                Return Math.Round(Size, Digits).ToString & " B"
        End Select
    End Function
End Module
