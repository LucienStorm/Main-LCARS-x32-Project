Imports System.IO

''' <summary>
''' Maps file extensions to LCARS browser tab kinds.
''' </summary>
Public Module FileOpenRouter

    ''' <summary>
    ''' Returns the IBrowserTab.TabKind string for a local file path, or empty when unknown.
    ''' </summary>
    Public Function Resolve(ByVal path As String) As String
        If String.IsNullOrWhiteSpace(path) Then Return ""

        Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()

        Select Case ext
            Case ".html", ".htm", ".url"
                Return "Web"
            Case ".pdf", ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"
                Return "View"
            Case ".txt", ".log", ".json", ".xml", ".ini", ".cfg", ".md", ".rtf", ".docx"
                Return "Document"
            Case ".csv", ".xlsx"
                Return "Sheet"
            Case ".lcarsink", ".lcarscanvas", ".canvas"
                Return "Canvas"
            Case ""
                If File.Exists(path) AndAlso IsLikelyTextFile(path) Then Return "Document"
                Return ""
            Case Else
                Return ""
        End Select
    End Function

    ''' <summary>
    ''' Heuristic for extensionless or unrecognized paths that are plain text.
    ''' </summary>
    Public Function IsLikelyTextFile(ByVal path As String) As Boolean
        If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then Return False

        Try
            Dim info As New FileInfo(path)
            If info.Length = 0 Then Return True
            If info.Length > 1048576 Then Return False

            Using stream As FileStream = File.OpenRead(path)
                Dim buffer(511) As Byte
                Dim read As Integer = stream.Read(buffer, 0, buffer.Length)
                For i As Integer = 0 To read - 1
                    If buffer(i) = 0 Then Return False
                Next
            End Using
            Return True
        Catch
            Return False
        End Try
    End Function
End Module
