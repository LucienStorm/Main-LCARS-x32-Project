' LCARSpic/Media/MediaSessionIpc.vb
Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Shell ↔ LCARSmedia IPC via WM_COPYDATA (magic LMED).
''' State: kind|title|playing|positionMs
''' Command: CMD|id  (PlayPause=1, Stop=2, ShowWindow=3)
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
    Private Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByRef lParam As COPYDATASTRUCT) As IntPtr
    End Function

    Public Sub BroadcastState(ByVal kind As MediaKind, ByVal title As String, ByVal playing As Boolean, ByVal positionMs As Integer)
        Dim payload As String = CInt(kind).ToString() & "|" &
            If(title, "").Replace("|"c, "/"c) & "|" &
            If(playing, "1", "0") & "|" &
            positionMs.ToString()
        SendPayloadToProcess("LCARSmain", payload)
    End Sub

    Private Sub SendPayloadToProcess(ByVal processName As String, ByVal payload As String)
        Try
            Dim bytes As Byte() = Encoding.Unicode.GetBytes(payload & ChrW(0))
            Dim handle As GCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned)
            Try
                Dim cds As New COPYDATASTRUCT()
                cds.dwData = New IntPtr(MediaMagic)
                cds.cbData = bytes.Length
                cds.lpData = handle.AddrOfPinnedObject()
                For Each p As Process In Process.GetProcessesByName(processName)
                    Try
                        If p.MainWindowHandle <> IntPtr.Zero Then
                            SendMessage(p.MainWindowHandle, WmCopyData, IntPtr.Zero, cds)
                        End If
                    Catch
                    End Try
                Next
            Finally
                handle.Free()
            End Try
        Catch
        End Try
    End Sub
End Module
