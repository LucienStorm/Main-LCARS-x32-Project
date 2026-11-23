Imports System
Imports System.Collections.Generic
Imports System.Data.SQLite
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    <StandardModule>
    Public NotInheritable Class AddressBookStore
        Private Class CardDavSettingsDto
            Public Property ServerUrl As String

            Public Property Username As String

            Public Property ConflictOn412Mode As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class CardDavSecondaryAccountDto
            Public Property ServerUrl As String

            Public Property Username As String

            Public Property BookKey As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class ContactDto
            Public Property DisplayName As String

            Public Property Email As String

            Public Property Company As String

            Public Property WorkPhone As String

            Public Property HomePhone As String

            Public Property Notes As String

            Public Property CardDavEtag As String

            Public Property SecondaryEmail As String

            Public Property FirstName As String

            Public Property LastName As String

            Public Property NickName As String

            Public Property JobTitle As String

            Public Property Department As String

            Public Property MobilePhone As String

            Public Property WebPage As String

            Public Property FaxNumber As String

            Public Property PagerNumber As String

            Public Property WorkAddress As String

            Public Property OfficeLocation As String

            Public Property HomeAddress As String

            Public Property Custom1 As String

            Public Property Custom2 As String

            Public Property Custom3 As String

            Public Property Custom4 As String

            Public Property MiddleName As String

            Public Property Birthday As String

            Public Property OtherPhone As String

            Public Property SpouseName As String

            Public Property Anniversary As String

            Public Property AssistantName As String

            Public Property ScreenName As String

            Public Property AimScreenName As String

            Public Property PopularityIndex As String

            Public Property RecordName As String

            Public Property PreferEmail As String

            Public Property MorkRowId As String

            Public Property CardDavDirty As Boolean

            Public Property IsGalEntry As Boolean

            Public Property GalPushDirty As Boolean

            Public Property GalPersonaId As String

            Public Property GalPersonaChangeKey As String

            Public Property GalDirectoryItemId As String

            Public Property GalDirectoryChangeKey As String

            Public Property EwsPersonalItemId As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub

            Public Shared Function FromModel(contact As MailContact) As ContactDto
                Dim contactDto As ContactDto = New ContactDto()
                contactDto.DisplayName = If(Not Equals(contact.DisplayName, Nothing), contact.DisplayName, "")
                contactDto.Email = If(Not Equals(contact.Email, Nothing), contact.Email, "")
                contactDto.Company = If(Not Equals(contact.Company, Nothing), contact.Company, "")
                contactDto.WorkPhone = If(Not Equals(contact.WorkPhone, Nothing), contact.WorkPhone, "")
                contactDto.HomePhone = If(Not Equals(contact.HomePhone, Nothing), contact.HomePhone, "")
                contactDto.Notes = If(Not Equals(contact.Notes, Nothing), contact.Notes, "")
                contactDto.CardDavEtag = If(Not Equals(contact.CardDavEtag, Nothing), contact.CardDavEtag, "")
                contactDto.SecondaryEmail = If(Not Equals(contact.SecondaryEmail, Nothing), contact.SecondaryEmail, "")
                contactDto.FirstName = If(Not Equals(contact.FirstName, Nothing), contact.FirstName, "")
                contactDto.LastName = If(Not Equals(contact.LastName, Nothing), contact.LastName, "")
                contactDto.NickName = If(Not Equals(contact.NickName, Nothing), contact.NickName, "")
                contactDto.JobTitle = If(Not Equals(contact.JobTitle, Nothing), contact.JobTitle, "")
                contactDto.Department = If(Not Equals(contact.Department, Nothing), contact.Department, "")
                contactDto.MobilePhone = If(Not Equals(contact.MobilePhone, Nothing), contact.MobilePhone, "")
                contactDto.WebPage = If(Not Equals(contact.WebPage, Nothing), contact.WebPage, "")
                contactDto.FaxNumber = If(Not Equals(contact.FaxNumber, Nothing), contact.FaxNumber, "")
                contactDto.PagerNumber = If(Not Equals(contact.PagerNumber, Nothing), contact.PagerNumber, "")
                contactDto.WorkAddress = If(Not Equals(contact.WorkAddress, Nothing), contact.WorkAddress, "")
                contactDto.OfficeLocation = If(Not Equals(contact.OfficeLocation, Nothing), contact.OfficeLocation, "")
                contactDto.HomeAddress = If(Not Equals(contact.HomeAddress, Nothing), contact.HomeAddress, "")
                contactDto.Custom1 = If(Not Equals(contact.Custom1, Nothing), contact.Custom1, "")
                contactDto.Custom2 = If(Not Equals(contact.Custom2, Nothing), contact.Custom2, "")
                contactDto.Custom3 = If(Not Equals(contact.Custom3, Nothing), contact.Custom3, "")
                contactDto.Custom4 = If(Not Equals(contact.Custom4, Nothing), contact.Custom4, "")
                contactDto.MiddleName = If(Not Equals(contact.MiddleName, Nothing), contact.MiddleName, "")
                contactDto.Birthday = If(Not Equals(contact.Birthday, Nothing), contact.Birthday, "")
                contactDto.OtherPhone = If(Not Equals(contact.OtherPhone, Nothing), contact.OtherPhone, "")
                contactDto.SpouseName = If(Not Equals(contact.SpouseName, Nothing), contact.SpouseName, "")
                contactDto.Anniversary = If(Not Equals(contact.Anniversary, Nothing), contact.Anniversary, "")
                contactDto.AssistantName = If(Not Equals(contact.AssistantName, Nothing), contact.AssistantName, "")
                contactDto.ScreenName = If(Not Equals(contact.ScreenName, Nothing), contact.ScreenName, "")
                contactDto.AimScreenName = If(Not Equals(contact.AimScreenName, Nothing), contact.AimScreenName, "")
                contactDto.PopularityIndex = If(Not Equals(contact.PopularityIndex, Nothing), contact.PopularityIndex, "")
                contactDto.RecordName = If(Not Equals(contact.RecordName, Nothing), contact.RecordName, "")
                contactDto.PreferEmail = If(Not Equals(contact.PreferEmail, Nothing), contact.PreferEmail, "")
                contactDto.MorkRowId = If(Not Equals(contact.MorkRowId, Nothing), contact.MorkRowId, "")
                contactDto.CardDavDirty = contact.CardDavDirty
                contactDto.IsGalEntry = contact.IsGalEntry
                contactDto.GalPushDirty = contact.GalPushDirty
                contactDto.GalPersonaId = If(Not Equals(contact.GalPersonaId, Nothing), contact.GalPersonaId, "")
                contactDto.GalPersonaChangeKey = If(Not Equals(contact.GalPersonaChangeKey, Nothing), contact.GalPersonaChangeKey, "")
                contactDto.GalDirectoryItemId = If(Not Equals(contact.GalDirectoryItemId, Nothing), contact.GalDirectoryItemId, "")
                contactDto.GalDirectoryChangeKey = If(Not Equals(contact.GalDirectoryChangeKey, Nothing), contact.GalDirectoryChangeKey, "")
                contactDto.EwsPersonalItemId = If(Not Equals(contact.EwsPersonalItemId, Nothing), contact.EwsPersonalItemId, "")
                Return contactDto
            End Function

            Public Function ToModel() As MailContact
                Dim mailContact As MailContact = New MailContact()
                Dim displayName = Me.DisplayName
                mailContact.DisplayName = If(Not Equals(displayName, Nothing), displayName, "")
                Dim email = Me.Email
                mailContact.Email = If(Not Equals(email, Nothing), email, "")
                Dim company = Me.Company
                mailContact.Company = If(Not Equals(company, Nothing), company, "")
                Dim workPhone = Me.WorkPhone
                mailContact.WorkPhone = If(Not Equals(workPhone, Nothing), workPhone, "")
                Dim homePhone = Me.HomePhone
                mailContact.HomePhone = If(Not Equals(homePhone, Nothing), homePhone, "")
                Dim notes = Me.Notes
                mailContact.Notes = If(Not Equals(notes, Nothing), notes, "")
                Dim cardDavEtag = Me.CardDavEtag
                mailContact.CardDavEtag = If(Not Equals(cardDavEtag, Nothing), cardDavEtag, "")
                Dim secondaryEmail = Me.SecondaryEmail
                mailContact.SecondaryEmail = If(Not Equals(secondaryEmail, Nothing), secondaryEmail, "")
                Dim firstName = Me.FirstName
                mailContact.FirstName = If(Not Equals(firstName, Nothing), firstName, "")
                Dim lastName = Me.LastName
                mailContact.LastName = If(Not Equals(lastName, Nothing), lastName, "")
                Dim nickName = Me.NickName
                mailContact.NickName = If(Not Equals(nickName, Nothing), nickName, "")
                Dim jobTitle = Me.JobTitle
                mailContact.JobTitle = If(Not Equals(jobTitle, Nothing), jobTitle, "")
                Dim department = Me.Department
                mailContact.Department = If(Not Equals(department, Nothing), department, "")
                Dim mobilePhone = Me.MobilePhone
                mailContact.MobilePhone = If(Not Equals(mobilePhone, Nothing), mobilePhone, "")
                Dim webPage = Me.WebPage
                mailContact.WebPage = If(Not Equals(webPage, Nothing), webPage, "")
                Dim faxNumber = Me.FaxNumber
                mailContact.FaxNumber = If(Not Equals(faxNumber, Nothing), faxNumber, "")
                Dim pagerNumber = Me.PagerNumber
                mailContact.PagerNumber = If(Not Equals(pagerNumber, Nothing), pagerNumber, "")
                Dim workAddress = Me.WorkAddress
                mailContact.WorkAddress = If(Not Equals(workAddress, Nothing), workAddress, "")
                Dim officeLocation = Me.OfficeLocation
                mailContact.OfficeLocation = If(Not Equals(officeLocation, Nothing), officeLocation, "")
                Dim homeAddress = Me.HomeAddress
                mailContact.HomeAddress = If(Not Equals(homeAddress, Nothing), homeAddress, "")
                Dim custom = Custom1
                mailContact.Custom1 = If(Not Equals(custom, Nothing), custom, "")
                Dim custom2 = Me.Custom2
                mailContact.Custom2 = If(Not Equals(custom2, Nothing), custom2, "")
                Dim custom3 = Me.Custom3
                mailContact.Custom3 = If(Not Equals(custom3, Nothing), custom3, "")
                Dim custom4 = Me.Custom4
                mailContact.Custom4 = If(Not Equals(custom4, Nothing), custom4, "")
                Dim middleName = Me.MiddleName
                mailContact.MiddleName = If(Not Equals(middleName, Nothing), middleName, "")
                Dim birthday = Me.Birthday
                mailContact.Birthday = If(Not Equals(birthday, Nothing), birthday, "")
                Dim otherPhone = Me.OtherPhone
                mailContact.OtherPhone = If(Not Equals(otherPhone, Nothing), otherPhone, "")
                Dim spouseName = Me.SpouseName
                mailContact.SpouseName = If(Not Equals(spouseName, Nothing), spouseName, "")
                Dim anniversary = Me.Anniversary
                mailContact.Anniversary = If(Not Equals(anniversary, Nothing), anniversary, "")
                Dim assistantName = Me.AssistantName
                mailContact.AssistantName = If(Not Equals(assistantName, Nothing), assistantName, "")
                Dim screenName = Me.ScreenName
                mailContact.ScreenName = If(Not Equals(screenName, Nothing), screenName, "")
                Dim aimScreenName = Me.AimScreenName
                mailContact.AimScreenName = If(Not Equals(aimScreenName, Nothing), aimScreenName, "")
                Dim popularityIndex = Me.PopularityIndex
                mailContact.PopularityIndex = If(Not Equals(popularityIndex, Nothing), popularityIndex, "")
                Dim recordName = Me.RecordName
                mailContact.RecordName = If(Not Equals(recordName, Nothing), recordName, "")
                Dim preferEmail = Me.PreferEmail
                mailContact.PreferEmail = If(Not Equals(preferEmail, Nothing), preferEmail, "")
                Dim morkRowId = Me.MorkRowId
                mailContact.MorkRowId = If(Not Equals(morkRowId, Nothing), morkRowId, "")
                mailContact.CardDavDirty = CardDavDirty
                mailContact.IsGalEntry = IsGalEntry
                mailContact.GalPushDirty = GalPushDirty
                Dim galPersonaId = Me.GalPersonaId
                mailContact.GalPersonaId = If(Not Equals(galPersonaId, Nothing), galPersonaId, "")
                Dim galPersonaChangeKey = Me.GalPersonaChangeKey
                mailContact.GalPersonaChangeKey = If(Not Equals(galPersonaChangeKey, Nothing), galPersonaChangeKey, "")
                Dim galDirectoryItemId = Me.GalDirectoryItemId
                mailContact.GalDirectoryItemId = If(Not Equals(galDirectoryItemId, Nothing), galDirectoryItemId, "")
                Dim galDirectoryChangeKey = Me.GalDirectoryChangeKey
                mailContact.GalDirectoryChangeKey = If(Not Equals(galDirectoryChangeKey, Nothing), galDirectoryChangeKey, "")
                Dim ewsPersonalItemId = Me.EwsPersonalItemId
                mailContact.EwsPersonalItemId = If(Not Equals(ewsPersonalItemId, Nothing), ewsPersonalItemId, "")
                Return mailContact
            End Function
        End Class

        Private Const ContactsFileName As String = "contacts.json"

        Private Const AbookFileName As String = "abook.sqlite"

        Private Const AbookMabFileName As String = "abook.mab"

        Private Const CardDavSettingsFileName As String = "carddav-settings.json"

        Private Const CardDavSecondaryAccountsFileName As String = "carddav-secondary-accounts.json"

        Private Const CardDavSecretKey As String = "carddav"

        Private Const SourceMabPathFileName As String = "abook-source-mab.txt"

        Private Const ImportSql As String = "SELECT c.id, TRIM(COALESCE(c.name, '')) AS card_name, TRIM(p.value) AS email FROM cards c INNER JOIN properties p ON p.card_id = c.id WHERE p.name = 'PrimaryEmail' AND TRIM(COALESCE(p.value, '')) <> '' ORDER BY card_name, email;"

        Public Shared LastError As String

        Public Shared LastCardDavPushCount As Integer

        Public Shared LastCardDavRemotePullCount As Integer = 0

        Public Shared LastCardDavSecondaryBooksMerged As Integer = 0

        Public Shared LastCardDavDiscoveredBooks As Integer = 0

        Public Shared LastCardDavBidirectionalUtc As Date = Date.MinValue

        Public Shared LastGalPersonalPushCount As Integer

        Public Shared LastImportSkipped As Boolean

        Public Shared LastCardDavLiveOverlayCount As Integer = 0

        Public Shared LastCardDav412BatchResolved As Integer = 0

        Public Shared Function ContactsJsonPath() As String
            Return Path.Combine(AccountStore.DataRoot(), "contacts.json")
        End Function

        Public Shared Function LoadContacts() As List(Of MailContact)
            Dim path As String = ContactsJsonPath()
            Dim result As List(Of MailContact)
            If Not File.Exists(path) Then
                result = New List(Of MailContact)()
            Else
                Try
                    Dim input = File.ReadAllText(path, Encoding.UTF8)
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of ContactDto))(input)
                    If list Is Nothing Then
                        result = New List(Of MailContact)()
                    Else
                        Dim list2 As List(Of MailContact) = New List(Of MailContact)()
                        For Each item In list
                            If item IsNot Nothing Then
                                Dim mailContact As MailContact = item.ToModel()
                                If mailContact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(mailContact.Email) Then
                                    list2.Add(mailContact)
                                End If
                            End If
                        Next
                        result = list2
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of MailContact)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryImportFromProfile(profileDir As String) As Integer
            LastError = Nothing
            LastImportSkipped = False
            Dim result As Integer
            If String.IsNullOrWhiteSpace(profileDir) Then
                LastError = "Profile folder is required."
                result = -1
            Else
                Dim path As String = profileDir.Trim()
                Dim list As List(Of MailContact) = Nothing
                Dim text = IO.Path.Combine(path, "abook.sqlite")
                If File.Exists(text) Then
                    Try
                        list = ReadContactsFromAbookSqlite(text)
                    Catch ex As Exception
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = ex2.Message
                        result = -1
                        Call ProjectData.ClearProjectError()
                        GoTo IL_017e
                    End Try
                End If
                If list Is Nothing Then
                    list = New List(Of MailContact)()
                End If
                Dim text2 = IO.Path.Combine(path, "abook.mab")
                If File.Exists(text2) Then
                    TrySaveSourceMabPath(text2)
                    MorkAbookParser.TryValidateSourceMabByteRoundTrip(text2)
                    Dim list2 = MorkAbookParser.TryParseContacts(text2)
                    If list2 IsNot Nothing AndAlso list2.Count > 0 Then
                        list = MergeContacts(list, list2)
                    ElseIf list.Count = 0 AndAlso Not String.IsNullOrEmpty(MorkAbookParser.LastError) Then
                        LastError = MorkAbookParser.LastError
                        result = -1
                        GoTo IL_017e
                    End If
                ElseIf list.Count = 0 Then
                    Dim text3 = IO.Path.Combine(path, "abook.ldif")
                    If File.Exists(text3) Then
                        Dim list3 = MorkAbookParser.TryParseContactsFromLdif(text3)
                        If list3 IsNot Nothing AndAlso list3.Count > 0 Then
                            list = list3
                        End If
                    End If
                    If list.Count = 0 Then
                        LastImportSkipped = True
                        result = 0
                        GoTo IL_017e
                    End If
                End If
                If Not SaveContacts(list) Then
                    result = -1
                Else
                    If File.Exists(text2) Then
                        MorkProfileAcceptanceStore.RecordProfileImport(text2, list.Count, MorkAbookParser.LastSourceMabRoundTripValidated, MorkAbookParser.LastSourceMabRoundTripByteIdentical, MorkAbookParser.LastError)
                    End If
                    result = list.Count
                End If
            End If
            GoTo IL_017e
