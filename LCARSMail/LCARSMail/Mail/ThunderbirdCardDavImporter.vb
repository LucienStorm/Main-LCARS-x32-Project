Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
    Public NotInheritable Class ThunderbirdCardDavImporter
        Private Shared ReadOnly UserPrefPattern As Regex = New Regex("^\s*user_pref\s*\(\s*""([^""]+)""\s*,\s*(.+?)\s*\)\s*;\s*$", RegexOptions.Compiled Or RegexOptions.CultureInvariant)

        Private Sub New()
        End Sub

        Public Shared Function TryLoadFromProfile(profileDir As String) As List(Of ThunderbirdCardDavAccount)
            Return TryLoadDavServersFromProfile(profileDir, "carddav")
        End Function

        Public Shared Function TryLoadCalDavFromProfile(profileDir As String) As List(Of ThunderbirdCardDavAccount)
            Return TryLoadDavServersFromProfile(profileDir, "caldav")
        End Function

        Private Shared Function TryLoadDavServersFromProfile(profileDir As String, serverType As String) As List(Of ThunderbirdCardDavAccount)
            Dim list As List(Of ThunderbirdCardDavAccount) = New List(Of ThunderbirdCardDavAccount)()
            If String.IsNullOrWhiteSpace(profileDir) Then
                Return list
            End If
            Dim text As String = Path.Combine(profileDir.Trim(), "prefs.js")
            If Not File.Exists(text) Then
                Return list
            End If
            Dim text2 As String = If(Not Equals(serverType, Nothing), serverType, "").Trim()
            If text2.Length = 0 Then
                Return list
            End If
            Dim dictionary = LoadPrefs(text)
            For Each key In dictionary.Keys
                If key.StartsWith("mail.server.", StringComparison.Ordinal) AndAlso key.EndsWith(".type", StringComparison.Ordinal) AndAlso String.Equals(dictionary(key), text2, StringComparison.OrdinalIgnoreCase) Then
                    Dim text3 = key.Substring("mail.server.".Length)
                    text3 = text3.Substring(0, text3.Length - ".type".Length)
                    Dim pref = GetPref(dictionary, "mail.server." & text3 & ".serverURI")
                    If String.IsNullOrWhiteSpace(pref) Then
                        pref = GetPref(dictionary, "mail.server." & text3 & ".hostname")
                    End If
                    Dim pref2 = GetPref(dictionary, "mail.server." & text3 & ".userName")
                    If Not String.IsNullOrWhiteSpace(pref) Then
                        list.Add(New ThunderbirdCardDavAccount With {
        .ServerUrl = pref,
        .Username = If(Not Equals(pref2, Nothing), pref2, "")
    })
                    End If
                End If
            Next
            Return list
        End Function

        Private Shared Function LoadPrefs(prefsJsPath As String) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            Dim array = File.ReadAllLines(prefsJsPath, Encoding.UTF8)
            For Each text In array
                Dim match As Match = UserPrefPattern.Match(text.Trim())
                If match.Success Then
                    dictionary(match.Groups(1).Value) = ParsePrefValue(match.Groups(2).Value.Trim())
                End If
            Next
            Return dictionary
        End Function

        Private Shared Function ParsePrefValue(valueRaw As String) As String
            If valueRaw.Length > 0 AndAlso valueRaw(0) = """"c Then
                Return valueRaw.Trim(""""c).Replace("\\", "\")
            End If
            Return valueRaw
        End Function

        Private Shared Function GetPref(prefs As Dictionary(Of String, String), key As String) As String
            Dim value As String = Nothing
            If prefs.TryGetValue(key, value) Then
                Return value
            End If
            Return Nothing
        End Function
    End Class

