Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

''' <summary>
''' Transparent ink overlay that docks over a host view and captures stylus/mouse input.
''' </summary>
''' <remarks>
''' Ink API choice: Microsoft.Ink is not present in .NET Framework 4.8 x86 reference assemblies
''' on this build host, so strokes are captured as polylines via WM_POINTER (pen pressure when
''' available) with MouseDown/Move/Up fallback for mouse/touch paths.
''' </remarks>
Public Class InkOverlayControl
    Inherits Control

    Private Const WM_POINTERDOWN As Integer = 581
    Private Const WM_POINTERUPDATE As Integer = 582
    Private Const WM_POINTERUP As Integer = 583
    Private Const WM_POINTERCAPTURECHANGED As Integer = 588

    Private Const PT_TOUCH As Integer = 2
    Private Const PT_PEN As Integer = 3
    Private Const PT_MOUSE As Integer = 4

    Private Const POINTER_FLAG_INCONTACT As UInteger = &H4000UI

    Private ReadOnly _document As InkDocument
    Private _currentStroke As InkStroke
    Private _inkSurface As Bitmap
    Private _pointerDrawing As Boolean = False
    Private _mouseDrawing As Boolean = False
    Private _activePointerId As Integer = -1

    Private _penColor As Color = Color.FromArgb(255, 153, 0)
    Private _penWidth As Single = 3.0F
    Private _eraserMode As Boolean = False
    Private _eraserWidth As Single = 24.0F
    Private _minPressureWidth As Single = 1.0F
    Private _maxPressureWidth As Single = 8.0F

    Public Event StrokeCompleted As EventHandler

    Public Sub New()
        _document = New InkDocument()

        SetStyle(ControlStyles.AllPaintingInWmPaint, True)
        SetStyle(ControlStyles.OptimizedDoubleBuffer, True)
        SetStyle(ControlStyles.UserPaint, True)
        SetStyle(ControlStyles.SupportsTransparentBackColor, True)

        BackColor = Color.Transparent
        Dock = DockStyle.Fill
        TabStop = False

        AddHandler _document.DocumentChanged, AddressOf OnDocumentChanged
    End Sub

    <Browsable(False)>
    Public ReadOnly Property Document As InkDocument
        Get
            Return _document
        End Get
    End Property

    Public Property PenColor As Color
        Get
            Return _penColor
        End Get
        Set(ByVal value As Color)
            _penColor = value
        End Set
    End Property

    Public Property PenWidth As Single
        Get
            Return _penWidth
        End Get
        Set(ByVal value As Single)
            _penWidth = Math.Max(1.0F, value)
        End Set
    End Property

    Public Property EraserMode As Boolean
        Get
            Return _eraserMode
        End Get
        Set(ByVal value As Boolean)
            _eraserMode = value
        End Set
    End Property

    Public Property EraserWidth As Single
        Get
            Return _eraserWidth
        End Get
        Set(ByVal value As Single)
            _eraserWidth = Math.Max(4.0F, value)
        End Set
    End Property

    ''' <summary>
    ''' Rebuilds the ink bitmap from the bound document.
    ''' </summary>
    Public Sub RefreshInkSurface()
        EnsureInkSurface()
        Using graphics As Graphics = Graphics.FromImage(_inkSurface)
            graphics.Clear(Color.Transparent)
            _document.DrawStrokes(graphics, _inkSurface.Width, _inkSurface.Height)
        End Using
        Invalidate()
    End Sub

    Protected Overrides Sub OnHandleCreated(ByVal e As EventArgs)
        MyBase.OnHandleCreated(e)
        EnableMouseInPointer(True)
        EnsureInkSurface()
    End Sub

    Protected Overrides Sub OnResize(ByVal e As EventArgs)
        MyBase.OnResize(e)
        ResizeInkSurface()
        SyncDocumentCanvasSize()
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        If _inkSurface IsNot Nothing Then
            e.Graphics.DrawImageUnscaled(_inkSurface, 0, 0)
        End If

        If _currentStroke IsNot Nothing AndAlso _currentStroke.Points.Count > 0 Then
            _document.DrawSingleStroke(e.Graphics, _currentStroke, ClientSize.Width, ClientSize.Height)
        End If
    End Sub

    Protected Overrides Sub OnMouseDown(ByVal e As MouseEventArgs)
        MyBase.OnMouseDown(e)
        If _pointerDrawing Then Return
        If e.Button <> MouseButtons.Left Then Return

        If _mouseDrawing Then
            ResetPointerState()
        End If

        _mouseDrawing = True
        Capture = True
        BeginStroke(e.X, e.Y, MousePressure(e))
    End Sub

    Protected Overrides Sub OnMouseMove(ByVal e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If Not _mouseDrawing OrElse _pointerDrawing Then Return
        ContinueStroke(e.X, e.Y, MousePressure(e))
    End Sub

    Protected Overrides Sub OnMouseUp(ByVal e As MouseEventArgs)
        MyBase.OnMouseUp(e)
        If Not _mouseDrawing OrElse _pointerDrawing Then Return
        If e.Button <> MouseButtons.Left Then Return

        _mouseDrawing = False
        Capture = False
        EndStroke()
    End Sub

    Protected Overrides Sub OnMouseCaptureChanged(ByVal e As EventArgs)
        MyBase.OnMouseCaptureChanged(e)
        If Not Capture AndAlso _mouseDrawing Then
            _mouseDrawing = False
            EndStroke()
        End If
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_POINTERCAPTURECHANGED Then
            If _pointerDrawing AndAlso _activePointerId >= 0 Then
                If IsPointerInContact(CUInt(_activePointerId)) Then
                    MyBase.WndProc(m)
                    Return
                End If
            End If
            If _pointerDrawing Then
                ResetPointerState()
                EndStroke()
            End If
            MyBase.WndProc(m)
            Return
        End If

        If m.Msg = WM_POINTERDOWN OrElse m.Msg = WM_POINTERUPDATE OrElse m.Msg = WM_POINTERUP Then
            If HandlePointerMessage(m) Then
                Return
            End If
        End If

        MyBase.WndProc(m)
    End Sub

    Private Function HandlePointerMessage(ByRef m As Message) As Boolean
        Dim pointerId As Integer = GetPointerId(m)
        Dim info As POINTER_INFO
        If Not GetPointerInfo(CUInt(pointerId), info) Then
            Return False
        End If

        Dim pointerType As Integer = CInt(info.pointerType)

        Dim clientPoint As System.Drawing.Point = PointFromPointer(info.ptPixelLocation)
        Dim pressure As Single = 0.5F

        If pointerType = PT_PEN Then
            Dim penInfo As POINTER_PEN_INFO
            If GetPointerPenInfo(CUInt(pointerId), penInfo) Then
                pressure = NormalizePressure(CInt(penInfo.pressure))
            End If
        ElseIf pointerType = PT_TOUCH OrElse pointerType = PT_MOUSE Then
            pressure = 0.5F
        Else
            Return False
        End If

        If m.Msg = WM_POINTERDOWN Then
            If _mouseDrawing Then
                _mouseDrawing = False
                Capture = False
            End If
            If _pointerDrawing Then Return True
            _pointerDrawing = True
            _activePointerId = pointerId
            BeginStroke(clientPoint.X, clientPoint.Y, pressure)
            Return True
        End If

        If m.Msg = WM_POINTERUPDATE Then
            If Not _pointerDrawing OrElse pointerId <> _activePointerId Then Return True
            ' Do not require POINTER_FLAG_INCONTACT — many stylus/touch stacks omit it on MOVE,
            ' which produced only single-point "dots" on the tablet.
            ContinueStroke(clientPoint.X, clientPoint.Y, pressure)
            Return True
        End If

        If m.Msg = WM_POINTERUP Then
            If Not _pointerDrawing OrElse pointerId <> _activePointerId Then
                ResetPointerState()
                Return True
            End If
            ResetPointerState()
            EndStroke()
            Return True
        End If

        Return False
    End Function

    Private Sub ResetPointerState()
        _pointerDrawing = False
        _activePointerId = -1
    End Sub

    Private Sub BeginStroke(ByVal x As Integer, ByVal y As Integer, ByVal pressure As Single)
        SyncDocumentCanvasSize()

        _currentStroke = New InkStroke()
        _currentStroke.IsEraser = _eraserMode
        _currentStroke.Color = _penColor
        _currentStroke.Width = StrokeWidthForPressure(pressure)
        _currentStroke.Points.Add(New InkPoint(x, y, pressure))
        Invalidate()
    End Sub

    Private Sub ContinueStroke(ByVal x As Integer, ByVal y As Integer, ByVal pressure As Single)
        If _currentStroke Is Nothing Then Return

        Dim lastPoint As InkPoint = _currentStroke.Points(_currentStroke.Points.Count - 1)
        ' Accept near-identical points when pressure changes; only skip exact duplicates.
        If CInt(lastPoint.X) = x AndAlso CInt(lastPoint.Y) = y Then Return

        _currentStroke.Width = StrokeWidthForPressure(pressure)
        _currentStroke.Points.Add(New InkPoint(x, y, pressure))
        Invalidate()
        Update()
    End Sub

    Private Sub EndStroke()
        If _currentStroke Is Nothing OrElse _currentStroke.Points.Count = 0 Then
            _currentStroke = Nothing
            Return
        End If

        _document.AddStroke(_currentStroke)
        _currentStroke = Nothing
        RaiseEvent StrokeCompleted(Me, EventArgs.Empty)
    End Sub

    Private Function StrokeWidthForPressure(ByVal pressure As Single) As Single
        If _eraserMode Then
            Return _eraserWidth
        End If

        Dim clamped As Single = Math.Max(0.0F, Math.Min(1.0F, pressure))

        If _penWidth > 0 Then
            Dim pressureFactor As Single = 0.25F + (0.75F * clamped)
            Return Math.Max(1.0F, _penWidth * pressureFactor)
        End If

        Return _minPressureWidth + ((_maxPressureWidth - _minPressureWidth) * clamped)
    End Function

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            RemoveHandler _document.DocumentChanged, AddressOf OnDocumentChanged

            If _inkSurface IsNot Nothing Then
                _inkSurface.Dispose()
                _inkSurface = Nothing
            End If
        End If

        MyBase.Dispose(disposing)
    End Sub

    Private Sub OnDocumentChanged(ByVal sender As Object, ByVal e As EventArgs)
        RefreshInkSurface()
    End Sub

    Private Sub EnsureInkSurface()
        Dim width As Integer = Math.Max(1, ClientSize.Width)
        Dim height As Integer = Math.Max(1, ClientSize.Height)

        If _inkSurface IsNot Nothing AndAlso _inkSurface.Width = width AndAlso _inkSurface.Height = height Then
            Return
        End If

        If _inkSurface IsNot Nothing Then
            _inkSurface.Dispose()
        End If

        _inkSurface = New Bitmap(width, height, Imaging.PixelFormat.Format32bppArgb)
    End Sub

    Private Sub ResizeInkSurface()
        EnsureInkSurface()
        RefreshInkSurface()
    End Sub

    Private Sub SyncDocumentCanvasSize()
        If ClientSize.Width > 0 AndAlso ClientSize.Height > 0 Then
            _document.CanvasWidth = ClientSize.Width
            _document.CanvasHeight = ClientSize.Height
        End If
    End Sub

    Private Function PointFromPointer(ByVal screenPoint As NativePoint) As System.Drawing.Point
        Return PointToClient(New System.Drawing.Point(screenPoint.X, screenPoint.Y))
    End Function

    Private Shared Function GetPointerId(ByRef m As Message) As Integer
        Return (m.WParam.ToInt32() And 65535)
    End Function

    Private Shared Function NormalizePressure(ByVal rawPressure As Integer) As Single
        If rawPressure <= 0 Then Return 0.5F
        Dim normalized As Single = rawPressure / 1024.0F
        Return Math.Max(0.0F, Math.Min(1.0F, normalized))
    End Function

    Private Shared Function MousePressure(ByVal e As MouseEventArgs) As Single
        Dim extra As Integer = GetMessageExtraInfo().ToInt32()
        If extra <> 0 Then
            Return 0.75F
        End If
        Return 0.5F
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetMessageExtraInfo() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetPointerPenInfo(ByVal pointerId As UInt32, ByRef penInfo As POINTER_PEN_INFO) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetPointerInfo(ByVal pointerId As UInt32, ByRef pointerInfo As POINTER_INFO) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function IsPointerInContact(ByVal pointerId As UInt32) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function EnableMouseInPointer(ByVal enable As Boolean) As Boolean
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure NativePoint
        Public X As Integer
        Public Y As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure POINTER_INFO
        Public pointerType As UInt32
        Public pointerId As UInt32
        Public frameId As UInt32
        Public pointerFlags As UInt32
        Public sourceDevice As IntPtr
        Public hwndTarget As IntPtr
        Public ptPixelLocation As NativePoint
        Public ptHimetricLocation As NativePoint
        Public ptPixelLocationRaw As NativePoint
        Public ptHimetricLocationRaw As NativePoint
        Public dwTime As UInt32
        Public historyCount As UInt32
        Public inputData As Int32
        Public dwKeyStates As UInt32
        Public PerformanceCount As UInt64
        Public ButtonChangeType As UInt32
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure POINTER_PEN_INFO
        Public pointerInfo As POINTER_INFO
        Public penFlags As UInt32
        Public penMask As UInt32
        Public pressure As UInt32
        Public rotation As UInt32
        Public tiltX As Int32
        Public tiltY As Int32
    End Structure
End Class
