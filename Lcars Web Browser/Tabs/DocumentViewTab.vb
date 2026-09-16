Imports Microsoft.Web.WebView2.Core
Imports Microsoft.Web.WebView2.WinForms
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Windows.Forms

''' <summary>
''' IBrowserTab host for local PDF and image viewing via WebView2 with PictureBox fallback for images.
''' </summary>
Public Class DocumentViewTab
    Implements IBrowserTab

    Private Shared ReadOnly ImageExtensions As String() = {".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp"}

    Private Shared runtimeWarningShown As Boolean = False

    Private ReadOnly _hostPanel As Panel
    Private ReadOnly _webView As WebView2
    Private ReadOnly _pictureBox As PictureBox
    Private ReadOnly _inkOverlay As InkOverlayControl

    Private _filePath As String = ""
    Private _usingPictureFallback As Boolean = False
    Private _annotateMode As Boolean = False
    Private _inkDirty As Boolean = False

    Public Event NavigationStarting As EventHandler(Of CoreWebView2NavigationStartingEventArgs)
    Public Event NavigationCompleted As EventHandler(Of CoreWebView2NavigationCompletedEventArgs)
    Public Event ContentChanged As EventHandler

    Public Sub New()
        _hostPanel = New Panel()
        _hostPanel.Dock = DockStyle.Fill

        _webView = New WebView2()
        _webView.Name = "DocumentWebView2"
        _webView.Dock = DockStyle.Fill

        _pictureBox = New PictureBox()
        _pictureBox.Name = "DocumentPictureBox"
        _pictureBox.Dock = DockStyle.Fill
        _pictureBox.SizeMode = PictureBoxSizeMode.Zoom
        _pictureBox.BackColor = Drawing.Color.Black
        _pictureBox.Visible = False

        _inkOverlay = New InkOverlayControl()
        _inkOverlay.Visible = False

        _hostPanel.Controls.Add(_inkOverlay)
        _hostPanel.Controls.Add(_pictureBox)
        _hostPanel.Controls.Add(_webView)

        AddHandler _inkOverlay.StrokeCompleted, AddressOf OnInkStrokeCompleted
    End Sub

    Public ReadOnly Property WebView As WebView2
        Get
            Return _webView
        End Get
    End Property

    Public ReadOnly Property InkOverlay As InkOverlayControl
        Get
            Return _inkOverlay
        End Get
    End Property

    Public Property AnnotateMode As Boolean
        Get
            Return _annotateMode
        End Get
        Set(ByVal value As Boolean)
            If _annotateMode = value Then Return
            _annotateMode = value
            _inkOverlay.Visible = _annotateMode
            If _annotateMode Then
                _inkOverlay.BringToFront()
                _inkOverlay.Focus()
            End If
            RaiseEvent ContentChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Public ReadOnly Property TabKind As String Implements IBrowserTab.TabKind
        Get
            Return "View"
        End Get
    End Property

    Public ReadOnly Property Title As String Implements IBrowserTab.Title
        Get
            If String.IsNullOrEmpty(_filePath) Then Return "DOCUMENT"
            Dim name As String = System.IO.Path.GetFileName(_filePath)
            If _inkDirty Then Return name & "*"
            Return name
        End Get
    End Property

    Public ReadOnly Property IsDirty As Boolean Implements IBrowserTab.IsDirty
        Get
            Return _inkDirty
        End Get
    End Property

    Public ReadOnly Property ContentControl As Control Implements IBrowserTab.ContentControl
        Get
            Return _hostPanel
        End Get
    End Property

    Public Function GetPathOrUrl() As String Implements IBrowserTab.GetPathOrUrl
        Return _filePath
    End Function

    Public Sub NavigateOrOpen(ByVal pathOrUrl As String) Implements IBrowserTab.NavigateOrOpen
        Dim path As String = ResolveLocalPath(pathOrUrl)
        If String.IsNullOrEmpty(path) OrElse Not File.Exists(path) Then Return

        _filePath = path
        _usingPictureFallback = False
        _inkDirty = False
        HidePictureFallback()

        TryLoadInkSidecar()

        If _webView.CoreWebView2 Is Nothing Then Return

        Dim target As String = ToFileUri(path)
        _webView.CoreWebView2.Navigate(target)
    End Sub

    Public Function Save(ByVal path As String) As Boolean Implements IBrowserTab.Save
        If String.IsNullOrWhiteSpace(path) Then Return False

        Try
            Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
            If ext = ".png" OrElse ext = ".jpg" OrElse ext = ".jpeg" OrElse ext = ".bmp" OrElse ext = ".gif" OrElse ext = ".webp" Then
                If Not HasInkStrokes() Then Return False
                ExportAnnotatedImage(path)
                Return True
            End If

            If Not _inkDirty AndAlso Not (_annotateMode AndAlso IsSidecarPath(path) AndAlso HasInkStrokes()) Then Return False
            SaveInkSidecar()
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Sub FocusContent() Implements IBrowserTab.FocusContent
        If _annotateMode Then
            _inkOverlay.Focus()
        ElseIf _usingPictureFallback Then
            _pictureBox.Focus()
        Else
            _webView.Focus()
        End If
    End Sub

    ''' <summary>
    ''' Persists ink strokes to the sidecar .lcarsink beside the viewed file.
    ''' </summary>
    Public Sub SaveInkSidecar()
        If String.IsNullOrEmpty(_filePath) Then Return

        Dim sidecarPath As String = GetInkSidecarPath(_filePath)
        EnsureParentDirectory(sidecarPath)
        SyncInkDocumentSize()
        _inkOverlay.Document.Save(sidecarPath)
        _inkDirty = False
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Flattens the base image and ink strokes into a PNG file.
    ''' </summary>
    Public Sub ExportAnnotatedImage(ByVal outputPath As String)
        If Not IsImagePath(_filePath) Then
            SaveInkSidecar()
            Return
        End If

        EnsureParentDirectory(outputPath)
        SyncInkDocumentSize()

        Using baseImage As Image = Image.FromFile(_filePath)
            Using bitmap As New Bitmap(baseImage.Width, baseImage.Height, PixelFormat.Format32bppArgb)
                Using graphics As Graphics = Graphics.FromImage(bitmap)
                    graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                    graphics.DrawImage(baseImage, 0, 0, baseImage.Width, baseImage.Height)
                    _inkOverlay.Document.DrawStrokes(graphics, baseImage.Width, baseImage.Height, includeEraser:=False)
                End Using
                bitmap.Save(outputPath, ImageFormat.Png)
            End Using
        End Using

        SaveInkSidecar()
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
    ''' Attaches navigation handlers to the hosted WebView2.
    ''' </summary>
    Private Sub WireWebViewEvents()
        AddHandler _webView.NavigationStarting, AddressOf OnNavigationStarting
        AddHandler _webView.NavigationCompleted, AddressOf OnNavigationCompleted
    End Sub

    Private Sub OnNavigationStarting(ByVal sender As Object, ByVal e As CoreWebView2NavigationStartingEventArgs)
        RaiseEvent NavigationStarting(Me, e)
    End Sub

    Private Sub OnNavigationCompleted(ByVal sender As Object, ByVal e As CoreWebView2NavigationCompletedEventArgs)
        If Not e.IsSuccess AndAlso IsImagePath(_filePath) Then
            Try
                ShowPictureFallback(_filePath)
            Catch
            End Try
        End If
        RaiseEvent NavigationCompleted(Me, e)
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub OnInkStrokeCompleted(ByVal sender As Object, ByVal e As EventArgs)
        If _inkDirty Then Return
        _inkDirty = True
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Shows the image in a PictureBox when WebView2 cannot render it.
    ''' </summary>
    Private Sub ShowPictureFallback(ByVal path As String)
        Dim previous As Drawing.Image = _pictureBox.Image
        _pictureBox.Image = Drawing.Image.FromFile(path)
        If previous IsNot Nothing Then previous.Dispose()

        _usingPictureFallback = True
        _webView.Visible = False
        _pictureBox.Visible = True
        If _annotateMode Then
            _inkOverlay.BringToFront()
        End If
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub HidePictureFallback()
        _webView.Visible = True
        _pictureBox.Visible = False
    End Sub

    Private Sub TryLoadInkSidecar()
        If String.IsNullOrEmpty(_filePath) Then Return

        Dim sidecarPath As String = GetInkSidecarPath(_filePath)
        If Not File.Exists(sidecarPath) Then
            _inkOverlay.Document.Clear()
            Return
        End If

        Try
            _inkOverlay.Document.Load(sidecarPath)
            _inkOverlay.RefreshInkSurface()
        Catch
            _inkOverlay.Document.Clear()
        End Try
    End Sub

    Private Sub SyncInkDocumentSize()
        If _hostPanel.ClientSize.Width > 0 AndAlso _hostPanel.ClientSize.Height > 0 Then
            _inkOverlay.Document.CanvasWidth = _hostPanel.ClientSize.Width
            _inkOverlay.Document.CanvasHeight = _hostPanel.ClientSize.Height
        End If
    End Sub

    Private Function HasInkStrokes() As Boolean
        Return _inkOverlay.Document.Strokes.Count > 0
    End Function

    ''' <summary>
    ''' Marks ink edits (including clear) as unsaved changes.
    ''' </summary>
    Public Sub MarkInkModified()
        If _inkDirty Then Return
        _inkDirty = True
        RaiseEvent ContentChanged(Me, EventArgs.Empty)
    End Sub

    Private Function IsSidecarPath(ByVal path As String) As Boolean
        If String.IsNullOrEmpty(_filePath) Then Return False
        Return String.Equals(path, GetInkSidecarPath(_filePath), StringComparison.OrdinalIgnoreCase)
    End Function

    Public Shared Function GetInkSidecarPath(ByVal filePath As String) As String
        Dim directory As String = System.IO.Path.GetDirectoryName(filePath)
        Dim baseName As String = System.IO.Path.GetFileNameWithoutExtension(filePath)
        Return System.IO.Path.Combine(directory, baseName & ".lcarsink")
    End Function

    Private Shared Function IsImagePath(ByVal path As String) As Boolean
        If String.IsNullOrEmpty(path) Then Return False
        Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
        Return ImageExtensions.Contains(ext)
    End Function

    Private Shared Sub EnsureParentDirectory(ByVal path As String)
        Dim parentDir As String = System.IO.Path.GetDirectoryName(path)
        If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
            Directory.CreateDirectory(parentDir)
        End If
    End Sub

    Private Shared Function ResolveLocalPath(ByVal pathOrUrl As String) As String
        Dim text As String = If(pathOrUrl, "").Trim()
        If text = "" Then Return ""

        If text.StartsWith("file://", StringComparison.OrdinalIgnoreCase) Then
            Try
                Return New Uri(text).LocalPath
            Catch
                Return text
            End Try
        End If

        Return text
    End Function

    Private Shared Function ToFileUri(ByVal path As String) As String
        Return New Uri(path).AbsoluteUri
    End Function

    ''' <summary>
    ''' Releases hosted controls and image resources.
    ''' </summary>
    Public Sub Dispose()
        If _inkOverlay IsNot Nothing Then
            _inkOverlay.Dispose()
        End If
        If _pictureBox.Image IsNot Nothing Then
            _pictureBox.Image.Dispose()
            _pictureBox.Image = Nothing
        End If
        If _webView IsNot Nothing Then
            _webView.Dispose()
        End If
        If _hostPanel IsNot Nothing Then
            _hostPanel.Dispose()
        End If
    End Sub
End Class
