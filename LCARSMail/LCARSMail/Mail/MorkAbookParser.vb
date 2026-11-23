Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MorkAbookParser
        Public Shared LastError As String

        Public Shared LastSourceMabRoundTripValidated As Boolean = False

        Public Shared LastSourceMabRoundTripByteIdentical As Boolean = False

        Public Shared LastMabExportByteIdentical As Boolean = False

        Public Shared LastMabExportVerifiedAgainstSource As Boolean = False

        Public Shared LastMabExportEmailSetMatches As Boolean = False

        Public Shared LastMabExportFieldsMatch As Boolean = False

        Public Shared LastMabExportSourceByteLength As Long = 0L

        Public Shared LastMabExportDestByteLength As Long = 0L

        Public Shared LastMabExportHeaderPrefixMatchBytes As Integer = 0

        Private Const ExportHeaderPrefixCompareBytes As Integer = 512

        Public Shared LastValidatedSourceMabPath As String = ""

        Public Shared LastMorkColumnKeyCount As Integer = 0

        Public Shared LastMorkRowPropertyFieldCount As Integer = 0

        Public Shared LastMorkTableCount As Integer = 0

        Public Shared LastMorkParsedContactCount As Integer = 0

        Public Shared LastMorkRowMapCount As Integer = 0

        Public Shared LastMorkPreserveExportFieldValidated As Boolean = False

        Private Shared ReadOnly EmailPattern As Regex = New Regex("[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled Or RegexOptions.CultureInvariant)

        Public Shared Function TryMeasureExportHeaderPrefixMatch(sourceMabPath As String, exportedMabPath As String) As Integer
            LastMabExportHeaderPrefixMatchBytes = 0
            Dim result As Integer
            Try
                If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                    result = 0
                ElseIf String.IsNullOrWhiteSpace(exportedMabPath) OrElse Not File.Exists(exportedMabPath) Then
                    result = 0
                Else
                    Dim array = File.ReadAllBytes(sourceMabPath)
                    Dim array2 = File.ReadAllBytes(exportedMabPath)
                    Dim num = Math.Min(512, Math.Min(array.Length, array2.Length))
                    Dim num2 = 0
                    Dim num3 = num - 1
                    Dim i = 0

                    While i <= num3 AndAlso array(i) = array2(i)
                        num2 += 1
                        i += 1
                    End While
                    LastMabExportHeaderPrefixMatchBytes = num2
                    result = num2
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = 0
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Sub RecordExportByteSizes(sourceMabPath As String, exportedMabPath As String)
            LastMabExportSourceByteLength = 0L
            LastMabExportDestByteLength = 0L
            Try
                If Not String.IsNullOrWhiteSpace(sourceMabPath) AndAlso File.Exists(sourceMabPath) Then
                    LastMabExportSourceByteLength = New FileInfo(sourceMabPath).Length
                End If
                If Not String.IsNullOrWhiteSpace(exportedMabPath) AndAlso File.Exists(exportedMabPath) Then
                    LastMabExportDestByteLength = New FileInfo(exportedMabPath).Length
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Private Sub New()
        End Sub

        Public Shared Function TryParseContacts(mabPath As String) As List(Of MailContact)
            LastError = Nothing
            Dim result As List(Of MailContact)
            If String.IsNullOrWhiteSpace(mabPath) OrElse Not File.Exists(mabPath) Then
                LastError = "abook.mab not found."
                result = New List(Of MailContact)()
            Else
                Try
                    Dim array = File.ReadAllBytes(mabPath)
                    Dim encoding = DetectMorkFileEncoding(array)
                    result = ExtractContacts(encoding.GetString(array))
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = New List(Of MailContact)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Friend Shared Function ExtractContacts(morkText As String) As List(Of MailContact)
            Dim dictionary As Dictionary(Of String, MailContact) = New Dictionary(Of String, MailContact)(StringComparer.OrdinalIgnoreCase)
            If String.IsNullOrEmpty(morkText) Then
                Return New List(Of MailContact)()
            End If
            Dim dictionary2 = ExtractPropertyMap(morkText, "DisplayName")
            Dim dictionary3 = ExtractPropertyMap(morkText, "FirstName")
            Dim dictionary4 = ExtractPropertyMap(morkText, "LastName")
            Dim dictionary5 = ExtractPropertyMap(morkText, "NickName")
            Dim dictionary6 = ExtractPropertyMap(morkText, "JobTitle")
            Dim dictionary7 = ExtractPropertyMap(morkText, "Department")
            Dim dictionary8 = ExtractPropertyMap(morkText, "WebPage1")
            Dim dictionary9 = ExtractPropertyMap(morkText, "PrimaryEmail")
            Dim dictionary10 = ExtractPropertyMap(morkText, "SecondEmail")
            Dim dictionary11 = ExtractPropertyMap(morkText, "Company")
            Dim dictionary12 = ExtractPropertyMap(morkText, "WorkPhone")
            Dim dictionary13 = ExtractPropertyMap(morkText, "CellularNumber")
            Dim dictionary14 = ExtractPropertyMap(morkText, "HomePhone")
            Dim dictionary15 = ExtractPropertyMap(morkText, "FaxNumber")
            Dim dictionary16 = ExtractPropertyMap(morkText, "PagerNumber")
            Dim dictionary17 = ExtractPropertyMap(morkText, "Custom1")
            Dim dictionary18 = ExtractPropertyMap(morkText, "Custom2")
            Dim dictionary19 = ExtractPropertyMap(morkText, "Custom3")
            Dim dictionary20 = ExtractPropertyMap(morkText, "Custom4")
            Dim dictionary21 = ExtractPropertyMap(morkText, "SpouseName")
            Dim dictionary22 = ExtractPropertyMap(morkText, "Notes")
            Dim dictionary23 = ExtractPropertyMap(morkText, "ScreenName")
            Dim dictionary24 = ExtractPropertyMap(morkText, "AimScreenName")
            Dim dictionary25 = ExtractPropertyMap(morkText, "PopularityIndex")
            Dim dictionary26 = ExtractPropertyMap(morkText, "RecordName")
            Dim dictionary27 = ExtractPropertyMap(morkText, "PreferEmail")
            Dim dictionary28 = ExtractPropertyMap(morkText, "LowercasePrimaryEmail")
            Dim dictionary29 = ExtractPropertyMap(morkText, "WorkAddress")
            Dim dictionary30 = ExtractPropertyMap(morkText, "HomeAddress")
            Dim dictionary31 = ExtractPropertyMap(morkText, "BirthDay")
            MergePropertyMaps(dictionary31, ExtractPropertyMap(morkText, "Birthday"))
            Dim dictionary32 = ExtractPropertyMap(morkText, "Anniversary")
            Dim dictionary33 = ExtractPropertyMap(morkText, "MiddleName")
            Dim dictionary34 = ExtractPropertyMap(morkText, "OfficeLocation")
            Dim dictionary35 = ExtractPropertyMap(morkText, "OtherPhone")
            Dim dictionary36 = ExtractPropertyMap(morkText, "AssistantName")
            Dim dictionary37 = ExtractColumnKeyMap(morkText)
            LastMorkColumnKeyCount = If(dictionary37 IsNot Nothing, dictionary37.Count, 0)
            Dim dictionary38 = BuildRowPropertyMaps(morkText, dictionary37)
            LastMorkRowPropertyFieldCount = 0
            If dictionary38 IsNot Nothing AndAlso dictionary38.Count > 0 Then
                Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                For Each item In dictionary38
                    If item.Value Is Nothing Then
                        Continue For
                    End If
                    For Each key3 In item.Value.Keys
                        hashSet.Add(key3)
                    Next
                Next
                LastMorkRowPropertyFieldCount = hashSet.Count
            End If
            LastMorkRowMapCount = If(dictionary38 IsNot Nothing, dictionary38.Count, 0)
            MergeMapsFromRowProps(dictionary38, "PrimaryEmail", dictionary9)
            MergeMapsFromRowProps(dictionary38, "SecondEmail", dictionary10)
            MergeMapsFromRowProps(dictionary38, "DisplayName", dictionary2)
            MergeMapsFromRowProps(dictionary38, "FirstName", dictionary3)
            MergeMapsFromRowProps(dictionary38, "LastName", dictionary4)
            MergeMapsFromRowProps(dictionary38, "NickName", dictionary5)
            MergeMapsFromRowProps(dictionary38, "JobTitle", dictionary6)
            MergeMapsFromRowProps(dictionary38, "Department", dictionary7)
            MergeMapsFromRowProps(dictionary38, "WebPage1", dictionary8)
            MergeMapsFromRowProps(dictionary38, "Company", dictionary11)
            MergeMapsFromRowProps(dictionary38, "WorkPhone", dictionary12)
            MergeMapsFromRowProps(dictionary38, "CellularNumber", dictionary13)
            MergeMapsFromRowProps(dictionary38, "HomePhone", dictionary14)
            MergeMapsFromRowProps(dictionary38, "FaxNumber", dictionary15)
            MergeMapsFromRowProps(dictionary38, "PagerNumber", dictionary16)
            MergeMapsFromRowProps(dictionary38, "Custom1", dictionary17)
            MergeMapsFromRowProps(dictionary38, "Custom2", dictionary18)
            MergeMapsFromRowProps(dictionary38, "Custom3", dictionary19)
            MergeMapsFromRowProps(dictionary38, "Custom4", dictionary20)
            MergeMapsFromRowProps(dictionary38, "SpouseName", dictionary21)
            MergeMapsFromRowProps(dictionary38, "Notes", dictionary22)
            MergeMapsFromRowProps(dictionary38, "ScreenName", dictionary23)
            MergeMapsFromRowProps(dictionary38, "AimScreenName", dictionary24)
            MergeMapsFromRowProps(dictionary38, "PopularityIndex", dictionary25)
            MergeMapsFromRowProps(dictionary38, "RecordName", dictionary26)
            MergeMapsFromRowProps(dictionary38, "PreferEmail", dictionary27)
            MergeMapsFromRowProps(dictionary38, "LowercasePrimaryEmail", dictionary28)
            MergeMapsFromRowProps(dictionary38, "WorkAddress", dictionary29)
            MergeMapsFromRowProps(dictionary38, "HomeAddress", dictionary30)
            MergeMapsFromRowProps(dictionary38, "BirthDay", dictionary31)
            MergeMapsFromRowProps(dictionary38, "Anniversary", dictionary32)
            MergeMapsFromRowProps(dictionary38, "MiddleName", dictionary33)
            MergeMapsFromRowProps(dictionary38, "OfficeLocation", dictionary34)
            MergeMapsFromRowProps(dictionary38, "OtherPhone", dictionary35)
            MergeMapsFromRowProps(dictionary38, "AssistantName", dictionary36)
            If dictionary37.Count > 0 Then
                MergePropertyMaps(dictionary9, ExtractPropertyMapViaColumns(morkText, dictionary37, "PrimaryEmail"))
                MergePropertyMaps(dictionary10, ExtractPropertyMapViaColumns(morkText, dictionary37, "SecondEmail"))
                MergePropertyMaps(dictionary2, ExtractPropertyMapViaColumns(morkText, dictionary37, "DisplayName"))
                MergePropertyMaps(dictionary3, ExtractPropertyMapViaColumns(morkText, dictionary37, "FirstName"))
                MergePropertyMaps(dictionary4, ExtractPropertyMapViaColumns(morkText, dictionary37, "LastName"))
                MergePropertyMaps(dictionary5, ExtractPropertyMapViaColumns(morkText, dictionary37, "NickName"))
                MergePropertyMaps(dictionary6, ExtractPropertyMapViaColumns(morkText, dictionary37, "JobTitle"))
                MergePropertyMaps(dictionary7, ExtractPropertyMapViaColumns(morkText, dictionary37, "Department"))
                MergePropertyMaps(dictionary8, ExtractPropertyMapViaColumns(morkText, dictionary37, "WebPage1"))
                MergePropertyMaps(dictionary11, ExtractPropertyMapViaColumns(morkText, dictionary37, "Company"))
                MergePropertyMaps(dictionary12, ExtractPropertyMapViaColumns(morkText, dictionary37, "WorkPhone"))
                MergePropertyMaps(dictionary13, ExtractPropertyMapViaColumns(morkText, dictionary37, "CellularNumber"))
                MergePropertyMaps(dictionary14, ExtractPropertyMapViaColumns(morkText, dictionary37, "HomePhone"))
                MergePropertyMaps(dictionary15, ExtractPropertyMapViaColumns(morkText, dictionary37, "FaxNumber"))
                MergePropertyMaps(dictionary16, ExtractPropertyMapViaColumns(morkText, dictionary37, "PagerNumber"))
                MergePropertyMaps(dictionary17, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom1"))
                MergePropertyMaps(dictionary18, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom2"))
                MergePropertyMaps(dictionary19, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom3"))
                MergePropertyMaps(dictionary20, ExtractPropertyMapViaColumns(morkText, dictionary37, "Custom4"))
                MergePropertyMaps(dictionary21, ExtractPropertyMapViaColumns(morkText, dictionary37, "SpouseName"))
                MergePropertyMaps(dictionary22, ExtractPropertyMapViaColumns(morkText, dictionary37, "Notes"))
                MergePropertyMaps(dictionary23, ExtractPropertyMapViaColumns(morkText, dictionary37, "ScreenName"))
                MergePropertyMaps(dictionary24, ExtractPropertyMapViaColumns(morkText, dictionary37, "AimScreenName"))
                MergePropertyMaps(dictionary25, ExtractPropertyMapViaColumns(morkText, dictionary37, "PopularityIndex"))
                MergePropertyMaps(dictionary26, ExtractPropertyMapViaColumns(morkText, dictionary37, "RecordName"))
                MergePropertyMaps(dictionary27, ExtractPropertyMapViaColumns(morkText, dictionary37, "PreferEmail"))
                MergePropertyMaps(dictionary28, ExtractPropertyMapViaColumns(morkText, dictionary37, "LowercasePrimaryEmail"))
                MergePropertyMaps(dictionary29, ExtractPropertyMapViaColumns(morkText, dictionary37, "WorkAddress"))
                MergePropertyMaps(dictionary30, ExtractPropertyMapViaColumns(morkText, dictionary37, "HomeAddress"))
                MergePropertyMaps(dictionary31, ExtractPropertyMapViaColumns(morkText, dictionary37, "BirthDay"))
                MergePropertyMaps(dictionary31, ExtractPropertyMapViaColumns(morkText, dictionary37, "Birthday"))
                MergePropertyMaps(dictionary32, ExtractPropertyMapViaColumns(morkText, dictionary37, "Anniversary"))
                MergePropertyMaps(dictionary33, ExtractPropertyMapViaColumns(morkText, dictionary37, "MiddleName"))
                MergePropertyMaps(dictionary34, ExtractPropertyMapViaColumns(morkText, dictionary37, "OfficeLocation"))
                MergePropertyMaps(dictionary35, ExtractPropertyMapViaColumns(morkText, dictionary37, "OtherPhone"))
                MergePropertyMaps(dictionary36, ExtractPropertyMapViaColumns(morkText, dictionary37, "AssistantName"))
            End If
            Dim dictionary39 As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            For Each item2 In dictionary9
                Dim text As String = item2.Value.Trim()
                If text.Length <> 0 AndAlso EmailPattern.IsMatch(text) Then
                    Dim displayName = ResolveDisplayName(item2.Key, dictionary2, dictionary3, dictionary4, dictionary5)
                    AddContact(dictionary, displayName, text, dictionary11, dictionary12, dictionary14, dictionary22, item2.Key)
                    Dim text2 As String = text.ToLowerInvariant()
                    dictionary39(item2.Key) = text2
                    If dictionary.ContainsKey(text2) Then
                        ApplyMorkNameFields(dictionary(text2), item2.Key, dictionary3, dictionary4, dictionary5)
                        ApplyMorkJobTitle(dictionary(text2), item2.Key, dictionary6)
                        ApplyMorkDepartment(dictionary(text2), item2.Key, dictionary7)
                        ApplyMorkWebPage(dictionary(text2), item2.Key, dictionary8)
                        ApplyMorkCellularNumber(dictionary(text2), item2.Key, dictionary13)
                        ApplyMorkFaxNumber(dictionary(text2), item2.Key, dictionary15)
                        ApplyMorkPagerNumber(dictionary(text2), item2.Key, dictionary16)
                        ApplyMorkCustom1(dictionary(text2), item2.Key, dictionary17)
                        ApplyMorkCustom2(dictionary(text2), item2.Key, dictionary18)
                        ApplyMorkCustom3(dictionary(text2), item2.Key, dictionary19)
                        ApplyMorkCustom4(dictionary(text2), item2.Key, dictionary20)
                        ApplyMorkSpouseName(dictionary(text2), item2.Key, dictionary21)
                        ApplyMorkScreenName(dictionary(text2), item2.Key, dictionary23)
                        ApplyMorkAimScreenName(dictionary(text2), item2.Key, dictionary24)
                        ApplyMorkPopularityIndex(dictionary(text2), item2.Key, dictionary25)
                        ApplyMorkRecordName(dictionary(text2), item2.Key, dictionary26)
                        ApplyMorkPreferEmail(dictionary(text2), item2.Key, dictionary27)
                        ApplyMorkWorkAddress(dictionary(text2), item2.Key, dictionary29)
                        ApplyMorkHomeAddress(dictionary(text2), item2.Key, dictionary30)
                        ApplyMorkBirthDay(dictionary(text2), item2.Key, dictionary31)
                        ApplyMorkAnniversary(dictionary(text2), item2.Key, dictionary32)
                        ApplyMorkMiddleName(dictionary(text2), item2.Key, dictionary33)
                        ApplyMorkOfficeLocation(dictionary(text2), item2.Key, dictionary34)
                        ApplyMorkOtherPhone(dictionary(text2), item2.Key, dictionary35)
                        ApplyMorkAssistantName(dictionary(text2), item2.Key, dictionary36)
                    End If
                End If
            Next
            For Each item3 In dictionary10
                Dim text3 As String = item3.Value.Trim()
                If text3.Length = 0 OrElse Not EmailPattern.IsMatch(text3) Then
                    Continue For
                End If
                If dictionary39.ContainsKey(item3.Key) Then
                    Dim key = dictionary39(item3.Key)
                    If dictionary.ContainsKey(key) Then
                        dictionary(key).SecondaryEmail = text3.Trim()
                    End If
                Else
                    Dim displayName2 = ResolveDisplayName(item3.Key, dictionary2, dictionary3, dictionary4, dictionary5)
                    AddContact(dictionary, displayName2, text3, dictionary11, dictionary12, dictionary14, dictionary22, item3.Key)
                End If
            Next
            For Each item4 In dictionary28
                If dictionary39.ContainsKey(item4.Key) Then
                    Continue For
                End If
                Dim text4 As String = item4.Value.Trim()
                If text4.Length <> 0 AndAlso EmailPattern.IsMatch(text4) Then
                    Dim displayName3 = ResolveDisplayName(item4.Key, dictionary2, dictionary3, dictionary4, dictionary5)
                    AddContact(dictionary, displayName3, text4, dictionary11, dictionary12, dictionary14, dictionary22, item4.Key)
                    dictionary39(item4.Key) = text4.ToLowerInvariant()
                    Dim key2 As String = text4.ToLowerInvariant()
                    If dictionary.ContainsKey(key2) Then
                        ApplyMorkPreferEmail(dictionary(key2), item4.Key, dictionary27)
                    End If
                End If
            Next
            If dictionary.Count = 0 Then
                For Each item5 As Match In EmailPattern.Matches(morkText)
                    AddContact(dictionary, "", item5.Value, Nothing, Nothing, Nothing, Nothing, "")
                Next
            End If
            LastMorkTableCount = Regex.Matches(morkText, "\(\d+:").Count
            Dim list As List(Of MailContact) = New List(Of MailContact)(dictionary.Values)
            list.Sort(Function(a, b) String.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase))
            LastMorkParsedContactCount = list.Count
            Return list
        End Function

        Private Shared Function ResolveDisplayName(rowId As String, displayNames As Dictionary(Of String, String), firstNames As Dictionary(Of String, String), lastNames As Dictionary(Of String, String), nickNames As Dictionary(Of String, String)) As String
            If displayNames.ContainsKey(rowId) Then
                Dim text As String = displayNames(rowId).Trim()
                If text.Length > 0 Then
                    Return text
                End If
            End If
            Dim text2 = ""
            Dim text3 = ""
            If firstNames.ContainsKey(rowId) Then
                text2 = firstNames(rowId).Trim()
            End If
            If lastNames.ContainsKey(rowId) Then
                text3 = lastNames(rowId).Trim()
            End If
            Dim text4 As String = (text2 & " " & text3).Trim()
            If text4.Length > 0 Then
                Return text4
            End If
            If nickNames.ContainsKey(rowId) Then
                Return nickNames(rowId).Trim()
            End If
            Return ""
        End Function

        Private Shared Sub ApplyMorkNameFields(contact As MailContact, rowId As String, firstNames As Dictionary(Of String, String), lastNames As Dictionary(Of String, String), nickNames As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) Then
                If firstNames IsNot Nothing AndAlso firstNames.ContainsKey(rowId) Then
                    contact.FirstName = firstNames(rowId).Trim()
                End If
                If lastNames IsNot Nothing AndAlso lastNames.ContainsKey(rowId) Then
                    contact.LastName = lastNames(rowId).Trim()
                End If
                If nickNames IsNot Nothing AndAlso nickNames.ContainsKey(rowId) Then
                    contact.NickName = nickNames(rowId).Trim()
                End If
            End If
        End Sub

        Private Shared Sub ApplyMorkJobTitle(contact As MailContact, rowId As String, jobTitles As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso jobTitles IsNot Nothing AndAlso jobTitles.ContainsKey(rowId) Then
                contact.JobTitle = jobTitles(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkDepartment(contact As MailContact, rowId As String, departments As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso departments IsNot Nothing AndAlso departments.ContainsKey(rowId) Then
                contact.Department = departments(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkWebPage(contact As MailContact, rowId As String, webPages As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso webPages IsNot Nothing AndAlso webPages.ContainsKey(rowId) Then
                contact.WebPage = webPages(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkCellularNumber(contact As MailContact, rowId As String, cellularNumbers As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso cellularNumbers IsNot Nothing AndAlso cellularNumbers.ContainsKey(rowId) Then
                Dim text As String = cellularNumbers(rowId).Trim()
                If text.Length > 0 Then
                    contact.MobilePhone = text
                End If
            End If
        End Sub

        Private Shared Sub ApplyMorkFaxNumber(contact As MailContact, rowId As String, faxNumbers As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso faxNumbers IsNot Nothing AndAlso faxNumbers.ContainsKey(rowId) Then
                contact.FaxNumber = faxNumbers(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkPagerNumber(contact As MailContact, rowId As String, pagerNumbers As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso pagerNumbers IsNot Nothing AndAlso pagerNumbers.ContainsKey(rowId) Then
                contact.PagerNumber = pagerNumbers(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkCustom1(contact As MailContact, rowId As String, custom1Values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso custom1Values IsNot Nothing AndAlso custom1Values.ContainsKey(rowId) Then
                contact.Custom1 = custom1Values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkCustom2(contact As MailContact, rowId As String, custom2Values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso custom2Values IsNot Nothing AndAlso custom2Values.ContainsKey(rowId) Then
                contact.Custom2 = custom2Values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkCustom3(contact As MailContact, rowId As String, custom3Values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso custom3Values IsNot Nothing AndAlso custom3Values.ContainsKey(rowId) Then
                contact.Custom3 = custom3Values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkCustom4(contact As MailContact, rowId As String, custom4Values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso custom4Values IsNot Nothing AndAlso custom4Values.ContainsKey(rowId) Then
                contact.Custom4 = custom4Values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkSpouseName(contact As MailContact, rowId As String, spouseNames As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso spouseNames IsNot Nothing AndAlso spouseNames.ContainsKey(rowId) Then
                contact.SpouseName = spouseNames(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkScreenName(contact As MailContact, rowId As String, screenNames As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso screenNames IsNot Nothing AndAlso screenNames.ContainsKey(rowId) Then
                contact.ScreenName = screenNames(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkAimScreenName(contact As MailContact, rowId As String, aimNames As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso aimNames IsNot Nothing AndAlso aimNames.ContainsKey(rowId) Then
                contact.AimScreenName = aimNames(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkPopularityIndex(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.PopularityIndex = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkRecordName(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.RecordName = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkPreferEmail(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.PreferEmail = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkWorkAddress(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.WorkAddress = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkHomeAddress(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.HomeAddress = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkBirthDay(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.Birthday = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkAnniversary(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.Anniversary = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkMiddleName(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.MiddleName = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkOfficeLocation(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.OfficeLocation = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkOtherPhone(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.OtherPhone = values(rowId).Trim()
            End If
        End Sub

        Private Shared Sub ApplyMorkAssistantName(contact As MailContact, rowId As String, values As Dictionary(Of String, String))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(rowId) AndAlso values IsNot Nothing AndAlso values.ContainsKey(rowId) Then
                contact.AssistantName = values(rowId).Trim()
            End If
        End Sub

        Private Shared Function PickPrimaryEmailTableMap(tableMaps As List(Of Dictionary(Of String, String))) As Dictionary(Of String, String)
            If tableMaps Is Nothing OrElse tableMaps.Count = 0 Then
                Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            End If
            Dim result = tableMaps(0)
            Dim num = 0
            For Each tableMap In tableMaps
                If tableMap IsNot Nothing AndAlso FindColumnId(tableMap, "PrimaryEmail").Length <> 0 AndAlso tableMap.Count > num Then
                    num = tableMap.Count
                    result = tableMap
                End If
            Next
            Return result
        End Function

        Private Shared Sub AddContact(byEmail As Dictionary(Of String, MailContact), displayName As String, email As String, companies As Dictionary(Of String, String), workPhones As Dictionary(Of String, String), homePhones As Dictionary(Of String, String), notesMap As Dictionary(Of String, String), rowId As String)
            Dim key As String = email.ToLowerInvariant()
            If Not byEmail.ContainsKey(key) Then
                Dim company = ""
                Dim workPhone = ""
                Dim homePhone = ""
                Dim notes = ""
                If companies IsNot Nothing AndAlso companies.ContainsKey(rowId) Then
                    company = companies(rowId).Trim()
                End If
                If workPhones IsNot Nothing AndAlso workPhones.ContainsKey(rowId) Then
                    workPhone = workPhones(rowId).Trim()
                End If
                If homePhones IsNot Nothing AndAlso homePhones.ContainsKey(rowId) Then
                    homePhone = homePhones(rowId).Trim()
                End If
                If notesMap IsNot Nothing AndAlso notesMap.ContainsKey(rowId) Then
                    notes = notesMap(rowId).Trim()
                End If
                byEmail(key) = New MailContact With {
        .DisplayName = If(Not Equals(displayName, Nothing), displayName, "").Trim(),
        .Email = email.Trim(),
        .Company = company,
        .WorkPhone = workPhone,
        .HomePhone = homePhone,
        .Notes = notes,
        .MorkRowId = If(Not Equals(rowId, Nothing), rowId, "").Trim()
    }
            End If
        End Sub

        Private Shared Sub MergeMapsFromRowProps(rowProps As Dictionary(Of String, Dictionary(Of String, String)), propertyName As String, target As Dictionary(Of String, String))
            If rowProps Is Nothing OrElse target Is Nothing Then
                Return
            End If
            For Each rowProp In rowProps
                If rowProp.Value IsNot Nothing Then
                    Dim value As String = Nothing
                    If rowProp.Value.TryGetValue(propertyName, value) AndAlso Not String.IsNullOrWhiteSpace(value) AndAlso Not target.ContainsKey(rowProp.Key) Then
                        target(rowProp.Key) = value.Trim()
                    End If
                End If
            Next
        End Sub

        Friend Shared Function BuildRowPropertyMaps(text As String, colIdToProp As Dictionary(Of String, String)) As Dictionary(Of String, Dictionary(Of String, String))
            Dim dictionary As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))(StringComparer.Ordinal)
            If String.IsNullOrEmpty(text) Then
                Return dictionary
            End If
            Dim dictionary2 = colIdToProp
            If dictionary2 Is Nothing OrElse dictionary2.Count = 0 Then
                dictionary2 = ExtractColumnKeyMap(text)
            End If
            Dim dictionary3 = ParseRawRowBodies(text)
            Dim dictionary4 As Dictionary(Of String, Dictionary(Of String, String)) = New Dictionary(Of String, Dictionary(Of String, String))(StringComparer.Ordinal)
            For Each item In dictionary3
                dictionary4(item.Key) = ParseRowCells(item.Value, dictionary2)
            Next
            For Each key In dictionary4.Keys
                Dim dictionary5 As Dictionary(Of String, String) = ResolveRowProperties(key, dictionary2, dictionary4, New HashSet(Of String)(StringComparer.Ordinal))
                If dictionary5 IsNot Nothing AndAlso dictionary5.Count > 0 Then
                    dictionary(key) = dictionary5
                End If
            Next
            Return dictionary
        End Function

        Private Shared Function ParseRawRowBodies(text As String) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            Dim regex As Regex = New Regex("(?:^|[\s\r\n])@([^=\s<>]+)=", RegexOptions.Compiled)
            Dim matchCollection = regex.Matches(text)
            Dim num = matchCollection.Count - 1
            For i = 0 To num
                Dim match = matchCollection(i)
                Dim text2 As String = match.Groups(1).Value.Trim()
                If text2.Length <> 0 Then
                    Dim num2 = match.Index + match.Length
                    Dim num3 = If(i < matchCollection.Count - 1, matchCollection(i + 1).Index, text.Length)
                    Dim value = text.Substring(num2, Math.Max(0, num3 - num2))
                    If Not dictionary.ContainsKey(text2) Then
                        dictionary(text2) = value
                    End If
                End If
            Next
            Return dictionary
        End Function

        Private Shared Function ParseRowCells(body As String, colIdToProp As Dictionary(Of String, String)) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            If String.IsNullOrEmpty(body) Then
                Return dictionary
            End If
            If colIdToProp IsNot Nothing Then
                For Each item In colIdToProp
                    Dim key = item.Key
                    Dim regex As Regex = New Regex("\^" & Regex.Escape(key) & "\^([^\^<()]+)", RegexOptions.Compiled)
                    Dim match = regex.Match(body)
                    If match.Success Then
                        dictionary(key) = match.Groups(1).Value.Trim()
                    End If
                Next
            End If
            Dim regex2 As Regex = New Regex("\(([^=\)]+)=([^\)]+)\)", RegexOptions.Compiled)
            For Each item2 As Match In regex2.Matches(body)
                Dim text As String = item2.Groups(1).Value.Trim()
                Dim value As String = item2.Groups(2).Value.Trim()
                If text.Length = 0 Then
                    Continue For
                End If
                If text.StartsWith("d=", StringComparison.OrdinalIgnoreCase) Then
                    Dim text2 As String = text.Substring(2).Trim()
                    If text2.StartsWith("c", StringComparison.OrdinalIgnoreCase) AndAlso text2.Length > 1 Then
                        text = text2.Substring(1)
                    Else
                        If Not text2.StartsWith("%", StringComparison.OrdinalIgnoreCase) Then
                            dictionary("d:" & text2) = value
                            Continue For
                        End If
                        Dim num = text2.IndexOf(":"c)
                        If num > 1 Then
                            text = text2.Substring(1, num - 1)
                        End If
                    End If
                End If
                If Not dictionary.ContainsKey(text) Then
                    dictionary(text) = value
                End If
            Next
            Dim regex3 As Regex = New Regex("\(d=([^>\)]+)>([^<(\s]+)", RegexOptions.Compiled)
            For Each item3 As Match In regex3.Matches(body)
                Dim text3 As String = item3.Groups(1).Value.Trim()
                Dim text4 As String = item3.Groups(2).Value.Trim()
                If text3.Length <> 0 AndAlso text4.Length <> 0 Then
                    dictionary("d:" & text3) = text4
                End If
            Next
            Return dictionary
        End Function

        Private Shared Function ResolveRowProperties(rowId As String, colIdToProp As Dictionary(Of String, String), rawCells As Dictionary(Of String, Dictionary(Of String, String)), visiting As HashSet(Of String)) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            If String.IsNullOrEmpty(rowId) OrElse rawCells Is Nothing Then
                Return dictionary
            End If
            If Not rawCells.ContainsKey(rowId) Then
                Return dictionary
            End If
            If visiting.Contains(rowId) Then
                Return dictionary
            End If
            visiting.Add(rowId)
            Dim dictionary2 = rawCells(rowId)
            For Each item In dictionary2
                Dim key = item.Key
                Dim text = ResolveCellValue(rowId, key, item.Value, colIdToProp, rawCells, visiting, 0)
                If text.Length <> 0 Then
                    If key.StartsWith("d:", StringComparison.Ordinal) Then
                        dictionary(key.Substring(2)) = text
                    ElseIf colIdToProp IsNot Nothing AndAlso colIdToProp.ContainsKey(key) Then
                        dictionary(colIdToProp(key)) = text
                    End If
                End If
            Next
            visiting.Remove(rowId)
            Return dictionary
        End Function

        Private Shared Function ResolveCellValue(rowId As String, colKey As String, raw As String, colIdToProp As Dictionary(Of String, String), rawCells As Dictionary(Of String, Dictionary(Of String, String)), visiting As HashSet(Of String), depth As Integer) As String
            If depth > 24 Then
                Return ""
            End If
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            text = UnescapeMorkValue(text)
            If IsRowReference(text) AndAlso rawCells.ContainsKey(text) Then
                If rawCells(text).ContainsKey(colKey) Then
                    Return ResolveCellValue(text, colKey, rawCells(text)(colKey), colIdToProp, rawCells, visiting, depth + 1)
                End If
                Dim dictionary = ResolveRowProperties(text, colIdToProp, rawCells, visiting)
                If colIdToProp IsNot Nothing AndAlso colIdToProp.ContainsKey(colKey) Then
                    Dim key = colIdToProp(colKey)
                    If dictionary.ContainsKey(key) Then
                        Return dictionary(key)
                    End If
                End If
                If dictionary.ContainsKey("PrimaryEmail") Then
                    Return dictionary("PrimaryEmail")
                End If
            End If
            Return text
        End Function

        Private Shared Function IsRowReference(value As String) As Boolean
            Dim text As String = If(Not Equals(value, Nothing), value, "").Trim()
            If text.Length = 0 OrElse text.Length > 16 Then
                Return False
            End If
            If text.IndexOf("@"c) >= 0 OrElse text.IndexOf("."c) >= 0 Then
                Return False
            End If
            Return Regex.IsMatch(text, "^-?[0-9A-Fa-f]+$")
        End Function

        Private Shared Function ExtractColumnKeyMap(text As String) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            If String.IsNullOrEmpty(text) Then
                Return dictionary
            End If
            Dim regex As Regex = New Regex("\(k=([^\)]*)\)", RegexOptions.Compiled)
            For Each item As Match In regex.Matches(text)
                Dim value = item.Groups(1).Value
                Dim regex2 As Regex = New Regex("%([0-9A-Fa-f]+):([A-Za-z0-9_]+)", RegexOptions.Compiled)
                For Each item2 As Match In regex2.Matches(value)
                    Dim text2 As String = item2.Groups(1).Value.Trim()
                    Dim text3 As String = item2.Groups(2).Value.Trim()
                    If text2.Length > 0 AndAlso text3.Length > 0 AndAlso Not dictionary.ContainsKey(text2) Then
                        dictionary(text2) = text3
                    End If
                Next
                Dim array = value.Split("^"c)
                For i = 0 To array.Length - 1 - 1 Step 2
                    Dim text4 As String = array(i).Trim()
                    Dim text5 As String = array(i + 1).Trim()
                    If text4.StartsWith("%") Then
                        Dim num = text4.IndexOf(":"c)
                        If num > 1 Then
                            text5 = text4.Substring(num + 1).Trim()
                            text4 = text4.Substring(1, num - 1).Trim()
                        End If
                    End If
                    If text4.Length > 0 AndAlso text5.Length > 0 AndAlso Regex.IsMatch(text4, "^[0-9A-Fa-f]+$") AndAlso Not dictionary.ContainsKey(text4) Then
                        dictionary(text4) = text5
                    End If
                Next
            Next
            Return dictionary
        End Function

        Private Shared Function ParseColumnKeyChunk(chunk As String) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            If String.IsNullOrEmpty(chunk) Then
                Return dictionary
            End If
            Dim regex As Regex = New Regex("%([0-9A-Fa-f]+):([A-Za-z0-9_]+)", RegexOptions.Compiled)
            For Each item As Match In regex.Matches(chunk)
                Dim text As String = item.Groups(1).Value.Trim()
                Dim text2 As String = item.Groups(2).Value.Trim()
                If text.Length > 0 AndAlso text2.Length > 0 AndAlso Not dictionary.ContainsKey(text) Then
                    dictionary(text) = text2
                End If
            Next
            Dim array = chunk.Split("^"c)
            For i = 0 To array.Length - 1 - 1 Step 2
                Dim text3 As String = array(i).Trim()
                Dim text4 As String = array(i + 1).Trim()
                If text3.StartsWith("%") Then
                    Dim num = text3.IndexOf(":"c)
                    If num > 1 Then
                        text4 = text3.Substring(num + 1).Trim()
                        text3 = text3.Substring(1, num - 1).Trim()
                    End If
                End If
                If text3.Length > 0 AndAlso text4.Length > 0 AndAlso Regex.IsMatch(text3, "^[0-9A-Fa-f]+$") AndAlso Not dictionary.ContainsKey(text3) Then
                    dictionary(text3) = text4
                End If
            Next
            Return dictionary
        End Function

        Private Shared Function ExtractPerTableColumnMaps(text As String) As List(Of Dictionary(Of String, String))
            Dim list As List(Of Dictionary(Of String, String)) = New List(Of Dictionary(Of String, String))()
            If String.IsNullOrEmpty(text) Then
                Return list
            End If
            Dim regex As Regex = New Regex("\(k=([^\)]*)\)", RegexOptions.Compiled)
            For Each item As Match In regex.Matches(text)
                Dim dictionary = ParseColumnKeyChunk(item.Groups(1).Value)
                If dictionary.Count > 0 Then
                    list.Add(dictionary)
                End If
            Next
            Return list
        End Function

        Private Shared Function ResolveColumnMapForRowBody(rowBody As String, tableMaps As List(Of Dictionary(Of String, String))) As Dictionary(Of String, String)
            If tableMaps Is Nothing OrElse tableMaps.Count = 0 Then
                Return New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            End If
            Dim text = If(Not Equals(rowBody, Nothing), rowBody, "")
            Dim result = tableMaps(0)
            Dim num = -1
            For Each tableMap In tableMaps
                If tableMap Is Nothing Then
                    Continue For
                End If
                Dim num2 = 0
                For Each key In tableMap.Keys
                    If text.IndexOf("^" & key & "^", StringComparison.Ordinal) >= 0 Then
                        num2 = num2 + 1
                    End If
                Next
                If num2 > num Then
                    num = num2
                    result = tableMap
                End If
            Next
            Return result
        End Function

        Private Shared Sub MergePropertyMaps(target As Dictionary(Of String, String), source As Dictionary(Of String, String))
            If target Is Nothing OrElse source Is Nothing Then
                Return
            End If
            For Each item In source
                If Not target.ContainsKey(item.Key) Then
                    target(item.Key) = item.Value
                End If
            Next
        End Sub

        Private Shared Function ExtractPropertyMapViaColumns(text As String, colIdToProp As Dictionary(Of String, String), propertyName As String) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            If String.IsNullOrEmpty(text) OrElse colIdToProp Is Nothing OrElse colIdToProp.Count = 0 Then
                Return dictionary
            End If
            Dim text2 As String = Nothing
            For Each item In colIdToProp
                If String.Equals(item.Value, propertyName, StringComparison.OrdinalIgnoreCase) Then
                    text2 = item.Key
                    Exit For
                End If
            Next
            If String.IsNullOrEmpty(text2) Then
                Return dictionary
            End If
            Dim pattern As String = "\^" & Regex.Escape(text2) & "\^([^\^<()]+)"
            Dim regex As Regex = New Regex(pattern, RegexOptions.Compiled)
            Dim regex2 As Regex = New Regex("@([^=]+)=([^@]*)", RegexOptions.Compiled)
            For Each item2 As Match In regex2.Matches(text)
                Dim text3 As String = item2.Groups(1).Value.Trim()
                If text3.Length = 0 Then
                    Continue For
                End If
                Dim match2 = regex.Match(item2.Groups(2).Value)
                If match2.Success Then
                    Dim text4 As String = UnescapeMorkValue(match2.Groups(1).Value.Trim())
                    If text4.Length > 0 AndAlso Not dictionary.ContainsKey(text3) Then
                        dictionary(text3) = text4
                    End If
                End If
            Next
            Return dictionary
        End Function

        Private Shared Function ExtractPropertyMap(text As String, propertyName As String) As Dictionary(Of String, String)
            Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
            If String.IsNullOrEmpty(text) OrElse String.IsNullOrEmpty(propertyName) Then
                Return dictionary
            End If
            Dim text2 = "(d=" & propertyName & ")"
            Dim num = 0
            While num < text.Length
                Dim num2 = text.IndexOf(text2, num, StringComparison.Ordinal)
                If num2 < 0 Then
                    Exit While
                End If
                Dim num3 = text.LastIndexOf("@"c, num2)
                If num3 < 0 Then
                    num = num2 + text2.Length
                    Continue While
                End If
                Dim num4 = text.IndexOf(">"c, num2)
                If num4 < 0 Then
                    num = num2 + text2.Length
                    Continue While
                End If
                Dim text3 = ExtractRowId(text, num3, num2)
                Dim text4 = ExtractCellValue(text, num4 + 1)
                If text3.Length > 0 AndAlso text4.Length > 0 AndAlso Not dictionary.ContainsKey(text3) Then
                    dictionary(text3) = text4
                End If
                num = num4 + 1
            End While
            Return dictionary
        End Function

        Private Shared Function ExtractRowId(text As String, rowStart As Integer, markerPos As Integer) As String
            Dim text2 = text.Substring(rowStart, Math.Max(0, markerPos - rowStart))
            If text2.StartsWith("@", StringComparison.Ordinal) Then
                Dim num = text2.IndexOf("="c)
                If num > 1 Then
                    Return text2.Substring(1, num - 1).Trim()
                End If
            End If
            Dim match = Regex.Match(text2, "\(([^=\)]+)=", RegexOptions.RightToLeft)
            If match.Success Then
                Return match.Groups(1).Value.Trim()
            End If
            Return ""
        End Function

        Private Shared Function ExtractCellValue(text As String, start As Integer) As String
            If start >= text.Length Then
                Return ""
            End If
            Dim num = text.IndexOfAny(New Char(4) {"<"c, "("c, "@"c, ChrW(10), ChrW(13)}, start)
            If num < 0 Then
                num = Math.Min(text.Length, start + 200)
            End If
            Dim text2 As String = text.Substring(start, num - start).Trim()
            text2 = text2.Trim(")"c, "("c, "^"c, " "c, ChrW(9))
            If text2.StartsWith("=") Then
                text2 = text2.Substring(1)
            End If
            Return UnescapeMorkValue(text2)
        End Function

        Private Shared Function UnescapeMorkValue(raw As String) As String
            If Equals(raw, Nothing) Then
                Return ""
            End If
            Return raw.Replace("$20", " ").Replace("$40", "@").Replace("$2C", ",").Replace("$28", "(").Replace("$29", ")")
        End Function

        Public Shared Function TryParseContactsFromLdif(ldifPath As String) As List(Of MailContact)
            LastError = Nothing
            Dim list As List(Of MailContact) = New List(Of MailContact)()
            Dim result As List(Of MailContact)
            If String.IsNullOrWhiteSpace(ldifPath) OrElse Not File.Exists(ldifPath) Then
                LastError = "LDIF file not found."
                result = list
            Else
                Try
                    Dim text As String = Nothing
                    Dim text2 As String = Nothing
                    Dim array = File.ReadAllLines(ldifPath, Encoding.UTF8)
                    For Each text3 In array
                        Dim text4 As String = If(Not Equals(text3, Nothing), text3, "").Trim()
                        If text4.Length = 0 Then
                            If Not Equals(text, Nothing) AndAlso text.Length > 0 Then
                                list.Add(New MailContact With {
        .Email = text,
        .DisplayName = If(Not Equals(text2, Nothing), text2, text).Trim()
    })
                            End If
                            text = Nothing
                            text2 = Nothing
                        ElseIf text4.StartsWith("mail:", StringComparison.OrdinalIgnoreCase) Then
                            text = text4.Substring(5).Trim()
                        ElseIf text4.StartsWith("cn:", StringComparison.OrdinalIgnoreCase) Then
                            text2 = text4.Substring(3).Trim()
                        End If
                    Next
                    If Not Equals(text, Nothing) AndAlso text.Length > 0 Then
                        list.Add(New MailContact With {
        .Email = text,
        .DisplayName = If(Not Equals(text2, Nothing), text2, text).Trim()
    })
                    End If
                    result = list
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = New List(Of MailContact)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function ExportContactsToLdif(contacts As List(Of MailContact), destPath As String) As Boolean
            LastError = Nothing
            Dim result As Boolean
            If contacts Is Nothing OrElse String.IsNullOrWhiteSpace(destPath) Then
                LastError = "Contacts and path required."
                result = False
            Else
                Try
                    Dim stringBuilder As StringBuilder = New StringBuilder()
                    Dim num = 1
                    For Each contact In contacts
                        If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                            stringBuilder.AppendLine("dn: cn=" & num.ToString() & ",ou=contacts")
                            stringBuilder.AppendLine("objectClass: inetOrgPerson")
                            Dim mailContact = contact
                            stringBuilder.AppendLine("cn: " & If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, contact.Email))
                            stringBuilder.AppendLine("mail: " & contact.Email)
                            stringBuilder.AppendLine("")
                            num = num + 1
                        End If
                    Next
                    Call File.WriteAllText(destPath, stringBuilder.ToString(), Encoding.UTF8)
                    result = True
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryWriteMorkPreservingSource(sourceMabPath As String, contacts As List(Of MailContact), destPath As String, ByRef byteIdentical As Boolean) As Boolean
            LastError = Nothing
            byteIdentical = False
            LastMorkPreserveExportFieldValidated = False
            Dim result As Boolean
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                LastError = "Source abook.mab not found."
                result = False
            ElseIf contacts Is Nothing OrElse String.IsNullOrWhiteSpace(destPath) Then
                LastError = "Contacts and path required."
                result = False
            ElseIf TryContactsUnchangedVersusSource(sourceMabPath, contacts) Then
                Try
                    File.Copy(sourceMabPath, destPath, overwrite:=True)
                    byteIdentical = True
                    LastMabExportByteIdentical = True
                    LastMorkPreserveExportFieldValidated = True
                    LastMabExportVerifiedAgainstSource = TryVerifyExportBytesMatchSource(sourceMabPath, destPath)
                    result = True
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            Else
                Try
                    Dim array = File.ReadAllBytes(sourceMabPath)
                    Dim encoding = DetectMorkFileEncoding(array)
                    Dim text = encoding.GetString(array)
                    Dim lineBreak = If(text.IndexOf(vbCrLf, StringComparison.Ordinal) >= 0, vbCrLf, vbLf)
                    Dim morkText = text
                    Dim list = ExtractPerTableColumnMaps(text)
                    Dim dictionary = ExtractColumnKeyMap(text)
                    Dim dictionary2 = ParseRawRowBodies(text)
                    Dim num = FindMaxMorkRowId(text) + 1
                    For Each contact In contacts
                        If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                            Continue For
                        End If
                        Dim text2 As String = contact.Email.Trim()
                        Dim emailEsc = EscapeMorkValue(text2)
                        Dim mailContact = contact
                        Dim nameEsc As String = EscapeMorkValue(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, text2).Trim())
                        Dim text3 = FindMorkRowIdForEmail(text, text2)
                        If text3.Length = 0 AndAlso Not String.IsNullOrWhiteSpace(contact.MorkRowId) Then
                            text3 = contact.MorkRowId.Trim()
                        End If
                        Dim colMap = dictionary
                        If text3.Length > 0 AndAlso dictionary2.ContainsKey(text3) AndAlso list.Count > 0 Then
                            colMap = ResolveColumnMapForRowBody(dictionary2(text3), list)
                        ElseIf list.Count > 0 Then
                            colMap = PickPrimaryEmailTableMap(list)
                        End If
                        Dim useColumnCells = PreferColumnCellRows(text, colMap)
                        If text3.Length > 0 Then
                            Dim text4 = TryReplaceMorkRowLine(morkText, text3, colMap, useColumnCells, contact, emailEsc, nameEsc, lineBreak)
                            If text4.Length > 0 Then
                                morkText = text4
                                Continue For
                            End If
                        End If
                        If FindMorkRowIdForEmail(morkText, text2).Length <= 0 Then
                            Dim text5 As String = num.ToString()
                            num += 1
                            Dim rowLine = BuildMorkAppendRow(text5, colMap, useColumnCells, contact, emailEsc, nameEsc)
                            contact.MorkRowId = text5
                            morkText = TryInsertMorkRowBeforeTableEnd(morkText, rowLine, lineBreak, colMap)
                        End If
                    Next
                    Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                    For Each contact2 In contacts
                        If contact2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact2.Email) Then
                            hashSet.Add(contact2.Email.Trim())
                        End If
                    Next
                    morkText = TryRemoveOrphanMorkRows(morkText, hashSet, lineBreak)
                    morkText = TryRemoveOrphanMorkRowsFromRawBodies(morkText, dictionary2, hashSet, lineBreak)
                    Dim bytes = encoding.GetBytes(morkText)
                    File.WriteAllBytes(destPath, bytes)
                    Dim list2 = TryParseContacts(destPath)
                    If list2 Is Nothing OrElse list2.Count = 0 Then
                        LastError = "Preserving export failed parse."
                        result = False
                    Else
                        byteIdentical = If(array.Length = bytes.Length AndAlso StructuralCompareBytes(array, bytes), True, False)
                        LastMabExportByteIdentical = byteIdentical
                        If byteIdentical Then
                            LastMabExportVerifiedAgainstSource = TryVerifyExportBytesMatchSource(sourceMabPath, destPath)
                        End If
                        If Not TryValidateMorkRoundTrip(contacts, destPath) Then
                            result = False
                        Else
                            LastMorkPreserveExportFieldValidated = True
                            result = True
                        End If
                    End If
                Catch ex3 As Exception
                    ProjectData.SetProjectError(ex3)
                    Dim ex4 = ex3
                    LastError = ex4.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryWriteMorkFromContacts(contacts As List(Of MailContact), destPath As String) As Boolean
            LastError = Nothing
            Dim result As Boolean
            If contacts Is Nothing OrElse String.IsNullOrWhiteSpace(destPath) Then
                LastError = "Contacts and path required."
                result = False
            Else
                Try
                    Dim stringBuilder As StringBuilder = New StringBuilder()
                    stringBuilder.AppendLine("// <! <(meta 1)> // (f=iso-8859-1)")
                    stringBuilder.AppendLine("(k=^80^PrimaryEmail^81^DisplayName^82^Company^83^WorkPhone^84^HomePhone^85^Notes^86^FirstName^87^LastName)")
                    Dim num = 1
                    For Each contact In contacts
                        If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                            Continue For
                        End If
                        Dim text As String = num.ToString()
                        Dim mailContact = contact
                        Dim nameEsc = EscapeMorkValue(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, contact.Email))
                        Dim emailEsc As String = EscapeMorkValue(contact.Email.Trim())
                        Dim dictionary As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                        dictionary.Add("80", "PrimaryEmail")
                        dictionary.Add("81", "DisplayName")
                        dictionary.Add("82", "Company")
                        dictionary.Add("83", "WorkPhone")
                        dictionary.Add("84", "HomePhone")
                        dictionary.Add("85", "Notes")
                        Dim colMap = dictionary
                        Dim flag = num Mod 2 = 0
                        Dim text2 = BuildMorkAppendRow(text, colMap, flag, contact, emailEsc, nameEsc)
                        If Not flag Then
                            If Not String.IsNullOrWhiteSpace(contact.WorkPhone) Then
                                text2 = text2 & "<" & text & ">(d=WorkPhone)>" & EscapeMorkValue(contact.WorkPhone)
                            End If
                            If Not String.IsNullOrWhiteSpace(contact.HomePhone) Then
                                text2 = text2 & "<" & text & ">(d=HomePhone)>" & EscapeMorkValue(contact.HomePhone)
                            End If
                            If Not String.IsNullOrWhiteSpace(contact.Notes) Then
                                text2 = text2 & "<" & text & ">(d=Notes)>" & EscapeMorkValue(contact.Notes)
                            End If
                        End If
                        stringBuilder.AppendLine(text2)
                        num = num + 1
                    Next
                    Dim encoding = Encoding.GetEncoding("iso-8859-1")
                    Call File.WriteAllBytes(destPath, encoding.GetBytes(stringBuilder.ToString()))
                    result = If(TryValidateMorkRoundTrip(contacts, destPath), True, False)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Friend Shared Function DetectMorkFileEncoding(fileBytes As Byte()) As Encoding
            If fileBytes Is Nothing OrElse fileBytes.Length = 0 Then
                Return Encoding.UTF8
            End If
            Dim text = Encoding.ASCII.GetString(fileBytes, 0, Math.Min(fileBytes.Length, 4096))
            If text.IndexOf("iso-8859-1", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return Encoding.GetEncoding("iso-8859-1")
            End If
            Return New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)
        End Function

        Public Shared Function TryWriteMorkFromSourceLayout(sourceMabPath As String, contacts As List(Of MailContact), destPath As String) As Boolean
            LastError = Nothing
            Dim result As Boolean
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                LastError = "Source abook.mab not found."
                result = False
            ElseIf contacts Is Nothing OrElse String.IsNullOrWhiteSpace(destPath) Then
                LastError = "Contacts and path required."
                result = False
            Else
                Try
                    Dim array = File.ReadAllBytes(sourceMabPath)
                    Dim encoding = DetectMorkFileEncoding(array)
                    Dim text = encoding.GetString(array)
                    Dim text2 = If(text.IndexOf(vbCrLf, StringComparison.Ordinal) >= 0, vbCrLf, vbLf)
                    Dim list = ExtractPerTableColumnMaps(text)
                    Dim dictionary = ExtractColumnKeyMap(text)
                    Dim dictionary2 = ParseRawRowBodies(text)
                    Dim stringBuilder As StringBuilder = New StringBuilder()
                    Dim array2 = text.Split(New String(0) {text2}, StringSplitOptions.None)
                    For Each text3 In array2
                        If text3.StartsWith("@", StringComparison.Ordinal) Then
                            Exit For
                        End If
                        stringBuilder.Append(text3)
                        stringBuilder.Append(text2)
                    Next
                    If list.Count = 0 AndAlso dictionary.Count = 0 Then
                        stringBuilder.Append("(k=^80^PrimaryEmail^81^DisplayName^82^Company^83^WorkPhone^84^HomePhone^85^Notes^86^FirstName^87^LastName)")
                        stringBuilder.Append(text2)
                    End If
                    Dim num = 1
                    For Each contact In contacts
                        If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                            Dim mailContact = contact
                            Dim text4 As String = If(Not Equals(mailContact.MorkRowId, Nothing), mailContact.MorkRowId, "").Trim()
                            If text4.Length = 0 Then
                                text4 = num.ToString()
                                num += 1
                            End If
                            Dim colMap = dictionary
                            If dictionary2.ContainsKey(text4) AndAlso list.Count > 0 Then
                                colMap = ResolveColumnMapForRowBody(dictionary2(text4), list)
                            ElseIf list.Count > 0 Then
                                colMap = list(Math.Min(num - 1, list.Count - 1))
                            End If
                            Dim useColumnCells = PreferColumnCellRows(text, colMap)
                            Dim emailEsc As String = EscapeMorkValue(contact.Email.Trim())
                            mailContact = contact
                            Dim nameEsc As String = EscapeMorkValue(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, contact.Email).Trim())
                            stringBuilder.Append(BuildMorkAppendRow(text4, colMap, useColumnCells, contact, emailEsc, nameEsc))
                            stringBuilder.Append(text2)
                        End If
                    Next
                    Call File.WriteAllBytes(destPath, encoding.GetBytes(stringBuilder.ToString()))
                    result = TryValidateMorkRoundTrip(contacts, destPath)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryWriteMorkFullEncoderFromSource(sourceMabPath As String, contacts As List(Of MailContact), destPath As String) As Boolean
            LastError = Nothing
            Dim result As Boolean
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                LastError = "Source abook.mab not found."
                result = False
            ElseIf contacts Is Nothing OrElse String.IsNullOrWhiteSpace(destPath) Then
                LastError = "Contacts and path required."
                result = False
            Else
                Try
                    Dim array = File.ReadAllBytes(sourceMabPath)
                    Dim encoding = DetectMorkFileEncoding(array)
                    Dim text = encoding.GetString(array)
                    Dim text2 = If(text.IndexOf(vbCrLf, StringComparison.Ordinal) >= 0, vbCrLf, vbLf)
                    Dim list = ExtractPerTableColumnMaps(text)
                    Dim dictionary = ExtractColumnKeyMap(text)
                    Dim dictionary2 = ParseRawRowBodies(text)
                    Dim dictionary3 As Dictionary(Of String, MailContact) = New Dictionary(Of String, MailContact)(StringComparer.OrdinalIgnoreCase)
                    For Each contact In contacts
                        If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                            dictionary3(contact.Email.Trim()) = contact
                        End If
                    Next
                    Dim list2 As List(Of String) = New List(Of String)()
                    Dim array2 = text.Split(New String(0) {text2}, StringSplitOptions.None)
                    For Each text3 In array2
                        Dim text4 = text3
                        If text4.StartsWith("@", StringComparison.Ordinal) Then
                            Dim num = text4.IndexOf("="c)
                            If num > 1 Then
                                Dim text5 As String = text4.Substring(1, num - 1).Trim()
                                Dim text6 = text4.Substring(num + 1)
                                Dim mailContact As MailContact = Nothing
                                For Each item In dictionary3
                                    If text6.IndexOf(item.Key, StringComparison.OrdinalIgnoreCase) >= 0 Then
                                        mailContact = item.Value
                                        Exit For
                                    End If
                                Next
                                If mailContact Is Nothing AndAlso dictionary2.ContainsKey(text5) Then
                                    For Each item2 In dictionary3
                                        If Not String.IsNullOrWhiteSpace(item2.Value.MorkRowId) AndAlso String.Equals(item2.Value.MorkRowId, text5, StringComparison.OrdinalIgnoreCase) Then
                                            mailContact = item2.Value
                                            Exit For
                                        End If
                                    Next
                                End If
                                If mailContact IsNot Nothing Then
                                    Dim colMap = dictionary
                                    If dictionary2.ContainsKey(text5) AndAlso list.Count > 0 Then
                                        colMap = ResolveColumnMapForRowBody(dictionary2(text5), list)
                                    ElseIf list.Count > 0 Then
                                        colMap = list(0)
                                    End If
                                    Dim useColumnCells = PreferColumnCellRows(text, colMap)
                                    Dim emailEsc As String = EscapeMorkValue(mailContact.Email.Trim())
                                    Dim mailContact2 = mailContact
                                    Dim nameEsc As String = EscapeMorkValue(If(Not Equals(mailContact2.DisplayName, Nothing), mailContact2.DisplayName, mailContact.Email).Trim())
                                    text4 = BuildMorkAppendRow(text5, colMap, useColumnCells, mailContact, emailEsc, nameEsc)
                                End If
                            End If
                        End If
                        list2.Add(text4)
                    Next
                    Dim text7 As String = String.Join(text2, list2.ToArray())
                    If Operators.CompareString(text2, vbCrLf, TextCompare:=False) = 0 AndAlso Not text7.EndsWith(vbCrLf, StringComparison.Ordinal) AndAlso text.EndsWith(vbCrLf, StringComparison.Ordinal) Then
                        text7 += vbCrLf
                    End If
                    File.WriteAllBytes(destPath, encoding.GetBytes(text7))
                    If CountMorkDataRows(text) <> CountMorkDataRows(text7) Then
                        LastError = "Full encoder changed Mork row count."
                        result = False
                    Else
                        result = TryValidateMorkRoundTrip(contacts, destPath)
                    End If
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Function CountMorkDataRows(morkText As String) As Integer
            If String.IsNullOrEmpty(morkText) Then
                Return 0
            End If
            Dim num = 0
            Dim regex As Regex = New Regex("(?:^|[\r\n])@\d+=", RegexOptions.Compiled)
            Return regex.Matches(morkText).Count
        End Function

        Private Shared Function PreferColumnCellRows(morkText As String, colMap As Dictionary(Of String, String)) As Boolean
            If colMap Is Nothing OrElse colMap.Count = 0 Then
                Return False
            End If
            Dim num = 0
            Dim num2 = 0
            Dim regex As Regex = New Regex("@([^=]+)=([^@\r\n]*)", RegexOptions.Compiled)
            For Each item As Match In regex.Matches(If(Not Equals(morkText, Nothing), morkText, ""))
                Dim value = item.Groups(2).Value
                If value.IndexOf("^"c) >= 0 AndAlso value.IndexOf("(d=", StringComparison.Ordinal) < 0 Then
                    num += 1
                ElseIf value.IndexOf("(d=", StringComparison.Ordinal) >= 0 Then
                    num2 += 1
                End If
            Next
            If num = 0 AndAlso num2 = 0 Then
                Return True
            End If
            Return num >= num2
        End Function

        Private Shared Function FindColumnId(colMap As Dictionary(Of String, String), propertyName As String) As String
            If colMap Is Nothing Then
                Return Nothing
            End If
            For Each item In colMap
                If String.Equals(item.Value, propertyName, StringComparison.OrdinalIgnoreCase) Then
                    Return item.Key
                End If
            Next
            Return Nothing
        End Function

        Private Shared Function BuildMorkAppendRow(rowId As String, colMap As Dictionary(Of String, String), useColumnCells As Boolean, contact As MailContact, emailEsc As String, nameEsc As String) As String
            If useColumnCells AndAlso colMap IsNot Nothing AndAlso colMap.Count > 0 Then
                Dim value = FindColumnId(colMap, "PrimaryEmail")
                Dim value2 = FindColumnId(colMap, "DisplayName")
                If Not String.IsNullOrEmpty(value) Then
                    Dim stringBuilder As StringBuilder = New StringBuilder()
                    stringBuilder.Append("@"c).Append(rowId).Append("=^").Append(value).Append("^").Append(emailEsc)
                    If Not String.IsNullOrEmpty(value2) Then
                        stringBuilder.Append("^").Append(value2).Append("^").Append(nameEsc)
                    End If
                    Dim value3 = FindColumnId(colMap, "Company")
                    If Not String.IsNullOrEmpty(value3) AndAlso contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Company) Then
                        stringBuilder.Append("^").Append(value3).Append("^").Append(EscapeMorkValue(contact.Company))
                    End If
                    AppendColumnField(stringBuilder, colMap, "SecondEmail", If(contact Is Nothing, "", contact.SecondaryEmail))
                    AppendColumnField(stringBuilder, colMap, "FirstName", If(contact Is Nothing, "", contact.FirstName))
                    AppendColumnField(stringBuilder, colMap, "LastName", If(contact Is Nothing, "", contact.LastName))
                    AppendColumnField(stringBuilder, colMap, "NickName", If(contact Is Nothing, "", contact.NickName))
                    AppendColumnField(stringBuilder, colMap, "JobTitle", If(contact Is Nothing, "", contact.JobTitle))
                    AppendColumnField(stringBuilder, colMap, "Department", If(contact Is Nothing, "", contact.Department))
                    AppendColumnField(stringBuilder, colMap, "WebPage1", If(contact Is Nothing, "", contact.WebPage))
                    AppendColumnField(stringBuilder, colMap, "WorkPhone", If(contact Is Nothing, "", contact.WorkPhone))
                    AppendColumnField(stringBuilder, colMap, "HomePhone", If(contact Is Nothing, "", contact.HomePhone))
                    AppendColumnField(stringBuilder, colMap, "FaxNumber", If(contact Is Nothing, "", contact.FaxNumber))
                    AppendColumnField(stringBuilder, colMap, "PagerNumber", If(contact Is Nothing, "", contact.PagerNumber))
                    AppendColumnField(stringBuilder, colMap, "Custom1", If(contact Is Nothing, "", contact.Custom1))
                    AppendColumnField(stringBuilder, colMap, "Custom2", If(contact Is Nothing, "", contact.Custom2))
                    AppendColumnField(stringBuilder, colMap, "Custom3", If(contact Is Nothing, "", contact.Custom3))
                    AppendColumnField(stringBuilder, colMap, "Custom4", If(contact Is Nothing, "", contact.Custom4))
                    AppendColumnField(stringBuilder, colMap, "SpouseName", If(contact Is Nothing, "", contact.SpouseName))
                    AppendColumnField(stringBuilder, colMap, "Notes", If(contact Is Nothing, "", contact.Notes))
                    AppendColumnField(stringBuilder, colMap, "CellularNumber", If(contact Is Nothing, "", contact.MobilePhone))
                    AppendColumnField(stringBuilder, colMap, "WorkAddress", If(contact Is Nothing, "", contact.WorkAddress))
                    AppendColumnField(stringBuilder, colMap, "HomeAddress", If(contact Is Nothing, "", contact.HomeAddress))
                    AppendColumnField(stringBuilder, colMap, "BirthDay", If(contact Is Nothing, "", contact.Birthday))
                    AppendColumnField(stringBuilder, colMap, "Anniversary", If(contact Is Nothing, "", contact.Anniversary))
                    AppendColumnField(stringBuilder, colMap, "MiddleName", If(contact Is Nothing, "", contact.MiddleName))
                    AppendColumnField(stringBuilder, colMap, "OfficeLocation", If(contact Is Nothing, "", contact.OfficeLocation))
                    AppendColumnField(stringBuilder, colMap, "OtherPhone", If(contact Is Nothing, "", contact.OtherPhone))
                    AppendColumnField(stringBuilder, colMap, "AssistantName", If(contact Is Nothing, "", contact.AssistantName))
                    If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                        Call AppendColumnField(stringBuilder, colMap, "LowercasePrimaryEmail", contact.Email.Trim().ToLowerInvariant())
                    End If
                    AppendColumnField(stringBuilder, colMap, "ScreenName", If(contact Is Nothing, "", contact.ScreenName))
                    AppendColumnField(stringBuilder, colMap, "AimScreenName", If(contact Is Nothing, "", contact.AimScreenName))
                    AppendColumnField(stringBuilder, colMap, "PopularityIndex", If(contact Is Nothing, "", contact.PopularityIndex))
                    AppendColumnField(stringBuilder, colMap, "RecordName", If(contact Is Nothing, "", contact.RecordName))
                    AppendColumnField(stringBuilder, colMap, "PreferEmail", If(contact Is Nothing, "", contact.PreferEmail))
                    Return stringBuilder.ToString()
                End If
            End If
            Dim text = "@" & rowId & "=<" & rowId & ">(d=PrimaryEmail)>" & emailEsc & "<" & rowId & ">(d=DisplayName)>" & nameEsc
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Company) Then
                text = text & "<" & rowId & ">(d=Company)>" & EscapeMorkValue(contact.Company)
            End If
            text += AppendRowRefField(rowId, "SecondEmail", If(contact Is Nothing, "", contact.SecondaryEmail))
            text += AppendRowRefField(rowId, "FirstName", If(contact Is Nothing, "", contact.FirstName))
            text += AppendRowRefField(rowId, "LastName", If(contact Is Nothing, "", contact.LastName))
            text += AppendRowRefField(rowId, "NickName", If(contact Is Nothing, "", contact.NickName))
            text += AppendRowRefField(rowId, "JobTitle", If(contact Is Nothing, "", contact.JobTitle))
            text += AppendRowRefField(rowId, "Department", If(contact Is Nothing, "", contact.Department))
            text += AppendRowRefField(rowId, "WebPage1", If(contact Is Nothing, "", contact.WebPage))
            text += AppendRowRefField(rowId, "WorkPhone", If(contact Is Nothing, "", contact.WorkPhone))
            text += AppendRowRefField(rowId, "HomePhone", If(contact Is Nothing, "", contact.HomePhone))
            text += AppendRowRefField(rowId, "FaxNumber", If(contact Is Nothing, "", contact.FaxNumber))
            text += AppendRowRefField(rowId, "PagerNumber", If(contact Is Nothing, "", contact.PagerNumber))
            text += AppendRowRefField(rowId, "Custom1", If(contact Is Nothing, "", contact.Custom1))
            text += AppendRowRefField(rowId, "Custom2", If(contact Is Nothing, "", contact.Custom2))
            text += AppendRowRefField(rowId, "Custom3", If(contact Is Nothing, "", contact.Custom3))
            text += AppendRowRefField(rowId, "Custom4", If(contact Is Nothing, "", contact.Custom4))
            text += AppendRowRefField(rowId, "SpouseName", If(contact Is Nothing, "", contact.SpouseName))
            text += AppendRowRefField(rowId, "Notes", If(contact Is Nothing, "", contact.Notes))
            text += AppendRowRefField(rowId, "CellularNumber", If(contact Is Nothing, "", contact.MobilePhone))
            text += AppendRowRefField(rowId, "WorkAddress", If(contact Is Nothing, "", contact.WorkAddress))
            text += AppendRowRefField(rowId, "HomeAddress", If(contact Is Nothing, "", contact.HomeAddress))
            text += AppendRowRefField(rowId, "BirthDay", If(contact Is Nothing, "", contact.Birthday))
            text += AppendRowRefField(rowId, "Anniversary", If(contact Is Nothing, "", contact.Anniversary))
            text += AppendRowRefField(rowId, "MiddleName", If(contact Is Nothing, "", contact.MiddleName))
            text += AppendRowRefField(rowId, "OfficeLocation", If(contact Is Nothing, "", contact.OfficeLocation))
            text += AppendRowRefField(rowId, "OtherPhone", If(contact Is Nothing, "", contact.OtherPhone))
            text += AppendRowRefField(rowId, "AssistantName", If(contact Is Nothing, "", contact.AssistantName))
            If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                text += AppendRowRefField(rowId, "LowercasePrimaryEmail", contact.Email.Trim().ToLowerInvariant())
            End If
            text += AppendRowRefField(rowId, "ScreenName", If(contact Is Nothing, "", contact.ScreenName))
            text += AppendRowRefField(rowId, "AimScreenName", If(contact Is Nothing, "", contact.AimScreenName))
            text += AppendRowRefField(rowId, "PopularityIndex", If(contact Is Nothing, "", contact.PopularityIndex))
            text += AppendRowRefField(rowId, "RecordName", If(contact Is Nothing, "", contact.RecordName))
            Return text & AppendRowRefField(rowId, "PreferEmail", If(contact Is Nothing, "", contact.PreferEmail))
        End Function

        Private Shared Function TryInsertMorkRowBeforeTableEnd(morkText As String, rowLine As String, lineBreak As String, Optional colMap As Dictionary(Of String, String) = Nothing) As String
            If String.IsNullOrEmpty(morkText) OrElse String.IsNullOrEmpty(rowLine) Then
                Return morkText
            End If
            Dim num = FindPrimaryEmailTableEndIndex(morkText, colMap)
            If num <= 0 Then
                num = morkText.LastIndexOf("(end", StringComparison.OrdinalIgnoreCase)
            End If
            If num <= 0 Then
                Return morkText & lineBreak & rowLine
            End If
            Return morkText.Substring(0, num) & rowLine & lineBreak & morkText.Substring(num)
        End Function

        Private Shared Function FindPrimaryEmailTableEndIndex(morkText As String, colMap As Dictionary(Of String, String)) As Integer
            If String.IsNullOrEmpty(morkText) Then
                Return -1
            End If
            Dim tableMaps = ExtractPerTableColumnMaps(morkText)
            Dim dictionary = colMap
            If dictionary Is Nothing OrElse FindColumnId(dictionary, "PrimaryEmail").Length = 0 Then
                dictionary = PickPrimaryEmailTableMap(tableMaps)
            End If
            If dictionary Is Nothing OrElse FindColumnId(dictionary, "PrimaryEmail").Length = 0 Then
                Return -1
            End If
            Dim regex As Regex = New Regex("\(k=([^\)]*)\)", RegexOptions.Compiled)
            For Each item As Match In regex.Matches(morkText)
                Dim dictionary2 = ParseColumnKeyChunk(item.Groups(1).Value)
                If dictionary2 Is Nothing OrElse FindColumnId(dictionary2, "PrimaryEmail").Length = 0 OrElse dictionary2.Count <> dictionary.Count Then
                    Continue For
                End If
                Dim flag = True
                For Each item2 In dictionary
                    Dim value As String = Nothing
                    If Not dictionary2.TryGetValue(item2.Key, value) OrElse Not String.Equals(value, item2.Value, StringComparison.OrdinalIgnoreCase) Then
                        flag = False
                        Exit For
                    End If
                Next
                If flag Then
                    Dim num = morkText.IndexOf("(end", item.Index + item.Length, StringComparison.OrdinalIgnoreCase)
                    If num > 0 Then
                        Return num
                    End If
                End If
            Next
            For Each item3 As Match In regex.Matches(morkText)
                Dim dictionary3 = ParseColumnKeyChunk(item3.Groups(1).Value)
                If dictionary3 IsNot Nothing AndAlso FindColumnId(dictionary3, "PrimaryEmail").Length <> 0 Then
                    Dim num2 = morkText.IndexOf("(end", item3.Index + item3.Length, StringComparison.OrdinalIgnoreCase)
                    If num2 > 0 Then
                        Return num2
                    End If
                End If
            Next
            Return -1
        End Function

        Private Shared Function TryRemoveOrphanMorkRowsFromRawBodies(morkText As String, rawRows As Dictionary(Of String, String), wantedEmails As HashSet(Of String), lineBreak As String) As String
            If String.IsNullOrEmpty(morkText) OrElse rawRows Is Nothing OrElse rawRows.Count = 0 OrElse wantedEmails Is Nothing Then
                Return morkText
            End If
            Dim text = morkText
            For Each rawRow In rawRows
                Dim key = rawRow.Key
                Dim value = rawRow.Value
                Dim text2 = If(Not Equals(value, Nothing), value, "")
                If text2.Length = 0 Then
                    Continue For
                End If
                Dim text3 = ExtractEmailFromMorkRowBody(text2)
                If text3.Length <> 0 AndAlso Not wantedEmails.Contains(text3) Then
                    Dim value2 = "@" & key & "="
                    Dim num = text.IndexOf(value2, StringComparison.Ordinal)
                    If num >= 0 Then
                        Dim num2 = text.IndexOf(lineBreak, num)
                        num2 = If(num2 >= 0, num2 + lineBreak.Length, text.Length)
                        text = text.Remove(num, num2 - num)
                    End If
                End If
            Next
            Return text
        End Function

        Private Shared Function ExtractEmailFromMorkRowBody(rowBody As String) As String
            Dim input = If(Not Equals(rowBody, Nothing), rowBody, "")
            For Each item As Match In EmailPattern.Matches(input)
                If item.Success Then
                    Return item.Value.Trim()
                End If
            Next
            Return ""
        End Function

        Private Shared Sub AppendColumnField(sb As StringBuilder, colMap As Dictionary(Of String, String), propertyName As String, rawValue As String)
            If sb Is Nothing OrElse colMap Is Nothing Then
                Return
            End If
            Dim text As String = If(Not Equals(rawValue, Nothing), rawValue, "").Trim()
            If text.Length <> 0 Then
                Dim value = FindColumnId(colMap, propertyName)
                If Not String.IsNullOrEmpty(value) Then
                    sb.Append("^").Append(value).Append("^").Append(EscapeMorkValue(text))
                End If
            End If
        End Sub

        Private Shared Function AppendRowRefField(rowId As String, prop As String, rawValue As String) As String
            Dim text As String = If(Not Equals(rawValue, Nothing), rawValue, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Return "<" & rowId & ">(d=" & prop & ")>" & EscapeMorkValue(text)
        End Function

        Private Shared Function FindMorkRowIdForEmail(morkText As String, email As String) As String
            Dim text As String = If(Not Equals(email, Nothing), email, "").Trim()
            If text.Length = 0 OrElse String.IsNullOrEmpty(morkText) Then
                Return ""
            End If
            Dim dictionary = ExtractPropertyMap(morkText, "PrimaryEmail")
            For Each item In dictionary
                If String.Equals(item.Value, text, StringComparison.OrdinalIgnoreCase) Then
                    Return item.Key
                End If
            Next
            Dim dictionary2 = ExtractColumnKeyMap(morkText)
            If dictionary2.Count > 0 Then
                Dim dictionary3 = ExtractPropertyMapViaColumns(morkText, dictionary2, "PrimaryEmail")
                For Each item2 In dictionary3
                    If String.Equals(item2.Value, text, StringComparison.OrdinalIgnoreCase) Then
                        Return item2.Key
                    End If
                Next
            End If
            Return ""
        End Function

        Private Shared Function TryRemoveOrphanMorkRows(morkText As String, wantedEmails As HashSet(Of String), lineBreak As String) As String
            If String.IsNullOrEmpty(morkText) OrElse wantedEmails Is Nothing Then
                Return morkText
            End If
            Dim dictionary = ExtractPropertyMap(morkText, "PrimaryEmail")
            Dim dictionary2 = ExtractColumnKeyMap(morkText)
            If dictionary.Count = 0 AndAlso dictionary2.Count > 0 Then
                dictionary = ExtractPropertyMapViaColumns(morkText, dictionary2, "PrimaryEmail")
            End If
            Dim text = morkText
            For Each item In dictionary
                Dim value = item.Value
                Dim text2 As String = If(Not Equals(value, Nothing), value, "").Trim()
                If text2.Length <> 0 AndAlso Not wantedEmails.Contains(text2) Then
                    Dim key = item.Key
                    Dim value2 = "@" & key & "="
                    Dim num = text.IndexOf(value2, StringComparison.Ordinal)
                    If num >= 0 Then
                        Dim num2 = text.IndexOf(lineBreak, num)
                        num2 = If(num2 >= 0, num2 + lineBreak.Length, text.Length)
                        text = text.Remove(num, num2 - num)
                    End If
                End If
            Next
            Return text
        End Function

        Private Shared Function TryReplaceMorkRowLine(morkText As String, rowId As String, colMap As Dictionary(Of String, String), useColumnCells As Boolean, contact As MailContact, emailEsc As String, nameEsc As String, lineBreak As String) As String
            If String.IsNullOrEmpty(morkText) OrElse String.IsNullOrWhiteSpace(rowId) Then
                Return ""
            End If
            Dim value = "@" & rowId & "="
            Dim num = morkText.IndexOf(value, StringComparison.Ordinal)
            If num < 0 Then
                Return ""
            End If
            Dim num2 = morkText.IndexOf(lineBreak, num)
            If num2 < 0 Then
                num2 = morkText.Length
            End If
            Dim oldLine = morkText.Substring(num, num2 - num)
            Dim newLine = BuildMorkAppendRow(rowId, colMap, useColumnCells, contact, emailEsc, nameEsc)
            newLine = AppendPreservedUnknownColumnCells(oldLine, newLine, colMap)
            Return morkText.Substring(0, num) & newLine & morkText.Substring(num2)
        End Function

        Private Shared Function IsKnownMorkExportProperty(propertyName As String) As Boolean
            Dim text As String = If(Not Equals(propertyName, Nothing), propertyName, "").Trim()
            If text.Length = 0 Then
                Return False
            End If
            Dim text2 As String = text.ToLowerInvariant()
            If Operators.CompareString(text2, "primaryemail", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "secondemail", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "displayname", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "firstname", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "lastname", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "nickname", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "jobtitle", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "department", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "webpage1", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "company", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "workphone", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "homephone", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "faxnumber", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "pagernumber", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "custom1", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "custom2", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "custom3", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "custom4", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "spousename", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "notes", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "cellularnumber", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "workaddress", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "homeaddress", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "birthday", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "anniversary", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "middlename", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "officelocation", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "otherphone", TextCompare:=False) <> 0 AndAlso Operators.CompareString(text2, "assistantname", TextCompare:=False) <> 0 Then
                Select Case text2
                    Case "lowercaseprimaryemail", "screenname", "aimscreenname", "popularityindex", "recordname", "preferemail", "birthday", "anniversary", "workaddress", "homeaddress", "middlename", "officelocation", "otherphone", "assistantname"
                    Case Else
                        Return False
                End Select
            End If
            Return True
        End Function

        Private Shared Function AppendPreservedUnknownColumnCells(oldLine As String, newLine As String, colMap As Dictionary(Of String, String)) As String
            If String.IsNullOrEmpty(oldLine) OrElse String.IsNullOrEmpty(newLine) Then
                Return newLine
            End If
            If Not oldLine.StartsWith("@", StringComparison.Ordinal) OrElse Not newLine.StartsWith("@", StringComparison.Ordinal) Then
                Return newLine
            End If
            Dim num = oldLine.IndexOf("="c)
            If num < 0 Then
                Return newLine
            End If
            Dim text As String = oldLine.Substring(num + 1).Trim()
            If text.Length = 0 OrElse text.StartsWith("<") Then
                Return newLine
            End If
            Dim array = text.Split("^"c)
            If array.Length < 2 Then
                Return newLine
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder(newLine)
            Dim num2 = 0
            While num2 < array.Length - 1
                Dim text2 As String = array(num2).Trim()
                Dim value = array(num2 + 1)
                num2 += 2
                If text2.Length <> 0 AndAlso Regex.IsMatch(text2, "^[0-9A-Fa-f]+$") AndAlso newLine.IndexOf("^" & text2 & "^", StringComparison.Ordinal) < 0 Then
                    stringBuilder.Append("^").Append(text2).Append("^").Append(value)
                End If
            End While
            Return stringBuilder.ToString()
        End Function

        Private Shared Function FindMaxMorkRowId(morkText As String) As Integer
            Dim num = 0
            If String.IsNullOrEmpty(morkText) Then
                Return num
            End If
            Dim regex As Regex = New Regex("@(\d+)=", RegexOptions.Compiled)
            For Each item As Match In regex.Matches(morkText)
                Dim result = 0
                If Integer.TryParse(item.Groups(1).Value, result) AndAlso result > num Then
                    num = result
                End If
            Next
            Return num
        End Function

        Private Shared Function StructuralCompareBytes(a As Byte(), b As Byte()) As Boolean
            If a Is Nothing OrElse b Is Nothing Then
                Return False
            End If
            If a.Length <> b.Length Then
                Return False
            End If
            Dim num = a.Length - 1
            For i = 0 To num
                If a(i) <> b(i) Then
                    Return False
                End If
            Next
            Return True
        End Function

        Private Shared Function EscapeMorkValue(raw As String) As String
            Dim text = If(Not Equals(raw, Nothing), raw, "")
            Return text.Replace(" ", "$20").Replace("@", "$40").Replace(",", "$2C").Replace("(", "$28").Replace(")", "$29")
        End Function

        Private Shared Function TryValidateMorkRoundTrip(source As List(Of MailContact), morkPath As String) As Boolean
            LastError = Nothing
            Dim list = TryParseContacts(morkPath)
            If list Is Nothing OrElse list.Count = 0 Then
                LastError = "Mork export failed round-trip parse."
                Return False
            End If
            Dim num = 0
            For Each item In source
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                    num = num + 1
                End If
            Next
            If list.Count < num Then
                LastError = "Mork round-trip lost contacts (" & list.Count.ToString() & " vs " & num.ToString() & ")."
                Return False
            End If
            For Each item2 In source
                If item2 Is Nothing OrElse String.IsNullOrWhiteSpace(item2.Email) Then
                    Continue For
                End If
                Dim mailContact As MailContact = Nothing
                For Each item3 In list
                    If item3 IsNot Nothing AndAlso String.Equals(item3.Email, item2.Email, StringComparison.OrdinalIgnoreCase) Then
                        mailContact = item3
                        Exit For
                    End If
                Next
                If mailContact Is Nothing Then
                    LastError = "Mork round-trip missing " & item2.Email
                    Return False
                End If
                Dim mailContact2 = item2
                Dim text As String = If(Not Equals(mailContact2.PreferEmail, Nothing), mailContact2.PreferEmail, "").Trim()
                If text.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b As String = If(Not Equals(mailContact2.PreferEmail, Nothing), mailContact2.PreferEmail, "").Trim()
                    If Not String.Equals(text, b, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip PreferEmail mismatch for " & item2.Email
                        Return False
                    End If
                End If
                mailContact2 = item2
                Dim text2 As String = If(Not Equals(mailContact2.SecondaryEmail, Nothing), mailContact2.SecondaryEmail, "").Trim()
                If text2.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b2 As String = If(Not Equals(mailContact2.SecondaryEmail, Nothing), mailContact2.SecondaryEmail, "").Trim()
                    If Not String.Equals(text2, b2, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip SecondEmail mismatch for " & item2.Email
                        Return False
                    End If
                End If
                mailContact2 = item2
                Dim text3 As String = If(Not Equals(mailContact2.MorkRowId, Nothing), mailContact2.MorkRowId, "").Trim()
                If text3.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b3 As String = If(Not Equals(mailContact2.MorkRowId, Nothing), mailContact2.MorkRowId, "").Trim()
                    If Not String.Equals(text3, b3, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip MorkRowId mismatch for " & item2.Email
                        Return False
                    End If
                End If
                mailContact2 = item2
                Dim text4 As String = If(Not Equals(mailContact2.MobilePhone, Nothing), mailContact2.MobilePhone, "").Trim()
                If text4.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b4 As String = If(Not Equals(mailContact2.MobilePhone, Nothing), mailContact2.MobilePhone, "").Trim()
                    If Not String.Equals(text4, b4, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip CellularNumber mismatch for " & item2.Email
                        Return False
                    End If
                End If
                mailContact2 = item2
                Dim text5 As String = If(Not Equals(mailContact2.ScreenName, Nothing), mailContact2.ScreenName, "").Trim()
                If text5.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b5 As String = If(Not Equals(mailContact2.ScreenName, Nothing), mailContact2.ScreenName, "").Trim()
                    If Not String.Equals(text5, b5, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip ScreenName mismatch for " & item2.Email
                        Return False
                    End If
                End If
                mailContact2 = item2
                Dim text6 As String = If(Not Equals(mailContact2.JobTitle, Nothing), mailContact2.JobTitle, "").Trim()
                If text6.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b6 As String = If(Not Equals(mailContact2.JobTitle, Nothing), mailContact2.JobTitle, "").Trim()
                    If Not String.Equals(text6, b6, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip JobTitle mismatch for " & item2.Email
                        Return False
                    End If
                End If
                mailContact2 = item2
                Dim text7 As String = If(Not Equals(mailContact2.NickName, Nothing), mailContact2.NickName, "").Trim()
                If text7.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b7 As String = If(Not Equals(mailContact2.NickName, Nothing), mailContact2.NickName, "").Trim()
                    If Not String.Equals(text7, b7, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip NickName mismatch for " & item2.Email
                        Return False
                    End If
                End If
                mailContact2 = item2
                Dim text8 As String = If(Not Equals(mailContact2.AimScreenName, Nothing), mailContact2.AimScreenName, "").Trim()
                If text8.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b8 As String = If(Not Equals(mailContact2.AimScreenName, Nothing), mailContact2.AimScreenName, "").Trim()
                    If Not String.Equals(text8, b8, StringComparison.OrdinalIgnoreCase) Then
                        LastError = "Mork round-trip AimScreenName mismatch for " & item2.Email
                        Return False
                    End If
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Company, mailContact.Company, "Company", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Department, mailContact.Department, "Department", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.WorkPhone, mailContact.WorkPhone, "WorkPhone", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.PopularityIndex, mailContact.PopularityIndex, "PopularityIndex", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.RecordName, mailContact.RecordName, "RecordName", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.HomePhone, mailContact.HomePhone, "HomePhone", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.FaxNumber, mailContact.FaxNumber, "FaxNumber", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.PagerNumber, mailContact.PagerNumber, "PagerNumber", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.WebPage, mailContact.WebPage, "WebPage", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.SpouseName, mailContact.SpouseName, "SpouseName", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Anniversary, mailContact.Anniversary, "Anniversary", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Birthday, mailContact.Birthday, "Birthday", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Custom1, mailContact.Custom1, "Custom1", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Custom2, mailContact.Custom2, "Custom2", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Custom3, mailContact.Custom3, "Custom3", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Custom4, mailContact.Custom4, "Custom4", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.Notes, mailContact.Notes, "Notes", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.OtherPhone, mailContact.OtherPhone, "OtherPhone", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.WorkAddress, mailContact.WorkAddress, "WorkAddress", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.HomeAddress, mailContact.HomeAddress, "HomeAddress", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.DisplayName, mailContact.DisplayName, "DisplayName", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.FirstName, mailContact.FirstName, "FirstName", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.LastName, mailContact.LastName, "LastName", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.MiddleName, mailContact.MiddleName, "MiddleName", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.OfficeLocation, mailContact.OfficeLocation, "OfficeLocation", item2.Email) Then
                    Return False
                End If
                If Not ValidateOptionalMorkFieldMatch(item2.AssistantName, mailContact.AssistantName, "AssistantName", item2.Email) Then
                    Return False
                End If
                Dim text9 As String = item2.Email.Trim().ToLowerInvariant()
                If text9.Length > 0 Then
                    mailContact2 = mailContact
                    Dim b9 As String = If(Not Equals(mailContact2.Email, Nothing), mailContact2.Email, "").Trim().ToLowerInvariant()
                    If Not String.Equals(text9, b9, StringComparison.Ordinal) Then
                        LastError = "Mork round-trip LowercasePrimaryEmail mismatch for " & item2.Email
                        Return False
                    End If
                End If
            Next
            Return True
        End Function

        Private Shared Function ValidateOptionalMorkFieldMatch(sourceValue As String, roundTripValue As String, fieldLabel As String, email As String) As Boolean
            Dim text As String = If(Not Equals(sourceValue, Nothing), sourceValue, "").Trim()
            If text.Length = 0 Then
                Return True
            End If
            Dim b As String = If(Not Equals(roundTripValue, Nothing), roundTripValue, "").Trim()
            If String.Equals(text, b, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            LastError = "Mork round-trip " & fieldLabel & " mismatch for " & email
            Return False
        End Function

        Public Shared Function TryVerifyExportBytesMatchSource(sourceMabPath As String, exportedMabPath As String) As Boolean
            LastMabExportVerifiedAgainstSource = False
            Dim result As Boolean
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                result = False
            ElseIf String.IsNullOrWhiteSpace(exportedMabPath) OrElse Not File.Exists(exportedMabPath) Then
                result = False
            Else
                Try
                    Dim a = File.ReadAllBytes(sourceMabPath)
                    Dim b = File.ReadAllBytes(exportedMabPath)
                    LastMabExportVerifiedAgainstSource = StructuralCompareBytes(a, b)
                    result = LastMabExportVerifiedAgainstSource
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryExportByteIdenticalSourceCopy(sourceMabPath As String, contacts As List(Of MailContact), destPath As String) As Boolean
            LastError = Nothing
            LastMabExportByteIdentical = False
            Dim result As Boolean
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                result = False
            ElseIf Not TryContactsUnchangedVersusSource(sourceMabPath, contacts) Then
                result = False
            Else
                Try
                    File.Copy(sourceMabPath, destPath, overwrite:=True)
                    LastMabExportByteIdentical = True
                    result = True
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryContactsUnchangedVersusSource(sourceMabPath As String, contacts As List(Of MailContact)) As Boolean
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                Return False
            End If
            Dim list = TryParseContacts(sourceMabPath)
            If list Is Nothing Then
                Return False
            End If
            If contacts Is Nothing Then
                Return list.Count = 0
            End If
            Dim dictionary As Dictionary(Of String, MailContact) = New Dictionary(Of String, MailContact)(StringComparer.OrdinalIgnoreCase)
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                    dictionary(item.Email.Trim()) = item
                End If
            Next
            Dim num = 0
            For Each contact In contacts
                If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                    num = num + 1
                    Dim key As String = contact.Email.Trim()
                    If Not dictionary.ContainsKey(key) Then
                        Return False
                    End If
                    If Not ContactExportFieldsEqual(contact, dictionary(key)) Then
                        Return False
                    End If
                End If
            Next
            Return num = dictionary.Count
        End Function

        Private Shared Function ContactExportFieldsEqual(a As MailContact, b As MailContact) As Boolean
            If a Is Nothing OrElse b Is Nothing Then
                Return False
            End If
            Return If(String.Equals(Norm(a.DisplayName), Norm(b.DisplayName), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.FirstName), Norm(b.FirstName), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.LastName), Norm(b.LastName), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Company), Norm(b.Company), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.WorkPhone), Norm(b.WorkPhone), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.MobilePhone), Norm(b.MobilePhone), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.JobTitle), Norm(b.JobTitle), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Department), Norm(b.Department), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.SecondaryEmail), Norm(b.SecondaryEmail), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.NickName), Norm(b.NickName), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.HomePhone), Norm(b.HomePhone), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Birthday), Norm(b.Birthday), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Notes), Norm(b.Notes), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.WebPage), Norm(b.WebPage), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.FaxNumber), Norm(b.FaxNumber), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.AssistantName), Norm(b.AssistantName), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.OfficeLocation), Norm(b.OfficeLocation), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.MiddleName), Norm(b.MiddleName), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.HomeAddress), Norm(b.HomeAddress), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.WorkAddress), Norm(b.WorkAddress), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.SpouseName), Norm(b.SpouseName), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Anniversary), Norm(b.Anniversary), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Custom1), Norm(b.Custom1), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.PagerNumber), Norm(b.PagerNumber), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Custom2), Norm(b.Custom2), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Custom3), Norm(b.Custom3), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.Custom4), Norm(b.Custom4), StringComparison.Ordinal) AndAlso String.Equals(Norm(a.ScreenName), Norm(b.ScreenName), StringComparison.Ordinal), True, False)
        End Function

        Private Shared Function Norm(s As String) As String
            Return If(Not Equals(s, Nothing), s, "").Trim()
        End Function

        Public Shared Function TryAssertExportEmailSetMatchesSource(sourceMabPath As String, exportedMabPath As String) As Boolean
            LastMabExportEmailSetMatches = False
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                Return False
            End If
            If String.IsNullOrWhiteSpace(exportedMabPath) OrElse Not File.Exists(exportedMabPath) Then
                Return False
            End If
            Dim list = TryParseContacts(sourceMabPath)
            Dim list2 = TryParseContacts(exportedMabPath)
            If list Is Nothing OrElse list2 Is Nothing Then
                Return False
            End If
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                    hashSet.Add(item.Email.Trim())
                End If
            Next
            Dim hashSet2 As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item2 In list2
                If item2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item2.Email) Then
                    hashSet2.Add(item2.Email.Trim())
                End If
            Next
            LastMabExportEmailSetMatches = If(hashSet.Count = hashSet2.Count AndAlso hashSet.SetEquals(hashSet2), True, False)
            Return LastMabExportEmailSetMatches
        End Function

        Public Shared Function TryAssertExportContactFieldsMatchSource(sourceMabPath As String, exportedMabPath As String) As Boolean
            LastMabExportFieldsMatch = False
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                Return False
            End If
            If String.IsNullOrWhiteSpace(exportedMabPath) OrElse Not File.Exists(exportedMabPath) Then
                Return False
            End If
            Dim list = TryParseContacts(sourceMabPath)
            Dim list2 = TryParseContacts(exportedMabPath)
            If list Is Nothing OrElse list2 Is Nothing Then
                Return False
            End If
            Dim dictionary As Dictionary(Of String, MailContact) = New Dictionary(Of String, MailContact)(StringComparer.OrdinalIgnoreCase)
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                    dictionary(item.Email.Trim()) = item
                End If
            Next
            Dim num = 0
            For Each item2 In list2
                If item2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item2.Email) Then
                    Dim key As String = item2.Email.Trim()
                    If Not dictionary.ContainsKey(key) Then
                        Return False
                    End If
                    If Not ContactExportFieldsEqual(item2, dictionary(key)) Then
                        Return False
                    End If
                    num = num + 1
                End If
            Next
            LastMabExportFieldsMatch = num = dictionary.Count
            Return LastMabExportFieldsMatch
        End Function

        Public Shared Function TryValidateSourceMabByteRoundTrip(sourceMabPath As String) As Boolean
            LastError = Nothing
            LastSourceMabRoundTripValidated = False
            LastSourceMabRoundTripByteIdentical = False
            LastValidatedSourceMabPath = If(Not Equals(sourceMabPath, Nothing), sourceMabPath, "").Trim()
            If String.IsNullOrWhiteSpace(sourceMabPath) OrElse Not File.Exists(sourceMabPath) Then
                LastError = "User abook.mab source path missing."
                Return False
            End If
            Dim list = TryParseContacts(sourceMabPath)
            If list Is Nothing Then
                Return False
            End If
            Dim text As String = Path.Combine(Path.GetTempPath(), "lcars-mork-preserve-" & Guid.NewGuid().ToString("N") & ".mab")
            Dim text2 As String = Path.Combine(Path.GetTempPath(), "lcars-mork-full-" & Guid.NewGuid().ToString("N") & ".mab")
            Try
                Dim byteIdentical = False
                If Not TryWriteMorkPreservingSource(sourceMabPath, list, text, byteIdentical) Then
                    Return False
                End If
                LastSourceMabRoundTripByteIdentical = byteIdentical
                If Not TryWriteMorkFullEncoderFromSource(sourceMabPath, list, text2) Then
                    Return False
                End If
                If Not TryValidateMorkRoundTrip(list, text2) Then
                    Return False
                End If
                LastSourceMabRoundTripValidated = True
                Return True
            Finally
                Try
                    If File.Exists(text) Then
                        File.Delete(text)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    Call ProjectData.ClearProjectError()
                End Try
                Try
                    If File.Exists(text2) Then
                        File.Delete(text2)
                    End If
                Catch projectError2 As Exception
                    ProjectData.SetProjectError(projectError2)
                    Call ProjectData.ClearProjectError()
                End Try
            End Try
        End Function

        Public Shared Function TryValidateThunderbirdFixture(ByRef contactCount As Integer) As Boolean
            LastError = Nothing
            contactCount = 0
            Dim path = IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestFixtures")
            Dim text = IO.Path.Combine(path, "thunderbird-abook-sample.mork")
            Dim text2 = IO.Path.Combine(path, "thunderbird-abook-golden.mork")
            Dim text3 = IO.Path.Combine(path, "thunderbird-abook-profile.mork")
            Dim text4 = IO.Path.Combine(path, "thunderbird-abook-mixed.mork")
            Dim text5 = IO.Path.Combine(path, "thunderbird-abook-phones.mork")
            Dim text6 = IO.Path.Combine(path, "thunderbird-abook-rowref.mork")
            Dim text7 = IO.Path.Combine(path, "thunderbird-abook-company.mork")
            Dim text8 = IO.Path.Combine(path, "thunderbird-abook-nick.mork")
            Dim text9 = IO.Path.Combine(path, "thunderbird-abook-jobtitle.mork")
            Dim text10 = IO.Path.Combine(path, "thunderbird-abook-department.mork")
            Dim text11 = IO.Path.Combine(path, "thunderbird-abook-webpage.mork")
            Dim text12 = IO.Path.Combine(path, "thunderbird-abook-fax.mork")
            Dim text13 = IO.Path.Combine(path, "thunderbird-abook-pager.mork")
            Dim text14 = IO.Path.Combine(path, "thunderbird-abook-custom1.mork")
            Dim text15 = IO.Path.Combine(path, "thunderbird-abook-custom2.mork")
            Dim text16 = IO.Path.Combine(path, "thunderbird-abook-custom3.mork")
            Dim text17 = IO.Path.Combine(path, "thunderbird-abook-custom4.mork")
            Dim text18 = IO.Path.Combine(path, "thunderbird-abook-spouse.mork")
            Dim text19 = IO.Path.Combine(path, "thunderbird-abook-multitable.mork")
            If Not File.Exists(text) Then
                LastError = "Missing thunderbird-abook-sample.mork"
                Return False
            End If
            If Not File.Exists(text2) Then
                LastError = "Missing thunderbird-abook-golden.mork"
                Return False
            End If
            If Not File.Exists(text3) Then
                LastError = "Missing thunderbird-abook-profile.mork"
                Return False
            End If
            If Not File.Exists(text4) Then
                LastError = "Missing thunderbird-abook-mixed.mork"
                Return False
            End If
            If Not File.Exists(text5) Then
                LastError = "Missing thunderbird-abook-phones.mork"
                Return False
            End If
            If Not File.Exists(text6) Then
                LastError = "Missing thunderbird-abook-rowref.mork"
                Return False
            End If
            If Not File.Exists(text7) Then
                LastError = "Missing thunderbird-abook-company.mork"
                Return False
            End If
            If Not File.Exists(text8) Then
                LastError = "Missing thunderbird-abook-nick.mork"
                Return False
            End If
            If Not File.Exists(text9) Then
                LastError = "Missing thunderbird-abook-jobtitle.mork"
                Return False
            End If
            If Not File.Exists(text10) Then
                LastError = "Missing thunderbird-abook-department.mork"
                Return False
            End If
            If Not File.Exists(text11) Then
                LastError = "Missing thunderbird-abook-webpage.mork"
                Return False
            End If
            If Not File.Exists(text12) Then
                LastError = "Missing thunderbird-abook-fax.mork"
                Return False
            End If
            If Not File.Exists(text13) Then
                LastError = "Missing thunderbird-abook-pager.mork"
                Return False
            End If
            If Not File.Exists(text14) Then
                LastError = "Missing thunderbird-abook-custom1.mork"
                Return False
            End If
            If Not File.Exists(text15) Then
                LastError = "Missing thunderbird-abook-custom2.mork"
                Return False
            End If
            If Not File.Exists(text16) Then
                LastError = "Missing thunderbird-abook-custom3.mork"
                Return False
            End If
            If Not File.Exists(text17) Then
                LastError = "Missing thunderbird-abook-custom4.mork"
                Return False
            End If
            If Not File.Exists(text18) Then
                LastError = "Missing thunderbird-abook-spouse.mork"
                Return False
            End If
            If Not File.Exists(text19) Then
                LastError = "Missing thunderbird-abook-multitable.mork"
                Return False
            End If
            Dim list = TryParseContacts(text)
            If list Is Nothing OrElse list.Count < 2 Then
                LastError = "Sample fixture parse failed."
                Return False
            End If
            If Not FixtureContainsEmail(list, "alice@example.com") Then
                LastError = "Sample fixture missing alice@example.com"
                Return False
            End If
            Dim byteIdentical = False
            Dim destPath As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-sample-preserve.mab")
            If Not TryWriteMorkPreservingSource(text, list, destPath, byteIdentical) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Sample preserve export failed.")
                Return False
            End If
            If Not byteIdentical Then
                LastError = "Sample preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list2 = TryParseContacts(text2)
            If list2 Is Nothing OrElse list2.Count < 2 Then
                LastError = "Golden fixture parse failed."
                Return False
            End If
            If Not FixtureContainsEmail(list2, "carol@example.net") OrElse Not FixtureContainsEmail(list2, "dave@example.com") Then
                LastError = "Golden fixture missing expected contacts."
                Return False
            End If
            Dim list3 = TryParseContacts(text3)
            If list3 Is Nothing OrElse list3.Count < 2 Then
                LastError = "Profile fixture parse failed."
                Return False
            End If
            If Not FixtureContainsEmail(list3, "eve@example.com") OrElse Not FixtureContainsEmail(list3, "frank@example.net") Then
                LastError = "Profile fixture missing expected contacts."
                Return False
            End If
            If Not FixtureHasEmailOrSecondary(list3, "eve.alias@example.com") Then
                LastError = "Profile fixture missing SecondEmail."
                Return False
            End If
            Dim text20 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-golden-roundtrip.mab")
            Dim byteIdentical2 = False
            If Not TryWriteMorkPreservingSource(text2, list2, text20, byteIdentical2) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Golden preserve export failed.")
                Return False
            End If
            If Not byteIdentical2 Then
                LastError = "Golden preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list4 = TryParseContacts(text20)
            If list4 Is Nothing OrElse list4.Count < list2.Count Then
                LastError = "Golden export round-trip failed."
                Return False
            End If
            Dim list5 As List(Of MailContact) = New List(Of MailContact)(list2)
            For Each item In list5
                If item IsNot Nothing AndAlso String.Equals(item.Email, "carol@example.net", StringComparison.OrdinalIgnoreCase) Then
                    item.DisplayName = "Carol Updated"
                    Exit For
                End If
            Next
            Dim text21 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-golden-inplace.mab")
            Dim byteIdentical3 = False
            If Not TryWriteMorkPreservingSource(text2, list5, text21, byteIdentical3) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Golden in-place patch export failed.")
                Return False
            End If
            If byteIdentical3 Then
                LastError = "Golden in-place patch should change bytes when display name updates."
                Return False
            End If
            Dim list6 = TryParseContacts(text21)
            If list6 Is Nothing OrElse Not FixtureContactHasDisplayName(list6, "carol@example.net", "Carol Updated") Then
                LastError = "Golden in-place patch round-trip failed."
                Return False
            End If
            Dim text22 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-profile-layout.mab")
            If Not TryWriteMorkFromSourceLayout(text3, list3, text22) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Profile layout re-encode failed.")
                Return False
            End If
            Dim list7 = TryParseContacts(text22)
            If list7 Is Nothing OrElse list7.Count < list3.Count Then
                LastError = "Profile layout round-trip failed."
                Return False
            End If
            If Not FixtureHasEmailOrSecondary(list7, "eve.alias@example.com") Then
                LastError = "Profile layout export lost SecondEmail."
                Return False
            End If
            Dim text23 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-golden-layout.mab")
            If Not TryWriteMorkFromSourceLayout(text2, list2, text23) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Golden layout re-encode failed.")
                Return False
            End If
            Dim list8 = TryParseContacts(text23)
            If list8 Is Nothing OrElse Not FixtureContactHasFirstName(list8, "carol@example.net", "Carol") Then
                LastError = "Golden layout export lost FirstName column."
                Return False
            End If
            Dim text24 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-ldif-roundtrip.ldif")
            If Not ExportContactsToLdif(list, text24) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "LDIF export failed.")
                Return False
            End If
            Dim list9 = TryParseContactsFromLdif(text24)
            If list9 Is Nothing OrElse list9.Count < list.Count Then
                LastError = "LDIF round-trip parse failed."
                Return False
            End If
            If Not FixtureContainsEmail(list9, "alice@example.com") Then
                LastError = "LDIF round-trip missing alice@example.com"
                Return False
            End If
            Dim list10 = TryParseContacts(text4)
            If list10 Is Nothing OrElse list10.Count < 2 Then
                LastError = "Mixed-table fixture parse failed."
                Return False
            End If
            If Not FixtureContainsEmail(list10, "mixed@example.com") OrElse Not FixtureContainsEmail(list10, "alt@example.net") Then
                LastError = "Mixed-table fixture missing expected contacts."
                Return False
            End If
            Dim destPath2 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-mixed-preserve.mab")
            Dim byteIdentical4 = False
            If Not TryWriteMorkPreservingSource(text4, list10, destPath2, byteIdentical4) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Mixed-table preserve export failed.")
                Return False
            End If
            If Not byteIdentical4 Then
                LastError = "Mixed-table preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim text25 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-mixed-layout.mab")
            If Not TryWriteMorkFromSourceLayout(text4, list10, text25) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Mixed-table layout re-encode failed.")
                Return False
            End If
            Dim list11 = TryParseContacts(text25)
            If list11 Is Nothing OrElse Not FixtureContainsEmail(list11, "alt@example.net") Then
                LastError = "Mixed-table layout round-trip failed."
                Return False
            End If
            Dim list12 = TryParseContacts(text5)
            If list12 Is Nothing OrElse list12.Count < 1 Then
                LastError = "Phones fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasPhones(list12, "phones@example.com", "555-0100", "555-0199") Then
                LastError = "Phones fixture missing WorkPhone/HomePhone."
                Return False
            End If
            Dim destPath3 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-phones-preserve.mab")
            Dim byteIdentical5 = False
            If Not TryWriteMorkPreservingSource(text5, list12, destPath3, byteIdentical5) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Phones preserve export failed.")
                Return False
            End If
            If Not byteIdentical5 Then
                LastError = "Phones preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list13 = TryParseContacts(text6)
            If list13 Is Nothing OrElse Not FixtureContainsEmail(list13, "rowref@example.com") Then
                LastError = "Row-ref fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasNotes(list13, "rowref@example.com", "Call before noon") Then
                LastError = "Row-ref fixture missing Notes."
                Return False
            End If
            Dim destPath4 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-rowref-preserve.mab")
            Dim byteIdentical6 = False
            If Not TryWriteMorkPreservingSource(text6, list13, destPath4, byteIdentical6) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Row-ref preserve export failed.")
                Return False
            End If
            If Not byteIdentical6 Then
                LastError = "Row-ref preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list14 = TryParseContacts(text7)
            If list14 Is Nothing OrElse Not FixtureContainsEmail(list14, "company@example.com") Then
                LastError = "Company fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasCompany(list14, "company@example.com", "Acme Corp") Then
                LastError = "Company fixture missing Company column."
                Return False
            End If
            Dim destPath5 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-company-preserve.mab")
            Dim byteIdentical7 = False
            If Not TryWriteMorkPreservingSource(text7, list14, destPath5, byteIdentical7) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Company preserve export failed.")
                Return False
            End If
            If Not byteIdentical7 Then
                LastError = "Company preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list15 = TryParseContacts(text8)
            If list15 Is Nothing OrElse Not FixtureContainsEmail(list15, "nick@example.com") Then
                LastError = "Nick fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasNickName(list15, "nick@example.com", "Nicky") Then
                LastError = "Nick fixture missing NickName column."
                Return False
            End If
            Dim destPath6 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-nick-preserve.mab")
            Dim byteIdentical8 = False
            If Not TryWriteMorkPreservingSource(text8, list15, destPath6, byteIdentical8) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Nick preserve export failed.")
                Return False
            End If
            If Not byteIdentical8 Then
                LastError = "Nick preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list16 = TryParseContacts(text9)
            If list16 Is Nothing OrElse Not FixtureContainsEmail(list16, "job@example.com") Then
                LastError = "JobTitle fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasJobTitle(list16, "job@example.com", "Chief Engineer") Then
                LastError = "JobTitle fixture missing JobTitle column."
                Return False
            End If
            Dim destPath7 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-jobtitle-preserve.mab")
            Dim byteIdentical9 = False
            If Not TryWriteMorkPreservingSource(text9, list16, destPath7, byteIdentical9) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "JobTitle preserve export failed.")
                Return False
            End If
            If Not byteIdentical9 Then
                LastError = "JobTitle preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list17 = TryParseContacts(text10)
            If list17 Is Nothing OrElse Not FixtureContainsEmail(list17, "dept@example.com") Then
                LastError = "Department fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasDepartment(list17, "dept@example.com", "Engineering") Then
                LastError = "Department fixture missing Department column."
                Return False
            End If
            Dim destPath8 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-department-preserve.mab")
            Dim byteIdentical10 = False
            If Not TryWriteMorkPreservingSource(text10, list17, destPath8, byteIdentical10) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Department preserve export failed.")
                Return False
            End If
            If Not byteIdentical10 Then
                LastError = "Department preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list18 = TryParseContacts(text11)
            If list18 Is Nothing OrElse Not FixtureContainsEmail(list18, "web@example.com") Then
                LastError = "WebPage fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasWebPage(list18, "web@example.com", "https://example.com/~web") Then
                LastError = "WebPage fixture missing WebPage1 column."
                Return False
            End If
            Dim destPath9 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-webpage-preserve.mab")
            Dim byteIdentical11 = False
            If Not TryWriteMorkPreservingSource(text11, list18, destPath9, byteIdentical11) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "WebPage preserve export failed.")
                Return False
            End If
            If Not byteIdentical11 Then
                LastError = "WebPage preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list19 = TryParseContacts(text12)
            If list19 Is Nothing OrElse Not FixtureContainsEmail(list19, "fax@example.com") Then
                LastError = "Fax fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasFaxNumber(list19, "fax@example.com", "555-0142") Then
                LastError = "Fax fixture missing FaxNumber column."
                Return False
            End If
            Dim destPath10 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-fax-preserve.mab")
            Dim byteIdentical12 = False
            If Not TryWriteMorkPreservingSource(text12, list19, destPath10, byteIdentical12) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Fax preserve export failed.")
                Return False
            End If
            If Not byteIdentical12 Then
                LastError = "Fax preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list20 = TryParseContacts(text13)
            If list20 Is Nothing OrElse Not FixtureContainsEmail(list20, "pager@example.com") Then
                LastError = "Pager fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasPagerNumber(list20, "pager@example.com", "555-0199") Then
                LastError = "Pager fixture missing PagerNumber column."
                Return False
            End If
            Dim destPath11 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-pager-preserve.mab")
            Dim byteIdentical13 = False
            If Not TryWriteMorkPreservingSource(text13, list20, destPath11, byteIdentical13) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Pager preserve export failed.")
                Return False
            End If
            If Not byteIdentical13 Then
                LastError = "Pager preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list21 = TryParseContacts(text14)
            If list21 Is Nothing OrElse Not FixtureContainsEmail(list21, "custom@example.com") Then
                LastError = "Custom1 fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasCustom1(list21, "custom@example.com", "VIP-42") Then
                LastError = "Custom1 fixture missing Custom1 column."
                Return False
            End If
            Dim destPath12 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-custom1-preserve.mab")
            Dim byteIdentical14 = False
            If Not TryWriteMorkPreservingSource(text14, list21, destPath12, byteIdentical14) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Custom1 preserve export failed.")
                Return False
            End If
            If Not byteIdentical14 Then
                LastError = "Custom1 preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list22 = TryParseContacts(text15)
            If list22 Is Nothing OrElse Not FixtureContainsEmail(list22, "custom2@example.com") Then
                LastError = "Custom2 fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasCustom2(list22, "custom2@example.com", "TAG-99") Then
                LastError = "Custom2 fixture missing Custom2 column."
                Return False
            End If
            Dim destPath13 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-custom2-preserve.mab")
            Dim byteIdentical15 = False
            If Not TryWriteMorkPreservingSource(text15, list22, destPath13, byteIdentical15) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Custom2 preserve export failed.")
                Return False
            End If
            If Not byteIdentical15 Then
                LastError = "Custom2 preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list23 = TryParseContacts(text16)
            If list23 Is Nothing OrElse Not FixtureContainsEmail(list23, "custom3@example.com") Then
                LastError = "Custom3 fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasCustom3(list23, "custom3@example.com", "RANK-A") Then
                LastError = "Custom3 fixture missing Custom3 column."
                Return False
            End If
            Dim destPath14 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-custom3-preserve.mab")
            Dim byteIdentical16 = False
            If Not TryWriteMorkPreservingSource(text16, list23, destPath14, byteIdentical16) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Custom3 preserve export failed.")
                Return False
            End If
            If Not byteIdentical16 Then
                LastError = "Custom3 preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list24 = TryParseContacts(text17)
            If list24 Is Nothing OrElse Not FixtureContainsEmail(list24, "custom4@example.com") Then
                LastError = "Custom4 fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasCustom4(list24, "custom4@example.com", "POOL-7") Then
                LastError = "Custom4 fixture missing Custom4 column."
                Return False
            End If
            Dim destPath15 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-custom4-preserve.mab")
            Dim byteIdentical17 = False
            If Not TryWriteMorkPreservingSource(text17, list24, destPath15, byteIdentical17) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Custom4 preserve export failed.")
                Return False
            End If
            If Not byteIdentical17 Then
                LastError = "Custom4 preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list25 = TryParseContacts(text18)
            If list25 Is Nothing OrElse Not FixtureContainsEmail(list25, "spouse@example.com") Then
                LastError = "SpouseName fixture parse failed."
                Return False
            End If
            If Not FixtureContactHasSpouseName(list25, "spouse@example.com", "Alex Partner") Then
                LastError = "SpouseName fixture missing SpouseName column."
                Return False
            End If
            Dim destPath16 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-spouse-preserve.mab")
            Dim byteIdentical18 = False
            If Not TryWriteMorkPreservingSource(text18, list25, destPath16, byteIdentical18) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "SpouseName preserve export failed.")
                Return False
            End If
            If Not byteIdentical18 Then
                LastError = "SpouseName preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim list26 = TryParseContacts(text19)
            If list26 Is Nothing OrElse list26.Count < 2 Then
                LastError = "Multi-table fixture parse failed."
                Return False
            End If
            If Not FixtureContainsEmail(list26, "alpha@example.com") OrElse Not FixtureContainsEmail(list26, "beta@example.com") Then
                LastError = "Multi-table fixture missing table rows."
                Return False
            End If
            Dim text26 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-multitable-layout.mab")
            If Not TryWriteMorkFromSourceLayout(text19, list26, text26) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Multi-table layout export failed.")
                Return False
            End If
            Dim text27 = File.ReadAllText(text26)
            Dim num = 0
            Dim startIndex = 0
            While True
                startIndex = text27.IndexOf("(k=", startIndex, StringComparison.Ordinal)
                If startIndex < 0 Then
                    Exit While
                End If
                num += 1
                startIndex += 3
            End While
            If num < 2 Then
                LastError = "Multi-table layout export dropped (k=) tables."
                Return False
            End If
            If text27.IndexOf("^70^", StringComparison.Ordinal) < 0 Then
                LastError = "Multi-table layout export lost second-table column ids."
                Return False
            End If
            Dim destPath17 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-multitable-preserve.mab")
            Dim byteIdentical19 = False
            If Not TryWriteMorkPreservingSource(text19, list26, destPath17, byteIdentical19) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Multi-table preserve export failed.")
                Return False
            End If
            If Not byteIdentical19 Then
                LastError = "Multi-table preserve export not byte-identical to source .mab."
                Return False
            End If
            Dim text28 As String = IO.Path.Combine(IO.Path.GetTempPath(), "lcars-mork-mixed-fullenc.mab")
            If Not TryWriteMorkFullEncoderFromSource(text4, list10, text28) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Mixed-table full encoder export failed.")
                Return False
            End If
            If CountMorkDataRows(File.ReadAllText(text28)) <> CountMorkDataRows(File.ReadAllText(text4)) Then
                LastError = "Mixed-table full encoder row count mismatch."
                Return False
            End If
            contactCount = list.Count + list2.Count + list3.Count + list10.Count + list12.Count + list13.Count + list14.Count + list15.Count + list16.Count + list17.Count + list18.Count + list19.Count + list20.Count + list21.Count + list22.Count + list23.Count + list24.Count + list25.Count + list26.Count
            Return True
        End Function

        Private Shared Function FixtureContainsEmail(contacts As List(Of MailContact), email As String) As Boolean
            For Each contact In contacts
                If contact IsNot Nothing AndAlso String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Return True
                End If
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasDisplayName(contacts As List(Of MailContact), email As String, displayName As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, "").Trim(), displayName, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasNickName(contacts As List(Of MailContact), email As String, nickName As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.NickName, Nothing), mailContact.NickName, "").Trim(), nickName, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasJobTitle(contacts As List(Of MailContact), email As String, jobTitle As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.JobTitle, Nothing), mailContact.JobTitle, "").Trim(), jobTitle, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasDepartment(contacts As List(Of MailContact), email As String, department As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.Department, Nothing), mailContact.Department, "").Trim(), department, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasWebPage(contacts As List(Of MailContact), email As String, webPage As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.WebPage, Nothing), mailContact.WebPage, "").Trim(), webPage, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasFaxNumber(contacts As List(Of MailContact), email As String, faxNumber As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.FaxNumber, Nothing), mailContact.FaxNumber, "").Trim(), faxNumber, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasPagerNumber(contacts As List(Of MailContact), email As String, pagerNumber As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.PagerNumber, Nothing), mailContact.PagerNumber, "").Trim(), pagerNumber, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasCustom1(contacts As List(Of MailContact), email As String, custom1 As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.Custom1, Nothing), mailContact.Custom1, "").Trim(), custom1, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasCustom2(contacts As List(Of MailContact), email As String, custom2 As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.Custom2, Nothing), mailContact.Custom2, "").Trim(), custom2, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasCustom3(contacts As List(Of MailContact), email As String, custom3 As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.Custom3, Nothing), mailContact.Custom3, "").Trim(), custom3, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasCustom4(contacts As List(Of MailContact), email As String, custom4 As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.Custom4, Nothing), mailContact.Custom4, "").Trim(), custom4, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasSpouseName(contacts As List(Of MailContact), email As String, spouseName As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.SpouseName, Nothing), mailContact.SpouseName, "").Trim(), spouseName, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasCompany(contacts As List(Of MailContact), email As String, company As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.Company, Nothing), mailContact.Company, "").Trim(), company, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasNotes(contacts As List(Of MailContact), email As String, notes As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.Notes, Nothing), mailContact.Notes, "").Trim(), notes, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasPhones(contacts As List(Of MailContact), email As String, workPhone As String, homePhone As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Dim result As Integer
                If String.Equals(If(Not Equals(mailContact.WorkPhone, Nothing), mailContact.WorkPhone, "").Trim(), workPhone, StringComparison.OrdinalIgnoreCase) Then
                    Dim mailContact2 = contact
                    If String.Equals(If(Not Equals(mailContact2.HomePhone, Nothing), mailContact2.HomePhone, "").Trim(), homePhone, StringComparison.OrdinalIgnoreCase) Then
                        result = 1
                        GoTo IL_007a
                    End If
                End If
                result = 0
                GoTo IL_007a
IL_007a:
                Return CByte(result) <> 0
            Next
            Return False
        End Function

        Private Shared Function FixtureContactHasFirstName(contacts As List(Of MailContact), email As String, firstName As String) As Boolean
            For Each contact In contacts
                If contact Is Nothing OrElse Not String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim mailContact = contact
                Return String.Equals(If(Not Equals(mailContact.FirstName, Nothing), mailContact.FirstName, "").Trim(), firstName, StringComparison.OrdinalIgnoreCase)
            Next
            Return False
        End Function

        Private Shared Function FixtureHasEmailOrSecondary(contacts As List(Of MailContact), email As String) As Boolean
            For Each contact In contacts
                If contact IsNot Nothing Then
                    If String.Equals(contact.Email, email, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                    If String.Equals(contact.SecondaryEmail, email, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                End If
            Next
            Return False
        End Function

        Public Shared Function ExportContactsToVCardFile(contacts As List(Of MailContact), destPath As String) As Boolean
            LastError = Nothing
            Dim result As Boolean
            If contacts Is Nothing OrElse String.IsNullOrWhiteSpace(destPath) Then
                result = False
            Else
                Try
                    Dim stringBuilder As StringBuilder = New StringBuilder()
                    For Each contact In contacts
                        If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                            stringBuilder.AppendLine("BEGIN:VCARD")
                            stringBuilder.AppendLine("VERSION:3.0")
                            Dim mailContact = contact
                            stringBuilder.AppendLine("FN:" & If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, contact.Email))
                            stringBuilder.AppendLine("EMAIL:" & contact.Email)
                            If Not String.IsNullOrWhiteSpace(contact.Company) Then
                                stringBuilder.AppendLine("ORG:" & contact.Company)
                            End If
                            If Not String.IsNullOrWhiteSpace(contact.JobTitle) Then
                                stringBuilder.AppendLine("TITLE:" & contact.JobTitle)
                            End If
                            If Not String.IsNullOrWhiteSpace(contact.WebPage) Then
                                stringBuilder.AppendLine("URL:" & contact.WebPage)
                            End If
                            If Not String.IsNullOrWhiteSpace(contact.WorkPhone) Then
                                stringBuilder.AppendLine("TEL;TYPE=WORK:" & contact.WorkPhone)
                            End If
                            If Not String.IsNullOrWhiteSpace(contact.HomePhone) Then
                                stringBuilder.AppendLine("TEL;TYPE=HOME:" & contact.HomePhone)
                            End If
                            If Not String.IsNullOrWhiteSpace(contact.Notes) Then
                                stringBuilder.AppendLine("NOTE:" & contact.Notes.Replace(vbLf, "\n"))
                            End If
                            stringBuilder.AppendLine("END:VCARD")
                        End If
                    Next
                    Call File.WriteAllText(destPath, stringBuilder.ToString(), Encoding.UTF8)
                    result = True
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function
    End Class

