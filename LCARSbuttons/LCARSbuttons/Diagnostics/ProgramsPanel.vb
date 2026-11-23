' LCARSbuttons/LCARSbuttons/Diagnostics/ProgramsPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms
Imports LCARS.Controls
Imports Microsoft.Win32

Namespace Diagnostics

''' <summary>Installed programs from Uninstall registry + uninstall / Programs and Features.</summary>
Public Class ProgramsPanel
    Inherits Panel

    Private Class ProgRow
        Public Name As String = ""
        Public Publisher As String = ""
        Public Version As String = ""
        Public UninstallString As String = ""
        Public Overrides Function ToString() As String
            Return Name & If(String.IsNullOrEmpty(Version), "", "  " & Version)
        End Function
    End Class

    Private ReadOnly lst As New ListBox()
    Private ReadOnly status As New Label()
    Private ReadOnly txtFilter As New TextBox()
    Private fbRefresh As FlatButton
    Private fbUninstall As FlatButton
    Private fbOpen As FlatButton
    Private allRows As New List(Of ProgRow)()

    Public Sub New()
        BackColor = Color.Black
        Dock = DockStyle.Fill

        txtFilter.BackColor = Color.FromArgb(20, 20, 20)
        txtFilter.ForeColor = Color.Orange
        txtFilter.BorderStyle = BorderStyle.FixedSingle
        txtFilter.Font = New Font("Arial Narrow", 11.0!, FontStyle.Bold)
        AddHandler txtFilter.TextChanged, Sub() ApplyFilter()

        lst.BackColor = Color.Black
        lst.ForeColor = Color.Orange
        lst.BorderStyle = BorderStyle.FixedSingle
        lst.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        lst.IntegralHeight = False

        status.ForeColor = Color.Orange
        status.BackColor = Color.Black
        status.Font = New Font("Arial Narrow", 10.0!, FontStyle.Bold)
        status.Text = "READY"
        status.AutoSize = False

        fbRefresh = MakeButton("REFRESH", LCARS.LCARScolorStyles.PrimaryFunction)
        AddHandler fbRefresh.Click, Sub() Reload()
        fbUninstall = MakeButton("UNINSTALL", LCARS.LCARScolorStyles.Orange)
        AddHandler fbUninstall.Click, AddressOf OnUninstall
        fbOpen = MakeButton("PROGRAMS CPL", LCARS.LCARScolorStyles.MiscFunction)
        AddHandler fbOpen.Click, Sub() ShellProcess.StartControlPanel("appwiz.cpl")

        Controls.Add(txtFilter)
        Controls.Add(lst)
        Controls.Add(status)
        Controls.Add(fbRefresh)
        Controls.Add(fbUninstall)
        Controls.Add(fbOpen)

        AddHandler Me.Resize, Sub() LayoutChildren()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible AndAlso allRows.Count = 0 Then Reload()
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
        fbUninstall.Location = New Point(pad + 128, y)
        fbOpen.Location = New Point(pad + 256, y)
        y += 32
        txtFilter.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), 24)
        y += 28
        status.SetBounds(pad, Math.Max(y + 20, ClientSize.Height - 22), Math.Max(80, ClientSize.Width - pad * 2), 18)
        lst.SetBounds(pad, y, Math.Max(80, ClientSize.Width - pad * 2), Math.Max(40, status.Top - y - 4))
    End Sub

    Public Sub Reload()
        status.Text = "LOADING…"
        status.Refresh()
        allRows = LoadPrograms()
        ApplyFilter()
        status.Text = allRows.Count.ToString() & " PROGRAMS"
        LayoutChildren()
    End Sub

    Private Sub ApplyFilter()
        Dim filter As String = If(txtFilter.Text, "").Trim().ToUpperInvariant()
        lst.BeginUpdate()
        lst.Items.Clear()
        For Each r As ProgRow In allRows
            If filter.Length > 0 AndAlso Not (r.Name & " " & r.Publisher).ToUpperInvariant().Contains(filter) Then Continue For
            lst.Items.Add(r)
        Next
        lst.EndUpdate()
    End Sub

    Private Function LoadPrograms() As List(Of ProgRow)
        Dim map As New Dictionary(Of String, ProgRow)(StringComparer.OrdinalIgnoreCase)
        ReadUninstallHive(Registry.LocalMachine, "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", map)
        ReadUninstallHive(Registry.LocalMachine, "SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall", map)
        ReadUninstallHive(Registry.CurrentUser, "SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", map)
        Dim list As New List(Of ProgRow)(map.Values)
        list.Sort(Function(a, b) String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase))
        Return list
    End Function

    Private Sub ReadUninstallHive(ByVal root As RegistryKey, ByVal path As String, ByVal map As Dictionary(Of String, ProgRow))
        Try
            Using key As RegistryKey = root.OpenSubKey(path, False)
                If key Is Nothing Then Return
                For Each subName As String In key.GetSubKeyNames()
                    Try
                        Using subKey As RegistryKey = key.OpenSubKey(subName, False)
                            If subKey Is Nothing Then Continue For
                            Dim nameObj As Object = subKey.GetValue("DisplayName")
                            If nameObj Is Nothing Then Continue For
                            Dim name As String = nameObj.ToString().Trim()
                            If name = "" Then Continue For
                            If subKey.GetValue("SystemComponent") IsNot Nothing Then
                                Dim sc As Integer = 0
                                Integer.TryParse(subKey.GetValue("SystemComponent").ToString(), sc)
                                If sc = 1 Then Continue For
                            End If
                            Dim row As New ProgRow()
                            row.Name = name
                            Dim pub As Object = subKey.GetValue("Publisher")
                            If pub IsNot Nothing Then row.Publisher = pub.ToString()
                            Dim ver As Object = subKey.GetValue("DisplayVersion")
                            If ver IsNot Nothing Then row.Version = ver.ToString()
                            Dim un As Object = subKey.GetValue("UninstallString")
                            If un IsNot Nothing Then row.UninstallString = un.ToString()
                            If Not map.ContainsKey(name) Then map.Add(name, row)
                        End Using
                    Catch
                    End Try
                Next
            End Using
        Catch
        End Try
    End Sub

    Private Sub OnUninstall(ByVal sender As Object, ByVal e As EventArgs)
        Dim row As ProgRow = TryCast(lst.SelectedItem, ProgRow)
        If row Is Nothing Then
            status.Text = "SELECT A PROGRAM"
            Return
        End If
        If String.IsNullOrEmpty(row.UninstallString) Then
            status.Text = "NO UNINSTALL STRING"
            Return
        End If
        Try
            Dim cmd As String = row.UninstallString.Trim()
            If cmd.StartsWith("""") Then
                Dim endQ As Integer = cmd.IndexOf(""""c, 1)
                If endQ > 1 Then
                    Dim exe As String = cmd.Substring(1, endQ - 1)
                    Dim args As String = cmd.Substring(endQ + 1).Trim()
                    Process.Start(exe, args)
                Else
                    Process.Start(cmd)
                End If
            Else
                Dim parts() As String = cmd.Split(New Char() {" "c}, 2)
                If parts.Length = 1 Then
                    Process.Start(parts(0))
                Else
                    Process.Start(parts(0), parts(1))
                End If
            End If
            status.Text = "LAUNCHED UNINSTALLER"
        Catch ex As Exception
            status.Text = "UNINSTALL FAILED: " & ex.Message
        End Try
    End Sub
End Class

End Namespace
