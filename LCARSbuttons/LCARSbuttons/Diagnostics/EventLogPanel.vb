' LCARSbuttons/LCARSbuttons/Diagnostics/EventLogPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>Application / System / Security event log viewer with filter, clear, and detail popup.</summary>
Public Class EventLogPanel
    Inherits Panel

    Private Class EntryDetail
        Public TimeText As String
        Public TypeText As String
        Public Source As String
        Public IdText As String
        Public Message As String
    End Class

    <DllImport("uxtheme.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function SetWindowTheme(ByVal hWnd As IntPtr, ByVal pszSubAppName As String, ByVal pszSubIdList As String) As Integer
    End Function

    Private ReadOnly cboLog As New ComboBox()
    Private ReadOnly txtFilter As New TextBox()
    Private ReadOnly lst As New ListView()
    Private ReadOnly status As New Label()
    Private ReadOnly fbRefresh As FlatButton
    Private ReadOnly fbClear As FlatButton
    Private ReadOnly fbDisplay As FlatButton
    Private _busy As Boolean = False
    Private _loadedOnce As Boolean = False

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        cboLog.DropDownStyle = ComboBoxStyle.DropDownList
        cboLog.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        cboLog.BackColor = Color.Black
        cboLog.ForeColor = Color.Orange
        cboLog.Items.AddRange(New Object() {"Application", "System", "Security"})
        cboLog.SelectedIndex = 0
        AddHandler cboLog.SelectedIndexChanged, Sub()
                                                    If _loadedOnce Then Reload()
                                                End Sub

        txtFilter.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        txtFilter.BackColor = Color.FromArgb(20, 20, 20)
        txtFilter.ForeColor = Color.Orange
        txtFilter.BorderStyle = BorderStyle.FixedSingle
        AddHandler txtFilter.KeyDown, Sub(s As Object, e As KeyEventArgs)
                                          If e.KeyCode = Keys.Enter Then
                                              e.SuppressKeyPress = True
                                              Reload()
                                          End If
                                      End Sub

        fbRefresh = MakeButton("REFRESH", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRefresh.Click, Sub() Reload()
        fbDisplay = MakeButton("DISPLAY LOG", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbDisplay.Click, AddressOf OnDisplay
        fbClear = MakeButton("CLEAR LOG", LCARS.LCARScolorStyles.Orange)
        AddHandler fbClear.Click, AddressOf OnClear

        lst.View = View.Details
        lst.FullRowSelect = True
        lst.MultiSelect = False
        lst.HeaderStyle = ColumnHeaderStyle.Nonclickable
        lst.BackColor = Color.Black
        lst.ForeColor = Color.Orange
        lst.BorderStyle = BorderStyle.None
        lst.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        lst.Columns.Add("TIME", 140)
        lst.Columns.Add("TYPE", 70)
        lst.Columns.Add("SOURCE", 140)
        lst.Columns.Add("ID", 50)
        lst.Columns.Add("MESSAGE", 400)
        AddHandler lst.DoubleClick, AddressOf OnDisplay
        AddHandler lst.HandleCreated, Sub()
                                          Try
                                              SetWindowTheme(lst.Handle, "", "")
                                          Catch
                                          End Try
                                      End Sub

        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        status.Text = "READY"
        status.AutoSize = False

        Controls.Add(lst)
        Controls.Add(status)
        Controls.Add(fbClear)
        Controls.Add(fbDisplay)
        Controls.Add(fbRefresh)
        Controls.Add(txtFilter)
        Controls.Add(cboLog)

        AddHandler Me.Resize, Sub() LayoutChildren()
        AddHandler Me.VisibleChanged, AddressOf OnVisibleChanged
        AddHandler Me.HandleCreated, Sub() BeginInvoke(New MethodInvoker(AddressOf EnsureLoaded))
        LayoutChildren()
    End Sub

    Private Sub OnVisibleChanged(ByVal sender As Object, ByVal e As EventArgs)
        If Visible Then EnsureLoaded()
    End Sub

    Private Sub EnsureLoaded()
        If Not IsHandleCreated Then Return
        If _loadedOnce AndAlso lst.Items.Count > 0 Then Return
        Reload()
    End Sub

    Private Function MakeButton(ByVal caption As String, ByVal color As LCARS.LCARScolorStyles) As FlatButton
        Dim b As New FlatButton()
        b.ButtonText = caption
        b.Text = caption
        b.Clickable = True
        b.Beeping = True
        b.Color = color
        b.ButtonTextAlign = ContentAlignment.BottomRight
        b.Size = New Size(110, 26)
        Return b
    End Function

    Private Sub LayoutChildren()
        Dim pad As Integer = 6
        Dim y As Integer = pad
        cboLog.SetBounds(pad, y, 130, 24)
        Dim rightButtons As Integer = 110 * 3 + 16
        txtFilter.SetBounds(cboLog.Right + 8, y, Math.Max(60, ClientSize.Width - cboLog.Right - rightButtons - 20), 24)
        fbClear.Location = New Point(Math.Max(pad, ClientSize.Width - 116), y)
        fbDisplay.Location = New Point(Math.Max(pad, ClientSize.Width - 234), y)
        fbRefresh.Location = New Point(Math.Max(pad, ClientSize.Width - 352), y)
        y += 32
        status.SetBounds(pad, Math.Max(y + 20, ClientSize.Height - 22), Math.Max(80, ClientSize.Width - pad * 2), 18)
        lst.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), Math.Max(40, status.Top - y - 4))
        If lst.Columns.Count >= 5 Then
            Dim w As Integer = Math.Max(300, lst.ClientSize.Width)
            lst.Columns(0).Width = 140
            lst.Columns(1).Width = 70
            lst.Columns(2).Width = 120
            lst.Columns(3).Width = 50
            lst.Columns(4).Width = Math.Max(100, w - 380)
        End If
    End Sub

    Public Sub Reload()
        If _busy Then Return
        _busy = True
        Try
            LayoutChildren()
            Dim logName As String = If(cboLog.SelectedItem, "Application").ToString()
            Dim filter As String = If(txtFilter.Text, "").Trim()
            Dim entries As New List(Of ListViewItem)()
            status.Text = "LOADING " & logName.ToUpperInvariant() & "…"
            status.Refresh()
            Using log As New EventLog(logName)
                Dim count As Integer = log.Entries.Count
                Dim start As Integer = Math.Max(0, count - 400)
                For i As Integer = count - 1 To start Step -1
                    Try
                        Dim e As EventLogEntry = log.Entries(i)
                        Dim msg As String = ""
                        Try
                            msg = If(e.Message, "")
                        Catch
                            msg = "(message unavailable)"
                        End Try
                        If filter.Length > 0 Then
                            Dim hay As String = (If(e.Source, "") & " " & msg).ToUpperInvariant()
                            If Not hay.Contains(filter.ToUpperInvariant()) Then Continue For
                        End If
                        Dim item As New ListViewItem(e.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss"))
                        item.SubItems.Add(e.EntryType.ToString())
                        item.SubItems.Add(If(e.Source, ""))
                        item.SubItems.Add(e.InstanceId.ToString())
                        Dim shortMsg As String = msg.Replace(ControlChars.Cr, " "c).Replace(ControlChars.Lf, " "c)
                        If shortMsg.Length > 200 Then shortMsg = shortMsg.Substring(0, 200) & "…"
                        item.SubItems.Add(shortMsg)
                        Dim detail As New EntryDetail()
                        detail.TimeText = item.Text
                        detail.TypeText = e.EntryType.ToString()
                        detail.Source = If(e.Source, "")
                        detail.IdText = e.InstanceId.ToString()
                        detail.Message = msg
                        item.Tag = detail
                        entries.Add(item)
                        If entries.Count >= 250 Then Exit For
                    Catch
                    End Try
                Next
            End Using
            lst.BeginUpdate()
            lst.Items.Clear()
            For Each item As ListViewItem In entries
                lst.Items.Add(item)
            Next
            lst.EndUpdate()
            _loadedOnce = True
            status.Text = entries.Count.ToString() & " ENTRIES — " & logName.ToUpperInvariant() & " — SELECT + DISPLAY LOG"
        Catch ex As Exception
            status.Text = "LOG ERROR: " & ex.Message
            _loadedOnce = True
        Finally
            _busy = False
        End Try
    End Sub

    Private Sub OnDisplay(ByVal sender As Object, ByVal e As EventArgs)
        If lst.SelectedItems.Count = 0 Then
            status.Text = "SELECT AN ENTRY FIRST"
            Return
        End If
        Dim detail As EntryDetail = TryCast(lst.SelectedItems(0).Tag, EntryDetail)
        If detail Is Nothing Then
            status.Text = "NO DETAIL FOR SELECTION"
            Return
        End If
        Dim body As String =
            "TIME: " & detail.TimeText & ControlChars.CrLf &
            "TYPE: " & detail.TypeText & ControlChars.CrLf &
            "SOURCE: " & detail.Source & ControlChars.CrLf &
            "ID: " & detail.IdText & ControlChars.CrLf & ControlChars.CrLf &
            detail.Message
        Using dlg As New EventLogDetailForm("EVENT DETAIL", body)
            dlg.ShowDialog(FindForm())
        End Using
    End Sub

    Private Sub OnClear(ByVal sender As Object, ByVal e As EventArgs)
        Dim logName As String = If(cboLog.SelectedItem, "Application").ToString()
        Dim answer As DialogResult = MessageBox.Show(
            "Clear all entries from " & logName & "?",
            "CLEAR EVENT LOG",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning)
        If answer <> DialogResult.Yes Then Return
        Try
            Using log As New EventLog(logName)
                log.Clear()
            End Using
            status.Text = "CLEARED " & logName.ToUpperInvariant()
            Reload()
        Catch ex As Exception
            status.Text = "CLEAR FAILED: " & ex.Message
        End Try
    End Sub
End Class

End Namespace
