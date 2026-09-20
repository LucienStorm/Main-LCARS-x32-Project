' LCARSexplorer/SmbShareEnumerator.vb
Option Strict On
Option Explicit On

Imports System.Collections.Generic
Imports System.Runtime.InteropServices

Public Module SmbShareEnumerator
    Private Const MAX_PREFERRED_LENGTH As Integer = -1
    Private Const STYPE_DISKTREE As Integer = 0

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Private Structure SHARE_INFO_1
        Public shi1_netname As String
        Public shi1_type As Integer
        Public shi1_remark As String
    End Structure

    <DllImport("netapi32.dll", CharSet:=CharSet.Unicode)>
    Private Function NetShareEnum(
        ByVal serverName As String,
        ByVal level As Integer,
        ByRef bufPtr As IntPtr,
        ByVal prefMaxLen As Integer,
        ByRef entriesRead As Integer,
        ByRef totalEntries As Integer,
        ByRef resumeHandle As Integer) As Integer
    End Function

    <DllImport("netapi32.dll")>
    Private Function NetApiBufferFree(ByVal buffer As IntPtr) As Integer
    End Function

    Public Function ListShares(ByVal host As String, Optional ByVal includeAdminShares As Boolean = False) As List(Of String)
        Dim result As New List(Of String)()
        If (String.IsNullOrEmpty(host) OrElse host.Trim().Length = 0) Then Return result

        Dim server As String = host.Trim()
        If server.StartsWith("\\") Then server = server.TrimStart("\"c)
        Dim slash As Integer = server.IndexOf("\"c)
        If slash > 0 Then server = server.Substring(0, slash)

        Dim bufPtr As IntPtr = IntPtr.Zero
        Dim entriesRead As Integer = 0
        Dim totalEntries As Integer = 0
        Dim resumeHandle As Integer = 0
        Try
            Dim enumStatus As Integer = NetShareEnum("\\" & server, 1, bufPtr, MAX_PREFERRED_LENGTH, entriesRead, totalEntries, resumeHandle)
            If enumStatus <> 0 OrElse bufPtr = IntPtr.Zero OrElse entriesRead <= 0 Then
                Return result
            End If
            Dim structSize As Integer = Marshal.SizeOf(GetType(SHARE_INFO_1))
            For i As Integer = 0 To entriesRead - 1
                Dim item As SHARE_INFO_1 = CType(Marshal.PtrToStructure(New IntPtr(bufPtr.ToInt64() + CLng(i) * structSize), GetType(SHARE_INFO_1)), SHARE_INFO_1)
                Dim name As String = If(item.shi1_netname, "").Trim()
                If name = "" Then Continue For
                Dim shareType As Integer = item.shi1_type And &HFFFF
                If shareType <> STYPE_DISKTREE Then Continue For
                If Not includeAdminShares AndAlso IsAdminShare(name) Then Continue For
                result.Add(name)
            Next
            result.Sort(StringComparer.OrdinalIgnoreCase)
        Finally
            If bufPtr <> IntPtr.Zero Then NetApiBufferFree(bufPtr)
        End Try
        Return result
    End Function

    Private Function IsAdminShare(ByVal name As String) As Boolean
        If String.Equals(name, "IPC$", StringComparison.OrdinalIgnoreCase) Then Return True
        If String.Equals(name, "ADMIN$", StringComparison.OrdinalIgnoreCase) Then Return True
        If name.EndsWith("$", StringComparison.Ordinal) Then Return True
        Return False
    End Function
End Module
