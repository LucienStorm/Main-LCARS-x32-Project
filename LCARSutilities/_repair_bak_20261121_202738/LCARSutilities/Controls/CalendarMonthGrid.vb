' LCARSutilities/LCARSutilities/Controls/CalendarMonthGrid.vb
Option Strict On
Option Explicit On
Imports System.Drawing
Imports System.Globalization
Imports System.Windows.Forms
''' <summary>Lightning-style month grid.</summary>
Public Class CalendarMonthGrid
Inherits Control
Private viewMonth As DateTime = DateTime.Today
Private ReadOnly eventDays As New HashSet(Of Integer)()
Public Sub New() SetStyle(ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer Or ControlStyles.ResizeRedraw, True) BackColor = Color.FromArgb(20, 20, 20) ForeColor = Color.Orange Font = New Font("Arial Narrow", 9.0F)
End Sub
Public Sub SetMonth(ByVal month As DateTime, ByVal events As IList(Of CalendarEvent)) viewMonth = month eventDays.Clear()
If events IsNot Nothing Then For Each ev As CalendarEvent In events
If ev Is Nothing Then Continue For CalendarRecurrence.AddOccurrenceDaysInMonth(ev, viewMonth, eventDays)
Next
End If Invalidate()
End Sub
Protected Overrides Sub OnPaint(ByVal e As PaintEventArgs)
MyBase.OnPaint(e)
Dim g As Graphics = e.Graphics g.Clear(BackColor)
Dim cellW As Integer = Math.Max(24, Width \ 7)
Dim cellH As Integer = Math.Max(22, Height \ 7)
Dim headers As String() = {"S", "M", "T", "W", "T", "F", "S"}
For i As Integer = 0 To 6 g.DrawString(headers(i), Font, Brushes.Orange, i * cellW + 4, 2)
Next
Dim first As New DateTime(viewMonth.Year, viewMonth.Month, 1)
Dim startOffset As Integer = CInt(first.DayOfWeek)
Dim daysInMonth As Integer = DateTime.DaysInMonth(viewMonth.Year, viewMonth.Month)
For day As Integer = 1 To daysInMonth
Dim idx As Integer = startOffset + day - 1
Dim row As Integer = idx \ 7
Dim col As Integer = idx Mod 7
Dim x As Integer = col * cellW + 4
Dim y As Integer = 18 + row * cellH
Dim label As String = day.ToString()
If eventDays.Contains(day) Then label &= "•" g.DrawString(label, Font, Brushes.Orange, x, y)
Next
End Sub
End Class
