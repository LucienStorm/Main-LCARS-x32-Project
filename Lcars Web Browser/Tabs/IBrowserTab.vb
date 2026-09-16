Imports System.Windows.Forms

''' <summary>
''' Common contract for LCARS browser workspace tabs (web, docs, editors, ink).
''' </summary>
Public Interface IBrowserTab
    ReadOnly Property TabKind As String
    ReadOnly Property Title As String
    ReadOnly Property IsDirty As Boolean
    ReadOnly Property ContentControl As Control
    Function GetPathOrUrl() As String
    Sub NavigateOrOpen(ByVal pathOrUrl As String)
    Function Save(ByVal path As String) As Boolean
    Sub FocusContent()
End Interface