IL_017e:
            Return result
        End Function

        Public Shared Function TryExportContactsLdif(destPath As String) As Boolean
            LastError = Nothing
            Return MorkAbookParser.ExportContactsToLdif(LoadContacts(), destPath)
        End Function

        Public Shared Function TryExportContactsVCard(destPath As String) As Boolean
            LastError = Nothing
            Return MorkAbookParser.ExportContactsToVCardFile(LoadContacts(), destPath)
        End Function

        Public Shared Function TryExportContactsMork(destPath As String) As Boolean
            LastError = Nothing
            Dim contactCount = 0
            If Not MorkAbookParser.TryValidateThunderbirdFixture(contactCount) Then
                LastError = If(Not Equals(MorkAbookParser.LastError, Nothing), MorkAbookParser.LastError, "Mork golden self-test failed.")
                Return False
            End If
            Dim text As String = TryLoadSourceMabPath()
            Dim contacts As List(Of MailContact) = LoadContacts()
            If text.Length > 0 Then
                MorkAbookParser.TryValidateSourceMabByteRoundTrip(text)
                If MorkAbookParser.TryContactsUnchangedVersusSource(text, contacts) Then
                    If MorkAbookParser.TryExportByteIdenticalSourceCopy(text, contacts, destPath) AndAlso MorkAbookParser.TryVerifyExportBytesMatchSource(text, destPath) Then
                        Return True
                    End If
                    Dim byteIdentical = False
                    If MorkAbookParser.TryWriteMorkPreservingSource(text, contacts, destPath, byteIdentical) AndAlso byteIdentical Then
                        MorkAbookParser.LastMabExportByteIdentical = True
                        If MorkAbookParser.TryVerifyExportBytesMatchSource(text, destPath) Then
                            Return True
                        End If
                    End If
                    LastError = "Unchanged abook requires byte-identical .mab export (slice 13 REAL gate)."
                    Return False
                End If
                Dim byteIdentical2 = False
                If MorkAbookParser.TryWriteMorkPreservingSource(text, contacts, destPath, byteIdentical2) Then
                    MorkAbookParser.LastMabExportByteIdentical = byteIdentical2
                    If MorkAbookParser.TryVerifyExportBytesMatchSource(text, destPath) Then
                        Return True
                    End If
                End If
                If MorkAbookParser.TryWriteMorkFullEncoderFromSource(text, contacts, destPath) Then
                    LastError = "Edited contacts exported with full Mork encoder — not byte-identical TB .mab (slice 13 Open)."
                    Return True
                End If
                LastError = If(Not Equals(MorkAbookParser.LastError, Nothing), MorkAbookParser.LastError, "Mork export failed for edited contacts.")
                Return False
            End If
            Return MorkAbookParser.TryWriteMorkFromContacts(contacts, destPath)
        End Function

        Public Shared Function LoadLiveContacts() As List(Of MailContact)
            Return CardDavLiveBookCache.GetLiveContacts()
        End Function

        Public Shared Function TryFetchLiveCardDavContacts() As List(Of MailContact)
            LastError = Nothing
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCardDavSettings(serverUrl, username, password) Then
                Return Nothing
            End If
            Dim list = CardDavSyncService.FetchRemoteContacts(serverUrl, username, password)
            If list Is Nothing Then
                LastError = CardDavSyncService.LastError
                Return Nothing
            End If
            Dim list2 As List(Of MailContact) = New List(Of MailContact)()
            For Each item In list
                If item IsNot Nothing AndAlso item.Contact IsNot Nothing Then
                    If Not String.IsNullOrWhiteSpace(item.Etag) Then
                        item.Contact.CardDavEtag = item.Etag
                    End If
                    list2.Add(item.Contact)
                End If
            Next
            Return TryOverlayLocalCardDavEditsOnRemote(list2)
        End Function

        Public Shared Function TryOverlayLocalCardDavEditsOnRemote(remote As List(Of MailContact)) As List(Of MailContact)
            Dim dictionary As Dictionary(Of String, MailContact) = New Dictionary(Of String, MailContact)(StringComparer.OrdinalIgnoreCase)
            If remote IsNot Nothing Then
                For Each item In remote
                    If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                        dictionary(item.Email.Trim()) = item
                    End If
                Next
            End If
            For Each item2 In LoadContacts()
                If item2 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item2.Email) Then
                    Dim key As String = item2.Email.Trim()
                    If item2.CardDavDirty OrElse Not dictionary.ContainsKey(key) Then
                        dictionary(key) = item2
                    End If
                End If
            Next
            Dim list As List(Of MailContact) = New List(Of MailContact)(dictionary.Values)
            list.Sort(Function(a, b) String.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase))
            LastCardDavLiveOverlayCount = list.Count
            Return list
        End Function

        Public Shared Function TryRemoveContact(email As String, Optional deleteRemoteCardDav As Boolean = True) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(email, Nothing), email, "").Trim()
            If text.Length = 0 Then
                LastError = "Email is required."
                Return False
            End If
            Dim list As List(Of MailContact) = LoadContacts()
            If list.RemoveAll(Function(c) If(c IsNot Nothing AndAlso String.Equals(c.Email, text, StringComparison.OrdinalIgnoreCase), True, False)) <= 0 Then
                LastError = "Contact not found."
                Return False
            End If
            If Not SaveContacts(list) Then
                Return False
            End If
            If deleteRemoteCardDav Then
                TryDeleteContactCardDav(text)
            End If
            Return True
        End Function

        Public Shared Function TryDeleteContactCardDav(email As String) As Boolean
            LastError = Nothing
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCardDavSettings(serverUrl, username, password) Then
                Return False
            End If
            If Not CardDavSyncService.DeleteContact(serverUrl, username, password, email) Then
                LastError = CardDavSyncService.LastError
                Return False
            End If
            Return True
        End Function

        Public Shared Function TryPushContactCardDav(serverUrl As String, username As String, password As String, contact As MailContact) As Boolean
            LastError = Nothing
            If Not CardDavSyncService.PutContactAutoMerge(serverUrl, username, password, contact) Then
                LastError = CardDavSyncService.LastError
                Return False
            End If
            Dim list As List(Of MailContact) = New List(Of MailContact)()
            list.Add(contact)
            If TryMergeContacts(list) < 0 Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "Contact pushed but local save failed.")
                Return False
            End If
            SaveCardDavSettings(serverUrl, username, password)
            Return True
        End Function

        Public Shared Function TryPushAllLocalContactsCardDav(serverUrl As String, username As String, password As String) As Integer
            LastError = Nothing
            Dim num = 0
            Dim list As List(Of MailContact) = LoadContacts()
            list.Sort(Function(a, b)
                          If a Is Nothing AndAlso b Is Nothing Then
                              Return 0
                          End If
                          If a Is Nothing Then
                              Return 1
                          End If
                          If b Is Nothing Then
                              Return -1
                          End If
                          If a.CardDavDirty = b.CardDavDirty Then
                              Return String.Compare(a.Email, b.Email, StringComparison.OrdinalIgnoreCase)
                          End If
                          Return If(Not a.CardDavDirty, 1, -1)
                      End Function)
            Dim flag = False
            For Each item In list
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) AndAlso (item.CardDavDirty OrElse String.IsNullOrWhiteSpace(item.CardDavEtag)) AndAlso TryPushContactCardDav(serverUrl, username, password, item) Then
                    item.CardDavDirty = False
                    num = num + 1
                    flag = True
                End If
            Next
            If flag Then
                SaveContacts(list)
            End If
            Return num
        End Function

        Public Shared Function TryMergeContacts(incoming As List(Of MailContact)) As Integer
            LastError = Nothing
            If incoming Is Nothing OrElse incoming.Count = 0 Then
                Return 0
            End If
            Dim contacts As List(Of MailContact) = MergeContacts(LoadContacts(), incoming)
            If Not SaveContacts(contacts) Then
                Return -1
            End If
            Return incoming.Count
        End Function

        Public Shared Function TrySyncCardDav(serverUrl As String, username As String, password As String) As Integer
            LastError = Nothing
            Dim text As String = If(Not Equals(serverUrl, Nothing), serverUrl, "").Trim()
            If text.Length = 0 Then
                LastError = "CardDAV URL is required."
                Return -1
            End If
            Dim list = CardDavSyncService.FetchRemoteContacts(text, username, password)
            If list Is Nothing Then
                LastError = If(Not Equals(CardDavSyncService.LastError, Nothing), CardDavSyncService.LastError, "CardDAV sync failed.")
                Return -1
            End If
            Dim list2 As List(Of MailContact) = New List(Of MailContact)()
            For Each item In list
                If item IsNot Nothing AndAlso item.Contact IsNot Nothing Then
                    If Not String.IsNullOrWhiteSpace(item.Etag) Then
                        item.Contact.CardDavEtag = item.Etag
                    End If
                    list2.Add(item.Contact)
                End If
            Next
            Dim list3 As List(Of MailContact) = LoadContacts()
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item2 In list3
                If item2 IsNot Nothing AndAlso item2.CardDavDirty AndAlso Not String.IsNullOrWhiteSpace(item2.Email) Then
                    hashSet.Add(item2.Email.Trim())
                End If
            Next
            Dim list4 = MergeContacts(list3, list2)
            For Each item3 In list4
                If item3 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item3.Email) AndAlso Not String.IsNullOrWhiteSpace(item3.CardDavEtag) AndAlso Not hashSet.Contains(item3.Email.Trim()) Then
                    item3.CardDavDirty = False
                End If
            Next
            If Not SaveContacts(list4) Then
                Return -1
            End If
            LastCardDavRemotePullCount = list2.Count
            CardDavLiveBookCache.NotifyLocalBookUpdated(list4)
            SaveCardDavSettings(text, username, password)
            Return list4.Count
        End Function

        Private Shared Function TryMergeCardDavRemoteOnly(serverUrl As String, username As String, password As String) As Integer
            LastError = Nothing
            Dim text As String = If(Not Equals(serverUrl, Nothing), serverUrl, "").Trim()
            If text.Length = 0 Then
                Return 0
            End If
            Dim list = CardDavSyncService.FetchRemoteContacts(text, username, password)
            If list Is Nothing Then
                Return 0
            End If
            Dim list2 As List(Of MailContact) = New List(Of MailContact)()
            For Each item In list
                If item IsNot Nothing AndAlso item.Contact IsNot Nothing Then
                    If Not String.IsNullOrWhiteSpace(item.Etag) Then
                        item.Contact.CardDavEtag = item.Etag
                    End If
                    list2.Add(item.Contact)
                End If
            Next
            If list2.Count = 0 Then
                Return 0
            End If
            Dim list3 As List(Of MailContact) = LoadContacts()
            Dim hashSet As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item2 In list3
                If item2 IsNot Nothing AndAlso item2.CardDavDirty AndAlso Not String.IsNullOrWhiteSpace(item2.Email) Then
                    hashSet.Add(item2.Email.Trim())
                End If
            Next
            Dim list4 = MergeContacts(list3, list2)
            For Each item3 In list4
                If item3 IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item3.Email) AndAlso Not String.IsNullOrWhiteSpace(item3.CardDavEtag) AndAlso Not hashSet.Contains(item3.Email.Trim()) Then
                    item3.CardDavDirty = False
                End If
            Next
            If Not SaveContacts(list4) Then
                Return -1
            End If
            CardDavLiveBookCache.NotifyLocalBookUpdated(list4)
            Return list2.Count
        End Function

        Public Shared Sub SaveCardDavSettings(serverUrl As String, username As String, password As String, Optional conflictOn412Mode As String = Nothing)
            Dim text As String = If(Not Equals(serverUrl, Nothing), serverUrl, "").Trim()
            Dim username2 As String = If(Not Equals(username, Nothing), username, "").Trim()
            If text.Length = 0 Then
                Return
            End If
            Try
                Dim text2 As String = If(Not Equals(conflictOn412Mode, Nothing), conflictOn412Mode, CardDavSyncService.ConflictOn412Mode).Trim()
                If text2.Length = 0 Then
                    text2 = "ClientMerge"
                End If
                CardDavSyncService.ConflictOn412Mode = text2
                Dim cardDavSettingsDto As CardDavSettingsDto = New CardDavSettingsDto()
                cardDavSettingsDto.ServerUrl = text
                cardDavSettingsDto.Username = username2
                cardDavSettingsDto.ConflictOn412Mode = text2
                Dim obj = cardDavSettingsDto
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(CardDavSettingsPath(), javaScriptSerializer.Serialize(obj), Encoding.UTF8)
                If Not Equals(password, Nothing) Then
                    AccountStore.SavePassword("carddav", password)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryLoadCardDavSettings(ByRef serverUrl As String, ByRef username As String, ByRef password As String) As Boolean
            serverUrl = Nothing
            username = Nothing
            password = Nothing
            Dim path As String = CardDavSettingsPath()
            Dim result As Boolean
            If Not File.Exists(path) Then
                result = False
            Else
                Try
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim cardDavSettingsDto = javaScriptSerializer.Deserialize(Of CardDavSettingsDto)(File.ReadAllText(path, Encoding.UTF8))
                    If cardDavSettingsDto Is Nothing OrElse String.IsNullOrWhiteSpace(cardDavSettingsDto.ServerUrl) Then
                        result = False
                    Else
                        serverUrl = cardDavSettingsDto.ServerUrl.Trim()
                        Dim username2 = cardDavSettingsDto.Username
                        username = If(Not Equals(username2, Nothing), username2, "").Trim()
                        username2 = cardDavSettingsDto.ConflictOn412Mode
                        Dim text As String = If(Not Equals(username2, Nothing), username2, "").Trim()
                        If text.Length > 0 Then
                            CardDavSyncService.ConflictOn412Mode = text
                        End If
                        result = AccountStore.TryGetPassword("carddav", password)
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TrySyncStoredCardDav() As Integer
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCardDavSettings(serverUrl, username, password) Then
                Return 0
            End If
            Return TrySyncCardDav(serverUrl, username, password)
        End Function

        Public Shared Function TryApplyThunderbirdCardDavFromProfile(profileDir As String) As Integer
            LastError = Nothing
            Dim list = ThunderbirdCardDavImporter.TryLoadFromProfile(profileDir)
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            SaveThunderbirdCardDavSecondaryAccounts(list)
            Dim thunderbirdCardDavAccount = list(0)
            If thunderbirdCardDavAccount Is Nothing OrElse String.IsNullOrWhiteSpace(thunderbirdCardDavAccount.ServerUrl) Then
                Return 0
            End If
            Dim serverUrl = thunderbirdCardDavAccount.ServerUrl
            Dim thunderbirdCardDavAccount2 = thunderbirdCardDavAccount
            SaveCardDavSettings(serverUrl, If(Not Equals(thunderbirdCardDavAccount2.Username, Nothing), thunderbirdCardDavAccount2.Username, ""), Nothing)
            Return list.Count
        End Function

        Public Shared Sub SaveThunderbirdCardDavSecondaryAccounts(accounts As List(Of ThunderbirdCardDavAccount))
            If accounts Is Nothing OrElse accounts.Count = 0 Then
                Return
            End If
            Try
                Dim list As List(Of CardDavSecondaryAccountDto) = New List(Of CardDavSecondaryAccountDto)()
                Dim num = 0
                For Each account In accounts
                    If account IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(account.ServerUrl) Then
                        num = num + 1
                        Dim thunderbirdCardDavAccount = account
                        Dim text As String = If(Not Equals(thunderbirdCardDavAccount.BookKey, Nothing), thunderbirdCardDavAccount.BookKey, "").Trim()
                        If text.Length = 0 Then
                            text = "carddav-tb-" & num.ToString()
                        End If
                        Dim cardDavSecondaryAccountDto As CardDavSecondaryAccountDto = New CardDavSecondaryAccountDto With {
        .ServerUrl = account.ServerUrl.Trim()
    }
                        thunderbirdCardDavAccount = account
                        cardDavSecondaryAccountDto.Username = If(Not Equals(thunderbirdCardDavAccount.Username, Nothing), thunderbirdCardDavAccount.Username, "").Trim()
                        cardDavSecondaryAccountDto.BookKey = text
                        list.Add(cardDavSecondaryAccountDto)
                    End If
                Next
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Call File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "carddav-secondary-accounts.json"), javaScriptSerializer.Serialize(list), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryLoadThunderbirdCardDavSecondaryAccounts() As List(Of ThunderbirdCardDavAccount)
            Dim list As List(Of ThunderbirdCardDavAccount) = New List(Of ThunderbirdCardDavAccount)()
            Try
                Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "carddav-secondary-accounts.json")
                If Not File.Exists(path) Then
                    Return list
                End If
                Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                Dim list2 = javaScriptSerializer.Deserialize(Of List(Of CardDavSecondaryAccountDto))(File.ReadAllText(path, Encoding.UTF8))
                If list2 Is Nothing Then
                    Return list
                End If
                For Each item In list2
                    If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.ServerUrl) Then
                        Dim thunderbirdCardDavAccount As ThunderbirdCardDavAccount = New ThunderbirdCardDavAccount With {
        .ServerUrl = item.ServerUrl.Trim()
    }
                        Dim username = item.Username
                        thunderbirdCardDavAccount.Username = If(Not Equals(username, Nothing), username, "").Trim()
                        Dim bookKey = item.BookKey
                        thunderbirdCardDavAccount.BookKey = If(Not Equals(bookKey, Nothing), bookKey, "").Trim()
                        list.Add(thunderbirdCardDavAccount)
                    End If
                Next
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
            Return list
        End Function

        Public Shared Function TryRefreshDiscoveredCardDavBooksFromServer(serverUrl As String, username As String, password As String) As Integer
            LastCardDavDiscoveredBooks = 0
            Dim list = CardDavSyncService.EnumerateAddressBookUrls(serverUrl, username, password)
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            Dim list2 As List(Of ThunderbirdCardDavAccount) = New List(Of ThunderbirdCardDavAccount)()
            Dim num = 0
            For Each item In list
                If Not String.IsNullOrWhiteSpace(item) Then
                    num = num + 1
                    list2.Add(New ThunderbirdCardDavAccount With {
        .ServerUrl = item.Trim(),
        .Username = If(Not Equals(username, Nothing), username, "").Trim(),
        .BookKey = "carddav-disc-" & num.ToString()
    })
                End If
            Next
            SaveThunderbirdCardDavSecondaryAccounts(list2)
            LastCardDavDiscoveredBooks = list2.Count
            Return list2.Count
        End Function

        Public Shared Function TryMergeSecondaryThunderbirdCardDavBooks() As Integer
            LastCardDavSecondaryBooksMerged = 0
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCardDavSettings(serverUrl, username, password) Then
                Return 0
            End If
            If String.IsNullOrEmpty(password) Then
                Return 0
            End If
            Dim list As List(Of ThunderbirdCardDavAccount) = TryLoadThunderbirdCardDavSecondaryAccounts()
            If list Is Nothing OrElse list.Count = 0 Then
                Return 0
            End If
            Dim num = 0
            Dim num2 = 0
            For Each item In list
                If item Is Nothing OrElse String.IsNullOrWhiteSpace(item.ServerUrl) Then
                    Continue For
                End If
                num2 += 1
                If Not String.Equals(item.ServerUrl.Trim(), serverUrl.Trim(), StringComparison.OrdinalIgnoreCase) Then
                    Dim thunderbirdCardDavAccount = item
                    Dim text As String = If(Not Equals(thunderbirdCardDavAccount.Username, Nothing), thunderbirdCardDavAccount.Username, "").Trim()
                    If text.Length = 0 Then
                        text = username
                    End If
                    Dim password2 = password
                    thunderbirdCardDavAccount = item
                    Dim text2 As String = If(Not Equals(thunderbirdCardDavAccount.BookKey, Nothing), thunderbirdCardDavAccount.BookKey, "").Trim()
                    If text2.Length = 0 Then
                        text2 = "carddav-tb-" & num2.ToString()
                    End If
                    Dim password3 As String = Nothing
                    If AccountStore.TryGetPassword(text2, password3) AndAlso Not String.IsNullOrEmpty(password3) Then
                        password2 = password3
                    End If
                    Dim num3 = TryMergeCardDavRemoteOnly(item.ServerUrl, text, password2)
                    If num3 > 0 Then
                        num += num3
                    End If
                End If
            Next
            LastCardDavSecondaryBooksMerged = num
            Return num
        End Function

        Public Shared Function CountGalPushDirtyContacts() As Integer
            Dim num = 0
            For Each item In LoadContacts()
                If item IsNot Nothing AndAlso item.IsGalEntry AndAlso item.GalPushDirty Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function CountCardDavDirtyContacts() As Integer
            Dim num = 0
            For Each item In LoadContacts()
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) AndAlso item.CardDavDirty Then
                    num = num + 1
                End If
            Next
            Return num
        End Function

        Public Shared Function TrySyncStoredCardDavBidirectional() As Integer
            LastCardDavPushCount = 0
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            Dim flag = TryLoadCardDavSettings(serverUrl, username, password)
            If flag AndAlso CountCardDavDirtyContacts() > 0 Then
                Dim val = TryPushAllLocalContactsCardDav(serverUrl, username, password)
                LastCardDavPushCount = Math.Max(0, val)
            End If
            Dim num As Integer = TrySyncStoredCardDav()
            If num < 0 Then
                Return num
            End If
            If Not flag Then
                Return num
            End If
            Dim num2 = TryPushAllLocalContactsCardDav(serverUrl, username, password)
            LastCardDavPushCount = Math.Max(LastCardDavPushCount, Math.Max(0, num2))
            If num2 < 0 Then
                Return num
            End If
            LastCardDavBidirectionalUtc = Date.UtcNow
            Dim val2 As Integer = TryMergeSecondaryThunderbirdCardDavBooks()
            Return Math.Max(Math.Max(num, num2), val2)
        End Function

        Private Shared Function CardDavSettingsPath() As String
            Return Path.Combine(AccountStore.DataRoot(), "carddav-settings.json")
        End Function

        Private Shared Sub TrySaveSourceMabPath(mabPath As String)
            Try
                Call File.WriteAllText(Path.Combine(AccountStore.DataRoot(), "abook-source-mab.txt"), mabPath.Trim(), Encoding.UTF8)
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Public Shared Function TryValidateStoredSourceMab() As Boolean
            Dim text As String = TryLoadSourceMabPath()
            If text.Length = 0 Then
                Return False
            End If
            Return MorkAbookParser.TryValidateSourceMabByteRoundTrip(text)
        End Function

        Public Shared Function TryGetStoredSourceMabPath() As String
            Return TryLoadSourceMabPath()
        End Function

        Private Shared Function TryLoadSourceMabPath() As String
            Dim result As String
            Try
                Dim path As String = IO.Path.Combine(AccountStore.DataRoot(), "abook-source-mab.txt")
                If Not File.Exists(path) Then
                    result = ""
                Else
                    Dim text As String = File.ReadAllText(path, Encoding.UTF8).Trim()
                    result = If(text.Length <> 0 AndAlso File.Exists(text), text, "")
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function TryResolveAllCardDav412DirtyContacts() As Integer
            LastCardDav412BatchResolved = 0
            Dim serverUrl As String = Nothing
            Dim username As String = Nothing
            Dim password As String = Nothing
            If Not TryLoadCardDavSettings(serverUrl, username, password) Then
                Return 0
            End If
            If String.IsNullOrEmpty(password) Then
                Return 0
            End If
            Dim conflictOn412Mode = CardDavSyncService.ConflictOn412Mode
            Dim num = 0
            For Each item In LoadContacts()
                If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) AndAlso item.CardDavDirty AndAlso TryResolveCardDav412ConflictForEmail(item.Email, serverUrl, username, password, conflictOn412Mode) Then
                    num = num + 1
                End If
            Next
            LastCardDav412BatchResolved = num
            Return num
        End Function

        Public Shared Function TryResolveCardDav412ConflictForEmail(email As String, serverUrl As String, username As String, password As String, perContactMode As String) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(email, Nothing), email, "").Trim()
            If text.Length = 0 Then
                LastError = "Email required."
                Return False
            End If
            Dim text2 As String = If(Not Equals(perContactMode, Nothing), perContactMode, CardDavSyncService.ConflictOn412Mode).Trim()
            If text2.Length = 0 Then
                text2 = "ClientMerge"
            End If
            Dim conflictOn412Mode = CardDavSyncService.ConflictOn412Mode
            CardDavSyncService.ConflictOn412Mode = text2
            Try
                Dim mailContact As MailContact = Nothing
                For Each item In LoadContacts()
                    If item IsNot Nothing AndAlso String.Equals(item.Email, text, StringComparison.OrdinalIgnoreCase) Then
                        mailContact = item
                        Exit For
                    End If
                Next
                If mailContact Is Nothing Then
                    LastError = "Contact not found locally."
                    Return False
                End If
                mailContact.CardDavDirty = True
                Return TryPushContactCardDav(serverUrl, username, password, mailContact)
            Finally
                CardDavSyncService.ConflictOn412Mode = conflictOn412Mode
            End Try
        End Function

        Public Shared Function SaveContacts(contacts As List(Of MailContact)) As Boolean
            LastError = Nothing
            Dim result As Boolean
            If contacts Is Nothing Then
                LastError = "contacts is required."
                result = False
            Else
                Try
                    Dim list As List(Of ContactDto) = New List(Of ContactDto)()
                    For Each contact In contacts
                        If contact IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(contact.Email) Then
                            list.Add(ContactDto.FromModel(contact))
                        End If
                    Next
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim contents = javaScriptSerializer.Serialize(list)
                    Call File.WriteAllText(ContactsJsonPath(), contents, Encoding.UTF8)
                    CardDavLiveBookCache.NotifyLocalBookUpdated(contacts)
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

        Private Shared Function ReadContactsFromAbookSqlite(abookPath As String) As List(Of MailContact)
            Dim dictionary As Dictionary(Of String, MailContact) = New Dictionary(Of String, MailContact)(StringComparer.OrdinalIgnoreCase)
            Dim dictionary2 As Dictionary(Of Integer, String) = New Dictionary(Of Integer, String)()
            Dim val As SQLiteConnection = New SQLiteConnection("Data Source=" & abookPath & ";Mode=ReadOnly;")
            Try
                val.Open()
                AddressBookStore.LoadDisplayNameProperties(val, dictionary2)
                Dim val2 As SQLiteCommand = New SQLiteCommand("SELECT c.id, TRIM(COALESCE(c.name, '')) AS card_name, TRIM(p.value) AS email FROM cards c INNER JOIN properties p ON p.card_id = c.id WHERE p.name = 'PrimaryEmail' AND TRIM(COALESCE(p.value, '')) <> '' ORDER BY card_name, email;", val)
                Try
                    Dim val3 As SQLiteDataReader = val2.ExecuteReader()
                    Try
                        While val3.Read()
                            Dim int As Integer = val3.GetInt32(0)
                            Dim text As String = If(val3.IsDBNull(1), "", val3.GetString(1)).Trim()
                            Dim text2 As String = If(val3.IsDBNull(2), "", val3.GetString(2)).Trim()
                            If text2.Length <> 0 Then
                                Dim text3 = text
                                If text3.Length = 0 AndAlso dictionary2.ContainsKey(int) Then
                                    text3 = dictionary2(int)
                                End If
                                Dim mailContact As MailContact = New MailContact()
                                mailContact.DisplayName = text3
                                mailContact.Email = text2
                                Dim value = mailContact
                                Dim key As String = text2.ToLowerInvariant()
                                If Not dictionary.ContainsKey(key) Then
                                    dictionary(key) = value
                                End If
                            End If
                        End While

                    Finally
                        If val3 IsNot Nothing Then CType(val3, IDisposable).Dispose()
                    End Try

                Finally
                    If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                End Try

            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
            Dim list As List(Of MailContact) = New List(Of MailContact)(dictionary.Values)
            list.Sort(Function(a, b) String.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase))
            Return list
        End Function

        Private Shared Function MergeContacts(primary As List(Of MailContact), extra As List(Of MailContact)) As List(Of MailContact)
            Dim dictionary As Dictionary(Of String, MailContact) = New Dictionary(Of String, MailContact)(StringComparer.OrdinalIgnoreCase)
            If primary IsNot Nothing Then
                For Each item In primary
                    If item IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(item.Email) Then
                        dictionary(item.Email.Trim().ToLowerInvariant()) = item
                    End If
                Next
            End If
            If extra IsNot Nothing Then
                For Each item2 In extra
                    If item2 Is Nothing OrElse String.IsNullOrWhiteSpace(item2.Email) Then
                        Continue For
                    End If
                    Dim key As String = item2.Email.Trim().ToLowerInvariant()
                    If dictionary.ContainsKey(key) Then
                        Dim mailContact = dictionary(key)
                        If Not String.IsNullOrWhiteSpace(item2.DisplayName) Then
                            mailContact.DisplayName = item2.DisplayName
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Company) Then
                            mailContact.Company = item2.Company
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.WorkPhone) Then
                            mailContact.WorkPhone = item2.WorkPhone
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.HomePhone) Then
                            mailContact.HomePhone = item2.HomePhone
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Notes) Then
                            mailContact.Notes = item2.Notes
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.CardDavEtag) Then
                            mailContact.CardDavEtag = item2.CardDavEtag
                        End If
                        If item2.CardDavDirty Then
                            mailContact.CardDavDirty = True
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.SecondaryEmail) Then
                            mailContact.SecondaryEmail = item2.SecondaryEmail
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.FirstName) Then
                            mailContact.FirstName = item2.FirstName
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.LastName) Then
                            mailContact.LastName = item2.LastName
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.NickName) Then
                            mailContact.NickName = item2.NickName
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.JobTitle) Then
                            mailContact.JobTitle = item2.JobTitle
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Department) Then
                            mailContact.Department = item2.Department
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.MobilePhone) Then
                            mailContact.MobilePhone = item2.MobilePhone
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.WebPage) Then
                            mailContact.WebPage = item2.WebPage
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.FaxNumber) Then
                            mailContact.FaxNumber = item2.FaxNumber
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.PagerNumber) Then
                            mailContact.PagerNumber = item2.PagerNumber
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.WorkAddress) Then
                            mailContact.WorkAddress = item2.WorkAddress
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.OfficeLocation) Then
                            mailContact.OfficeLocation = item2.OfficeLocation
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.HomeAddress) Then
                            mailContact.HomeAddress = item2.HomeAddress
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Custom1) Then
                            mailContact.Custom1 = item2.Custom1
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Custom2) Then
                            mailContact.Custom2 = item2.Custom2
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Custom3) Then
                            mailContact.Custom3 = item2.Custom3
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Custom4) Then
                            mailContact.Custom4 = item2.Custom4
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.MiddleName) Then
                            mailContact.MiddleName = item2.MiddleName
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Birthday) Then
                            mailContact.Birthday = item2.Birthday
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.OtherPhone) Then
                            mailContact.OtherPhone = item2.OtherPhone
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.SpouseName) Then
                            mailContact.SpouseName = item2.SpouseName
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.Anniversary) Then
                            mailContact.Anniversary = item2.Anniversary
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.AssistantName) Then
                            mailContact.AssistantName = item2.AssistantName
                        End If
                        If item2.IsGalEntry Then
                            mailContact.IsGalEntry = True
                        End If
                        If item2.GalPushDirty Then
                            mailContact.GalPushDirty = True
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.GalPersonaId) Then
                            mailContact.GalPersonaId = item2.GalPersonaId
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.GalPersonaChangeKey) Then
                            mailContact.GalPersonaChangeKey = item2.GalPersonaChangeKey
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.GalDirectoryItemId) Then
                            mailContact.GalDirectoryItemId = item2.GalDirectoryItemId
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.GalDirectoryChangeKey) Then
                            mailContact.GalDirectoryChangeKey = item2.GalDirectoryChangeKey
                        End If
                        If Not String.IsNullOrWhiteSpace(item2.EwsPersonalItemId) Then
                            mailContact.EwsPersonalItemId = item2.EwsPersonalItemId
                        End If
                    Else
                        dictionary(key) = item2
                    End If
                Next
            End If
            Dim list As List(Of MailContact) = New List(Of MailContact)(dictionary.Values)
            list.Sort(Function(a, b) String.Compare(a.ListCaption(), b.ListCaption(), StringComparison.OrdinalIgnoreCase))
            Return list
        End Function

        Private Shared Sub LoadDisplayNameProperties(conn As SQLiteConnection, displayNameByCardId As Dictionary(Of Integer, String))
            Dim val As SQLiteCommand = New SQLiteCommand("SELECT card_id, TRIM(value) FROM properties WHERE name = 'DisplayName' AND TRIM(COALESCE(value, '')) <> '';", conn)
            Try
                Dim val2 As SQLiteDataReader = val.ExecuteReader()
                Try
                    While val2.Read()
                        Dim int As Integer = val2.GetInt32(0)
                        Dim text As String = If(val2.IsDBNull(1), "", val2.GetString(1)).Trim()
                        If text.Length <> 0 AndAlso Not displayNameByCardId.ContainsKey(int) Then
                            displayNameByCardId(int) = text
                        End If
                    End While

                Finally
                    If val2 IsNot Nothing Then CType(val2, IDisposable).Dispose()
                End Try

            Finally
                If val IsNot Nothing Then CType(val, IDisposable).Dispose()
            End Try
        End Sub
    End Class

