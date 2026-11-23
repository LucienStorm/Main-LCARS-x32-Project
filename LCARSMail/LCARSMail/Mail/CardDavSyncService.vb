Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class CardDavSyncService
        Public Class CardDavRemoteContact
            Public Contact As MailContact

            Public Etag As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Public Shared LastError As String

        Public Shared LastMergedAfter412 As Boolean

        Public Shared Last412ConflictEmail As String = ""

        Public Shared ConflictOn412Mode As String = "ClientMerge"

        Private Sub New()
        End Sub

        Public Shared Function FetchContacts(serverUrl As String, username As String, password As String) As List(Of MailContact)
            LastError = Nothing
            Dim text = ResolveAddressBookUrl(serverUrl, username, password)
            If text.Length = 0 Then
                Return Nothing
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><C:addressbook-query xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:carddav""><D:prop><D:getetag/><C:address-data/></D:prop></C:addressbook-query>"
            Dim text2 = DavRequest(text, "REPORT", username, password, body, "1")
            If text2.Length = 0 Then
                Return Nothing
            End If
            Return ParseAddressDataResponses(text2)
        End Function

        Public Shared Function FetchRemoteContacts(serverUrl As String, username As String, password As String) As List(Of CardDavRemoteContact)
            LastError = Nothing
            Dim text = ResolveAddressBookUrl(serverUrl, username, password)
            If text.Length = 0 Then
                Return Nothing
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><C:addressbook-query xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:carddav""><D:prop><D:getetag/><C:address-data/></D:prop></C:addressbook-query>"
            Dim text2 = DavRequest(text, "REPORT", username, password, body, "1")
            If text2.Length = 0 Then
                Return Nothing
            End If
            Return ParseRemoteContacts(text2)
        End Function

        Public Shared Function PutContact(serverUrl As String, username As String, password As String, contact As MailContact) As Boolean
            LastError = Nothing
            If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                LastError = "Contact email is required."
                Return False
            End If
            Dim text = ResolveAddressBookUrl(serverUrl, username, password)
            If text.Length = 0 Then
                Return False
            End If
            Return PutContactAutoMerge(serverUrl, username, password, contact)
        End Function

        Public Shared Function PutContactAutoMerge(serverUrl As String, username As String, password As String, contact As MailContact) As Boolean
            LastError = Nothing
            LastMergedAfter412 = False
            If contact Is Nothing OrElse String.IsNullOrWhiteSpace(contact.Email) Then
                LastError = "Contact email is required."
                Return False
            End If
            Dim text = ResolveAddressBookUrl(serverUrl, username, password)
            If text.Length = 0 Then
                Return False
            End If
            Dim text2 = BuildContactResourceUrl(text, contact.Email)
            Dim vcard = BuildVCard(contact)
            Dim mailContact = contact
            Dim ifMatchEtag As String = If(Not Equals(mailContact.CardDavEtag, Nothing), mailContact.CardDavEtag, "").Trim()
            Dim httpStatus = 0
            Dim responseEtag As String = Nothing
            If DavPutStatus(text2, username, password, vcard, ifMatchEtag, httpStatus, responseEtag) Then
                If Not String.IsNullOrWhiteSpace(responseEtag) Then
                    contact.CardDavEtag = responseEtag
                End If
                Return True
            End If
            If httpStatus <> 412 Then
                Return False
            End If
            Last412ConflictEmail = contact.Email.Trim()
            Dim contact2 As MailContact = Nothing
            Dim etag As String = Nothing
            If Not TryGetContactResource(text2, username, password, contact2, etag) Then
                LastError = If(Not Equals(LastError, Nothing), LastError, "CardDAV 412 — could not refetch remote contact.")
                Return False
            End If
            If String.Equals(ConflictOn412Mode, "ServerWins", StringComparison.OrdinalIgnoreCase) Then
                ApplyRemoteContactWins(contact, contact2)
            Else
                MergeContactForConflict(contact, contact2)
            End If
            If Not String.IsNullOrWhiteSpace(etag) Then
                contact.CardDavEtag = etag
            End If
            vcard = BuildVCard(contact)
            mailContact = contact
            ifMatchEtag = If(Not Equals(mailContact.CardDavEtag, Nothing), mailContact.CardDavEtag, "").Trim()
            LastMergedAfter412 = True
            If DavPutStatus(text2, username, password, vcard, ifMatchEtag, httpStatus, responseEtag) Then
                If Not String.IsNullOrWhiteSpace(responseEtag) Then
                    contact.CardDavEtag = responseEtag
                End If
                Return True
            End If
            LastError = If(Not Equals(LastError, Nothing), LastError, "CardDAV merge retry failed.")
            Return False
        End Function

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown. (Parameter 'node')
''' Actual value was System.Net.HttpWebResponse { StatusCode: System.Net.HttpStatusCode.NotFound }.
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToVariableDeclaratorOrNull(IsPatternExpressionSyntax node) in /_/CodeConverter/VB/CommonConversions.cs:line 175
'''    at System.Linq.Enumerable.SelectListIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereEnumerableIterator`1.MoveNext()
'''    at System.Collections.Generic.LargeArrayBuilder`1.AddRange(IEnumerable`1 items)
'''    at System.Collections.Generic.SparseArrayBuilder`1.ReserveOrAdd(IEnumerable`1 items)
'''    at System.Linq.Enumerable.Concat2Iterator`1.ToArray()
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToDeclarationStatement(List`1 des, List`1 isPatternExpressions) in /_/CodeConverter/VB/CommonConversions.cs:line 105
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.InsertRequiredDeclarations(SyntaxList`1 convertedStatements, CSharpSyntaxNode originaNode) in /_/CodeConverter/VB/CommonConversions.cs:line 89
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatement(StatementSyntax statement, CSharpSyntaxVisitor`1 methodBodyVisitor) in /_/CodeConverter/VB/CommonConversions.cs:line 75
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass10_0.<ConvertStatements>b__0(StatementSyntax s) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at System.Linq.Enumerable.SelectManySingleSelectorIterator`2.MoveNext()
'''    at Microsoft.CodeAnalysis.SyntaxList`1.CreateNode(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.SyntaxList`1..ctor(IEnumerable`1 nodes)
'''    at Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory.List[TNode](IEnumerable`1 nodes)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatements(SyntaxList`1 statements, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 69
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertBody(BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, Boolean hasReturnType, MethodBodyExecutableStatementVisitor iteratorState) in /_/CodeConverter/VB/CommonConversions.cs:line 49
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node) in /_/CodeConverter/VB/NodesVisitor.cs:line 428
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping) in /_/CodeConverter/VB/CommentConvertingVisitorWrapper.cs:line 20
''' 
''' Input:
''' 
''' 	public static bool DeleteContact(string serverUrl, string username, string password, string email)
''' 	{
''' 		LCARSMail.CardDavSyncService.LastError = null;
''' 		string text = ((email != null) ? email : "").Trim();
''' 		bool result;
''' 		if (text.Length == 0)
''' 		{
''' 			LCARSMail.CardDavSyncService.LastError = "Email is required.";
''' 			result = false;
''' 		}
''' 		else
''' 		{
''' 			string text2 = LCARSMail.CardDavSyncService.ResolveAddressBookUrl(serverUrl, username, password);
''' 			if (text2.Length == 0)
''' 			{
''' 				result = false;
''' 			}
''' 			else
''' 			{
''' 				string requestUriString = LCARSMail.CardDavSyncService.BuildContactResourceUrl(text2, text);
''' 				try
''' 				{
''' 					System.Net.HttpWebRequest httpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(requestUriString);
''' 					httpWebRequest.Method = "DELETE";
''' 					httpWebRequest.Credentials = new System.Net.NetworkCredential(username, password);
''' 					httpWebRequest.PreAuthenticate = true;
''' 					httpWebRequest.Timeout = 45000;
''' 					using System.Net.HttpWebResponse httpWebResponse = (System.Net.HttpWebResponse)httpWebRequest.GetResponse();
''' 					int statusCode = (int)httpWebResponse.StatusCode;
''' 					result = ((statusCode == 200 || statusCode == 204 || statusCode == 404) ? true : false);
''' 				}
''' 				catch (System.Net.WebException ex)
''' 				{
''' 					Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(ex);
''' 					System.Net.WebException ex2 = ex;
''' 					if (ex2.Response is System.Net.HttpWebResponse { StatusCode: System.Net.HttpStatusCode.NotFound })
''' 					{
''' 						result = true;
''' 						Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError();
''' 					}
''' 					else
''' 					{
''' 						LCARSMail.CardDavSyncService.LastError = ex2.Message;
''' 						result = false;
''' 						Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError();
''' 					}
''' 				}
''' 				catch (System.Exception ex3)
''' 				{
''' 					Microsoft.VisualBasic.CompilerServices.ProjectData.SetProjectError(ex3);
''' 					System.Exception ex4 = ex3;
''' 					LCARSMail.CardDavSyncService.LastError = ex4.Message;
''' 					result = false;
''' 					Microsoft.VisualBasic.CompilerServices.ProjectData.ClearProjectError();
''' 				}
''' 			}
''' 		}
''' 		return result;
''' 	}
''' 
''' 

        Private Shared Function BuildContactResourceUrl(bookUrl As String, email As String) As String
            Dim text = bookUrl
            If Not text.EndsWith("/") Then
                text += "/"
            End If
            Return text & SanitizeToken(email) & ".vcf"
        End Function

        Private Shared Sub ApplyRemoteContactWins(local As MailContact, remote As MailContact)
            If local IsNot Nothing AndAlso remote IsNot Nothing Then
                local.DisplayName = remote.DisplayName
                local.Company = remote.Company
                local.WorkPhone = remote.WorkPhone
                local.HomePhone = remote.HomePhone
                local.Notes = remote.Notes
                local.SecondaryEmail = remote.SecondaryEmail
                local.FirstName = remote.FirstName
                local.LastName = remote.LastName
                local.NickName = remote.NickName
                local.MobilePhone = remote.MobilePhone
                local.JobTitle = remote.JobTitle
                local.Department = remote.Department
            End If
        End Sub

        Private Shared Sub MergeBlankContactField(ByRef localVal As String, remoteVal As String)
            If String.IsNullOrWhiteSpace(localVal) AndAlso Not String.IsNullOrWhiteSpace(remoteVal) Then
                localVal = remoteVal
            End If
        End Sub

        Private Shared Sub MergeContactForConflict(local As MailContact, remote As MailContact)
            If local IsNot Nothing AndAlso remote IsNot Nothing Then
                MergeBlankContactField(local.DisplayName, remote.DisplayName)
                MergeBlankContactField(local.Company, remote.Company)
                MergeBlankContactField(local.WorkPhone, remote.WorkPhone)
                MergeBlankContactField(local.HomePhone, remote.HomePhone)
                MergeBlankContactField(local.MobilePhone, remote.MobilePhone)
                MergeBlankContactField(local.FaxNumber, remote.FaxNumber)
                MergeBlankContactField(local.PagerNumber, remote.PagerNumber)
                MergeBlankContactField(local.OtherPhone, remote.OtherPhone)
                MergeBlankContactField(local.Notes, remote.Notes)
                MergeBlankContactField(local.SecondaryEmail, remote.SecondaryEmail)
                MergeBlankContactField(local.FirstName, remote.FirstName)
                MergeBlankContactField(local.LastName, remote.LastName)
                MergeBlankContactField(local.MiddleName, remote.MiddleName)
                MergeBlankContactField(local.NickName, remote.NickName)
                MergeBlankContactField(local.JobTitle, remote.JobTitle)
                MergeBlankContactField(local.Department, remote.Department)
                MergeBlankContactField(local.OfficeLocation, remote.OfficeLocation)
                MergeBlankContactField(local.WebPage, remote.WebPage)
                MergeBlankContactField(local.WorkAddress, remote.WorkAddress)
                MergeBlankContactField(local.HomeAddress, remote.HomeAddress)
                MergeBlankContactField(local.Custom1, remote.Custom1)
                MergeBlankContactField(local.Custom2, remote.Custom2)
                MergeBlankContactField(local.Custom3, remote.Custom3)
                MergeBlankContactField(local.Custom4, remote.Custom4)
                MergeBlankContactField(local.Birthday, remote.Birthday)
                MergeBlankContactField(local.Anniversary, remote.Anniversary)
                MergeBlankContactField(local.SpouseName, remote.SpouseName)
                MergeBlankContactField(local.AssistantName, remote.AssistantName)
                MergeBlankContactField(local.ScreenName, remote.ScreenName)
                MergeBlankContactField(local.AimScreenName, remote.AimScreenName)
                MergeBlankContactField(local.PreferEmail, remote.PreferEmail)
                MergeBlankContactField(local.RecordName, remote.RecordName)
                MergeBlankContactField(local.PopularityIndex, remote.PopularityIndex)
            End If
        End Sub

        Private Shared Function TryGetContactResource(resourceUrl As String, username As String, password As String, ByRef contact As MailContact, ByRef etag As String) As Boolean
            contact = Nothing
            etag = Nothing
            Dim result As Boolean
            Try
                Dim httpWebRequest = CType(WebRequest.Create(resourceUrl), HttpWebRequest)
                httpWebRequest.Method = "GET"
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                httpWebRequest.PreAuthenticate = True
                httpWebRequest.Timeout = 45000
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                etag = httpWebResponse.Headers("ETag")
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                Dim text As String = streamReader.ReadToEnd()
                Dim list = ParseVCardBlocks(text)
                If list Is Nothing OrElse list.Count = 0 Then
                    LastError = "Remote vCard empty."
                    result = False
                Else
                    contact = list(0)
                    result = True
                End If
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Public Shared Function ResolveAddressBookUrl(serverUrl As String, username As String, password As String) As String
            LastError = Nothing
            Dim text As String = If(Not Equals(serverUrl, Nothing), serverUrl, "").Trim()
            If text.Length = 0 Then
                LastError = "CardDAV server URL is required."
                Return ""
            End If
            If text.IndexOf(".vcf", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Return text
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><D:propfind xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:carddav""><D:prop><D:resourcetype/><D:displayname/></D:prop></D:propfind>"
            Dim text2 = DavRequest(text, "PROPFIND", username, password, body, "1")
            If text2.Length = 0 Then
                Return ""
            End If
            Dim list = ExtractHrefBlocks(text2)
            For Each item In list
                If item.IndexOf("addressbook", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Dim text3 = ExtractTagValue(item, "href")
                    If text3.Length > 0 Then
                        Return CombineUrl(text, text3)
                    End If
                End If
            Next
            Return text
        End Function

        Public Shared Function EnumerateAddressBookUrls(serverUrl As String, username As String, password As String) As List(Of String)
            LastError = Nothing
            Dim list As List(Of String) = New List(Of String)()
            Dim text As String = If(Not Equals(serverUrl, Nothing), serverUrl, "").Trim()
            If text.Length = 0 Then
                Return list
            End If
            Dim body = "<?xml version=""1.0"" encoding=""utf-8""?><D:propfind xmlns:D=""DAV:"" xmlns:C=""urn:ietf:params:xml:ns:carddav""><D:prop><D:resourcetype/><D:displayname/></D:prop></D:propfind>"
            Dim text2 = DavRequest(text, "PROPFIND", username, password, body, "1")
            If text2.Length = 0 Then
                Return list
            End If
            For Each item In ExtractHrefBlocks(text2)
                If item.IndexOf("addressbook", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
                Dim text3 = ExtractTagValue(item, "href")
                If text3.Length = 0 Then
                    Continue For
                End If
                Dim text4 = CombineUrl(text, text3)
                Dim flag = False
                For Each item2 In list
                    If String.Equals(item2, text4, StringComparison.OrdinalIgnoreCase) Then
                        flag = True
                        Exit For
                    End If
                Next
                If Not flag Then
                    list.Add(text4)
                End If
            Next
            Return list
        End Function

        Public Shared Function ParseVCardContacts(vcardPayload As String) As List(Of MailContact)
            Return ParseVCardBlocks(vcardPayload)
        End Function

        Private Shared Function ParseAddressDataResponses(xml As String) As List(Of MailContact)
            Dim list As List(Of MailContact) = New List(Of MailContact)()
            For Each item In ParseRemoteContacts(xml)
                If item IsNot Nothing AndAlso item.Contact IsNot Nothing Then
                    list.Add(item.Contact)
                End If
            Next
            Return list
        End Function

        Private Shared Function ParseRemoteContacts(xml As String) As List(Of CardDavRemoteContact)
            Dim list As List(Of CardDavRemoteContact) = New List(Of CardDavRemoteContact)()
            If String.IsNullOrEmpty(xml) Then
                Return list
            End If
            For Each item In ExtractHrefBlocks(xml)
                Dim text = ExtractTagValue(item, "D:getetag")
                If text.Length = 0 Then
                    text = ExtractTagValue(item, "getetag")
                End If
                Dim list2 = ExtractAddressDataBlocks(item)
                For Each item2 In list2
                    For Each item3 In ParseVCardBlocks(item2)
                        If item3 IsNot Nothing Then
                            item3.CardDavEtag = text
                            list.Add(New CardDavRemoteContact With {
        .Contact = item3,
        .Etag = text
    })
                        End If
                    Next
                Next
            Next
            Return list
        End Function

        Private Shared Function ParseVCardBlocks(text As String) As List(Of MailContact)
            Dim list As List(Of MailContact) = New List(Of MailContact)()
            If String.IsNullOrWhiteSpace(text) Then
                Return list
            End If
            Dim regex As Regex = New Regex("BEGIN:VCARD[\s\S]*?END:VCARD", RegexOptions.IgnoreCase)
            For Each item As Match In regex.Matches(text)
                Dim value = item.Value
                Dim list2 = ExtractVCardLines(value, "EMAIL")
                If list2 Is Nothing OrElse list2.Count = 0 Then
                    Continue For
                End If
                Dim text2 = PickPreferredVCardEmail(value, list2)
                If text2.Length > 0 Then
                    list2.Remove(text2)
                    list2.Insert(0, text2)
                End If
                Dim text3 = ExtractVCardLine(value, "FN")
                If text3.Length = 0 Then
                    text3 = ExtractVCardLine(value, "N").Replace(";"c, " "c).Trim()
                End If
                Dim nickName = ExtractVCardLine(value, "NICKNAME")
                Dim text4 = ExtractVCardLine(value, "X-SCREENNAME")
                If text4.Length = 0 Then
                    text4 = ExtractVCardLine(value, "X-TB-SCREENNAME")
                End If
                If text4.Length = 0 Then
                    text4 = ExtractVCardLine(value, "X-MOZILLA-SCREENNAME")
                End If
                Dim text5 = ExtractVCardLine(value, "X-AIM")
                If text5.Length = 0 Then
                    text5 = ExtractImppAimScreenName(value)
                End If
                Dim text6 = ExtractVCardLine(value, "ORG")
                Dim company = text6
                Dim department = ""
                If text6.Length > 0 Then
                    Dim array = text6.Split(";"c)
                    company = array(0).Trim()
                    If array.Length > 1 Then
                        department = array(1).Trim()
                    End If
                End If
                Dim text7 = ExtractVCardLine(value, "TITLE")
                If text7.Length = 0 Then
                    text7 = ExtractVCardLine(value, "ROLE")
                End If
                Dim webPage = ExtractVCardLine(value, "URL")
                Dim text8 = ExtractVCardLine(value, "NOTE")
                Dim text9 = ExtractVCardLine(value, "CATEGORIES")
                Dim text10 = ExtractVCardLine(value, "X-CUSTOM1")
                If text10.Length = 0 AndAlso text9.Length > 0 Then
                    text10 = text9
                End If
                Dim custom = ExtractVCardLine(value, "X-CUSTOM2")
                Dim text11 = ExtractVCardLine(value, "X-CUSTOM3")
                Dim custom2 = ExtractVCardLine(value, "X-CUSTOM4")
                Dim text12 = ExtractVCardLine(value, "LANG")
                If text12.Length > 0 AndAlso text11.Length = 0 Then
                    text11 = text12
                End If
                Dim birthday = ExtractVCardLine(value, "BDAY")
                Dim anniversary = ExtractVCardLine(value, "ANNIVERSARY")
                Dim text13 = ExtractVCardLine(value, "REV")
                If text13.Length > 0 AndAlso String.IsNullOrWhiteSpace(text8) Then
                    text8 = "REV " & text13
                End If
                Dim text14 = ExtractVCardLine(value, "X-SPOUSE")
                If text14.Length = 0 Then
                    text14 = ExtractVCardLine(value, "RELATED;TYPE=SPOUSE")
                End If
                Dim text15 = ExtractVCardLine(value, "X-ASSISTANT")
                If text15.Length = 0 Then
                    text15 = ExtractVCardLine(value, "RELATED;TYPE=ASSISTANT")
                End If
                Dim text16 = ExtractVCardLine(value, "X-OFFICE")
                If text16.Length = 0 Then
                    text16 = ExtractVCardLine(value, "X-MOZILLA-OFFICE")
                End If
                Dim text17 = ExtractVCardLine(value, "TEL;TYPE=OTHER")
                If text17.Length = 0 Then
                    text17 = ExtractVCardLine(value, "TEL;TYPE=VOICE")
                End If
                Dim text18 = PickPreferredVCardTel(value, "WORK")
                If text18.Length = 0 Then
                    text18 = ExtractVCardLine(value, "TEL;TYPE=WORK")
                End If
                If text18.Length = 0 Then
                    text18 = ExtractVCardLine(value, "TEL")
                End If
                Dim text19 = PickPreferredVCardTel(value, "HOME")
                If text19.Length = 0 Then
                    text19 = ExtractVCardLine(value, "TEL;TYPE=HOME")
                End If
                Dim text20 = PickPreferredVCardTel(value, "CELL")
                If text20.Length = 0 Then
                    text20 = PickPreferredVCardTel(value, "MOBILE")
                End If
                If text20.Length = 0 Then
                    text20 = PickPreferredVCardTel(value, "IPHONE")
                End If
                If text20.Length = 0 Then
                    text20 = ExtractVCardLine(value, "TEL;TYPE=CELL")
                End If
                If text20.Length = 0 Then
                    text20 = ExtractVCardLine(value, "TEL;TYPE=MOBILE")
                End If
                If text20.Length = 0 Then
                    text20 = ExtractVCardLine(value, "TEL;TYPE=IPHONE")
                End If
                Dim text21 = PickPreferredVCardTel(value, "FAX")
                If text21.Length = 0 Then
                    text21 = ExtractVCardLine(value, "TEL;TYPE=FAX")
                End If
                Dim text22 = PickPreferredVCardTel(value, "PAGER")
                If text22.Length = 0 Then
                    text22 = ExtractVCardLine(value, "TEL;TYPE=PAGER")
                End If
                Dim text23 = ExtractVCardLine(value, "ADR;TYPE=WORK")
                Dim rawAdr = ExtractVCardLine(value, "ADR;TYPE=HOME")
                If text23.Length = 0 Then
                    text23 = ExtractVCardLine(value, "ADR")
                End If
                Dim text24 = ExtractVCardLine(value, "N")
                Dim firstName = ""
                Dim lastName = ""
                Dim middleName = ""
                If text24.Length > 0 Then
                    Dim array2 = text24.Split(";"c)
                    If array2.Length > 1 Then
                        lastName = array2(0).Trim()
                    End If
                    If array2.Length > 2 Then
                        firstName = array2(1).Trim()
                    End If
                    If array2.Length > 3 Then
                        middleName = array2(2).Trim()
                    End If
                End If
                Dim mailContact As MailContact = New MailContact()
                mailContact.Email = list2(0)
                mailContact.DisplayName = text3
                mailContact.NickName = nickName
                mailContact.Company = company
                mailContact.Department = department
                mailContact.JobTitle = text7
                mailContact.Notes = text8
                mailContact.WorkPhone = text18
                mailContact.HomePhone = text19
                mailContact.MobilePhone = text20
                mailContact.FaxNumber = text21
                mailContact.PagerNumber = text22
                mailContact.WorkAddress = FormatVCardAdr(text23)
                mailContact.HomeAddress = FormatVCardAdr(rawAdr)
                mailContact.WebPage = webPage
                mailContact.FirstName = firstName
                mailContact.LastName = lastName
                mailContact.Custom1 = text10
                mailContact.Custom2 = custom
                mailContact.Custom3 = text11
                mailContact.Custom4 = custom2
                mailContact.Birthday = birthday
                mailContact.OtherPhone = text17
                mailContact.Anniversary = anniversary
                mailContact.SpouseName = text14
                mailContact.MiddleName = middleName
                mailContact.ScreenName = text4
                mailContact.AimScreenName = text5
                mailContact.AssistantName = text15
                mailContact.OfficeLocation = text16
                Dim mailContact2 = mailContact
                If list2.Count > 1 Then
                    mailContact2.SecondaryEmail = list2(1)
                End If
                list.Add(mailContact2)
            Next
            If list.Count = 0 Then
                Dim regex2 As Regex = New Regex("[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled)
                For Each item2 As Match In regex2.Matches(text)
                    list.Add(New MailContact With {
        .Email = item2.Value,
        .DisplayName = ""
    })
                Next
            End If
            Return list
        End Function

        Private Shared Function PickPreferredVCardTel(card As String, typeToken As String) As String
            If String.IsNullOrWhiteSpace(card) OrElse String.IsNullOrWhiteSpace(typeToken) Then
                Return ""
            End If
            Dim array = card.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If Not text2.StartsWith("TEL", StringComparison.OrdinalIgnoreCase) OrElse text2.IndexOf("PREF", StringComparison.OrdinalIgnoreCase) < 0 OrElse text2.IndexOf(typeToken, StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
                Dim num = text2.LastIndexOf(":"c)
                If num >= 0 Then
                    Dim text3 As String = text2.Substring(num + 1).Trim()
                    If text3.Length > 0 Then
                        Return text3
                    End If
                End If
            Next
            Return ""
        End Function

        Private Shared Function PickPreferredVCardEmail(card As String, emails As List(Of String)) As String
            If emails Is Nothing OrElse emails.Count = 0 Then
                Return ""
            End If
            Dim array = card.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If Not text2.StartsWith("EMAIL", StringComparison.OrdinalIgnoreCase) OrElse text2.IndexOf("PREF", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
                Dim num = text2.LastIndexOf(":"c)
                If num >= 0 Then
                    Dim text3 As String = text2.Substring(num + 1).Trim()
                    If text3.Length > 0 Then
                        Return text3
                    End If
                End If
            Next
            Return ""
        End Function

        Private Shared Function ExtractImppAimScreenName(card As String) As String
            If String.IsNullOrWhiteSpace(card) Then
                Return ""
            End If
            Dim array = card.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.IndexOf("IMPP", StringComparison.OrdinalIgnoreCase) < 0 OrElse text2.IndexOf("AIM", StringComparison.OrdinalIgnoreCase) < 0 AndAlso text2.IndexOf("aim:", StringComparison.OrdinalIgnoreCase) < 0 Then
                    Continue For
                End If
                Dim num = text2.LastIndexOf(":"c)
                If num >= 0 Then
                    Dim text3 As String = text2.Substring(num + 1).Trim()
                    If text3.StartsWith("aim:", StringComparison.OrdinalIgnoreCase) Then
                        text3 = text3.Substring(4).Trim()
                    End If
                    If text3.Length > 0 Then
                        Return text3
                    End If
                End If
            Next
            Return ""
        End Function

        Private Shared Function ExtractVCardLine(card As String, key As String) As String
            Dim array = card.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If text2.StartsWith(key & ":", StringComparison.OrdinalIgnoreCase) OrElse text2.StartsWith(key & ";", StringComparison.OrdinalIgnoreCase) Then
                    Dim num = text2.IndexOf(":"c)
                    If num >= 0 Then
                        Return text2.Substring(num + 1).Trim()
                    End If
                End If
            Next
            Return ""
        End Function

        Private Shared Function ExtractVCardLines(card As String, key As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Dim array = card.Split(New Char(1) {ChrW(10), ChrW(13)}, StringSplitOptions.RemoveEmptyEntries)
            For Each text In array
                Dim text2 As String = text.Trim()
                If Not text2.StartsWith(key, StringComparison.OrdinalIgnoreCase) OrElse Not text2.StartsWith(key & ":", StringComparison.OrdinalIgnoreCase) AndAlso Not text2.StartsWith(key & ";", StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim num = text2.IndexOf(":"c)
                If num >= 0 Then
                    Dim text3 As String = text2.Substring(num + 1).Trim()
                    If text3.Length > 0 Then
                        list.Add(text3)
                    End If
                End If
            Next
            Return list
        End Function

        Private Shared Function BuildVCard(contact As MailContact) As String
            Dim text As String = contact.Email.Trim()
            Dim mailContact = contact
            Dim text2 As String = If(Not Equals(mailContact.DisplayName, Nothing), mailContact.DisplayName, "").Trim()
            If text2.Length = 0 Then
                text2 = text
            End If
            mailContact = contact
            Dim text3 As String = If(Not Equals(mailContact.FirstName, Nothing), mailContact.FirstName, "").Trim()
            mailContact = contact
            Dim text4 As String = If(Not Equals(mailContact.LastName, Nothing), mailContact.LastName, "").Trim()
            If text3.Length = 0 AndAlso text4.Length = 0 AndAlso text2.Length > 0 Then
                Dim num = text2.IndexOf(" "c)
                If num > 0 Then
                    text3 = text2.Substring(0, num).Trim()
                    text4 = text2.Substring(num + 1).Trim()
                Else
                    text4 = text2
                End If
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            stringBuilder.AppendLine("BEGIN:VCARD")
            stringBuilder.AppendLine("PRODID:-//LCARS Mail//CardDAV//EN")
            stringBuilder.AppendLine("VERSION:3.0")
            stringBuilder.AppendLine("FN:" & EscapeVCard(text2))
            mailContact = contact
            Dim value As String = If(Not Equals(mailContact.MiddleName, Nothing), mailContact.MiddleName, "").Trim()
            Dim array = New String(7) {"N:", EscapeVCard(text4), ";", EscapeVCard(text3), ";", EscapeVCard(value), ";;", Nothing}
            mailContact = contact
            array(7) = EscapeVCard(If(Not Equals(mailContact.NickName, Nothing), mailContact.NickName, ""))
            stringBuilder.AppendLine(String.Concat(array))
            stringBuilder.AppendLine("EMAIL;TYPE=INTERNET,PREF:" & text)
            If Not String.IsNullOrWhiteSpace(contact.SecondaryEmail) Then
                stringBuilder.AppendLine("EMAIL;TYPE=INTERNET:" & contact.SecondaryEmail.Trim())
            End If
            If Not String.IsNullOrWhiteSpace(contact.NickName) Then
                stringBuilder.AppendLine("NICKNAME:" & EscapeVCard(contact.NickName))
            End If
            If Not String.IsNullOrWhiteSpace(contact.ScreenName) Then
                stringBuilder.AppendLine("X-SCREENNAME:" & EscapeVCard(contact.ScreenName))
                stringBuilder.AppendLine("X-MOZILLA-SCREENNAME:" & EscapeVCard(contact.ScreenName))
            End If
            If Not String.IsNullOrWhiteSpace(contact.AimScreenName) Then
                stringBuilder.AppendLine("X-AIM:" & EscapeVCard(contact.AimScreenName))
                stringBuilder.AppendLine("IMPP;TYPE=AIM:" & EscapeVCard("aim:" & contact.AimScreenName.Trim()))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Company) Then
                Dim text5 = EscapeVCard(contact.Company)
                If Not String.IsNullOrWhiteSpace(contact.Department) Then
                    text5 = text5 & ";" & EscapeVCard(contact.Department)
                End If
                stringBuilder.AppendLine("ORG:" & text5)
            ElseIf Not String.IsNullOrWhiteSpace(contact.Department) Then
                stringBuilder.AppendLine("ORG:" & EscapeVCard(contact.Department))
            End If
            If Not String.IsNullOrWhiteSpace(contact.JobTitle) Then
                stringBuilder.AppendLine("TITLE:" & EscapeVCard(contact.JobTitle))
                stringBuilder.AppendLine("ROLE:" & EscapeVCard(contact.JobTitle))
            End If
            Dim flag = Not String.IsNullOrWhiteSpace(contact.MobilePhone)
            Dim flag2 = Not String.IsNullOrWhiteSpace(contact.WorkPhone)
            Dim flag3 = Not String.IsNullOrWhiteSpace(contact.HomePhone)
            Dim flag4 = If(flag OrElse flag2 OrElse flag3, True, False)
            If flag2 Then
                Dim text6 = If(flag, "WORK", "WORK,PREF")
                stringBuilder.AppendLine("TEL;TYPE=" & text6 & ":" & EscapeVCard(contact.WorkPhone))
            End If
            If flag3 Then
                Dim text7 = If(flag OrElse flag2, "HOME", "HOME,PREF")
                stringBuilder.AppendLine("TEL;TYPE=" & text7 & ":" & EscapeVCard(contact.HomePhone))
            End If
            If flag Then
                stringBuilder.AppendLine("TEL;TYPE=CELL,PREF:" & EscapeVCard(contact.MobilePhone))
                stringBuilder.AppendLine("TEL;TYPE=IPHONE:" & EscapeVCard(contact.MobilePhone))
            End If
            If Not String.IsNullOrWhiteSpace(contact.FaxNumber) Then
                Dim text8 = If(flag4, "FAX", "FAX,PREF")
                stringBuilder.AppendLine("TEL;TYPE=" & text8 & ":" & EscapeVCard(contact.FaxNumber))
            End If
            If Not String.IsNullOrWhiteSpace(contact.PagerNumber) Then
                Dim text9 = If(flag4 OrElse Not String.IsNullOrWhiteSpace(contact.FaxNumber), "PAGER", "PAGER,PREF")
                stringBuilder.AppendLine("TEL;TYPE=" & text9 & ":" & EscapeVCard(contact.PagerNumber))
            End If
            Dim flag5 = Not String.IsNullOrWhiteSpace(contact.WorkAddress)
            Dim flag6 = Not String.IsNullOrWhiteSpace(contact.HomeAddress)
            If flag5 Then
                Dim text10 = If(flag6, "WORK", "WORK,PREF")
                stringBuilder.AppendLine("ADR;TYPE=" & text10 & ":" & BuildVCardAdrValue(contact.WorkAddress))
            End If
            If flag6 Then
                Dim text11 = If(flag5, "HOME", "HOME,PREF")
                stringBuilder.AppendLine("ADR;TYPE=" & text11 & ":" & BuildVCardAdrValue(contact.HomeAddress))
            End If
            If Not String.IsNullOrWhiteSpace(contact.WebPage) Then
                stringBuilder.AppendLine("URL:" & EscapeVCard(contact.WebPage))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Notes) Then
                stringBuilder.AppendLine("NOTE:" & EscapeVCard(contact.Notes))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Custom1) Then
                stringBuilder.AppendLine("X-CUSTOM1:" & EscapeVCard(contact.Custom1))
                stringBuilder.AppendLine("CATEGORIES:" & EscapeVCard(contact.Custom1))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Custom2) Then
                stringBuilder.AppendLine("X-CUSTOM2:" & EscapeVCard(contact.Custom2))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Custom3) Then
                stringBuilder.AppendLine("X-CUSTOM3:" & EscapeVCard(contact.Custom3))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Custom4) Then
                stringBuilder.AppendLine("X-CUSTOM4:" & EscapeVCard(contact.Custom4))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Birthday) Then
                stringBuilder.AppendLine("BDAY:" & EscapeVCard(contact.Birthday))
            End If
            If Not String.IsNullOrWhiteSpace(contact.Anniversary) Then
                stringBuilder.AppendLine("ANNIVERSARY:" & EscapeVCard(contact.Anniversary))
            End If
            If Not String.IsNullOrWhiteSpace(contact.SpouseName) Then
                stringBuilder.AppendLine("X-SPOUSE:" & EscapeVCard(contact.SpouseName))
            End If
            If Not String.IsNullOrWhiteSpace(contact.AssistantName) Then
                stringBuilder.AppendLine("X-ASSISTANT:" & EscapeVCard(contact.AssistantName))
                stringBuilder.AppendLine("RELATED;TYPE=ASSISTANT:" & EscapeVCard(contact.AssistantName))
            End If
            If Not String.IsNullOrWhiteSpace(contact.OfficeLocation) Then
                stringBuilder.AppendLine("X-OFFICE:" & EscapeVCard(contact.OfficeLocation))
                stringBuilder.AppendLine("X-MOZILLA-OFFICE:" & EscapeVCard(contact.OfficeLocation))
            End If
            If Not String.IsNullOrWhiteSpace(contact.OtherPhone) Then
                Dim text12 = If(flag4 OrElse Not String.IsNullOrWhiteSpace(contact.FaxNumber) OrElse Not String.IsNullOrWhiteSpace(contact.PagerNumber), "OTHER", "OTHER,PREF")
                stringBuilder.AppendLine("TEL;TYPE=" & text12 & ":" & EscapeVCard(contact.OtherPhone))
            End If
            mailContact = contact
            Dim text13 As String = If(Not Equals(mailContact.Custom3, Nothing), mailContact.Custom3, "").Trim()
            If text13.Length > 0 AndAlso text13.Length <= 16 AndAlso Not text13.Contains("@") Then
                stringBuilder.AppendLine("LANG:" & EscapeVCard(text13))
            Else
                stringBuilder.AppendLine("LANG:en")
            End If
            stringBuilder.AppendLine("UID:" & text)
            stringBuilder.AppendLine("REV:" & Date.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture))
            stringBuilder.AppendLine("END:VCARD")
            Return stringBuilder.ToString()
        End Function

        Private Shared Function FormatVCardAdr(rawAdr As String) As String
            Dim text As String = If(Not Equals(rawAdr, Nothing), rawAdr, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim array = text.Split(";"c)
            Dim text2 = ""
            If array.Length > 2 Then
                text2 = array(2).Trim()
            End If
            If text2.Length = 0 AndAlso array.Length > 0 Then
                text2 = array(array.Length - 1).Trim()
            End If
            Dim text3 = ""
            If array.Length > 3 Then
                text3 = array(3).Trim()
            End If
            Dim text4 = ""
            If array.Length > 4 Then
                text4 = array(4).Trim()
            End If
            Dim text5 = ""
            If array.Length > 5 Then
                text5 = array(5).Trim()
            End If
            Dim text6 = ""
            If array.Length > 6 Then
                text6 = array(6).Trim()
            End If
            Dim stringBuilder As StringBuilder = New StringBuilder()
            If text2.Length > 0 Then
                stringBuilder.Append(text2)
            End If
            If text3.Length > 0 Then
                If stringBuilder.Length > 0 Then
                    stringBuilder.Append(", ")
                End If
                stringBuilder.Append(text3)
            End If
            If text4.Length > 0 Then
                If stringBuilder.Length > 0 Then
                    stringBuilder.Append(", ")
                End If
                stringBuilder.Append(text4)
            End If
            If text5.Length > 0 Then
                If stringBuilder.Length > 0 Then
                    stringBuilder.Append(" ")
                End If
                stringBuilder.Append(text5)
            End If
            If text6.Length > 0 Then
                If stringBuilder.Length > 0 Then
                    stringBuilder.Append(", ")
                End If
                stringBuilder.Append(text6)
            End If
            If stringBuilder.Length > 0 Then
                Return stringBuilder.ToString()
            End If
            Return text2
        End Function

        Private Shared Function BuildVCardAdrValue(singleLineAddress As String) As String
            Dim text As String = If(Not Equals(singleLineAddress, Nothing), singleLineAddress, "").Trim()
            If text.Length = 0 Then
                Return ";;;;;;"
            End If
            Return ";;" & EscapeVCard(text) & ";;;;"
        End Function

        Private Shared Function EscapeVCard(value As String) As String
            Return If(Not Equals(value, Nothing), value, "").Replace(";", "\;").Replace(",", "\,").Replace(vbCrLf, "\n")
        End Function

        Private Shared Function DavRequest(url As String, method As String, username As String, password As String, body As String, depth As String) As String
            Dim result As String
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = method
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                httpWebRequest.PreAuthenticate = True
                httpWebRequest.Timeout = 45000
                httpWebRequest.ContentType = "application/xml; charset=utf-8"
                If Not Equals(depth, Nothing) Then
                    httpWebRequest.Headers.Add("Depth", depth)
                End If
                Dim bytes = Encoding.UTF8.GetBytes(body)
                httpWebRequest.ContentLength = bytes.Length
                Using stream As Stream = httpWebRequest.GetRequestStream()
                    stream.Write(bytes, 0, bytes.Length)
                End Using
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                Dim streamReader As StreamReader = New StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8)
                result = streamReader.ReadToEnd()
            Catch ex As Exception
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex
                LastError = ex2.Message
                result = ""
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function DavPutStatus(url As String, username As String, password As String, vcard As String, ifMatchEtag As String, ByRef httpStatus As Integer, ByRef responseEtag As String) As Boolean
            httpStatus = 0
            responseEtag = Nothing
            Dim result As Boolean
            Dim httpWebResponse2 As HttpWebResponse = Nothing
            Try
                Dim httpWebRequest = CType(WebRequest.Create(url), HttpWebRequest)
                httpWebRequest.Method = "PUT"
                httpWebRequest.Credentials = New NetworkCredential(username, password)
                httpWebRequest.PreAuthenticate = True
                httpWebRequest.Timeout = 45000
                httpWebRequest.ContentType = "text/vcard; charset=utf-8"
                Dim text As String = If(Not Equals(ifMatchEtag, Nothing), ifMatchEtag, "").Trim()
                If text.Length > 0 Then
                    httpWebRequest.Headers.Add("If-Match", text)
                Else
                    httpWebRequest.Headers.Add("If-None-Match", "*")
                End If
                Dim bytes = Encoding.UTF8.GetBytes(vcard)
                httpWebRequest.ContentLength = bytes.Length
                Using stream As Stream = httpWebRequest.GetRequestStream()
                    stream.Write(bytes, 0, bytes.Length)
                End Using
                Dim httpWebResponse As HttpWebResponse = CType(httpWebRequest.GetResponse(), HttpWebResponse)
                httpStatus = httpWebResponse.StatusCode
                responseEtag = httpWebResponse.Headers("ETag")
                result = If(httpStatus >= 200 AndAlso httpStatus < 300, True, False)
            Catch ex As WebException
                ProjectData.SetProjectError(ex)
                Dim ex2 = ex

                If Not (CSharpImpl.__Assign(httpWebResponse2, TryCast(ex2.Response, HttpWebResponse)) IsNot Nothing) Then
                    GoTo IL_01af
                End If
                httpStatus = httpWebResponse2.StatusCode
                responseEtag = httpWebResponse2.Headers("ETag")
                If httpStatus = 201 OrElse httpStatus = 204 Then
                    result = True
                    Call ProjectData.ClearProjectError()
                Else
                    If httpStatus <> 412 Then
                        GoTo IL_01af
                    End If
                    LastError = "CardDAV etag conflict (412)."
                    result = False
                    Call ProjectData.ClearProjectError()
                End If
                GoTo end_IL_013b
IL_01af:
                LastError = ex2.Message
                result = False
                Call ProjectData.ClearProjectError()
end_IL_013b:
            Catch ex3 As Exception
                ProjectData.SetProjectError(ex3)
                Dim ex4 = ex3
                LastError = ex4.Message
                result = False
                Call ProjectData.ClearProjectError()
            End Try
            Return result
        End Function

        Private Shared Function DavPut(url As String, username As String, password As String, vcard As String, Optional ifMatchEtag As String = Nothing) As Boolean
            Dim httpStatus = 0
            Dim responseEtag As String = Nothing
            Return DavPutStatus(url, username, password, vcard, If(Not Equals(ifMatchEtag, Nothing), ifMatchEtag, ""), httpStatus, responseEtag)
        End Function

        Private Shared Function ExtractAddressDataBlocks(xml As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            If String.IsNullOrEmpty(xml) Then
                Return list
            End If
            Dim text = "address-data"
            Dim num = 0
            While num < xml.Length
                Dim num2 = xml.IndexOf("<", num, StringComparison.Ordinal)
                If num2 < 0 Then
                    Exit While
                End If
                Dim num3 = xml.IndexOf(">"c, num2)
                If num3 < 0 Then
                    Exit While
                End If
                Dim text2 = xml.Substring(num2, num3 - num2 + 1)
                If text2.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0 Then
                    num = num3 + 1
                    Continue While
                End If
                Dim text3 = "</" & text & ">"
                Dim num4 = xml.IndexOf(text3, num3, StringComparison.OrdinalIgnoreCase)
                If num4 < 0 Then
                    Exit While
                End If
                Dim text4 = xml.Substring(num3 + 1, num4 - num3 - 1)
                list.Add(DecodeXmlText(text4.Trim()))
                num = num4 + text3.Length
            End While
            Return list
        End Function

        Private Shared Function ExtractHrefBlocks(xml As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Dim array = xml.Split(New String(1) {"<D:response", "<d:response"}, StringSplitOptions.None)
            Dim num = array.Length - 1
            For i = 1 To num
                list.Add(array(i))
            Next
            Return list
        End Function

        Private Shared Function ExtractTagValue(block As String, tag As String) As String
            Dim text = "<" & tag & ">"
            Dim value = "</" & tag & ">"
            Dim num = block.IndexOf(text, StringComparison.OrdinalIgnoreCase)
            If num < 0 Then
                Return ""
            End If
            num += text.Length
            Dim num2 = block.IndexOf(value, num, StringComparison.OrdinalIgnoreCase)
            If num2 < 0 Then
                Return ""
            End If
            Return block.Substring(num, num2 - num).Trim()
        End Function

        Private Shared Function CombineUrl(baseUrl As String, href As String) As String
            If href.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then
                Return href
            End If
            Dim baseUri As Uri = New Uri(baseUrl)
            Return New Uri(baseUri, href).ToString()
        End Function

        Private Shared Function DecodeXmlText(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return ""
            End If
            Return value.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", """")
        End Function

        Private Shared Function SanitizeToken(email As String) As String
            Dim input = email.Replace("@", "_at_").Replace(".", "_")
            Return Regex.Replace(input, "[^A-Za-z0-9_\-]", "_")
        End Function

        Private Class CSharpImpl
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class

