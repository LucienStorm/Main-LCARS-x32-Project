Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices
Imports MimeKit.Cryptography
    <StandardModule>
    Friend NotInheritable Class Program
        <DllImport("kernel32.dll", SetLastError:=True)>
        Private Shared Function AllocConsole() As Boolean
        End Function

        <STAThread>
        Public Shared Sub Main()
            If Not TryRunParseFixture(Environment.GetCommandLineArgs()) Then
                Call Application.EnableVisualStyles()
                Application.SetCompatibleTextRenderingDefault(defaultValue:=False)
                CryptographyContext.Register(CType((Function() CType(CObj(New LcarsOpenPgpContext()), OpenPgpContext)), Func(Of OpenPgpContext)))
                Dim text As String = ParseModeArg(Environment.GetCommandLineArgs())
                If String.IsNullOrWhiteSpace(text) Then
                    text = InferModeFromExeName()
                End If
                Call Application.Run(CType(CObj(New frmMail(text)), Form))
            End If
        End Sub

        Private Shared Function ParseModeArg(args As String()) As String
            If args Is Nothing Then
                Return Nothing
            End If
            Dim num = args.Length - 1
            For i = 1 To num
                Dim array = args
                Dim array2 = array
                Dim num2 = i
                Dim text As String = If(Not Equals(array2(num2), Nothing), array(num2), "").Trim()
                If text.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase) Then
                    Return text.Substring("--mode=".Length).Trim()
                End If
                If String.Equals(text, "--mode", StringComparison.OrdinalIgnoreCase) AndAlso i + 1 < args.Length Then
                    array = args
                    Dim array3 = array
                    num2 = i + 1
                    Return If(Not Equals(array3(num2), Nothing), array(num2), "").Trim()
                End If
                If Not text.StartsWith("-", StringComparison.Ordinal) Then
                    Dim text2 As String = text.ToLowerInvariant()
                    Select Case text2
                        Case "mail", "chat", "rss", "calendar", "cal"
                            Return text2
                    End Select
                End If
            Next
            Return Nothing
        End Function

        Private Shared Function InferModeFromExeName() As String
            Try
                Dim text As String = Path.GetFileNameWithoutExtension(Application.ExecutablePath).ToLowerInvariant()
                If text.IndexOf("chat", StringComparison.Ordinal) >= 0 Then
                    Return "chat"
                End If
                If text.IndexOf("rss", StringComparison.Ordinal) >= 0 Then
                    Return "rss"
                End If
                If text.IndexOf("calendar", StringComparison.Ordinal) >= 0 Then
                    Return "calendar"
                End If
                If text.EndsWith("cal", StringComparison.Ordinal) Then
                    Return "calendar"
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return Nothing
        End Function

        Private Shared Function TryRunParseFixture(args As String()) As Boolean
            If args Is Nothing OrElse args.Length < 2 Then
                Return False
            End If
            Dim text As String = Nothing
            Dim num = args.Length - 1
            For i = 1 To num
                If String.Equals(args(i), "--parse-fixture", StringComparison.OrdinalIgnoreCase) Then
                    text = If(i + 1 >= args.Length OrElse args(i + 1).StartsWith("-", StringComparison.Ordinal), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Testdata", "sample-prefs.js"), args(i + 1))
                    Exit For
                End If
            Next
            If String.IsNullOrWhiteSpace(text) Then
                Return False
            End If
            Call AllocConsole()
            Try
                Dim list = ThunderbirdPrefsParser.ParsePrefsJs(text)
                If Not String.IsNullOrEmpty(ThunderbirdPrefsParser.LastError) Then
                    Console.WriteLine("Parse error: {0}", ThunderbirdPrefsParser.LastError)
                    Environment.Exit(1)
                End If
                Console.WriteLine("Parsed {0} IMAP account(s) from {1}", list.Count, text)
                For Each item In list
                    Console.WriteLine("  {0}: {1} @ {2}:{3} ssl={4} startTls={5} user={6}", item.Id, item.EmailAddress, item.ImapHost, item.ImapPort, item.ImapSsl, item.ImapStartTls, item.Username)
                Next
                Environment.Exit(If(list.Count < 1, 1, 0))
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                Console.WriteLine("Parse error: {0}", ex2.Message)
                Environment.Exit(1)
                Call ProjectData.ClearProjectError()
            End Try
            Return True
        End Function
    End Class

