' LCARSMail/LCARSMail/Mail/MailModels.vb
Option Strict On
Option Infer On
Option Explicit On
Imports System.Collections.Generic
Imports System.Diagnostics

Public Class MailAccount
        Public Protocol As String

        Public EwsUrl As String

        Public Id As String

        Public DisplayName As String

        Public EmailAddress As String

        Public ImapHost As String

        Public ImapPort As Integer

        Public ImapSsl As Boolean

        Public ImapStartTls As Boolean

        Public Username As String

        Public SmtpHost As String

        Public SmtpPort As Integer

        Public SmtpSsl As Boolean

        Public SmtpStartTls As Boolean

        Public SmtpUsername As String

        Public StrictTls As Boolean

        Public AuthMethod As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

Public Class MailFolderInfo
        Public FullName As String

        Public DisplayName As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

Public Class MailMessageHeader
        Public Uid As ULong

        Public Folder As String

        Public AccountId As String

        Public Subject As String

        Public From As String

        Public ToAddresses As String

        Public CcAddresses As String

        Public DateUtc As Date

        Public IsSeen As Boolean

        Public MessageId As String

        Public InReplyTo As String

        Public References As String

        Public ReplyTo As String

        Public ListId As String

        Public ReturnPath As String

        Public SizeBytes As Long

        Public IsJunk As Boolean

        Public HeaderAttachmentCount As Integer

        Public FtsRank As Double

        Public Sub New()
            HeaderAttachmentCount = -1
        End Sub
    End Class

Public Class MailMessageBody
        Public TextBody As String

        Public HtmlBody As String

        Public RequiresPgpDecrypt As Boolean

        Public Attachments As List(Of MailAttachmentInfo)

        Public IcsCalendarParts As List(Of String)

        Public Sub New()
            Attachments = New List(Of MailAttachmentInfo)()
            IcsCalendarParts = New List(Of String)()
        End Sub

        Public Function GetReadingPaneText() As String
            If Not String.IsNullOrWhiteSpace(TextBody) Then
                Return TextBody
            End If
            If Not String.IsNullOrWhiteSpace(HtmlBody) Then
                Return StripHtmlLight(HtmlBody)
            End If
            Return ""
        End Function

        Private Shared Function StripHtmlLight(html As String) As String
            If String.IsNullOrEmpty(html) Then
                Return ""
            End If
            Dim input = Regex.Replace(html, "<[^>]+>", " ")
            input = Regex.Replace(input, "\s+", " ")
            Return input.Trim()
        End Function
    End Class

Public Class MailAttachmentInfo
        Public PartIndex As Integer

        Public FileName As String

        Public Size As Long

        Public CachedPath As String

        Public IsInline As Boolean

        Public ContentId As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub

        Public Overrides Function ToString() As String
            Dim text As String = If(Not Equals(FileName, Nothing), FileName, "attachment").Trim()
            If text.Length = 0 Then
                text = "attachment"
            End If
            If Size > 0 Then
                Return text & " (" & FormatAttachmentSize(Size) & ")"
            End If
            Return text
        End Function

        Private Shared Function FormatAttachmentSize(bytes As Long) As String
            If bytes < 1024 Then
                Return bytes.ToString() & " B"
            End If
            If bytes < 1048576 Then
                Return (bytes / 1024.0).ToString("0.#") & " KB"
            End If
            Return (bytes / 1048576.0).ToString("0.##") & " MB"
        End Function
    End Class

Public Class MailDraft
        Public Id As String

        Public AccountId As String

        Public ToAddresses As String

        Public CcAddresses As String

        Public Subject As String

        Public BodyPlain As String

        Public BodyHtml As String

        Public AttachmentPaths As List(Of String)

        Public UpdatedUtc As Date

        Public ImapDraftFolder As String

        Public ImapDraftUid As ULong

        Public Sub New()
            AttachmentPaths = New List(Of String)()
        End Sub
    End Class

