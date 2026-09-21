' LCARSTerminal/Rdp/RdpCredentialStore.vb
Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Optional RDP passwords via Windows Credential Manager (CRED_TYPE_GENERIC).
''' </summary>
Public Class RdpCredentialStore
    Private Const CredTypeGeneric As Integer = 1
    Private Const CredPersistLocalMachine As Integer = 2

    Public Shared Function TargetName(ByVal profileId As Guid) As String
        Return "LCARSTerminal/Remote/" & profileId.ToString("N")
    End Function

    Public Shared Function LegacyRdpTargetName(ByVal profileId As Guid) As String
        Return "LCARSTerminal/RDP/" & profileId.ToString("N")
    End Function

    Public Function TryGetPassword(ByVal profileId As Guid, ByRef password As String) As Boolean
        password = Nothing
        If TryReadTarget(TargetName(profileId), password) Then Return True
        Return TryReadTarget(LegacyRdpTargetName(profileId), password)
    End Function

    Private Function TryReadTarget(ByVal target As String, ByRef password As String) As Boolean
        password = Nothing
        Dim credPtr As IntPtr = IntPtr.Zero
        Try
            If Not CredRead(target, CredTypeGeneric, 0, credPtr) Then
                Return False
            End If
            Dim cred As NativeCredential = CType(Marshal.PtrToStructure(credPtr, GetType(NativeCredential)), NativeCredential)
            If cred.CredentialBlobSize = 0 OrElse cred.CredentialBlob = IntPtr.Zero Then
                Return False
            End If
            password = Marshal.PtrToStringUni(cred.CredentialBlob, CInt(cred.CredentialBlobSize \ 2))
            Return Not String.IsNullOrEmpty(password)
        Finally
            If credPtr <> IntPtr.Zero Then CredFree(credPtr)
        End Try
    End Function

    Public Sub SavePassword(ByVal profileId As Guid, ByVal username As String, ByVal password As String)
        If password Is Nothing Then password = ""
        Dim target As String = TargetName(profileId)
        Dim blob As Byte() = Encoding.Unicode.GetBytes(password)
        Dim blobHandle As GCHandle = GCHandle.Alloc(blob, GCHandleType.Pinned)
        Try
            Dim cred As New NativeCredential()
            cred.Flags = 0
            cred.Type = CredTypeGeneric
            cred.TargetName = target
            cred.Comment = "LCARS Terminal Remote"
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

    Public Sub DeletePassword(ByVal profileId As Guid)
        CredDelete(TargetName(profileId), CredTypeGeneric, 0)
        CredDelete(LegacyRdpTargetName(profileId), CredTypeGeneric, 0)
    End Sub

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)> _
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

    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)> _
    Private Shared Function CredWrite(ByRef credential As NativeCredential, ByVal flags As Integer) As Boolean
    End Function

    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)> _
    Private Shared Function CredRead(ByVal targetName As String, ByVal type As Integer, ByVal flags As Integer, ByRef credential As IntPtr) As Boolean
    End Function

    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)> _
    Private Shared Function CredDelete(ByVal targetName As String, ByVal type As Integer, ByVal flags As Integer) As Boolean
    End Function

    <DllImport("advapi32.dll", SetLastError:=True)> _
    Private Shared Sub CredFree(ByVal buffer As IntPtr)
    End Sub
End Class
