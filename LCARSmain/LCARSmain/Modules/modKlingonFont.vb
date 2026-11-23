Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Drawing.Text
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

''' <summary>
''' Loads the bundled pIqaD qolqoS face (SIL OFL) and maps tlhIngan Hol
''' romanization onto ConScript Unicode Registry pIqaD (U+F8D0..U+F8FF).
''' </summary>
Friend Module modKlingonFont
    Private ReadOnly LoadLock As New Object()
    Private fonts As PrivateFontCollection
    Private fontBytes As Byte()
    Private pin As GCHandle
    Private family As FontFamily
    Private gdiHandle As IntPtr
    Private loadAttempted As Boolean

    <DllImport("gdi32.dll")>
    Private Function AddFontMemResourceEx(ByVal pbFont As IntPtr, ByVal cbFont As Integer, ByVal pdv As IntPtr, ByRef pcFonts As Integer) As IntPtr
    End Function

    Friend ReadOnly Property HasPiqadFont() As Boolean
        Get
            EnsureLoaded()
            Return family IsNot Nothing
        End Get
    End Property

    ''' <summary>
    ''' Builds a disposable pIqaD font, or a sans-serif stand-in if the face failed to load.
    ''' </summary>
    Friend Function CreatePiqadFont(ByVal emSize As Single, Optional ByVal style As FontStyle = FontStyle.Regular) As Font
        EnsureLoaded()
        If family IsNot Nothing Then
            Return New Font(family, emSize, style, GraphicsUnit.Point)
        End If
        Return New Font(FontFamily.GenericSansSerif, emSize, style, GraphicsUnit.Point)
    End Function

    ''' <summary>
    ''' Builds a disposable Latin/LCARS font for Terran captions.
    ''' </summary>
    Friend Function CreateTerranFont(ByVal emSize As Single, Optional ByVal style As FontStyle = FontStyle.Bold) As Font
        Try
            Return New Font("LCARS", emSize, style, GraphicsUnit.Point)
        Catch
            Return New Font(FontFamily.GenericSansSerif, emSize, style, GraphicsUnit.Point)
        End Try
    End Function

    ''' <summary>
    ''' Formats Terran English with Klingon phonetics in parentheses to the right.
    ''' </summary>
    Friend Function TerranWithPhonetics(ByVal terran As String, ByVal klingonPhonetic As String) As String
        If String.IsNullOrEmpty(terran) Then Return If(klingonPhonetic, String.Empty)
        If String.IsNullOrEmpty(klingonPhonetic) Then Return terran
        Return terran & " (" & klingonPhonetic & ")"
    End Function

    ''' <summary>
    ''' Converts tlhIngan Hol romanization to CSUR pIqaD. Unmapped characters stay as-is.
    ''' </summary>
    Friend Function ToPiqad(ByVal latin As String) As String
        If String.IsNullOrEmpty(latin) Then Return String.Empty
        EnsureLoaded()
        If family Is Nothing Then Return latin

        Dim sb As New System.Text.StringBuilder(latin.Length)
        Dim i As Integer = 0
        Dim n As Integer = latin.Length
        While i < n
            If i + 2 < n AndAlso latin(i) = "t"c AndAlso latin(i + 1) = "l"c AndAlso latin(i + 2) = "h"c Then
                sb.Append(ChrW(&HF8E4))
                i += 3
            ElseIf i + 1 < n AndAlso latin(i) = "c"c AndAlso latin(i + 1) = "h"c Then
                sb.Append(ChrW(&HF8D2))
                i += 2
            ElseIf i + 1 < n AndAlso latin(i) = "g"c AndAlso latin(i + 1) = "h"c Then
                sb.Append(ChrW(&HF8D5))
                i += 2
            ElseIf i + 1 < n AndAlso latin(i) = "n"c AndAlso latin(i + 1) = "g"c Then
                sb.Append(ChrW(&HF8DC))
                i += 2
            Else
                sb.Append(MapSinglePiqad(latin(i)))
                i += 1
            End If
        End While
        Return sb.ToString()
    End Function

    Private Function MapSinglePiqad(ByVal c As Char) As Char
        Select Case c
            Case "a"c : Return ChrW(&HF8D0)
            Case "b"c : Return ChrW(&HF8D1)
            Case "D"c : Return ChrW(&HF8D3)
            Case "e"c : Return ChrW(&HF8D4)
            Case "H"c : Return ChrW(&HF8D6)
            Case "I"c : Return ChrW(&HF8D7)
            Case "j"c : Return ChrW(&HF8D8)
            Case "l"c : Return ChrW(&HF8D9)
            Case "m"c : Return ChrW(&HF8DA)
            Case "n"c : Return ChrW(&HF8DB)
            Case "o"c : Return ChrW(&HF8DD)
            Case "p"c : Return ChrW(&HF8DE)
            Case "q"c : Return ChrW(&HF8DF)
            Case "Q"c : Return ChrW(&HF8E0)
            Case "r"c : Return ChrW(&HF8E1)
            Case "S"c : Return ChrW(&HF8E2)
            Case "t"c : Return ChrW(&HF8E3)
            Case "u"c : Return ChrW(&HF8E5)
            Case "v"c : Return ChrW(&HF8E6)
            Case "w"c : Return ChrW(&HF8E7)
            Case "y"c : Return ChrW(&HF8E8)
            Case "'"c, ChrW(&H2018), ChrW(&H2019) : Return ChrW(&HF8E9)
            Case "0"c : Return ChrW(&HF8F0)
            Case "1"c : Return ChrW(&HF8F1)
            Case "2"c : Return ChrW(&HF8F2)
            Case "3"c : Return ChrW(&HF8F3)
            Case "4"c : Return ChrW(&HF8F4)
            Case "5"c : Return ChrW(&HF8F5)
            Case "6"c : Return ChrW(&HF8F6)
            Case "7"c : Return ChrW(&HF8F7)
            Case "8"c : Return ChrW(&HF8F8)
            Case "9"c : Return ChrW(&HF8F9)
            Case ","c : Return ChrW(&HF8FD)
            Case "."c : Return ChrW(&HF8FE)
            Case Else : Return c
        End Select
    End Function

    Private Sub EnsureLoaded()
        If loadAttempted Then Return
        SyncLock LoadLock
            If loadAttempted Then Return
            loadAttempted = True
            Try
                fontBytes = ReadEmbeddedFontBytes()
                If fontBytes Is Nothing OrElse fontBytes.Length < 16 Then Return

                pin = GCHandle.Alloc(fontBytes, GCHandleType.Pinned)
                Dim installed As Integer = 0
                gdiHandle = AddFontMemResourceEx(pin.AddrOfPinnedObject(), fontBytes.Length, IntPtr.Zero, installed)

                fonts = New PrivateFontCollection()
                fonts.AddMemoryFont(pin.AddrOfPinnedObject(), fontBytes.Length)
                If fonts.Families IsNot Nothing AndAlso fonts.Families.Length > 0 Then
                    family = fonts.Families(0)
                End If
            Catch
                family = Nothing
            End Try
        End SyncLock
    End Sub

    Private Function ReadEmbeddedFontBytes() As Byte()
        Dim asm As Assembly = GetType(modKlingonFont).Assembly
        Dim names() As String = asm.GetManifestResourceNames()
        If names IsNot Nothing Then
            For Each n As String In names
                If n IsNot Nothing AndAlso n.EndsWith("pIqaD-qolqoS.ttf", StringComparison.OrdinalIgnoreCase) Then
                    Using s As Stream = asm.GetManifestResourceStream(n)
                        If s Is Nothing Then Exit For
                        Using ms As New MemoryStream()
                            s.CopyTo(ms)
                            Return ms.ToArray()
                        End Using
                    End Using
                End If
            Next
        End If

        Dim sidecarPaths As String() = {
            Path.Combine(Application.StartupPath, "fonts", "pIqaD-qolqoS.ttf"),
            Path.Combine(Application.StartupPath, "Resources", "Fonts", "pIqaD-qolqoS.ttf"),
            Path.Combine(Application.StartupPath, "pIqaD-qolqoS.ttf")
        }
        For Each sidecar As String In sidecarPaths
            If File.Exists(sidecar) Then
                Return File.ReadAllBytes(sidecar)
            End If
        Next
        Return Nothing
    End Function
End Module
