Public Delegate Sub PluginCallback(ByVal TotalBytes As Long) ', ByRef ContinueRunning As Boolean)

Public Interface Compressor
    Sub CompressFile(ByVal SourceFile As String, ByVal DestFile As String, ByVal Decompress As Boolean, ByVal ReportProgress As PluginCallback)
End Interface
