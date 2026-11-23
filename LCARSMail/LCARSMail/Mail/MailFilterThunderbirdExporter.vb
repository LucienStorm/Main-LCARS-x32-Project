Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class MailFilterThunderbirdExporter
        Private Sub New()
        End Sub

        Public Shared Function ExportToFile(rules As List(Of MailFilterRule), path As String) As Boolean
            Dim result As Boolean
            If rules Is Nothing OrElse String.IsNullOrWhiteSpace(path) Then
                result = False
            Else
                Try
                    Dim list As List(Of String) = New List(Of String)()
                    list.Add("version=""9""")
                    list.Add("logging=""no""")
                    For Each rule In rules
                        If rule IsNot Nothing Then
                            AppendRuleLines(rule, list)
                        End If
                    Next
                    Call File.WriteAllLines(path, list.ToArray(), Encoding.UTF8)
                    result = True
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Private Shared Sub AppendRuleLines(rule As MailFilterRule, lines As List(Of String))
            lines.Add("name=""" & Escape(rule.Name) & """")
            lines.Add("enabled=""" & If(rule.Enabled, "yes", "no") & """")
            lines.Add("type=""" & If(rule.RunOnIncoming, "1", "0") & """")
            lines.Add("condition=""" & Escape(BuildConditionExpression(rule)) & """")
            For Each action In rule.Actions
                If action IsNot Nothing Then
                    lines.Add("action=""" & Escape(action.ActionName) & """")
                    If Not String.IsNullOrEmpty(action.ActionValue) Then
                        lines.Add("actionValue=""" & Escape(action.ActionValue) & """")
                    End If
                End If
            Next
        End Sub

        Private Shared Function BuildConditionExpression(rule As MailFilterRule) As String
            Dim text As String = If(Not Equals(rule.ConditionLogic, Nothing), rule.ConditionLogic, "AND").Trim().ToUpperInvariant()
            If Operators.CompareString(text, "OR", TextCompare:=False) <> 0 Then
                text = "AND"
            End If
            Dim list As List(Of String) = New List(Of String)()
            For Each condition In rule.Conditions
                If condition IsNot Nothing Then
                    Dim array = New String(6) {"(", Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}
                    Dim mailFilterCondition = condition
                    array(1) = If(Not Equals(mailFilterCondition.Field, Nothing), mailFilterCondition.Field, "")
                    array(2) = ","
                    Dim mailFilterCondition2 = condition
                    array(3) = If(Not Equals(mailFilterCondition2.Operator, Nothing), mailFilterCondition2.Operator, "")
                    array(4) = ","
                    Dim mailFilterCondition3 = condition
                    array(5) = If(Not Equals(mailFilterCondition3.Value, Nothing), mailFilterCondition3.Value, "")
                    array(6) = ")"
                    list.Add(String.Concat(array))
                End If
            Next
            If list.Count = 0 Then
                Return text & " (subject,contains,)"
            End If
            Return text & " " & String.Join(" ", list.ToArray())
        End Function

        Private Shared Function Escape(value As String) As String
            If String.IsNullOrEmpty(value) Then
                Return ""
            End If
            Return value.Replace("\", "\\").Replace("""", "\""")
        End Function
    End Class

