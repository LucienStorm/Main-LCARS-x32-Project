' LCARSpic/Media/ChromeController.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Content-driven rail chrome: slides type-specific controls out/in (~250ms)
''' and tweens elbow arm widths toward the target media layout.
''' </summary>
Public Class ChromeController
    Private ReadOnly hostForm As Form
    Private ReadOnly layoutAction As Action
    Private ReadOnly photoControls As Control()
    Private ReadOnly musicControls As Control()
    Private ReadOnly videoControls As Control()
    Private ReadOnly elbows As Control()
    Private ReadOnly animTimer As Timer
    Private Const DurationMs As Integer = 260
    Private Const TickMs As Integer = 16
    Private Const SlidePx As Integer = 140

    Private phase As Integer ' 0 idle, 1 out, 2 in
    Private elapsed As Integer
    Private outControls As List(Of Control)
    Private inControls As List(Of Control)
    Private outStart As Dictionary(Of Control, Point)
    Private inTarget As Dictionary(Of Control, Point)
    Private elbowStartW As Dictionary(Of Control, Integer)
    Private elbowTargetW As Integer
    Private pendingKind As MediaKind
    Private displayedKind As MediaKind = MediaKind.None
    Private applyVisibility As Action(Of MediaKind)
    Private onComplete As Action

    Public Sub New(ByVal form As Form,
                   ByVal photo As Control(),
                   ByVal music As Control(),
                   ByVal video As Control(),
                   ByVal elbowControls As Control(),
                   ByVal applyLayout As Action,
                   ByVal setVisibility As Action(Of MediaKind))
        hostForm = form
        photoControls = If(photo, New Control() {})
        musicControls = If(music, New Control() {})
        videoControls = If(video, New Control() {})
        elbows = If(elbowControls, New Control() {})
        layoutAction = applyLayout
        applyVisibility = setVisibility
        animTimer = New Timer()
        animTimer.Interval = TickMs
        AddHandler animTimer.Tick, AddressOf OnTick
    End Sub

    Public ReadOnly Property CurrentKind As MediaKind
        Get
            Return displayedKind
        End Get
    End Property

    Public Sub DisposeTimer()
        animTimer.Stop()
        RemoveHandler animTimer.Tick, AddressOf OnTick
        animTimer.Dispose()
    End Sub

    ''' <summary>Switch chrome for a media kind, animating when the kind actually changes.</summary>
    Public Sub TransitionTo(ByVal kind As MediaKind, Optional ByVal completed As Action = Nothing)
        onComplete = completed
        If kind = displayedKind AndAlso phase = 0 Then
            applyVisibility(kind)
            layoutAction()
            RaiseComplete()
            Return
        End If

        If phase <> 0 Then
            ' Interrupt: snap current animation then restart.
            FinishImmediate(pendingKind)
        End If

        pendingKind = kind
        Dim leaving As List(Of Control) = DiffControls(ControlsFor(displayedKind), ControlsFor(kind))
        Dim entering As List(Of Control) = DiffControls(ControlsFor(kind), ControlsFor(displayedKind))

        outControls = leaving
        inControls = entering
        outStart = New Dictionary(Of Control, Point)()
        For Each c As Control In leaving
            If c Is Nothing Then Continue For
            outStart(c) = c.Location
        Next

        elbowStartW = New Dictionary(Of Control, Integer)()
        elbowTargetW = ElbowWidthFor(kind)
        For Each e As Control In elbows
            If e Is Nothing Then Continue For
            elbowStartW(e) = e.Width
        Next

        elapsed = 0
        phase = 1
        animTimer.Start()
    End Sub

    Private Sub OnTick(ByVal sender As Object, ByVal e As EventArgs)
        elapsed += TickMs
        Dim t As Double = Math.Min(1.0, elapsed / CDbl(DurationMs))
        ' Ease-out cubic
        Dim ease As Double = 1.0 - Math.Pow(1.0 - t, 3.0)

        If phase = 1 Then
            For Each c As Control In outControls
                If c Is Nothing OrElse Not outStart.ContainsKey(c) Then Continue For
                Dim start As Point = outStart(c)
                c.Location = New Point(start.X + CInt(SlidePx * ease), start.Y)
            Next
            TweenElbows(ease)
            If t >= 1.0 Then
                BeginEnterPhase()
            End If
        ElseIf phase = 2 Then
            For Each c As Control In inControls
                If c Is Nothing OrElse Not inTarget.ContainsKey(c) Then Continue For
                Dim target As Point = inTarget(c)
                Dim startX As Integer = target.X + SlidePx
                c.Location = New Point(startX - CInt(SlidePx * ease), target.Y)
            Next
            TweenElbows(ease)
            If t >= 1.0 Then
                FinishImmediate(pendingKind)
            End If
        End If
    End Sub

    Private Sub BeginEnterPhase()
        For Each c As Control In outControls
            If c IsNot Nothing Then c.Visible = False
        Next
        displayedKind = pendingKind
        applyVisibility(pendingKind)
        layoutAction()

        inTarget = New Dictionary(Of Control, Point)()
        For Each c As Control In inControls
            If c Is Nothing Then Continue For
            inTarget(c) = c.Location
            c.Location = New Point(c.Left + SlidePx, c.Top)
            c.Visible = True
            c.BringToFront()
        Next

        ' Refresh elbow start from current (post-out) widths toward final target.
        elbowStartW = New Dictionary(Of Control, Integer)()
        For Each e As Control In elbows
            If e Is Nothing Then Continue For
            elbowStartW(e) = e.Width
        Next
        elbowTargetW = ElbowWidthFor(pendingKind)

        elapsed = 0
        phase = 2
    End Sub

    Private Sub FinishImmediate(ByVal kind As MediaKind)
        animTimer.Stop()
        phase = 0
        displayedKind = kind
        applyVisibility(kind)
        layoutAction()
        TweenElbows(1.0)
        RaiseComplete()
    End Sub

    Private Sub RaiseComplete()
        Dim cb As Action = onComplete
        onComplete = Nothing
        If cb IsNot Nothing Then cb()
    End Sub

    Private Sub TweenElbows(ByVal ease As Double)
        For Each e As Control In elbows
            If e Is Nothing OrElse Not elbowStartW.ContainsKey(e) Then Continue For
            Dim startW As Integer = elbowStartW(e)
            Dim w As Integer = startW + CInt((elbowTargetW - startW) * ease)
            Dim h As Integer = e.Height
            Dim rightAnchored As Boolean = (e.Anchor And AnchorStyles.Right) = AnchorStyles.Right AndAlso
                                           (e.Anchor And AnchorStyles.Left) <> AnchorStyles.Left
            If rightAnchored Then
                Dim rightEdge As Integer = e.Right
                e.Size = New Size(w, h)
                e.Left = rightEdge - w
            Else
                e.Size = New Size(w, h)
            End If
            ' LCARS Elbow ButtonWidth tracks the vertical arm thickness feel.
            Dim elbowCtrl As LCARS.Controls.Elbow = TryCast(e, LCARS.Controls.Elbow)
            If elbowCtrl IsNot Nothing Then
                elbowCtrl.ButtonWidth = Math.Max(30, w - 22)
            End If
        Next
    End Sub

    Private Shared Function ElbowWidthFor(ByVal kind As MediaKind) As Integer
        Select Case kind
            Case MediaKind.Video
                Return 96
            Case MediaKind.Music
                Return 56
            Case MediaKind.Photo
                Return 72
            Case Else
                Return 64
        End Select
    End Function

    Private Function ControlsFor(ByVal kind As MediaKind) As Control()
        Select Case kind
            Case MediaKind.Photo
                Return photoControls
            Case MediaKind.Music
                Return musicControls
            Case MediaKind.Video
                Return videoControls
            Case Else
                Return New Control() {}
        End Select
    End Function

    Private Shared Function DiffControls(ByVal fromSet As Control(), ByVal toSet As Control()) As List(Of Control)
        Dim result As New List(Of Control)()
        Dim keep As New HashSet(Of Control)(toSet)
        For Each c As Control In fromSet
            If c Is Nothing Then Continue For
            If Not keep.Contains(c) Then result.Add(c)
        Next
        Return result
    End Function
End Class
