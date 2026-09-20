' LCARSmain/Modules/modMediaSession.vb
Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Phase 1 stub: receives LCARSmedia WM_COPYDATA state for the future weather-row strip.
''' Magic dwData = &amp;H4C4D4544 ("LMED"). No UI yet — last payload cached only.
''' </summary>
Public Module modMediaSession
    Public Const MediaMagic As Integer = &H4C4D4544 ' 'LMED'

    Public LastKind As Integer = 0
    Public LastTitle As String = ""
    Public LastPlaying As Boolean = False
    Public LastPositionMs As Integer = 0
    Public LastRawPayload As String = ""

    ''' <summary>
    ''' Parse COPYDATA payload from LCARSmedia.
    ''' State: kind|title|playing|positionMs
    ''' Command echo (unused Phase 1): CMD|id
    ''' </summary>
    Public Function TryHandleCopyData(ByVal dwData As IntPtr, ByVal cbData As Integer, ByVal lpData As IntPtr) As Boolean
        If dwData.ToInt32() <> MediaMagic Then Return False
        If lpData = IntPtr.Zero OrElse cbData <= 0 Then Return True

        Try
            Dim bytes(cbData - 1) As Byte
            Marshal.Copy(lpData, bytes, 0, cbData)
            Dim payload As String = Encoding.Unicode.GetString(bytes).TrimEnd(ChrW(0))
            LastRawPayload = payload
            If payload.StartsWith("CMD|", StringComparison.OrdinalIgnoreCase) Then
                ' Future: forward PlayPause/Stop/ShowWindow to LCARSmedia process.
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
End Module
