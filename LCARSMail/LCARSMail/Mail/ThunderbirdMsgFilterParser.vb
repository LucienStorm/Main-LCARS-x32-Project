Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class ThunderbirdMsgFilterParser
        Private Shared ReadOnly KeyValuePattern As Regex = New Regex("^\s*([A-Za-z_]+)\s*=\s*""((?:\\.|[^""\\])*)""\s*$", RegexOptions.Compiled Or RegexOptions.CultureInvariant)

        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function ParseFile(path As String) As List(Of MailFilterRule)
            LastError = Nothing
            Dim result As List(Of MailFilterRule)
            If String.IsNullOrWhiteSpace(path) OrElse Not File.Exists(path) Then
                LastError = "msgFilterRules.dat not found."
                result = New List(Of MailFilterRule)()
            Else
                Try
                    Dim lines = File.ReadAllLines(path, Encoding.UTF8)
                    result = ParseLines(lines)
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = New List(Of MailFilterRule)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function ParseLines(lines As String()) As List(Of MailFilterRule)
            Dim list As List(Of MailFilterRule) = New List(Of MailFilterRule)()
            If lines Is Nothing Then
                Return list
            End If
            Dim mailFilterRule As MailFilterRule = Nothing
            For Each text In lines
                Dim text2 As String = If(Not Equals(text, Nothing), text, "").Trim()
                If text2.Length = 0 Then
                    Continue For
                End If
                Dim match = KeyValuePattern.Match(text2)
                If Not match.Success Then
                    Continue For
                End If
                Dim value = match.Groups(1).Value
                Dim text3 = UnescapeMozillaString(match.Groups(2).Value)
                Select Case value
                    Case "name"
                        If mailFilterRule IsNot Nothing Then
                            list.Add(mailFilterRule)
                        End If
                        Dim mailFilterRule2 As MailFilterRule = New MailFilterRule()
                        mailFilterRule2.Name = text3
                        mailFilterRule = mailFilterRule2
                        Exit Select
                    Case "enabled"
                        If mailFilterRule IsNot Nothing Then
                            mailFilterRule.Enabled = IsTruthy(text3)
                        End If
                    Case "type"
                        If mailFilterRule IsNot Nothing Then
                            Dim result = 0
                            Integer.TryParse(text3, result)
                            mailFilterRule.RunOnIncoming = If((result And 1) <> 0 OrElse result = 0, True, False)
                        End If
                    Case "condition"
                        If mailFilterRule IsNot Nothing Then
                            ParseConditionExpression(text3, mailFilterRule)
                        End If
                    Case "action"
                        If mailFilterRule IsNot Nothing Then
                            mailFilterRule.Actions.Add(New MailFilterAction With {.ActionName = text3})
                        End If
                    Case "actionValue"
                        If mailFilterRule IsNot Nothing AndAlso mailFilterRule.Actions.Count > 0 Then
                            mailFilterRule.Actions(mailFilterRule.Actions.Count - 1).ActionValue = text3
                        End If
                End Select
            Next
            If mailFilterRule IsNot Nothing Then
                list.Add(mailFilterRule)
            End If
            Return list
        End Function

        Private Shared Sub ParseConditionExpression(expr As String, rule As MailFilterRule)
            If rule Is Nothing Then
                Return
            End If
            rule.Conditions.Clear()
            If String.IsNullOrWhiteSpace(expr) Then
                Return
            End If
            Dim text As String = expr.Trim()
            If text.StartsWith("AND ", StringComparison.OrdinalIgnoreCase) Then
                rule.ConditionLogic = "AND"
                text = text.Substring(4).Trim()
            ElseIf text.StartsWith("OR ", StringComparison.OrdinalIgnoreCase) Then
                rule.ConditionLogic = "OR"
                text = text.Substring(3).Trim()
            End If
            Dim list = SplitTopLevelConditions(text)
            For Each item In list
                Dim mailFilterCondition = ParseSingleCondition(item)
                If mailFilterCondition IsNot Nothing Then
                    rule.Conditions.Add(mailFilterCondition)
                End If
            Next
        End Sub

        Private Shared Function ParseSingleCondition(part As String) As MailFilterCondition
            Dim text As String = If(Not Equals(part, Nothing), part, "").Trim()
            If text.StartsWith("(") AndAlso text.EndsWith(")") Then
                text = text.Substring(1, text.Length - 2).Trim()
            End If
            If text.Length = 0 Then
                Return Nothing
            End If
            Dim array = SplitConditionTokens(text)
            If array.Length < 3 Then
                Return Nothing
            End If
            Dim mailFilterCondition As MailFilterCondition = New MailFilterCondition()
            mailFilterCondition.Field = array(0)
            mailFilterCondition.Operator = array(1)
            mailFilterCondition.Value = String.Join(",", array, 2, array.Length - 2)
            Return mailFilterCondition
        End Function

        Private Shared Function SplitConditionTokens(p As String) As String()
            Dim list As List(Of String) = New List(Of String)()
            Dim stringBuilder As StringBuilder = New StringBuilder()
            Dim flag = False
            Dim num = p.Length - 1
            For i = 0 To num
                Dim c = p(i)
                Select Case c
                    Case """"c
                        flag = Not flag
                        Continue For
                    Case ","c
                        If Not flag Then
                            list.Add(stringBuilder.ToString().Trim())
                            stringBuilder.Clear()
                            Continue For
                        End If
                End Select
                stringBuilder.Append(c)
            Next
            If stringBuilder.Length > 0 Then
                list.Add(stringBuilder.ToString().Trim())
            End If
            Return list.ToArray()
        End Function

        Private Shared Function SplitTopLevelConditions(expr As String) As List(Of String)
            Dim list As List(Of String) = New List(Of String)()
            Dim num = 0
            Dim num2 = 0
            Dim num3 = expr.Length - 1
            For i = 0 To num3
                Dim c = expr(i)
                If c = "("c Then
                    num += 1
                End If
                If c = ")"c Then
                    num -= 1
                End If
                If c = ","c AndAlso num = 0 Then
                    list.Add(expr.Substring(num2, i - num2).Trim())
                    num2 = i + 1
                End If
            Next
            If num2 < expr.Length Then
                list.Add(expr.Substring(num2).Trim())
            End If
            Return list
        End Function

        Private Shared Function UnescapeMozillaString(s As String) As String
            If Equals(s, Nothing) Then
                Return ""
            End If
            Return s.Replace("\""", """").Replace("\\", "\").Replace("\n", vbLf).Replace("\r", "")
        End Function

        Private Shared Function IsTruthy(value As String) As Boolean
            Dim text As String = If(Not Equals(value, Nothing), value, "").Trim()
            Return If(String.Equals(text, "yes", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(text, "true", StringComparison.OrdinalIgnoreCase) OrElse Operators.CompareString(text, "1", TextCompare:=False) = 0, True, False)
        End Function
    End Class

