' LCARSTerminal.Tests/Program.vb — console self-test (no MSTest dependency)
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.IO
Imports LCARSTerminal

Module Program
    Private _failed As Integer = 0

    Sub Main()
        TestProfileRoundTrip()
        TestProfileMissingFile()
        TestRdpCodec()
        TestCredTargetName()
        TestCredRoundTrip()
        If _failed > 0 Then
            Console.WriteLine("FAILED: " & _failed.ToString() & " assertion(s)")
            Environment.Exit(1)
        End If
        Console.WriteLine("All RDP self-tests passed.")
    End Sub

    Private Sub Check(ByVal ok As Boolean, ByVal msg As String)
        If ok Then
            Console.WriteLine("OK  " & msg)
        Else
            Console.WriteLine("FAIL " & msg)
            _failed += 1
        End If
    End Sub

    Private Sub TestProfileRoundTrip()
        Dim dir As String = Path.Combine(Path.GetTempPath(), "LCARSTerminalRdpTests-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(dir)
        Try
            Dim store As New RdpProfileStore(dir)
            Dim p As New RdpConnectionProfile()
            p.Id = Guid.NewGuid()
            p.DisplayName = "Lab"
            p.Hostname = "192.168.1.10"
            p.Username = "admin"
            p.Domain = "HOME"
            p.Port = 3389
            p.RememberPassword = True
            store.SaveAll(New List(Of RdpConnectionProfile) From {p})
            Dim loaded As List(Of RdpConnectionProfile) = store.LoadAll()
            Check(loaded.Count = 1, "profile count")
            Check(loaded(0).Hostname = "192.168.1.10", "hostname")
            Check(loaded(0).Username = "admin", "username")
            Check(loaded(0).Domain = "HOME", "domain")
            Check(loaded(0).RememberPassword, "remember")
            Check(loaded(0).Id = p.Id, "id")
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

    Private Sub TestProfileMissingFile()
        Dim dir As String = Path.Combine(Path.GetTempPath(), "LCARSTerminalRdpTests-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(dir)
        Try
            Dim store As New RdpProfileStore(dir)
            Check(store.LoadAll().Count = 0, "missing file empty")
        Finally
            Directory.Delete(dir, True)
        End Try
    End Sub

    Private Sub TestRdpCodec()
        Dim p As New RdpConnectionProfile()
        p.Hostname = "box.local"
        p.Username = "sam"
        p.Domain = "WORK"
        Dim text As String = RdpFileCodec.FromProfile(p)
        Check(text.Contains("full address:s:box.local"), "codec emit host")
        Check(text.Contains("username:s:sam"), "codec emit user")
        Check(text.Contains("redirectclipboard:i:"), "codec emit clipboard")
        Dim raw As String =
            "full address:s:host.example" & vbCrLf &
            "server port:i:3390" & vbCrLf &
            "username:s:jane" & vbCrLf &
            "domain:s:CORP" & vbCrLf
        Dim parsed As RdpConnectionProfile = Nothing
        Check(RdpFileCodec.TryParse(raw, parsed), "codec parse")
        Check(parsed.Hostname = "host.example", "parse host")
        Check(parsed.Port = 3390, "parse port")
        Check(parsed.Username = "jane", "parse user")
        Check(parsed.Domain = "CORP", "parse domain")
    End Sub

    Private Sub TestCredTargetName()
        Dim id As Guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
        Check(RdpCredentialStore.TargetName(id) = "LCARSTerminal/Remote/aaaaaaaabbbbccccddddeeeeeeeeeeee", "cred target")
    End Sub

    Private Sub TestCredRoundTrip()
        Dim id As Guid = Guid.NewGuid()
        Dim store As New RdpCredentialStore()
        store.SavePassword(id, "tester", "secret-pass")
        Dim got As String = Nothing
        Check(store.TryGetPassword(id, got), "cred get")
        Check(got = "secret-pass", "cred value")
        store.DeletePassword(id)
        Check(Not store.TryGetPassword(id, got), "cred deleted")
    End Sub
End Module
