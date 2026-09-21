' Lcars Web Browser/Canvas/BoardHistory.vb
' Snapshot undo/redo for the Obsidian-style board.
Option Strict On
Option Explicit On

Imports System.Collections.Generic

Public Class BoardHistory
    Private ReadOnly _model As BoardModel
    Private ReadOnly _undo As New List(Of String)
    Private ReadOnly _redo As New List(Of String)
    Private _suppress As Boolean
    Private Const MaxDepth As Integer = 80

    Public Sub New(ByVal model As BoardModel)
        _model = model
    End Sub

    Public ReadOnly Property CanUndo As Boolean
        Get
            Return _undo.Count > 0
        End Get
    End Property

    Public ReadOnly Property CanRedo As Boolean
        Get
            Return _redo.Count > 0
        End Get
    End Property

    Public Sub CaptureBeforeChange()
        If _suppress Then Return
        Dim snap As String = _model.ExportSnapshotJson()
        If _undo.Count > 0 AndAlso String.Equals(_undo(_undo.Count - 1), snap, StringComparison.Ordinal) Then Return
        _undo.Add(snap)
        While _undo.Count > MaxDepth
            _undo.RemoveAt(0)
        End While
        _redo.Clear()
    End Sub

    Public Function Undo() As Boolean
        If _undo.Count = 0 Then Return False
        Dim current As String = _model.ExportSnapshotJson()
        Dim prior As String = _undo(_undo.Count - 1)
        _undo.RemoveAt(_undo.Count - 1)
        _redo.Add(current)
        _suppress = True
        Try
            _model.ImportSnapshotJson(prior)
        Finally
            _suppress = False
        End Try
        Return True
    End Function

    Public Function Redo() As Boolean
        If _redo.Count = 0 Then Return False
        Dim current As String = _model.ExportSnapshotJson()
        Dim nextSnap As String = _redo(_redo.Count - 1)
        _redo.RemoveAt(_redo.Count - 1)
        _undo.Add(current)
        _suppress = True
        Try
            _model.ImportSnapshotJson(nextSnap)
        Finally
            _suppress = False
        End Try
        Return True
    End Function

    Public Sub Clear()
        _undo.Clear()
        _redo.Clear()
    End Sub
End Class
