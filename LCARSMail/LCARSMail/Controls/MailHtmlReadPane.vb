Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices
Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms
    Public Class MailHtmlReadPane
        Inherits UserControl
        Private webView As WebView2

        Private fallbackLabel As Label

        Private initStarted As Boolean

        Private pendingHtml As String

        Private pendingPlain As String

        Public Sub New()
            MyBase.BackColor = Color.Black
            MyBase.DoubleBuffered = True
            fallbackLabel = New Label()
            fallbackLabel.Dock = DockStyle.Fill
            fallbackLabel.ForeColor = Color.Orange
            fallbackLabel.BackColor = Color.Black
            fallbackLabel.Font = New Font("Consolas", 9.5F, FontStyle.Regular)
            fallbackLabel.AutoSize = False
            fallbackLabel.TextAlign = ContentAlignment.TopLeft
            Controls.Add(fallbackLabel)
            AddHandler HandleCreated, AddressOf OnHandleCreatedAsync
        End Sub

        Public Sub Clear()
            pendingHtml = Nothing
            pendingPlain = Nothing
            fallbackLabel.Text = ""
            fallbackLabel.Visible = True
            If webView IsNot Nothing Then
                CType(CObj(webView), Control).Visible = False
            End If
        End Sub

        Public Sub ShowBody(body As MailMessageBody)
            ShowBody(body, Nothing)
        End Sub

        Public Sub ShowBody(body As MailMessageBody, attachments As List(Of MailAttachmentInfo))
            If body Is Nothing Then
                Clear()
                Return
            End If
            Dim text As String = If(Not Equals(body.HtmlBody, Nothing), body.HtmlBody, "").Trim()
            If text.Length > 0 Then
                ShowHtml(ApplyInlineContentIds(text, attachments))
            Else
                ShowPlain(body.GetReadingPaneText())
            End If
        End Sub

        Private Shared Function ApplyInlineContentIds(html As String, attachments As List(Of MailAttachmentInfo)) As String
            If String.IsNullOrEmpty(html) OrElse attachments Is Nothing Then
                Return html
            End If
            Dim text = html
            For Each attachment In attachments
                If attachment Is Nothing OrElse Not attachment.IsInline Then
                    Continue For
                End If
                Dim mailAttachmentInfo = attachment
                Dim text2 As String = If(Not Equals(mailAttachmentInfo.ContentId, Nothing), mailAttachmentInfo.ContentId, "").Trim()
                If text2.Length <> 0 Then
                    mailAttachmentInfo = attachment
                    Dim text3 As String = If(Not Equals(mailAttachmentInfo.CachedPath, Nothing), mailAttachmentInfo.CachedPath, "").Trim()
                    If text3.Length <> 0 AndAlso File.Exists(text3) Then
                        Dim absoluteUri As String = New Uri(text3).AbsoluteUri
                        Dim pattern = "cid:" & Regex.Escape(text2)
                        text = Regex.Replace(text, pattern, absoluteUri, RegexOptions.IgnoreCase)
                    End If
                End If
            Next
            Return text
        End Function

        Public Sub ShowPlain(text As String)
            pendingHtml = Nothing
            pendingPlain = If(Not Equals(text, Nothing), text, "")
            If webView IsNot Nothing Then
                CType(CObj(webView), Control).Visible = False
            End If
            fallbackLabel.Visible = True
            fallbackLabel.Text = pendingPlain
        End Sub

        Public Sub ShowHtml(html As String)
            pendingPlain = Nothing
            pendingHtml = WrapHtmlDocument(If(Not Equals(html, Nothing), html, ""))
            If webView IsNot Nothing AndAlso webView.CoreWebView2 IsNot Nothing Then
                CType(CObj(webView), Control).Visible = True
                fallbackLabel.Visible = False
                webView.CoreWebView2.NavigateToString(pendingHtml)
            Else
                fallbackLabel.Visible = True
                fallbackLabel.Text = StripTagsLight(pendingHtml)
                EnsureWebViewAsync()
            End If
        End Sub

        Private Async Sub OnHandleCreatedAsync(sender As Object, e As EventArgs)
            Await EnsureWebViewAsync()
        End Sub

        Private Async Function EnsureWebViewAsync() As Task
            If initStarted OrElse Not IsHandleCreated Then
                Return
            End If
            initStarted = True
            Try
                webView = New WebView2()
                CType(CObj(webView), Control).Dock = DockStyle.Fill
                webView.DefaultBackgroundColor = Color.Black
                CType(CObj(webView), Control).Visible = False
                Controls.Add(CType(CObj(webView), Control))
                Call CType(CObj(webView), Control).BringToFront()
                Await webView.EnsureCoreWebView2Async(CType(Nothing, CoreWebView2Environment))
                If Not String.IsNullOrEmpty(pendingHtml) Then
                    CType(CObj(webView), Control).Visible = True
                    fallbackLabel.Visible = False
                    webView.CoreWebView2.NavigateToString(pendingHtml)
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                initStarted = False
                Call ProjectData.ClearProjectError()
            End Try
        End Function

        Private Shared Function WrapHtmlDocument(innerHtml As String) As String
            Return "<!DOCTYPE html><html><head><meta charset=""utf-8""/><style>body{background:#000;color:#f90;font-family:Segoe UI,Arial,sans-serif;font-size:14px;margin:8px;}a{color:#fc6;}img{max-width:100%;}</style></head><body>" & innerHtml & "</body></html>"
        End Function

        Private Shared Function StripTagsLight(html As String) As String
            If String.IsNullOrEmpty(html) Then
                Return ""
            End If
            Return Regex.Replace(html, "<[^>]+>", " ").Trim()
        End Function
    End Class