Public Class MailContact
        Public DisplayName As String

        Public Email As String

        Public Company As String

        Public WorkPhone As String

        Public HomePhone As String

        Public Notes As String

        Public CardDavEtag As String

        Public SecondaryEmail As String

        Public FirstName As String

        Public LastName As String

        Public NickName As String

        Public JobTitle As String

        Public Department As String

        Public MobilePhone As String

        Public WebPage As String

        Public FaxNumber As String

        Public PagerNumber As String

        Public WorkAddress As String

        Public OfficeLocation As String

        Public HomeAddress As String

        Public Custom1 As String

        Public Custom2 As String

        Public Custom3 As String

        Public Custom4 As String

        Public MiddleName As String

        Public Birthday As String

        Public OtherPhone As String

        Public SpouseName As String

        Public Anniversary As String

        Public AssistantName As String

        Public ScreenName As String

        Public AimScreenName As String

        Public PopularityIndex As String

        Public RecordName As String

        Public PreferEmail As String

        Public CardDavDirty As Boolean

        Public IsGalEntry As Boolean

        Public GalPushDirty As Boolean

        Public GalPersonaId As String

        Public GalPersonaChangeKey As String

        Public GalDirectoryItemId As String

        Public GalDirectoryChangeKey As String

        Public EwsPersonalItemId As String

        Friend MorkRowId As String

        Public Sub New()
            DisplayName = String.Empty
            Email = String.Empty
            Company = String.Empty
            WorkPhone = String.Empty
            HomePhone = String.Empty
            Notes = String.Empty
            CardDavEtag = String.Empty
            SecondaryEmail = String.Empty
            FirstName = String.Empty
            LastName = String.Empty
            NickName = String.Empty
            JobTitle = String.Empty
            Department = String.Empty
            MobilePhone = String.Empty
            WebPage = String.Empty
            FaxNumber = String.Empty
            PagerNumber = String.Empty
            WorkAddress = String.Empty
            OfficeLocation = String.Empty
            HomeAddress = String.Empty
            Custom1 = String.Empty
            Custom2 = String.Empty
            Custom3 = String.Empty
            Custom4 = String.Empty
            MiddleName = String.Empty
            Birthday = String.Empty
            OtherPhone = String.Empty
            SpouseName = String.Empty
            Anniversary = String.Empty
            AssistantName = String.Empty
            ScreenName = String.Empty
            AimScreenName = String.Empty
            PopularityIndex = String.Empty
            RecordName = String.Empty
            PreferEmail = String.Empty
            GalPersonaId = String.Empty
            GalPersonaChangeKey = String.Empty
            GalDirectoryItemId = String.Empty
            GalDirectoryChangeKey = String.Empty
            EwsPersonalItemId = String.Empty
            MorkRowId = String.Empty
        End Sub

        Public Function FormatAddress() As String
            Dim text = text
            If Equals(text, Nothing) Then
                text = String.Empty
            End If
            text = text.Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim text2 = DisplayName
            If Equals(text2, Nothing) Then
                text2 = String.Empty
            End If
            text2 = text2.Trim()
            If text2.Length = 0 Then
                Return text
            End If
            If text2.IndexOf("@"c) >= 0 Then
                Return text
            End If
            Return """" & text2.Replace(""""c, "'"c) & """ <" & text & ">"
        End Function

        Public Function ListCaption() As String
            Dim text = text
            If Equals(text, Nothing) Then
                text = String.Empty
            End If
            text = text.Trim()
            Dim text2 = DisplayName
            If Equals(text2, Nothing) Then
                text2 = String.Empty
            End If
            text2 = text2.Trim()
            If text2.Length = 0 Then
                Return text
            End If
            If text.Length = 0 Then
                Return text2
            End If
            Return text2 & " <" & text & ">"
        End Function
    End Class

Public Class MailThreadListItem
        Public ThreadKey As String

        Public Representative As MailMessageHeader

        Public Messages As List(Of MailMessageHeader)

        Public ReadOnly Property MessageCount As Integer
            Get
                Return If(Messages IsNot Nothing, Messages.Count, 0)
            End Get
        End Property

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

Public Class CalDavCollectionDescriptor
        Public Url As String

        Public DisplayName As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

Public Class CalDavEvent
        Public Summary As String

        Public Uid As String

        Public StartUtc As Date

        Public EndUtc As Date

        Public AlarmMinutesBefore As Integer

        Public TaskCompleted As Boolean

        Public Location As String

        Public RecurrenceRule As String

        Public Category As String

        Public CalendarName As String

        Public Organizer As String

        Public AttendeeMailto As String

        Public MyPartStat As String

        Public IsMeetingInvitation As Boolean

        Public RecurrenceIdUtc As Date

        Public RecurrenceParentUid As String

        Public ExDateUtcCsv As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

Public Class RssFeedItem
        Public Title As String

        Public Link As String

        Public Description As String

        Public EnclosureUrl As String

        Public EnclosureType As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

Public Class NntpArticleHeader
        Public ArticleNumber As Integer

        Public Subject As String

        Public FromLine As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

Public Class ThunderbirdCardDavAccount
        Public ServerUrl As String

        Public Username As String

        Public BookKey As String

        <DebuggerNonUserCode>
        Public Sub New()
        End Sub
    End Class

''' <summary>Bridge helpers for calendar organizer SMTP normalization.</summary>
    Friend NotInheritable Class MailAttendeeHintBridge
        Friend Shared Function NormalizeOrganizerSmtp(organizerLine As String) As String
            Dim text As String = If(Not Equals(organizerLine, Nothing), organizerLine, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            Dim num = text.IndexOf("mailto:", StringComparison.OrdinalIgnoreCase)
            If num >= 0 Then
                text = text.Substring(num + 7)
            End If
            Dim num2 = text.IndexOf("@"c)
            If num2 <= 0 Then
                Return ""
            End If
            Dim num3 = text.IndexOf(";"c)
            If num3 > 0 Then
                text = text.Substring(0, num3)
            End If
            Return text.Trim()
        End Function
    End Class

