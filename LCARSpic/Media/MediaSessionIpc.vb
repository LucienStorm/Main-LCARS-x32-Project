' LCARSpic/Media/MediaSessionIpc.vb
Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Phase 1 hook for the future shell media strip (weather/clock row).
''' Uses WM_COPYDATA so LCARSmain can listen without a UI strip yet.
''' </summary>
Public Module MediaSessionIpc
    Public Const WmCopyData As Integer = &H4A
    Public Const MediaMagic As Integer = &H4C4D4544 ' 'LMED'

    Public Enum MediaCommand
        None = 0
        PlayPause = 1
        StopPlayback = 2
        ShowWindow = 3
    End Enum

    <StructLayout(LayoutKind.Sequential)>
    Private Structure COPYDATASTRUCT
        Public dwData As IntPtr
        Public cbData As Integer
        Public lpData As IntPtr
    End Structure

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Function FindWindow(ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByRef lParam As COPYDATASTRUCT) As IntPtr
    End Function

    ''' <summary>Broadcast a simple state line: kind|title|playing|positionMs</summary>
    Public Sub BroadcastState(ByVal kind As MediaKind, ByVal title As String, ByVal playing As Boolean, ByVal positionMs As Integer)
        Dim payload As String = CInt(kind).ToString() & "|" &
            If(title, "").Replace("|"c, "/"c) & "|" &
            If(playing, "1", "0") & "|" &
            positionMs.ToString()
        SendPayload(payload)
    End Sub

    Public Sub BroadcastCommand(ByVal cmd As MediaCommand)
        SendPayload("CMD|" & CInt(cmd).ToString())
    End Sub

    Private Sub SendPayload(ByVal payload As String)
        Try
            ' Shell mainscreens use varying titles; try common LCARS main form titles.
            Dim targets As String() = {"LCARS", "frmMainscreen1", "frmMainscreen2", "frmMainscreen3", "frmMainscreen4"}
            Dim bytes As Byte() = Encoding.Unicode.GetBytes(payload & ChrW(0))
            Dim handle As GCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned)
            Try
                Dim cds As New COPYDATASTRUCT()
                cds.dwData = New IntPtr(MediaMagic)
                cds.cbData = bytes.Length
                cds.lpData = handle.AddrOfPinnedObject()
                For Each title As String In targets
                    Dim hwnd As IntPtr = FindWindow(Nothing, title)
                    If hwnd <> IntPtr.Zero Then
                        SendMessage(hwnd, WmCopyData, IntPtr.Zero, cds)
                    End If
                Next
            Finally
                handle.Free()
            End Try
        Catch
        End Try
    End Sub
End Module
