' LCARSbuttons/LCARSbuttons/Diagnostics/DiskLayoutPanel.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports System.Management
Imports System.Text.RegularExpressions
Imports System.Windows.Forms

Namespace Diagnostics

''' <summary>Disk Management–style linear strips per physical disk (partitions + unallocated).</summary>
Public Class DiskLayoutPanel
    Inherits Panel

    Private Class PartSeg
        Public Label As String
        Public SizeBytes As Long
        Public FreeBytes As Long
        Public IsUnallocated As Boolean
        Public Color As Color
        Public Letter As String = ""
    End Class

    Private Class DiskRow
        Public Title As String
        Public SizeBytes As Long
        Public Parts As New List(Of PartSeg)()
    End Class

    Private ReadOnly disks As New List(Of DiskRow)()
    Private ReadOnly refreshTimer As New Timer()
    Private ReadOnly palette As Color() = {
        Color.FromArgb(255, 153, 0),
        Color.FromArgb(153, 204, 255),
        Color.FromArgb(204, 153, 255),
        Color.FromArgb(0, 204, 153),
        Color.FromArgb(255, 102, 102),
        Color.FromArgb(255, 204, 102)
    }

    Public Sub New()
        BackColor = Color.Black
        DoubleBuffered = True
        SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.UserPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True)
        refreshTimer.Interval = 5000
        AddHandler refreshTimer.Tick, Sub() Reload()
        AddHandler Me.VisibleChanged, Sub()
                                          If Visible Then
                                              Reload()
                                              refreshTimer.Start()
                                          Else
                                              refreshTimer.Stop()
                                          End If
                                      End Sub
        Reload()
    End Sub

    Public Sub Reload()
        Dim rows As New List(Of DiskRow)()
        Dim volumesByDisk As Dictionary(Of Integer, List(Of PartSeg)) = BuildVolumesByDiskIndex()

        Try
            Using driveQ As New ManagementObjectSearcher("SELECT Index,Size,Model,DeviceID FROM Win32_DiskDrive")
                For Each drive As ManagementObject In driveQ.Get()
                    Dim row As New DiskRow()
                    Dim idx As Integer = 0
                    Integer.TryParse(If(drive("Index"), "0").ToString(), idx)
                    Long.TryParse(If(drive("Size"), "0").ToString(), row.SizeBytes)
                    Dim model As String = If(drive("Model"), "DISK").ToString().Trim()
                    row.Title = "DISK " & idx.ToString() & " — " & model.ToUpperInvariant() & "  (" & FormatBytes(row.SizeBytes) & ")"

                    Dim covered As Long = 0
                    Dim colorIdx As Integer = 0
                    Dim usedLetters As New List(Of String)()

                    ' Prefer volumes mapped to this disk via LogicalDiskToPartition
                    If volumesByDisk.ContainsKey(idx) Then
                        For Each vol As PartSeg In volumesByDisk(idx)
                            Dim seg As New PartSeg()
                            seg.Letter = vol.Letter
                            seg.SizeBytes = vol.SizeBytes
                            seg.FreeBytes = vol.FreeBytes
                            seg.Label = vol.Label
                            seg.Color = palette(colorIdx Mod palette.Length)
                            colorIdx += 1
                            covered += Math.Max(0, seg.SizeBytes)
                            If seg.Letter.Length > 0 AndAlso Not usedLetters.Contains(seg.Letter.ToUpperInvariant()) Then
                                usedLetters.Add(seg.Letter.ToUpperInvariant())
                            End If
                            row.Parts.Add(seg)
                        Next
                    End If

                    ' Fill gaps with raw partitions that have no letter (EFI, recovery, etc.)
                    Try
                        Using partQ As New ManagementObjectSearcher(
                            "SELECT DeviceID,Size,Type,Name FROM Win32_DiskPartition WHERE DiskIndex=" & idx.ToString())
                            For Each part As ManagementObject In partQ.Get()
                                Dim partSize As Long = 0
                                Long.TryParse(If(part("Size"), "0").ToString(), partSize)
                                If partSize <= 0 Then Continue For
                                Dim letter As String = LookupLetterForPartition(If(part("DeviceID"), "").ToString())
                                If letter.Length > 0 AndAlso usedLetters.Contains(letter.ToUpperInvariant()) Then
                                    Continue For
                                End If
                                ' Skip partition if size already covered by a volume of similar size
                                Dim already As Boolean = False
                                For Each existing As PartSeg In row.Parts
                                    If Math.Abs(existing.SizeBytes - partSize) < 32L * 1024L * 1024L Then
                                        already = True
                                        Exit For
                                    End If
                                Next
                                If already Then
                                    Continue For
                                End If
                                Dim seg As New PartSeg()
                                seg.SizeBytes = partSize
                                seg.Letter = letter
                                If letter.Length > 0 Then
                                    ApplyDriveInfo(seg, letter)
                                    usedLetters.Add(letter.ToUpperInvariant())
                                Else
                                    seg.Label = If(part("Name"), "PARTITION").ToString().ToUpperInvariant() & "  " & FormatBytes(partSize)
                                End If
                                If String.IsNullOrEmpty(seg.Label) Then
                                    seg.Label = "PARTITION  " & FormatBytes(partSize)
                                End If
                                seg.Color = palette(colorIdx Mod palette.Length)
                                colorIdx += 1
                                covered += partSize
                                row.Parts.Add(seg)
                            Next
                        End Using
                    Catch
                    End Try

                    Dim unalloc As Long = row.SizeBytes - covered
                    If unalloc > 64L * 1024L * 1024L Then
                        row.Parts.Add(New PartSeg With {
                            .Label = "UNALLOCATED  " & FormatBytes(unalloc),
                            .SizeBytes = unalloc,
                            .IsUnallocated = True,
                            .Color = Color.FromArgb(60, 60, 60)
                        })
                    End If

                    If row.Parts.Count > 0 Then rows.Add(row)
                Next
            End Using
        Catch
            rows.Clear()
        End Try

        ' If WMI left only unallocated / empty, show DriveInfo volumes plainly
        Dim hasRealVolume As Boolean = False
        For Each r As DiskRow In rows
            For Each p As PartSeg In r.Parts
                If Not p.IsUnallocated AndAlso (p.Letter.Length > 0 OrElse p.Label.Contains(":"c)) Then
                    hasRealVolume = True
                    Exit For
                End If
            Next
            If hasRealVolume Then Exit For
        Next

        If Not hasRealVolume Then
            Dim fallback As New DiskRow()
            fallback.Title = "MOUNTED VOLUMES"
            fallback.SizeBytes = 0
            Dim colorIdx As Integer = 0
            For Each d As DriveInfo In DriveInfo.GetDrives()
                Try
                    If d.DriveType <> DriveType.Fixed AndAlso d.DriveType <> DriveType.Removable Then Continue For
                    Dim seg As New PartSeg()
                    seg.Letter = d.Name.TrimEnd("\"c)
                    If d.IsReady Then
                        seg.SizeBytes = d.TotalSize
                        seg.FreeBytes = d.TotalFreeSpace
                        Dim bits As New List(Of String)()
                        bits.Add(seg.Letter)
                        Try
                            If Not String.IsNullOrEmpty(d.VolumeLabel) Then bits.Add(d.VolumeLabel)
                        Catch
                        End Try
                        Try
                            bits.Add(d.DriveFormat)
                        Catch
                        End Try
                        bits.Add(FormatBytes(seg.SizeBytes))
                        bits.Add("FREE " & FormatBytes(seg.FreeBytes))
                        seg.Label = String.Join("  ", bits.ToArray()).ToUpperInvariant()
                    Else
                        seg.SizeBytes = 1
                        seg.Label = seg.Letter & "  (NOT READY)"
                    End If
                    seg.Color = palette(colorIdx Mod palette.Length)
                    colorIdx += 1
                    fallback.Parts.Add(seg)
                    fallback.SizeBytes += Math.Max(0, seg.SizeBytes)
                Catch
                End Try
            Next
            If fallback.Parts.Count > 0 Then
                rows.Clear()
                rows.Add(fallback)
            End If
        End If

        disks.Clear()
        disks.AddRange(rows)
        Invalidate()
    End Sub

    Private Function BuildVolumesByDiskIndex() As Dictionary(Of Integer, List(Of PartSeg))
        Dim map As New Dictionary(Of Integer, List(Of PartSeg))()
        Try
            Using relQ As New ManagementObjectSearcher("SELECT Antecedent,Dependent FROM Win32_LogicalDiskToPartition")
                For Each rel As ManagementObject In relQ.Get()
                    Dim antecedent As String = If(rel("Antecedent"), "").ToString()
                    Dim dependent As String = If(rel("Dependent"), "").ToString()
                    Dim idx As Integer = ParseDiskIndex(antecedent)
                    Dim letter As String = ParseDriveLetter(dependent)
                    If idx < 0 OrElse letter.Length = 0 Then Continue For
                    Dim seg As New PartSeg()
                    seg.Letter = letter
                    ApplyDriveInfo(seg, letter)
                    If Not map.ContainsKey(idx) Then map(idx) = New List(Of PartSeg)()
                    map(idx).Add(seg)
                Next
            End Using
        Catch
        End Try

        ' Ensure every ready fixed volume appears somewhere (DiskIndex 0 if unknown)
        For Each d As DriveInfo In DriveInfo.GetDrives()
            Try
                If d.DriveType <> DriveType.Fixed AndAlso d.DriveType <> DriveType.Removable Then Continue For
                Dim letter As String = d.Name.TrimEnd("\"c)
                Dim found As Boolean = False
                For Each kv As KeyValuePair(Of Integer, List(Of PartSeg)) In map
                    For Each p As PartSeg In kv.Value
                        If String.Equals(p.Letter, letter, StringComparison.OrdinalIgnoreCase) Then
                            found = True
                            Exit For
                        End If
                    Next
                    If found Then Exit For
                Next
                If found Then Continue For
                Dim seg As New PartSeg()
                seg.Letter = letter
                ApplyDriveInfo(seg, letter)
                Dim targetIdx As Integer = 0
                If Not map.ContainsKey(targetIdx) Then map(targetIdx) = New List(Of PartSeg)()
                map(targetIdx).Add(seg)
            Catch
            End Try
        Next
        Return map
    End Function

    Private Shared Function ParseDiskIndex(ByVal antecedent As String) As Integer
        ' ...Win32_DiskPartition.DeviceID="Disk #0, Partition #1"
        Dim m As Match = Regex.Match(antecedent, "Disk\s*#\s*(\d+)", RegexOptions.IgnoreCase)
        If m.Success Then
            Dim v As Integer = 0
            Integer.TryParse(m.Groups(1).Value, v)
            Return v
        End If
        Return -1
    End Function

    Private Shared Function ParseDriveLetter(ByVal dependent As String) As String
        ' ...Win32_LogicalDisk.DeviceID="C:"
        Dim m As Match = Regex.Match(dependent, "DeviceID\s*=\s*""([A-Za-z]:)""", RegexOptions.IgnoreCase)
        If m.Success Then Return m.Groups(1).Value.ToUpperInvariant()
        m = Regex.Match(dependent, "([A-Za-z]:)")
        If m.Success Then Return m.Groups(1).Value.ToUpperInvariant()
        Return ""
    End Function

    Private Shared Function LookupLetterForPartition(ByVal partDeviceId As String) As String
        If String.IsNullOrEmpty(partDeviceId) Then Return ""
        Try
            Dim esc As String = partDeviceId.Replace("\", "\\").Replace("""", "\""")
            Dim q As String = "ASSOCIATORS OF {Win32_DiskPartition.DeviceID=""" & esc & """} WHERE AssocClass=Win32_LogicalDiskToPartition"
            Using logQ As New ManagementObjectSearcher(q)
                For Each logDisk As ManagementObject In logQ.Get()
                    Return If(logDisk("DeviceID"), "").ToString().ToUpperInvariant()
                Next
            End Using
        Catch
        End Try
        Return ""
    End Function

    Private Shared Sub ApplyDriveInfo(ByVal seg As PartSeg, ByVal letter As String)
        Try
            Dim root As String = If(letter.EndsWith("\"c), letter, letter & "\")
            Dim d As New DriveInfo(root)
            If d.IsReady Then
                seg.SizeBytes = d.TotalSize
                seg.FreeBytes = d.TotalFreeSpace
                Dim bits As New List(Of String)()
                bits.Add(letter)
                Try
                    If Not String.IsNullOrEmpty(d.VolumeLabel) Then bits.Add(d.VolumeLabel)
                Catch
                End Try
                Try
                    bits.Add(d.DriveFormat)
                Catch
                End Try
                bits.Add(FormatBytes(seg.SizeBytes))
                bits.Add("FREE " & FormatBytes(seg.FreeBytes))
                seg.Label = String.Join("  ", bits.ToArray()).ToUpperInvariant()
            Else
                If seg.SizeBytes <= 0 Then seg.SizeBytes = 1
                seg.Label = letter & "  (NOT READY)"
            End If
        Catch
            If String.IsNullOrEmpty(seg.Label) Then seg.Label = letter
        End Try
    End Sub

    Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        g.Clear(BackColor)
        Dim pad As Integer = 4
        Dim y As Integer = pad
        Using titleFont As New Font("LCARS", 12.0!, FontStyle.Regular, GraphicsUnit.Point)
            Using br As New SolidBrush(Color.Orange)
                g.DrawString("DISK LAYOUT", titleFont, br, pad, y)
            End Using
        End Using
        y += 22

        Using f As New Font("Arial Narrow", 8.0!, FontStyle.Bold)
            For Each disk As DiskRow In disks
                Using br As New SolidBrush(Color.Orange)
                    g.DrawString(disk.Title, f, br, pad, y)
                End Using
                y += 14
                Dim barH As Integer = 22
                Dim barW As Integer = Math.Max(40, Width - pad * 2)
                Dim denom As Long = disk.SizeBytes
                If denom <= 0 Then
                    For Each p As PartSeg In disk.Parts
                        denom += p.SizeBytes
                    Next
                End If
                If denom <= 0 Then denom = 1
                Dim x As Integer = pad
                For Each p As PartSeg In disk.Parts
                    Dim w As Integer = CInt((p.SizeBytes * barW) \ denom)
                    If w < 2 AndAlso p.SizeBytes > 0 Then w = 2
                    If w <= 0 Then Continue For
                    Using br As New SolidBrush(p.Color)
                        g.FillRectangle(br, x, y, w, barH)
                    End Using
                    If Not p.IsUnallocated AndAlso p.SizeBytes > 0 AndAlso p.FreeBytes >= 0 AndAlso p.FreeBytes < p.SizeBytes Then
                        Dim usedW As Integer = CInt(((p.SizeBytes - p.FreeBytes) * w) \ p.SizeBytes)
                        Using br As New SolidBrush(Color.FromArgb(120, 0, 0, 0))
                            g.FillRectangle(br, x, y, usedW, barH)
                        End Using
                    End If
                    Using pen As New Pen(Color.Black, 1)
                        g.DrawRectangle(pen, x, y, w, barH)
                    End Using
                    x += w
                Next
                Using pen As New Pen(Color.Orange, 1)
                    g.DrawRectangle(pen, pad, y, barW, barH)
                End Using
                y += barH + 4
                For Each p As PartSeg In disk.Parts
                    Using br As New SolidBrush(If(p.IsUnallocated, Color.Gray, Color.Orange))
                        g.DrawString(p.Label, f, br, pad, y)
                    End Using
                    y += 12
                    If y > Height - 10 Then Return
                Next
                y += 8
            Next
        End Using
    End Sub

    Private Shared Function FormatBytes(ByVal bytes As Long) As String
        If bytes < 0 Then bytes = 0
        Dim gb As Double = bytes / (1024.0R * 1024.0R * 1024.0R)
        If gb >= 1.0R Then Return gb.ToString("0.00") & " GB"
        Dim mb As Double = bytes / (1024.0R * 1024.0R)
        Return mb.ToString("0") & " MB"
    End Function

    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            refreshTimer.Stop()
            refreshTimer.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub
End Class

End Namespace
