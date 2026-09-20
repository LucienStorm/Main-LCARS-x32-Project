' LCARSexplorer/NetworkCredentialStore.vb
Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' SMB credentials via Windows Credential Manager (CRED_TYPE_GENERIC).
''' </summary>
Public Class NetworkCredentialStore
    Private Const CredTypeGeneric As Integer = 1
    Private Const CredPersistLocalMachine As Integer = 2

    Public Shared Function TargetName(ByVal host As String) As String
        Return "LCARSExplorer/SMB/" & NormalizeHost(host)
    End Function

    Public Shared Function NormalizeHost(ByVal host As String) As String
        Dim h As String = If(host, "").Trim().Trim("\"c)
        Dim slash As Integer = h.IndexOf("\"c)
        If slash > 0 Then h = h.Substring(0, slash)
        Return h.ToLowerInvariant()
    End Function

    Public Function TryGet(ByVal host As String, ByRef username As String, ByRef password As String) As Boolean
        username = Nothing
        password = Nothing
        Dim credPtr As IntPtr = IntPtr.Zero
        Try
            If Not CredRead(TargetName(host), CredTypeGeneric, 0, credPtr) Then
                Return False
            End If
            Dim cred As NativeCredential = CType(Marshal.PtrToStructure(credPtr, GetType(NativeCredential)), NativeCredential)
            username = If(cred.UserName, "")
            If cred.CredentialBlobSize = 0 OrElse cred.CredentialBlob = IntPtr.Zero Then
                password = ""
            Else
                password = Marshal.PtrToStringUni(cred.CredentialBlob, CInt(cred.CredentialBlobSize \ 2))
            End If
            Return True
        Finally
            If credPtr <> IntPtr.Zero Then CredFree(credPtr)
        End Try
    End Function

    Public Sub Save(ByVal host As String, ByVal username As String, ByVal password As String)
        If password Is Nothing Then password = ""
        Dim target As String = TargetName(host)
        Dim blob As Byte() = Encoding.Unicode.GetBytes(password)
        Dim blobHandle As GCHandle = GCHandle.Alloc(blob, GCHandleType.Pinned)
        Try
            Dim cred As New NativeCredential()
            cred.Flags = 0
            cred.Type = CredTypeGeneric
            cred.TargetName = target
            cred.Comment = "LCARS Explorer SMB"
            cred.CredentialBlobSize = CUInt(blob.Length)
            cred.CredentialBlob = blobHandle.AddrOfPinnedObject()
            cred.Persist = CredPersistLocalMachine
            cred.UserName = If(username, "")
            If Not CredWrite(cred, 0) Then
                Throw New InvalidOperationException("CredWrite failed: " & Marshal.GetLastWin32Error().ToString())
            End If
        Finally
            blobHandle.Free()
        End Try
    End Sub

    Public Sub Delete(ByVal host As String)
        CredDelete(TargetName(host), CredTypeGeneric, 0)
    End Sub

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Private Structure NativeCredential
        Public Flags As Integer
        Public Type As Integer
        Public TargetName As String
        Public Comment As String
        Public LastWritten As Long
        Public CredentialBlobSize As UInteger
        Public CredentialBlob As IntPtr
        Public Persist As Integer
        Public AttributeCount As Integer
        Public Attributes As IntPtr
        Public TargetAlias As String
        Public UserName As String
    End Structure

    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function CredWrite(ByRef credential As NativeCredential, ByVal flags As Integer) As Boolean
    End Function

    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function CredRead(ByVal targetName As String, ByVal type As Integer, ByVal flags As Integer, ByRef credential As IntPtr) As Boolean
    End Function

    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function CredDelete(ByVal targetName As String, ByVal type As Integer, ByVal flags As Integer) As Boolean
    End Function

    <DllImport("advapi32.dll", SetLastError:=True)>
    Private Shared Sub CredFree(ByVal buffer As IntPtr)
    End Sub
End Class
