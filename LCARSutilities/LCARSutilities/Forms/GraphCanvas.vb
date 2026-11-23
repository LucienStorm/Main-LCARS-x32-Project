Imports System
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports Microsoft.VisualBasic.CompilerServices

    Friend Class GraphCanvas
        Inherits PictureBox
        Public Delegate Sub TraceRequestedEventHandler(sender As Object, location As Point)

        Private Structure GESTURECONFIG
            Public dwID As Integer

            Public dwWant As Integer

            Public dwBlock As Integer
        End Structure

        Private Structure POINTS
            Public x As Short

            Public y As Short
        End Structure

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
        End Structure

        Private Const WM_GESTURENOTIFY As Integer = 282

        Private Const WM_GESTURE As Integer = 281

        Private Const GF_BEGIN As Integer = 1

        Private Const GID_ZOOM As Integer = 3

        Private Const GID_PAN As Integer = 4

        Private Const GC_ALLGESTURES As Integer = 1

        Private dragActive As Boolean

        Private dragMoved As Boolean

        Private dragStart As Point

        Private lastDrag As Point

        Private Const DragThresholdPx As Integer = 6

        Private gestureZoomArguments As Long

        Private gestureConfigured As Boolean

        Private xmin As Double

        Private xmax As Double

        Private ymin As Double

        Private ymax As Double

        Private Const MinSpan As Double = 1E-06

        Private Const MaxSpan As Double = 1000000000.0
                

        Public Sub New()
            dragActive = False
            dragMoved = False
            gestureZoomArguments = 0L
            gestureConfigured = False
            xmin = -10.0
            xmax = 10.0
            ymin = -10.0
            ymax = 10.0
        End Sub

        <DllImport("user32.dll")>
        Private Shared Function SetGestureConfig(hWnd As IntPtr, dwReserved As Integer, cIDs As Integer, ByRef pGestureConfig As GESTURECONFIG, cbSize As Integer) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Shared Function GetGestureInfo(hGestureInfo As IntPtr, ByRef pGestureInfo As GESTUREINFO) As Boolean
        End Function

        <DllImport("user32.dll")>
        Private Shared Function CloseGestureInfoHandle(hGestureInfo As IntPtr) As Boolean
        End Function

        Public Sub SetWindow(xMin As Double, xMax As Double, yMin As Double, yMax As Double)
            xMin = xMin
            xMax = xMax
            yMin = yMin
            yMax = yMax
            NormalizeWindow()
        End Sub

        Public Sub GetWindow(ByRef xMin As Double, ByRef xMax As Double, ByRef yMin As Double, ByRef yMax As Double)
            xMin = xMin
            xMax = xMax
            yMin = yMin
            yMax = yMax
        End Sub

        Protected Overrides Sub OnHandleCreated(e As EventArgs)
            MyBase.OnHandleCreated(e)
            ConfigureGestures()
        End Sub

        Private Sub ConfigureGestures()
            If Not IsHandleCreated OrElse gestureConfigured Then
                Return
            End If
            Try
                Dim pGestureConfig As GESTURECONFIG = New GESTURECONFIG With {
        .dwID = 0,
        .dwWant = 1,
        .dwBlock = 0
    }
                If SetGestureConfig(Handle, 0, 1, pGestureConfig, Marshal.SizeOf(GetType(GESTURECONFIG))) Then
                    gestureConfigured = True
                End If
            Catch projectError As Exception
                ProjectData.SetProjectError(projectError)
                Call ProjectData.ClearProjectError()
            End Try
        End Sub

        Protected Overrides Sub WndProc(ByRef m As Message)
            If m.Msg = 282 Then
                ConfigureGestures()
            ElseIf m.Msg = 281 AndAlso HandleGesture(m) Then
                Dim result As IntPtr = New IntPtr(1)
                m.Result = result
                Return
            End If
            MyBase.WndProc(m)
        End Sub

        Private Function HandleGesture(ByRef m As Message) As Boolean
            Dim pGestureInfo As GESTUREINFO = New GESTUREINFO With {
        .cbSize = Marshal.SizeOf(GetType(GESTUREINFO))
    }
            If Not GetGestureInfo(m.LParam, pGestureInfo) Then
                Return False
            End If
            Try
                Dim p As Point = New Point(pGestureInfo.ptsLocation.x, pGestureInfo.ptsLocation.y)
                p = PointToClient(p)
                Select Case pGestureInfo.dwID
                    Case 3
                        If (pGestureInfo.dwFlags And 1) <> 0 Then
                            gestureZoomArguments = CLng(pGestureInfo.ullArguments)
                        ElseIf gestureZoomArguments > 0 Then
                            Dim num = pGestureInfo.ullArguments / gestureZoomArguments
                            If num > 0.01 AndAlso num < 100.0 Then
                                ZoomAt(p, 1.0 / num)
                                gestureZoomArguments = CLng(pGestureInfo.ullArguments)
                            End If
                        End If
                        Return True
                    Case 4
                        Return False
                End Select

            Finally
                CloseGestureInfoHandle(m.LParam)
            End Try
            Return False
        End Function

        Protected Overrides Sub OnMouseDown(e As MouseEventArgs)
            MyBase.OnMouseDown(e)
            If e.Button = MouseButtons.Left Then
                dragActive = True
                dragMoved = False
                dragStart = e.Location
                lastDrag = e.Location
                Capture = True
            End If
            Focus()
        End Sub

        Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
            MyBase.OnMouseMove(e)
            If Not dragActive OrElse e.Button <> MouseButtons.Left Then
                Return
            End If
            Dim num = e.X - lastDrag.X
            Dim num2 = e.Y - lastDrag.Y
            If Not dragMoved Then
                Dim num3 = Math.Abs(e.X - dragStart.X)
                Dim num4 = Math.Abs(e.Y - dragStart.Y)
                If num3 < 6 AndAlso num4 < 6 Then
                    Return
                End If
                dragMoved = True
            End If
            If num <> 0 OrElse num2 <> 0 Then
                PanByPixels(num, num2)
                lastDrag = e.Location
            End If
        End Sub

        Protected Overrides Sub OnMouseUp(e As MouseEventArgs)
            MyBase.OnMouseUp(e)
            If e.Button = MouseButtons.Left AndAlso dragActive Then
                If Not dragMoved Then
                    RaiseEvent TraceRequested(Me, e.Location)
                End If
                dragActive = False
                Capture = False
            End If
        End Sub

        Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
            MyBase.OnMouseWheel(e)
            Dim factor = If(e.Delta > 0, 0.85, 1.1764705882352942)
            ZoomAt(e.Location, factor)
        End Sub

        Public Sub ZoomAt(focalPx As Point, factor As Double)
            If Not factor <= 0.0 AndAlso IsFinite(factor) Then
                NormalizeWindow()
                Dim num = UnmapX(focalPx.X)
                Dim num2 = UnmapY(focalPx.Y)
                Dim num3 = (xmax - xmin) * factor
                Dim num4 = (ymax - ymin) * factor
                If num3 < 1E-06 Then
                    num3 = 1E-06
                End If
                If num4 < 1E-06 Then
                    num4 = 1E-06
                End If
                If num3 > 1000000000.0 Then
                    num3 = 1000000000.0
                End If
                If num4 > 1000000000.0 Then
                    num4 = 1000000000.0
                End If
                Dim num5 = (num - xmin) / (xmax - xmin)
                Dim num6 = (ymax - num2) / (ymax - ymin)
                If Not IsFinite(num5) Then
                    num5 = 0.5
                End If
                If Not IsFinite(num6) Then
                    num6 = 0.5
                End If
                xmin = num - num5 * num3
                xmax = xmin + num3
                ymax = num2 + num6 * num4
                ymin = ymax - num4
                NormalizeWindow()
                RaiseEvent WindowChanged(Me, EventArgs.Empty)
            End If
        End Sub

        Public Sub PanByPixels(dx As Integer, dy As Integer)
            Dim num = Math.Max(1, ClientSize.Width - 1)
            Dim num2 = Math.Max(1, ClientSize.Height - 1)
            Dim num3 = (xmax - xmin) / num
            Dim num4 = (ymax - ymin) / num2
            xmin -= dx * num3
            xmax -= dx * num3
            ymin += dy * num4
            ymax += dy * num4
            RaiseEvent WindowChanged(Me, EventArgs.Empty)
        End Sub

        Public Function UnmapX(px As Integer) As Double
            Dim num = Math.Max(1, ClientSize.Width - 1)
            Return xmin + (xmax - xmin) * px / num
        End Function

        Public Function UnmapY(py As Integer) As Double
            Dim num = Math.Max(1, ClientSize.Height - 1)
            Return ymax - (ymax - ymin) * py / num
        End Function

        Private Sub NormalizeWindow()
            If Not IsFinite(xmin) OrElse Not IsFinite(xmax) OrElse xmax <= xmin Then
                xmin = -10.0
                xmax = 10.0
            End If
            If Not IsFinite(ymin) OrElse Not IsFinite(ymax) OrElse ymax <= ymin Then
                ymin = -10.0
                ymax = 10.0
            End If
        End Sub

        Private Shared Function IsFinite(v As Double) As Boolean
            Return If(Not Double.IsNaN(v) AndAlso Not Double.IsInfinity(v), True, False)
        End Function
    End Class
