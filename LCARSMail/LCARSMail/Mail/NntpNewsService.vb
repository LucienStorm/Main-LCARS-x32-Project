Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Sockets
Imports System.Text
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class NntpNewsService
        Private NotInheritable Class NntpSession
            Implements IDisposable
            Private ReadOnly client As TcpClient

            Private ReadOnly reader As StreamReader

            Private ReadOnly writer As StreamWriter

            Public Sub New(host As String, port As Integer)
                client = New TcpClient(host, port)
                Dim stream As NetworkStream = client.GetStream()
                reader = New StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks:=False, 1024, leaveOpen:=True)
                writer = New StreamWriter(stream, Encoding.ASCII, 1024, leaveOpen:=True) With {
        .AutoFlush = True
    }
            End Sub

            Public Sub WriteLine(line As String)
                writer.WriteLine(line)
            End Sub

            Public Function ReadLine() As String
                Return reader.ReadLine()
            End Function

            Public Sub Dispose()
                Try
                    writer.Dispose()
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
                Try
                    reader.Dispose()
                Catch projectError2 As Exception
                    ProjectData.SetProjectError(projectError2)
                    Call ProjectData.ClearProjectError()
                End Try
                Try
                    client.Close()
                Catch projectError3 As Exception
                    ProjectData.SetProjectError(projectError3)
                    Call ProjectData.ClearProjectError()
                End Try
            End Sub

            Private Sub Dispose1() Implements IDisposable.Dispose
                'ILSpy generated this explicit interface implementation from .override directive in Dispose
                Dispose()
            End Sub
        End Class

        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function TryListGroups(host As String, port As Integer) As List(Of String)
            LastError = Nothing
            Dim list As List(Of String) = New List(Of String)()
            Using nntpSession = Connect(host, port)
                If nntpSession Is Nothing Then
                    Return list
                End If
                nntpSession.WriteLine("LIST")
                Dim text As String = nntpSession.ReadLine()
                If Equals(text, Nothing) OrElse Not text.StartsWith("215", StringComparison.Ordinal) Then
                    LastError = If(Not Equals(text, Nothing), text, "LIST failed.")
                    Return list
                End If
                Dim text2 As String = nntpSession.ReadLine()
                While Not Equals(text2, Nothing) AndAlso Operators.CompareString(text2, ".", TextCompare:=False) <> 0
                    Dim array = text2.Split(" "c)
                    If array.Length > 0 Then
                        list.Add(array(0))
                    End If
                    text2 = nntpSession.ReadLine()
                End While
            End Using
            Return list
        End Function

        Public Shared Function TryListArticles(host As String, port As Integer, group As String) As List(Of NntpArticleHeader)
            LastError = Nothing
            Dim list As List(Of NntpArticleHeader) = New List(Of NntpArticleHeader)()
            Using nntpSession = Connect(host, port)
                If nntpSession Is Nothing Then
                    Return list
                End If
                nntpSession.WriteLine("GROUP " & group)
                Dim text As String = nntpSession.ReadLine()
                If Equals(text, Nothing) OrElse Not text.StartsWith("211", StringComparison.Ordinal) Then
                    LastError = If(Not Equals(text, Nothing), text, "GROUP failed.")
                    Return list
                End If
                nntpSession.WriteLine("XOVER 1-")
                Dim text2 As String = nntpSession.ReadLine()
                If Equals(text2, Nothing) OrElse Not text2.StartsWith("224", StringComparison.Ordinal) Then
                    LastError = If(Not Equals(text2, Nothing), text2, "XOVER failed.")
                    Return list
                End If
                Dim text3 As String = nntpSession.ReadLine()
                While Not Equals(text3, Nothing) AndAlso Operators.CompareString(text3, ".", TextCompare:=False) <> 0
                    Dim array = text3.Split(":"c)
                    If array.Length >= 4 Then
                        Dim result = 0
                        Integer.TryParse(array(0), result)
                        list.Add(New NntpArticleHeader With {
        .ArticleNumber = result,
        .Subject = array(3),
        .FromLine = If(array.Length > 5, array(5), "")
    })
                    End If
                    text3 = nntpSession.ReadLine()
                End While
            End Using
            Return list
        End Function

        Public Shared Function TryFetchArticle(host As String, port As Integer, group As String, articleNumber As Integer) As String
            LastError = Nothing
            Dim nntpSession = Connect(host, port)
            If nntpSession Is Nothing Then
                Return ""
            End If
            nntpSession.WriteLine("GROUP " & group)
            Dim text As String = nntpSession.ReadLine()
            If Equals(text, Nothing) OrElse Not text.StartsWith("211", StringComparison.Ordinal) Then
                LastError = If(Not Equals(text, Nothing), text, "GROUP failed.")
                Return ""
            End If
            nntpSession.WriteLine("ARTICLE " & articleNumber.ToString())
            Dim text2 As String = nntpSession.ReadLine()
            If Equals(text2, Nothing) OrElse Not text2.StartsWith("220", StringComparison.Ordinal) Then
                LastError = If(Not Equals(text2, Nothing), text2, "ARTICLE failed.")
                Return ""
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            Dim text3 As String = nntpSession.ReadLine()
            While Not Equals(text3, Nothing) AndAlso Operators.CompareString(text3, ".", TextCompare:=False) <> 0
                stringBuilder.AppendLine(text3)
                text3 = nntpSession.ReadLine()
            End While
            Return stringBuilder.ToString()
        End Function

        Public Shared Function TryPostArticle(host As String, port As Integer, fromLine As String, subject As String, newsgroups As String, body As String) As Boolean
            LastError = Nothing
            Dim nntpSession = Connect(host, port)
            If nntpSession Is Nothing Then
                Return False
            End If
            nntpSession.WriteLine("POST")
            Dim text As String = nntpSession.ReadLine()
            If Equals(text, Nothing) OrElse Not text.StartsWith("340", StringComparison.Ordinal) Then
                LastError = If(Not Equals(text, Nothing), text, "POST not allowed.")
                Return False
            End If
            nntpSession.WriteLine("From: " & If(Not Equals(fromLine, Nothing), fromLine, "user@local"))
            nntpSession.WriteLine("Subject: " & If(Not Equals(subject, Nothing), subject, "(no subject)"))
            nntpSession.WriteLine("Newsgroups: " & If(Not Equals(newsgroups, Nothing), newsgroups, "misc.test"))
            nntpSession.WriteLine("")
            Dim array = If(Not Equals(body, Nothing), body, "").Split(New String(1) {vbCrLf, vbLf}, StringSplitOptions.None)
            For Each text2 In array
                If Operators.CompareString(text2, ".", TextCompare:=False) = 0 Then
                    nntpSession.WriteLine("..")
                Else
                    nntpSession.WriteLine(text2)
                End If
            Next
            nntpSession.WriteLine(".")
            Dim text3 As String = nntpSession.ReadLine()
            If Not Equals(text3, Nothing) AndAlso text3.StartsWith("240", StringComparison.Ordinal) Then
                Return True
            End If
            LastError = If(Not Equals(text3, Nothing), text3, "Post rejected.")
            Return False
        End Function

        Private Shared Function Connect(host As String, port As Integer) As NntpSession
            Dim text As String = If(Not Equals(host, Nothing), host, "").Trim()
            Dim result As NntpSession
            If text.Length = 0 Then
                LastError = "NNTP host is required."
                result = Nothing
            Else
                If port <= 0 Then
                    port = 119
                End If
                Try
                    Dim nntpSession As NntpSession = New NntpSession(text, port)
                    Dim text2 As String = nntpSession.ReadLine()
                    If Equals(text2, Nothing) OrElse Not text2.StartsWith("200", StringComparison.Ordinal) Then
                        LastError = If(Not Equals(text2, Nothing), text2, "NNTP greeting failed.")
                        nntpSession.Dispose()
                        result = Nothing
                    Else
                        result = nntpSession
                    End If
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = Nothing
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function
    End Class

