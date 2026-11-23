Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

    Public Class CalendarMonthGrid
        Inherits Control
        Private viewMonth As Date

        Private ReadOnly eventDays As HashSet(Of Integer)

        Public Sub New()
            viewMonth = Date.Today
            eventDays = New HashSet(Of Integer)()
            SetStyle(ControlStyles.ResizeRedraw Or ControlStyles.AllPaintingInWmPaint Or ControlStyles.OptimizedDoubleBuffer, value:=True)
            MyBase.BackColor = Color.FromArgb(20, 20, 20)
            MyBase.ForeColor = Color.Orange
            MyBase.Font = New Font("Arial Narrow", 9F)
        End Sub

        Public Sub SetMonth(month As Date, events As IList(Of CalendarEvent))
            viewMonth = month
            eventDays.Clear()
            If events IsNot Nothing Then
                For Each [event] In events
                    If [event] IsNot Nothing Then
                        CalendarRecurrence.AddOccurrenceDaysInMonth([event], viewMonth, eventDays)
                    End If
                Next
            End If
            Invalidate()
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim graphics = e.Graphics
            graphics.Clear(MyBase.BackColor)
            Dim num As Integer = Math.Max(24, Width / 7)
            Dim num2 As Integer = Math.Max(22, Height / 7)
            Dim array = New String(6) {"S", "M", "T", "W", "T", "F", "S"}
            Dim num3 = 0
            Do
                graphics.DrawString(array(num3), MyBase.Font, Brushes.Orange, num3 * num + 4, 2F)
                num3 += 1
            Loop While num3 <= 6
            Dim dayOfWeek As Integer = CInt(New DateTime(viewMonth.Year, viewMonth.Month, 1).DayOfWeek)
            Dim num4 = Date.DaysInMonth(viewMonth.Year, viewMonth.Month)
            Dim num5 = num4
            For i = 1 To num5
                Dim num6 = dayOfWeek + i - 1
                Dim num7 As Integer
                Dim num8 As Integer
                num7 = num6 / 7
                num8 = num6 Mod 7
                Dim num9 = num8 * num + 4
                Dim num10 = 18 + num7 * num2
                Dim text As String = i.ToString()
                If eventDays.Contains(i) Then
                    text += "•"
                End If
                graphics.DrawString(text, MyBase.Font, Brushes.Orange, num9, num10)
            Next
        End Sub
    End Class
