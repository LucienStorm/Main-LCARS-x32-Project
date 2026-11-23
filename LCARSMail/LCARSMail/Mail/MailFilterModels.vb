' LCARSMail/LCARSMail/Mail/MailFilterModels.vb
Option Strict On
Option Infer On
Option Explicit On
Imports System.Collections.Generic
''' <summary>One Thunderbird-style message filter rule.</summary>
Public Class MailFilterRule
Public Name As String = String.Empty
Public Enabled As Boolean = True
''' <summary>True when filter should run on incoming mail (Thunderbird type bit).</summary>
Public RunOnIncoming As Boolean = True
Public Conditions As New List(Of MailFilterCondition)()
Public ConditionLogic As String = "AND"
Public Actions As New List(Of MailFilterAction)()
End Class
Public Class MailFilterCondition
''' <summary>from, to, subject, body, age, size, cc, toorcc, etc.</summary>
Public Field As String = String.Empty
''' <summary>contains, doesn't contain, is, isn't, begins with, ends with, is greater than, ...</summary>
Public [Operator] As String = String.Empty
Public Value As String = String.Empty
End Class
Public Class MailFilterAction
''' <summary>Move to folder, Mark read, Delete, Stop execution, ...</summary>
Public ActionName As String = String.Empty
Public ActionValue As String = String.Empty
End Class
Public Class MailFilterRunSummary
Public RulesEvaluated As Integer
Public MessagesMatched As Integer
Public ActionsApplied As Integer
Public Warnings As New List(Of String)()
End Class
