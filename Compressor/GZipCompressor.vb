'This file is part of Create Synchronicity.
'
'Create Synchronicity is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
'Create Synchronicity is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
'You should have received a copy of the GNU General Public License along with Create Synchronicity.  If not, see <http://www.gnu.org/licenses/>.
'Created by:	Clément Pit--Claudel.
'Web site:		http://synchronicity.sourceforge.net.

Imports ICSharpCode.SharpZipLib.BZip2
Imports ICSharpCode.SharpZipLib.GZip
Imports ICSharpCode.SharpZipLib.Core
Imports System.Collections.Generic
Imports ICSharpCode.SharpZipLib.Zip.Compression.Streams

Public NotInheritable Class GenericCompressor
    Implements CS.Compressor

    Dim PreviousProgress As Long
    Dim ProgressReporter As CS.PluginCallback
    Dim SupportedExtensions As New List(Of String)({".gz", ".bz2"})

    Sub HandleProgress(ByVal Sender As Object, ByVal EventArgs As ProgressEventArgs)
        ProgressReporter(EventArgs.Processed - PreviousProgress)
        PreviousProgress = EventArgs.Processed
    End Sub

    Sub CompressFile(ByVal SourceFile As String, ByVal DestFile As String, ByVal Decompress As Boolean, ByVal ReportCallback As CS.PluginCallback) Implements CS.Compressor.CompressFile
        PreviousProgress = 0
        ProgressReporter = ReportCallback

        Dim Buffer(524228) As Byte 'DONE: Figure out the right buffer size. 281 739 264 Bytes -> 268435456:27s ; 67108864:32s ; 2097152:29s ; 524228:27s ; 4:32s
        Dim Handler As New ProgressHandler(AddressOf HandleProgress)

        Dim CompressionExt As String = If(Decompress, IO.Path.GetExtension(SourceFile), IO.Path.GetExtension(DestFile))

        If SupportedExtensions.Contains(CompressionExt) Then
            Using InputFile As New IO.FileStream(SourceFile, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
                Using OutputFile As New IO.FileStream(DestFile, IO.FileMode.Create)
                    If Decompress Then
                        Dim InputStream As IO.Stream = Nothing
                        Select Case CompressionExt
                            Case ".gz"
                                InputStream = New GZipInputStream(InputFile)
                            Case ".bz2"
                                InputStream = New BZip2InputStream(InputFile)
                        End Select

                        Try
                            StreamUtils.Copy(InputStream, OutputFile, Buffer, Handler, New TimeSpan(10000000), Nothing, "")
                        Finally
                            InputStream.Dispose()
                        End Try

                    Else
                        Dim OutputStream As IO.Stream = Nothing
                        Select Case CompressionExt
                            Case ".gz"
                                OutputStream = New GZipOutputStream(OutputFile)
                            Case ".bz2"
                                OutputStream = New BZip2OutputStream(OutputFile)
                        End Select

                        Try
                            StreamUtils.Copy(InputFile, OutputStream, Buffer, Handler, New TimeSpan(10000000), Nothing, "")
                        Finally
                            OutputStream.Dispose()
                        End Try
                    End If
                End Using
            End Using
        Else
            Throw New InvalidOperationException(String.Format("No file to compress! Extensions {0} not recognized.", CompressionExt))
        End If

        IO.File.SetLastWriteTimeUtc(DestFile, IO.File.GetLastWriteTimeUtc(SourceFile))
    End Sub

#If 0 Then
    'Broken code: Microsoft didn't implement GZip correctly before .Net 4
    Sub CompressFile(ByVal SourceFile As String, ByVal DestFile As String)
        Using Input As New IO.FileStream(SourceFile, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.Read)
            Using outFile As IO.FileStream = IO.File.Create(DestFile)
                Using Compress As IO.Compression.GZipStream = New IO.Compression.GZipStream(outFile, IO.Compression.CompressionMode.Compress)
                    'DONE: Figure out the right buffer size.
                    Dim Buffer(524228) As Byte '281 739 264 Bytes -> 268435456:27s ; 67108864:32s ; 2097152:29s ; 524228:27s ; 4:32s
                    Dim ReadBytes As Integer = 0

                    While True
                        ReadBytes = Input.Read(Buffer, 0, Buffer.Length)
                        If ReadBytes <= 0 Then Exit While
                        Compress.Write(Buffer, 0, ReadBytes)
                        Status.BytesCopied += ReadBytes
                    End While
                End Using
            End Using
        End Using
    End Sub
#End If
End Class
