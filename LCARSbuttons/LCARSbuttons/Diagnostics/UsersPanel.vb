' LCARSbuttons/LCARSbuttons/Diagnostics/UsersPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Management
Imports System.Windows.Forms
Imports LCARS.Controls

Namespace Diagnostics

''' <summary>Local user accounts (read) + User Accounts CPL.</summary>
Public Class UsersPanel
    Inherits Panel

    Private Class UserRow
        Public Name As String = ""
        Public FullName As String = ""
        Public Disabled As Boolean = False
        Public Local As Boolean = True
        Public Overrides Function ToString() As String
            Dim flags As String = ""
            If Disabled Then flags &= " [DISABLED]"
            If Not Local Then flags &= " [DOMAIN]"
            Return Name & If(String.IsNullOrEmpty(FullName), "", " — " & FullName) & flags
        End Function
    End Class

    Private ReadOnly lst As New ListBox()
    Private ReadOnly status As New Label()
    Private fbRefresh As FlatButton
    Private fbOpen As FlatButton

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        lst.BackColor = Color.Black
        lst.ForeColor = Color.Orange
        lst.BorderStyle = BorderStyle.FixedSingle
        lst.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        lst.IntegralHeight = False

        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        status.Text = "READY"
        status.AutoSize = False

        fbRefresh = MakeButton("REFRESH", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRefresh.Click, Sub() Reload()
        fbOpen = MakeButton("USERS CPL", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbOpen.Click, Sub()
                                     Try
                                         Process.Start("control.exe", "userpasswords2")
                                     Catch
                                         ShellProcess.StartControlPanel("nusrmgr.cpl")
                                     End Try
                                 End Sub

        Controls.Add(lst)
        Controls.Add(status)
        Controls.Add(fbRefresh)
        Controls.Add(fbOpen)

        AddHandler Me.Resize, Sub() LayoutChildren()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible Then Reload()
                                      End Sub
        LayoutChildren()
    End Sub

    Private Function MakeButton(ByVal caption As String, ByVal color As LCARS.LCARScolorStyles) As FlatButton
        Dim b As New FlatButton()
        b.ButtonText = caption
        b.Text = caption
        b.Clickable = True
        b.Beeping = True
        b.Color = color
        b.ButtonTextAlign = ContentAlignment.BottomRight
        b.Size = New Size(120, 26)
        Return b
    End Function

    Private Sub LayoutChildren()
        Dim pad As Integer = 6
        Dim y As Integer = pad
        fbRefresh.Location = New Point(pad, y)
        fbOpen.Location = New Point(pad + 128, y)
        y += 32
        status.SetBounds(pad, Math.Max(y + 20, ClientSize.Height - 22), Math.Max(80, ClientSize.Width - pad * 2), 18)
        lst.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), Math.Max(40, status.Top - y - 4))
    End Sub

    Public Sub Reload()
        status.Text = "LOADING…"
        status.Refresh()
        Dim rows As New List(Of UserRow)()
        Try
            Using s As New ManagementObjectSearcher("SELECT Name, FullName, Disabled, LocalAccount FROM Win32_UserAccount")
                For Each o As ManagementObject In s.Get()
                    Dim row As New UserRow()
                    row.Name = If(o("Name"), "").ToString()
                    row.FullName = If(o("FullName"), "").ToString()
                    Try
                        row.Disabled = Convert.ToBoolean(o("Disabled"))
                    Catch
                    End Try
                    Try
                        row.Local = Convert.ToBoolean(o("LocalAccount"))
                    Catch
                    End Try
                    If row.Name <> "" Then rows.Add(row)
                Next
            End Using
        Catch ex As Exception
            status.Text = "USERS ERROR: " & ex.Message
            LayoutChildren()
            Return
        End Try
        rows.Sort(Function(a, b) String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
        lst.BeginUpdate()
        lst.Items.Clear()
        For Each r As UserRow In rows
            lst.Items.Add(r)
        Next
        lst.EndUpdate()
        status.Text = rows.Count.ToString() & " USER ACCOUNTS"
        LayoutChildren()
    End Sub
End Class

End Namespace
