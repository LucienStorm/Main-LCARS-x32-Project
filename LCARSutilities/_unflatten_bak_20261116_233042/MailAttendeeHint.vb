' LCARSutilities/LCARSutilities/Modules/MailAttendeeHint.vb
Option Strict On
Option Explicit On
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
''' <summary>Resolve local mailbox
for CalDAV ATTENDEE lines.</summary>
Public Module MailAttendeeHint
Private Class AccountEmailDto
Public EmailAddress As String
Public Username As String
End Class
Public Function TryGetPrimaryAttendeeMailto() As String
Dim path As String = IO.Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LCARS x32", "Mail", "accounts.json")
If Not File.Exists(path) Then Return ""
Try
Dim ser As New JavaScriptSerializer()
Dim list As List(Of AccountEmailDto) = ser.Deserialize(Of List(Of AccountEmailDto))(File.ReadAllText(path, Encoding.UTF8))
If list Is Nothing Then Return ""
For Each dto As AccountEmailDto In list
If dto Is Nothing Then Continue For
Dim email As String = If(dto.EmailAddress, "").Trim()
If email.Length = 0 Then email = If(dto.Username, "").Trim()
If email.Length = 0 Then Continue For
If email.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) Then Return email
Return "mailto:" & email
Next
Catch
End Try
Return ""
End Function
Public Function TryLaunchOrganizerRsvpMail( ByVal organizerLine As String, ByVal eventSummary As String, ByVal partStat As String) As Boolean
Dim smtp As String = NormalizeOrganizerSmtp(organizerLine)
If smtp.Length = 0 Then Return False
Dim stat As String = If(partStat, "").Trim().ToUpperInvariant()
If stat.Length = 0 Then Return False
Dim subj As String = Uri.EscapeDataString("RE: " & If(eventSummary, "Meeting").Trim() & " — " & stat)
Dim body As String = Uri.EscapeDataString("PARTSTAT=" & stat)
Try Process.Start("mailto:" & smtp & "?subject=" & subj & "&body=" & body)
Return True
Catch
Return False
End Try
End Function
Private Function NormalizeOrganizerSmtp(ByVal organizerLine As String) As String
Dim raw As String = If(organizerLine, "").Trim()
If raw.Length = 0 Then Return ""
Dim mailtoIdx As Integer = raw.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase)
If mailtoIdx >= 0 Then raw = raw.Substring(mailtoIdx + 7)
Dim at As Integer = raw.IndexOf("@"c)
If at <= 0 Then Return ""
Dim semi As Integer = raw.IndexOf(";"c)
If semi > 0 Then raw = raw.Substring(0, semi)
Return raw.Trim()
End Function
End Module
