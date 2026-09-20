' LCARSmain/Modules/modMediaFolders.vb
Option Strict On
Option Explicit On

Imports System.IO
Imports System.Windows.Forms
Imports LCARS

Public Enum MediaFolderKind
    Documents = 0
    Pictures = 1
    Music = 2
    Videos = 3
End Enum

Public Module modMediaFolders
    Private Const AppName As String = "LCARS x32"
    Private Const Section As String = "Application"

    Private Function PathKey(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents : Return "DocumentsPath"
            Case MediaFolderKind.Pictures : Return "PicturesPath"
            Case MediaFolderKind.Music : Return "MusicPath"
            Case Else : Return "VideosPath"
        End Select
    End Function

    Private Function LabelKey(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents : Return "DocumentsLabel"
            Case MediaFolderKind.Pictures : Return "PicturesLabel"
            Case MediaFolderKind.Music : Return "MusicLabel"
            Case Else : Return "VideosLabel"
        End Select
    End Function

    Private Function DefaultWindowsPath(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents
                Return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            Case MediaFolderKind.Pictures
                Return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            Case MediaFolderKind.Music
                Return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            Case Else
                Try
                    Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser
                    myReg = myReg.OpenSubKey("Software\Microsoft\Windows\CurrentVersion\explorer\Shell Folders\", False)
                    If myReg IsNot Nothing Then
                        Dim v As Object = myReg.GetValue("My Video")
                        If v IsNot Nothing Then Return CStr(v)
                    End If
                Catch
                End Try
                Return ""
        End Select
    End Function

    Public Function GetMediaFolderPath(ByVal kind As MediaFolderKind) As String
        Dim custom As String = GetSetting(AppName, Section, PathKey(kind), "").Trim()
        If custom = "" AndAlso kind = MediaFolderKind.Videos Then
            custom = GetSetting(AppName, Section, "Videos", "").Trim()
        End If
        If custom <> "" Then Return custom
        Return DefaultWindowsPath(kind)
    End Function

    Public Function GetMediaFolderLabel(ByVal kind As MediaFolderKind) As String
        Return GetSetting(AppName, Section, LabelKey(kind), "").Trim()
    End Function

    Public Sub SetMediaFolderPath(ByVal kind As MediaFolderKind, ByVal path As String)
        SaveSetting(AppName, Section, PathKey(kind), If(path, "").Trim())
        If kind = MediaFolderKind.Videos Then
            ' Keep legacy key in sync for older code paths.
            SaveSetting(AppName, Section, "Videos", If(path, "").Trim())
        End If
    End Sub

    Public Sub SetMediaFolderLabel(ByVal kind As MediaFolderKind, ByVal label As String)
        SaveSetting(AppName, Section, LabelKey(kind), If(label, "").Trim())
    End Sub

    Public Sub ResetMediaFolder(ByVal kind As MediaFolderKind)
        SetMediaFolderPath(kind, "")
        SetMediaFolderLabel(kind, "")
    End Sub

    Public Function ControlNameFor(ByVal kind As MediaFolderKind) As String
        Select Case kind
            Case MediaFolderKind.Documents : Return "myDocuments"
            Case MediaFolderKind.Pictures : Return "myPictures"
            Case MediaFolderKind.Music : Return "myMusic"
            Case Else : Return "myVideos"
        End Select
    End Function

    Public Sub ApplyMediaFolderLabels(ByVal root As Control)
        If root Is Nothing Then Return
        For Each kind As MediaFolderKind In [Enum].GetValues(GetType(MediaFolderKind))
            Dim label As String = GetMediaFolderLabel(kind)
            If label = "" Then Continue For
            Dim found() As Control = root.Controls.Find(ControlNameFor(kind), True)
            If found Is Nothing OrElse found.Length = 0 Then Continue For
            Dim btn As LCARSbuttonClass = TryCast(found(0), LCARSbuttonClass)
            If btn IsNot Nothing Then
                btn.ButtonText = label
                btn.Text = label
            End If
        Next
    End Sub
End Module
