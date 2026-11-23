' LCARSutilities/LCARSutilities/Forms/GraphCanvas.vb
Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
''' <summary>
''' Graph surface
with mouse-wheel zoom, drag-pan, tap-to-trace, and Win32 pinch/pan gestures.
''' </summary>
Friend Class GraphCanvas
Inherits PictureBox
Public Event WindowChanged As EventHandler
Public Event TraceRequested(ByVal sender As Object, ByVal location As Point)
Private Const WM_GESTURENOTIFY As Integer = &H11A
Private Const WM_GESTURE As Integer = &H119
Private Const GF_BEGIN As Integer = 1
Private Const GID_ZOOM As Integer = 3
Private Const GID_PAN As Integer = 4
Private Const GC_ALLGESTURES As Integer = &H1
Private dragActive As Boolean = False
Private dragMoved As Boolean = False
Private dragStart As Point
Private lastDrag As Point
Private Const DragThresholdPx As Integer = 6
Private gestureZoomArguments As Long = 0
Private gestureConfigured As Boolean = False
Private xmin As Double = -10
Private xmax As Double = 10
Private ymin As Double = -10
Private ymax As Double = 10
Private Const MinSpan As Double = 0.000001
Private Const MaxSpan As Double = 1000000000.0 <StructLayout(LayoutKind.Sequential)>
Private Structure GESTURECONFIG
Public dwID As Integer
Public dwWant As Integer
Public dwBlock As Integer
End Structure <StructLayout(LayoutKind.Sequential)>
Private Structure POINTS
Public x As Short
Public y As Short
End Structure <StructLayout(LayoutKind.Sequential)>
Private Structure GESTUREINFO
Public cbSize As Integer
Public dwFlags As Integer
Public dwID As Integer
Public hwndTarget As IntPtr
Public ptsLocation As POINTS
Public dwInstanceID As Integer
Public dwSequenceID As Integer
Public ullArguments As ULong
Public cbExtraArgs As Integer
End Structure <DllImport("user32.dll")>
Private Shared Function SetGestureConfig(ByVal hWnd As IntPtr, ByVal dwReserved As Integer, ByVal cIDs As Integer, ByRef pGestureConfig As GESTURECONFIG, ByVal cbSize As Integer) As Boolean
End Function <DllImport("user32.dll")>
Private Shared Function GetGestureInfo(ByVal hGestureInfo As IntPtr, ByRef pGestureInfo As GESTUREINFO) As Boolean
End Function <DllImport("user32.dll")>
Private Shared Function CloseGestureInfoHandle(ByVal hGestureInfo As IntPtr) As Boolean
End Function
Public Sub SetWindow(ByVal xMin As Double, ByVal xMax As Double, ByVal yMin As Double, ByVal yMax As Double) xmin = xMin xmax = xMax ymin = yMin ymax = yMax NormalizeWindow()
End Sub
Public Sub GetWindow(ByRef xMin As Double, ByRef xMax As Double, ByRef yMin As Double, ByRef yMax As Double) xMin = xmin xMax = xmax yMin = ymin yMax = ymax
End Sub
Protected Overrides Sub OnHandleCreated(ByVal e As EventArgs)
MyBase.OnHandleCreated(e) ConfigureGestures()
End Sub
Private Sub ConfigureGestures()
If Not IsHandleCreated OrElse gestureConfigured Then Return
Try
Dim cfg As New GESTURECONFIG()
cfg.dwID = 0 cfg.dwWant = GC_ALLGESTURES cfg.dwBlock = 0
If SetGestureConfig(Handle, 0, 1, cfg, Marshal.SizeOf(GetType(GESTURECONFIG))) Then gestureConfigured = True
End If
Catch
End Try
End Sub
Protected Overrides Sub WndProc(ByRef m As Message)
If m.Msg = WM_GESTURENOTIFY Then ConfigureGestures()
ElseIf m.Msg = WM_GESTURE Then If HandleGesture(m) Then m.Result = New IntPtr(1)
Return
End If
End If MyBase.WndProc(m)
End Sub
Private Function HandleGesture(ByRef m As Message) As Boolean
Dim gi As New GESTUREINFO()
gi.cbSize = Marshal.SizeOf(GetType(GESTUREINFO))
If Not GetGestureInfo(m.LParam, gi) Then Return False
Try
Dim loc As New Point(CInt(gi.ptsLocation.x), CInt(gi.ptsLocation.y)) loc = PointToClient(loc)
Select Case gi.dwID
Case GID_ZOOM
If (gi.dwFlags And GF_BEGIN) <> 0 Then gestureZoomArguments = CLng(gi.ullArguments)
ElseIf gestureZoomArguments > 0 Then Dim factor As Double = CDbl(gi.ullArguments) / CDbl(gestureZoomArguments)
If factor > 0.01 AndAlso factor < 100 Then ZoomAt(loc, 1.0 / factor) gestureZoomArguments = CLng(gi.ullArguments)
End If
End If
Return True
Case GID_PAN ' ullArguments packs delta as two 16-bit signed values in lower dword on many builds; ' fall back to drag path — pan gestures often arrive as mouse moves too.
Return False
End Select
Finally CloseGestureInfoHandle(m.LParam)
End Try
Return False
End Function
Protected Overrides Sub OnMouseDown(ByVal e As MouseEventArgs)
MyBase.OnMouseDown(e)
If e.Button = MouseButtons.Left Then dragActive = True dragMoved = False dragStart = e.Location lastDrag = e.Location Capture = True
End If Focus()
End Sub
Protected Overrides Sub OnMouseMove(ByVal e As MouseEventArgs)
MyBase.OnMouseMove(e)
If Not dragActive OrElse e.Button <> MouseButtons.Left Then Return
Dim dx As Integer = e.X - lastDrag.X
Dim dy As Integer = e.Y - lastDrag.Y
If Not dragMoved Then Dim adx As Integer = Math.Abs(e.X - dragStart.X)
Dim ady As Integer = Math.Abs(e.Y - dragStart.Y)
If adx < DragThresholdPx AndAlso ady < DragThresholdPx Then Return dragMoved = True
End If
If dx <> 0 OrElse dy <> 0 Then PanByPixels(dx, dy) lastDrag = e.Location
End If
End Sub
Protected Overrides Sub OnMouseUp(ByVal e As MouseEventArgs)
MyBase.OnMouseUp(e)
If e.Button = MouseButtons.Left AndAlso dragActive Then If Not dragMoved Then RaiseEvent TraceRequested(Me, e.Location) ' tap (not a drag) sets TRACE
End If dragActive = False Capture = False
End If
End Sub
Protected Overrides Sub OnMouseWheel(ByVal e As MouseEventArgs)
MyBase.OnMouseWheel(e)
Dim factor As Double = If(e.Delta > 0, 0.85, 1.0 / 0.85) ZoomAt(e.Location, factor)
End Sub
Public Sub ZoomAt(ByVal focalPx As Point, ByVal factor As Double)
If factor <= 0 OrElse Not IsFinite(factor) Then Return NormalizeWindow()
Dim gx As Double = UnmapX(focalPx.X)
Dim gy As Double = UnmapY(focalPx.Y)
Dim xSpan As Double = (xmax - xmin) * factor
Dim ySpan As Double = (ymax - ymin) * factor
If xSpan < MinSpan Then xSpan = MinSpan
If ySpan < MinSpan Then ySpan = MinSpan
If xSpan > MaxSpan Then xSpan = MaxSpan
If ySpan > MaxSpan Then ySpan = MaxSpan
Dim rx As Double = (gx - xmin) / (xmax - xmin)
Dim ry As Double = (ymax - gy) / (ymax - ymin)
If Not IsFinite(rx) Then rx = 0.5
If Not IsFinite(ry) Then ry = 0.5 xmin = gx - rx * xSpan xmax = xmin + xSpan ymax = gy + ry * ySpan ymin = ymax - ySpan NormalizeWindow()
RaiseEvent WindowChanged(Me, EventArgs.Empty)
End Sub
Public Sub PanByPixels(ByVal dx As Integer, ByVal dy As Integer)
Dim w As Integer = Math.Max(1, ClientSize.Width - 1)
Dim h As Integer = Math.Max(1, ClientSize.Height - 1)
Dim xPer As Double = (xmax - xmin) / w
Dim yPer As Double = (ymax - ymin) / h xmin -= dx * xPer xmax -= dx * xPer ymin += dy * yPer ymax += dy * yPer
RaiseEvent WindowChanged(Me, EventArgs.Empty)
End Sub
Public Function UnmapX(ByVal px As Integer) As Double
Dim w As Integer = Math.Max(1, ClientSize.Width - 1)
Return xmin + (xmax - xmin) * px / w
End Function
Public Function UnmapY(ByVal py As Integer) As Double
Dim h As Integer = Math.Max(1, ClientSize.Height - 1)
Return ymax - (ymax - ymin) * py / h
End Function
Private Sub NormalizeWindow()
If Not IsFinite(xmin) OrElse Not IsFinite(xmax) OrElse xmax <= xmin Then xmin = -10 xmax = 10
End If
If Not IsFinite(ymin) OrElse Not IsFinite(ymax) OrElse ymax <= ymin Then ymin = -10 ymax = 10
End If
End Sub
Private Shared Function IsFinite(ByVal v As Double) As Boolean
Return Not Double.IsNaN(v) AndAlso Not Double.IsInfinity(v)
End Function
End Class
