Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms
Imports System.Windows.Forms

''' <summary>
''' IBrowserTab host for WebView2 web browsing.
''' </summary>
Public Class WebBrowserTab
    Implements IBrowserTab

    Private Shared runtimeWarningShown As Boolean = False

    Private ReadOnly _webView As WebView2

    Public Event NavigationStarting As EventHandler(Of CoreWebView2NavigationStartingEventArgs)
    Public Event NavigationCompleted As EventHandler(Of CoreWebView2NavigationCompletedEventArgs)
    Public Event DocumentTitleChanged As EventHandler
    Public Event SourceChanged As EventHandler(Of CoreWebView2SourceChangedEventArgs)
    Public Event NewWindowRequested As EventHandler(Of CoreWebView2NewWindowRequestedEventArgs)

    Public Sub New()
        _webView = New WebView2()
        _webView.Name = "WebView2"
        _webView.Dock = DockStyle.Fill
    End Sub

    Public ReadOnly Property WebView As WebView2
        Get
            Return _webView
        End Get
    End Property

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "Web"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            If _webView.CoreWebView2 Is Nothing Then Return "NEW PAGE"
            Dim documentTitle As String = _webView.CoreWebView2.DocumentTitle
            If String.IsNullOrEmpty(documentTitle) Then Return "NEW PAGE"
            Return documentTitle
        End Get
    End Property

    Public ReadOnly Property IsDirty As Boolean Implements IBrowserTab.IsDirty
        Get
            Return False
        End Get
    End Property

    Public ReadOnly Property ContentControl As Control Implements IBrowserTab.ContentControl
        Get
            Return _webView
        End Get
    End Property

    Public Function GetPathOrUrl() As String Implements IBrowserTab.GetPathOrUrl
        If _webView.CoreWebView2 IsNot Nothing Then
            Return _webView.CoreWebView2.Source
        End If
        If _webView.Source IsNot Nothing Then
            Return _webView.Source.ToString()
        End If
        Return ""
    End Function

    Public Sub NavigateOrOpen(ByVal pathOrUrl As String) Implements IBrowserTab.NavigateOrOpen
        If _webView.CoreWebView2 Is Nothing Then Return
        _webView.CoreWebView2.Navigate(pathOrUrl)
    End Sub

    Public Function Save(ByVal path As String) As Boolean Implements IBrowserTab.Save
        Return False
    End Function

    Public Sub FocusContent() Implements IBrowserTab.FocusContent
        _webView.Focus()
    End Sub

    ''' <summary>
    ''' Creates the WebView2 environment and wires navigation events.
    ''' </summary>
    Public Async Function InitializeAsync() As Task
        If Not WebView2RuntimeInstaller.EnsureInstalled() Then
            If Not runtimeWarningShown Then
                runtimeWarningShown = True
                MessageBox.Show(
                    "WebView2 Runtime is not available. LCARS tried to install it from Microsoft automatically.",
                    "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            End If
            Return
        End If

        Try
            Dim env As CoreWebView2Environment
            If WebView2RuntimeInstaller.EvergreenRuntimeAvailable() Then
                env = Await CoreWebView2Environment.CreateAsync()
            Else
                env = Await CoreWebView2Environment.CreateAsync(WebView2RuntimeInstaller.GetBundledRuntimeFolder())
            End If
            Await _webView.EnsureCoreWebView2Async(env)
            WireWebViewEvents()
        Catch ex As Exception
            If Not runtimeWarningShown Then
                runtimeWarningShown = True
                MessageBox.Show("Unable to start WebView2." & vbCrLf & ex.Message, "LCARS Web Browser", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End Try
    End Function

    ''' <summary>
    ''' Attaches navigation and UI sync handlers to the hosted WebView2.
    ''' </summary>
    Private Sub WireWebViewEvents()
        AddHandler _webView.NavigationStarting, AddressOf OnNavigationStarting
        AddHandler _webView.NavigationCompleted, AddressOf OnNavigationCompleted
        If _webView.CoreWebView2 IsNot Nothing Then
            AddHandler _webView.CoreWebView2.DocumentTitleChanged, AddressOf OnDocumentTitleChanged
            AddHandler _webView.CoreWebView2.SourceChanged, AddressOf OnSourceChanged
            AddHandler _webView.CoreWebView2.NewWindowRequested, AddressOf OnNewWindowRequested
        End If
    End Sub

    Private Sub OnNavigationStarting(ByVal sender As Object, ByVal e As CoreWebView2NavigationStartingEventArgs)
        RaiseEvent NavigationStarting(Me, e)
    End Sub

    Private Sub OnNavigationCompleted(ByVal sender As Object, ByVal e As CoreWebView2NavigationCompletedEventArgs)
        RaiseEvent NavigationCompleted(Me, e)
    End Sub

    Private Sub OnDocumentTitleChanged(ByVal sender As Object, ByVal e As Object)
        RaiseEvent DocumentTitleChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnSourceChanged(ByVal sender As Object, ByVal e As CoreWebView2SourceChangedEventArgs)
        RaiseEvent SourceChanged(Me, e)
    End Sub

    Private Sub OnNewWindowRequested(ByVal sender As Object, ByVal e As CoreWebView2NewWindowRequestedEventArgs)
        RaiseEvent NewWindowRequested(Me, e)
    End Sub

    ''' <summary>
    ''' Releases the hosted WebView2 control.
    ''' </summary>
    Public Sub Dispose()
        If _webView IsNot Nothing Then
            _webView.Dispose()
        End If
    End Sub
End Class
