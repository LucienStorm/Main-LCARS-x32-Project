' LCARSbuttons/LCARSbuttons/Diagnostics/PowerService.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Windows.Forms

Namespace Diagnostics

Public Class PowerPlanInfo
    Public Guid As String = ""
    Public Name As String = ""
    Public Overrides Function ToString() As String
        Return Name
    End Function
End Class

''' <summary>Battery + powercfg plan helpers for Engineering POWER section.</summary>
Public Module PowerService

    Private ReadOnly StandardSchemeGuids As String() = {
        "381b4222-f694-41f0-9685-ff5bb260df2e",
        "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c",
        "a1841308-3541-4fab-bc81-f71556f20b4a"
    }

    Public Function GetBatterySummary() As String
        Dim ps As PowerStatus = SystemInformation.PowerStatus
        Dim pct As Integer = CInt(Math.Round(ps.BatteryLifePercent * 100))
        Dim src As String = If(ps.PowerLineStatus = PowerLineStatus.Online, "AC", "BATTERY")
        Return pct.ToString() & "% " & src & " — " & GetActivePlanName()
    End Function

    Public Function ListPlans() As List(Of PowerPlanInfo)
        EnsureStandardPlansVisible()
        Dim list As New List(Of PowerPlanInfo)()
        Dim text As String = ShellProcess.RunCommand("powercfg", "/list")
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Power Scheme GUID", StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
            Dim guidStart As Integer = line.IndexOf(":"c) + 1
            Dim guidEnd As Integer = line.IndexOf("("c)
            If guidEnd < 0 Then guidEnd = line.Length
            Dim guid As String = line.Substring(guidStart, guidEnd - guidStart).Trim()
            Dim name As String = ""
            Dim p As Integer = line.IndexOf("("c)
            Dim q As Integer = line.LastIndexOf(")"c)
            If p >= 0 AndAlso q > p Then name = line.Substring(p + 1, q - p - 1).Trim()
            If String.IsNullOrEmpty(guid) Then Continue For
            Dim item As New PowerPlanInfo()
            item.Guid = guid
            item.Name = If(String.IsNullOrEmpty(name), guid, name)
            list.Add(item)
        Next
        Return list
    End Function

    Public Function GetActivePlanName() As String
        Dim text As String = ShellProcess.RunCommand("powercfg", "/getactivescheme")
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Power Scheme GUID", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim p As Integer = line.IndexOf("("c)
                Dim q As Integer = line.LastIndexOf(")"c)
                If p >= 0 AndAlso q > p Then Return line.Substring(p + 1, q - p - 1).Trim()
            End If
        Next
        Return "Unknown"
    End Function

    Public Function GetActivePlanGuid() As String
        Dim text As String = ShellProcess.RunCommand("powercfg", "/getactivescheme")
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Power Scheme GUID", StringComparison.OrdinalIgnoreCase) >= 0 Then
                Dim guidStart As Integer = line.IndexOf(":"c) + 1
                Dim guidEnd As Integer = line.IndexOf("("c)
                If guidEnd < 0 Then guidEnd = line.Length
                If guidStart > 0 AndAlso guidEnd > guidStart Then
                    Return line.Substring(guidStart, guidEnd - guidStart).Trim()
                End If
            End If
        Next
        Return ""
    End Function

    Public Function SetActivePlan(ByVal planGuid As String) As String
        If String.IsNullOrEmpty(planGuid) Then Return "no plan"
        Dim exitCode As Integer = -1
        Dim out As String = ShellProcess.RunCommandEx("powercfg", "/setactive " & planGuid, exitCode)
        If exitCode = 0 Then Return "OK:" & GetActivePlanName()
        Return If(String.IsNullOrEmpty(out), "powercfg failed", out.Trim())
    End Function

    Private Sub EnsureStandardPlansVisible()
        Dim plans As List(Of PowerPlanInfo) = ListPlansRaw()
        If plans.Count >= 2 Then Return
        For Each schemeGuid As String In StandardSchemeGuids
            Dim already As Boolean = False
            For Each p As PowerPlanInfo In plans
                If String.Equals(p.Guid, schemeGuid, StringComparison.OrdinalIgnoreCase) Then
                    already = True
                    Exit For
                End If
            Next
            If already Then Continue For
            Dim exitCode As Integer = -1
            ShellProcess.RunCommandEx("powercfg", "-duplicatescheme " & schemeGuid, exitCode)
        Next
    End Sub

    Private Function ListPlansRaw() As List(Of PowerPlanInfo)
        Dim list As New List(Of PowerPlanInfo)()
        Dim text As String = ShellProcess.RunCommand("powercfg", "/list")
        For Each line As String In text.Split(New String() {ControlChars.CrLf, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries)
            If line.IndexOf("Power Scheme GUID", StringComparison.OrdinalIgnoreCase) < 0 Then Continue For
            Dim guidStart As Integer = line.IndexOf(":"c) + 1
            Dim guidEnd As Integer = line.IndexOf("("c)
            If guidEnd < 0 Then guidEnd = line.Length
            Dim item As New PowerPlanInfo()
            item.Guid = line.Substring(guidStart, guidEnd - guidStart).Trim()
            Dim p As Integer = line.IndexOf("("c)
            Dim q As Integer = line.LastIndexOf(")"c)
            If p >= 0 AndAlso q > p Then item.Name = line.Substring(p + 1, q - p - 1).Trim()
            If item.Guid <> "" Then list.Add(item)
        Next
        Return list
    End Function

End Module

End Namespace
