' LCARSbuttons/LCARSbuttons/Diagnostics/CollapsibleSection.vb
Option Strict On
Option Explicit On

Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>Accordion section: header bar toggles content panel height.</summary>
Public Class CollapsibleSection
    Inherits Panel

    Public Event ExpandedChanged As EventHandler

    Private ReadOnly header As FlatButton
    Private ReadOnly contentHost As New Panel()
    Private _expanded As Boolean = True
    Private _expandedHeight As Integer = 200
    Private _title As String = "SECTION"
    Private _suspendEvents As Boolean = False

    Public Property Title As String
        Get
            Return _title
        End Get
        Set(ByVal value As String)
            _title = If(value, "")
            UpdateHeaderText()
        End Set
    End Property

    Public Property ExpandedHeight As Integer
        Get
            Return _expandedHeight
        End Get
        Set(ByVal value As Integer)
            _expandedHeight = Math.Max(40, value)
            ' Do not raise ExpandedChanged — height-only updates must not restack the host.
            ApplyExpanded(False)
        End Set
    End Property

    Public Property Expanded As Boolean
        Get
            Return _expanded
        End Get
        Set(ByVal value As Boolean)
            If _expanded = value Then Return
            _expanded = value
            ApplyExpanded(True)
            If Not _suspendEvents Then RaiseEvent ExpandedChanged(Me, EventArgs.Empty)
        End Set
    End Property

    Public ReadOnly Property ContentPanel As Panel
        Get
            Return contentHost
        End Get
    End Property

    Public Const HeaderHeight As Integer = 32

    ''' <summary>Current total height for host stacking (header + body when open).</summary>
    Public ReadOnly Property StackHeight As Integer
        Get
            Return If(_expanded, HeaderHeight + _expandedHeight, HeaderHeight)
        End Get
    End Property

    Public Sub New()
        BackColor = Color.Black
        SetStyle(ControlStyles.OptimizedDoubleBuffer Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint, True)
        Height = HeaderHeight + _expandedHeight

        header = New FlatButton()
        header.Clickable = True
        header.Beeping = True
        header.Color = LCARS.LCARScolorStyles.PrimaryFunction
        header.ButtonTextAlign = ContentAlignment.MiddleLeft
        header.Height = HeaderHeight
        AddHandler header.Click, Sub() Expanded = Not Expanded

        contentHost.BackColor = Color.Black
        contentHost.Visible = True
        Controls.Add(contentHost)
        Controls.Add(header)
        header.BringToFront()
        UpdateHeaderText()
        ApplyExpanded(False)
        AddHandler Me.Resize, Sub() LayoutInner()
    End Sub

    ''' <summary>Set expanded without raising ExpandedChanged (used during host construction).</summary>
    Public Sub SetExpandedSilent(ByVal value As Boolean)
        _suspendEvents = True
        Try
            _expanded = value
            ApplyExpanded(True)
        Finally
            _suspendEvents = False
        End Try
    End Sub

    Private Sub UpdateHeaderText()
        Dim marker As String = If(_expanded, "[-] ", "[+] ")
        header.ButtonText = marker & _title
        header.Text = header.ButtonText
    End Sub

    ''' <summary>Re-apply header/body bounds after the host changes Width/Height (collapse restack).</summary>
    Public Sub SyncLayout()
        ApplyExpanded(True)
    End Sub

    Private Sub ApplyExpanded(ByVal forceLayout As Boolean)
        Height = StackHeight
        LayoutInner()
        If forceLayout Then
            contentHost.PerformLayout()
            For Each c As Control In contentHost.Controls
                c.Dock = DockStyle.None
                If _expanded AndAlso contentHost.ClientSize.Width > 0 AndAlso contentHost.ClientSize.Height > 0 Then
                    c.Bounds = contentHost.ClientRectangle
                    c.Visible = True
                End If
                c.PerformLayout()
                c.Invalidate()
            Next
        End If
        UpdateHeaderText()
        header.Visible = True
        header.BringToFront()
        header.Invalidate()
        Me.Invalidate()
    End Sub

    Private Sub LayoutInner()
        Dim w As Integer = Math.Max(1, Width)
        header.SetBounds(0, 0, w, HeaderHeight)
        header.Visible = True
        header.BringToFront()
        If _expanded Then
            Dim bodyH As Integer = Math.Max(1, Height - HeaderHeight)
            contentHost.Visible = True
            contentHost.SetBounds(0, HeaderHeight, w, bodyH)
            For Each c As Control In contentHost.Controls
                c.Visible = True
                c.Dock = DockStyle.None
                c.SetBounds(0, 0, Math.Max(1, contentHost.ClientSize.Width), Math.Max(1, contentHost.ClientSize.Height))
            Next
        Else
            ' Keep host present but zero-height so siblings below stay in the scroll stack.
            contentHost.Visible = False
            contentHost.SetBounds(0, HeaderHeight, w, 0)
        End If
    End Sub
End Class

End Namespace
