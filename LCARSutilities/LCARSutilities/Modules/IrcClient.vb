' LCARSutilities/LCARSutilities/Modules/IrcClient.vb
Option Strict On
Option Explicit On
Option Infer On

Imports System.IO
Imports System.Net.Sockets
Imports System.Text
Imports System.Threading

Public Class IrcClient
    Implements IDisposable

    Private client As TcpClient
    Private stream As NetworkStream
    Private reader As StreamReader
    Private writer As StreamWriter
    Private readThread As Thread
    Private stopRequested As Boolean

    Public Event LineReceived As Action(Of String)

    Public Sub Connect(ByVal host As String, ByVal port As Integer, ByVal nick As String, ByVal channel As String)
        Disconnect()
        client = New TcpClient()
        client.Connect(host, port)
        stream = client.GetStream()
        reader = New StreamReader(stream, Encoding.UTF8)
        writer = New StreamWriter(stream, New UTF8Encoding(False)) With {.AutoFlush = True}
        SendRaw("NICK " & nick)
        SendRaw("USER " & nick & " 0 * :LCARS Chat")
        If Not String.IsNullOrWhiteSpace(channel) Then
            SendRaw("JOIN " & channel.Trim())
        End If
        stopRequested = False
        readThread = New Thread(AddressOf ReadLoop) With {.IsBackground = True}
        readThread.Start()
    End Sub

    Public Sub SendRaw(ByVal line As String)
        If writer Is Nothing Then Return
        writer.WriteLine(line)
    End Sub

    Public Sub SendChannelMessage(ByVal target As String, ByVal text As String)
        SendRaw("PRIVMSG " & target & " :" & text)
    End Sub

    Private Sub ReadLoop()
        Try
            While Not stopRequested AndAlso reader IsNot Nothing
                Dim line As String = reader.ReadLine()
                If line Is Nothing Then Exit While
                If line.StartsWith("PING ", StringComparison.OrdinalIgnoreCase) Then
                    SendRaw("PONG " & line.Substring(5))
                    Continue While
                End If
                RaiseEvent LineReceived(line)
            End While
        Catch
        End Try
    End Sub

    Public Sub Disconnect()
        stopRequested = True
        Try
            If reader IsNot Nothing Then reader.Dispose()
            If writer IsNot Nothing Then writer.Dispose()
            If stream IsNot Nothing Then stream.Dispose()
            If client IsNot Nothing Then client.Close()
        Catch
        End Try
        reader = Nothing
        writer = Nothing
        stream = Nothing
        client = Nothing
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Disconnect()
    End Sub
End Class
