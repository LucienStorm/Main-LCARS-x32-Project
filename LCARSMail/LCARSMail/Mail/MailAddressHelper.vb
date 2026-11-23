Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports Microsoft.VisualBasic.CompilerServices
Imports MimeKit
    Public NotInheritable Class MailAddressHelper
        Private Sub New()
        End Sub

        Public Shared Function FormatDisplayLine(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Try
                Dim val As InternetAddressList = InternetAddressList.Parse(text)
                Dim list As List(Of String) = New List(Of String)()
                For Each item As InternetAddress In val
                    list.Add(MailAddressHelper.FormatOne(item))
                Next
                If list.Count > 0 Then
                    Return String.Join("; ", list.ToArray())
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return text
        End Function

        Public Shared Function FormatOne(addr As InternetAddress) As String
            If addr Is Nothing Then
                Return ""
            End If
            Dim val As GroupAddress = CType(If((TypeOf addr Is GroupAddress), addr, Nothing), GroupAddress)
            Dim name As String
            If val IsNot Nothing Then
                Dim list As List(Of String) = New List(Of String)()
                For Each member As InternetAddress In val.Members
                    list.Add(MailAddressHelper.FormatOne(member))
                Next
                name = CType(val, InternetAddress).Name
                Dim text As String = If(Not Equals(name, Nothing), name, "").Trim()
                If text.Length = 0 Then
                    text = "group"
                End If
                Return text & ": " & String.Join(", ", list.ToArray()) & ";"
            End If
            Dim val2 As MailboxAddress = CType(If((TypeOf addr Is MailboxAddress), addr, Nothing), MailboxAddress)
            If val2 Is Nothing Then
                Return addr.ToString()
            End If
            Dim text2 = DecodeEmailForDisplay(val2.Address)
            name = CType(val2, InternetAddress).Name
            Dim text3 As String = If(Not Equals(name, Nothing), name, "").Trim()
            If text3.Length = 0 Then
                Return text2
            End If
            Return text3 & " <" & text2 & ">"
        End Function

        Public Shared Function SplitAddressTokens(raw As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return list
            End If
            Try
                Dim val As InternetAddressList = InternetAddressList.Parse(text)
                For Each item As InternetAddress In val
                    Dim val2 As MailboxAddress = CType(If((TypeOf item Is MailboxAddress), item, Nothing), MailboxAddress)
                    If val2 IsNot Nothing Then
                        list.Add(CType(val2, InternetAddress).ToString())
                        Continue For
                    End If
                    Dim val3 As GroupAddress = CType(If((TypeOf item Is GroupAddress), item, Nothing), GroupAddress)
                    If val3 Is Nothing Then
                        Continue For
                    End If
                    For Each member As InternetAddress In val3.Members
                        Dim val4 As MailboxAddress = CType(If((TypeOf member Is MailboxAddress), member, Nothing), MailboxAddress)
                        If val4 IsNot Nothing Then
                            list.Add(CType(val4, InternetAddress).ToString())
                        End If
                    Next
                Next
                Return list
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Dim array = text.Split(","c)
            For Each text2 In array
                Dim text3 As String = text2.Trim()
                If text3.Length > 0 Then
                    list.Add(text3)
                End If
            Next
            Return list
        End Function

        Public Shared Function NormalizeForTransport(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            Dim result As String
            If text.Length = 0 Then
                result = ""
            Else
                Try
                    Dim val As InternetAddressList = InternetAddressList.Parse(text)
                    Dim val2 As InternetAddressList = New InternetAddressList()
                    For Each item As InternetAddress In val
                        val2.Add(MailAddressHelper.EncodeAddress(item))
                    Next
                    result = val2.ToString()
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = EncodeEmailIdn(text)
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function EncodeAddress(addr As InternetAddress) As InternetAddress
            Dim val As MailboxAddress = CType(If((TypeOf addr Is MailboxAddress), addr, Nothing), MailboxAddress)
            If val IsNot Nothing Then
                Dim name As String = CType(val, InternetAddress).Name
                Return CType(New MailboxAddress(If(Not Equals(name, Nothing), name, ""), EncodeEmailIdn(val.Address)), InternetAddress)
            End If
            Dim val2 As GroupAddress = CType(If((TypeOf addr Is GroupAddress), addr, Nothing), GroupAddress)
            If val2 IsNot Nothing Then
                Dim val3 As InternetAddressList = New InternetAddressList()
                For Each member As InternetAddress In val2.Members
                    val3.Add(MailAddressHelper.EncodeAddress(member))
                Next
                Dim name As String = CType(val2, InternetAddress).Name
                Return CType(New GroupAddress(If(Not Equals(name, Nothing), name, ""), CType(val3, IEnumerable(Of InternetAddress))), InternetAddress)
            End If
            Return addr
        End Function

        Public Shared Function EncodeEmailIdn(email As String) As String
            Dim text As String = If(Not Equals(email, Nothing), email, "").Trim()
            If text.Length = 0 OrElse Not text.Contains("@") Then
                Return text
            End If
            Dim num = text.LastIndexOf("@"c)
            Dim text2 = text.Substring(0, num)
            Dim text3 = text.Substring(num + 1)
            Try
                Dim idnMapping As IdnMapping = New IdnMapping()
                text3 = idnMapping.GetAscii(text3)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return text2 & "@" & text3
        End Function

        Public Shared Function DecodeEmailForDisplay(email As String) As String
            Dim text As String = If(Not Equals(email, Nothing), email, "").Trim()
            If text.Length = 0 OrElse Not text.Contains("@") Then
                Return text
            End If
            Dim num = text.LastIndexOf("@"c)
            Dim text2 = text.Substring(0, num)
            Dim text3 = text.Substring(num + 1)
            Try
                Dim idnMapping As IdnMapping = New IdnMapping()
                text3 = idnMapping.GetUnicode(text3)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return text2 & "@" & text3
        End Function
    End Class

