Imports System
Imports System.IO
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailOfflinePolicy
        Private Const PolicyFileName As String = "cache-policy.txt"

        Private Const DefaultBodyRetentionDays As Integer = 90

        Private Const DefaultHeaderRetentionDays As Integer = 365

        Private Sub New()
        End Sub

        Public Shared Function BodyRetentionDays() As Integer
            Return ReadPolicyInt("body", 90)
        End Function

        Public Shared Function HeaderRetentionDays() As Integer
            Return ReadPolicyInt("header", 365)
        End Function

        Public Shared Sub SavePolicy(bodyDays As Integer, headerDays As Integer)
            If bodyDays <= 0 Then
                bodyDays = 90
            End If
            If headerDays <= 0 Then
                headerDays = 365
            End If
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "cache-policy.txt")
            Call File.WriteAllText(path, "body=" & bodyDays.ToString() & vbCrLf & "header=" & headerDays.ToString())
        End Sub

        Private Shared Function ReadPolicyInt(key As String, defaultValue As Integer) As Integer
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "cache-policy.txt")
            If Not File.Exists(path) Then
                Return defaultValue
            End If
            Try
                Dim array = File.ReadAllLines(path)
                For Each text In array
                    Dim text2 As String = If(Not Equals(text, Nothing), text, "").Trim()
                    If text2.StartsWith(key & "=", StringComparison.OrdinalIgnoreCase) Then
                        Dim result = 0
                        If Integer.TryParse(text2.Substring(key.Length + 1), result) AndAlso result > 0 Then
                            Return result
                        End If
                    End If
                    If Operators.CompareString(key, "body", TextCompare:=False) = 0 AndAlso Not text2.Contains("=") Then
                        Dim result2 = 0
                        If Integer.TryParse(text2, result2) AndAlso result2 > 0 Then
                            Return result2
                        End If
                    End If
                Next
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return defaultValue
        End Function

        Public Shared Sub ApplyHeaderRetention(accountId As String)
            If String.IsNullOrWhiteSpace(accountId) Then
                Return
            End If
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "cache", accountId)
            If Not Directory.Exists(path) Then
                Return
            End If
            Dim t As Date = Date.UtcNow.AddDays(-HeaderRetentionDays())
            Dim files = Directory.GetFiles(path, "headers.json", SearchOption.AllDirectories)
            For Each path2 In files
                Try
                    If Date.Compare(File.GetLastWriteTimeUtc(path2), t) < 0 Then
                        File.Delete(path2)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            Next
        End Sub

        Public Shared Sub ApplyBodyRetention(accountId As String)
            If String.IsNullOrWhiteSpace(accountId) Then
                Return
            End If
            Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "cache", accountId)
            If Not Directory.Exists(path) Then
                Return
            End If
            Dim t As Date = Date.UtcNow.AddDays(-BodyRetentionDays())
            Dim files = Directory.GetFiles(path, "*.body.json", SearchOption.AllDirectories)
            For Each path2 In files
                Try
                    If Date.Compare(File.GetLastWriteTimeUtc(path2), t) < 0 Then
                        File.Delete(path2)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
            Next
        End Sub
    End Class

