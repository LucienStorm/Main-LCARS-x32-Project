' LCARSmain/Modules/modMediaSession.vb
Option Strict On
Option Explicit On

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Receives LCARSmedia WM_COPYDATA state (magic LMED) and forwards strip commands to the player.
''' </summary>
Public Module modMediaSession
    Public Const MediaMagic As Integer = &H4C4D4544 ' 'LMED'
    Private Const WmCopyData As Integer = &H4A

    Public LastKind As Integer = 0
    Public LastTitle As String = ""
    Public LastPlaying As Boolean = False
    Public LastPositionMs As Integer = 0
    Public LastRawPayload As String = ""
    Public LastReceivedUtc As DateTime = DateTime.MinValue

    Public ReadOnly Property HasActiveSession As Boolean
        Get
            If LastKind <= 0 Then Return False
            ' Hide strip if no update for 45s and not playing
            If Not LastPlaying AndAlso (DateTime.UtcNow - LastReceivedUtc).TotalSeconds > 45 Then Return False
            Return True
        End Get
    End Property

    <StructLayout(LayoutKind.Sequential)>
    Private Structure COPYDATASTRUCT
        Public dwData As IntPtr
        Public cbData As Integer
        Public lpData As IntPtr
    End Structure

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Function SendMessage(ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByRef lParam As COPYDATASTRUCT) As IntPtr
    End Function

    Public Function TryHandleCopyData(ByVal dwData As IntPtr, ByVal cbData As Integer, ByVal lpData As IntPtr) As Boolean
        If dwData.ToInt32() <> MediaMagic Then Return False
        If lpData = IntPtr.Zero OrElse cbData <= 0 Then Return True

        Try
            Dim bytes(cbData - 1) As Byte
            Marshal.Copy(lpData, bytes, 0, cbData)
            Dim payload As String = Encoding.Unicode.GetString(bytes).TrimEnd(ChrW(0))
            LastRawPayload = payload
            LastReceivedUtc = DateTime.UtcNow
            If payload.StartsWith("CMD|", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            Dim parts As String() = payload.Split("|"c)
            If parts.Length >= 4 Then
                Integer.TryParse(parts(0), LastKind)
                LastTitle = parts(1)
                LastPlaying = (parts(2) = "1")
                Integer.TryParse(parts(3), LastPositionMs)
            End If
        Catch
        End Try
        Return True
    End Function

    Public Sub SendCommandToPlayer(ByVal cmdId As Integer)
        Try
            Dim payload As String = "CMD|" & cmdId.ToString()
            Dim bytes As Byte() = Encoding.Unicode.GetBytes(payload & ChrW(0))
            Dim handle As GCHandle = GCHandle.Alloc(bytes, GCHandleType.Pinned)
            Try
                Dim cds As New COPYDATASTRUCT()
                cds.dwData = New IntPtr(MediaMagic)
                cds.cbData = bytes.Length
                cds.lpData = handle.AddrOfPinnedObject()
                For Each p As Process In Process.GetProcessesByName("LCARSmedia")
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

    Public Sub ClearSession()
        LastKind = 0
        LastTitle = ""
        LastPlaying = False
        LastPositionMs = 0
    End Sub
End Module
