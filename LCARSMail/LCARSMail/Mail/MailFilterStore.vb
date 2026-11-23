Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Web.Script.Serialization
Imports Microsoft.VisualBasic.CompilerServices
    <StandardModule>
    Public NotInheritable Class MailFilterStore
        Private Class RuleDto
            Public Property Name As String

            Public Property Enabled As Boolean

            Public Property RunOnIncoming As Boolean

            Public Property ConditionLogic As String

            Public Property Conditions As List(Of ConditionDto)

            Public Property Actions As List(Of ActionDto)

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub

            Public Shared Function FromModel(rule As MailFilterRule) As RuleDto
                Dim ruleDto As RuleDto = New RuleDto()
                ruleDto.Name = If(Not Equals(rule.Name, Nothing), rule.Name, "")
                ruleDto.Enabled = rule.Enabled
                ruleDto.RunOnIncoming = rule.RunOnIncoming
                ruleDto.ConditionLogic = If(Not Equals(rule.ConditionLogic, Nothing), rule.ConditionLogic, "AND")
                ruleDto.Conditions = New List(Of ConditionDto)()
                ruleDto.Actions = New List(Of ActionDto)()
                Dim ruleDto2 = ruleDto
                For Each condition In rule.Conditions
                    Dim conditions = ruleDto2.Conditions
                    Dim conditionDto As ConditionDto = New ConditionDto()
                    Dim mailFilterCondition = condition
                    conditionDto.Field = If(Not Equals(mailFilterCondition.Field, Nothing), mailFilterCondition.Field, "")
                    Dim mailFilterCondition2 = condition
                    conditionDto.Op = If(Not Equals(mailFilterCondition2.Operator, Nothing), mailFilterCondition2.Operator, "")
                    Dim mailFilterCondition3 = condition
                    conditionDto.Value = If(Not Equals(mailFilterCondition3.Value, Nothing), mailFilterCondition3.Value, "")
                    conditions.Add(conditionDto)
                Next
                For Each action In rule.Actions
                    Dim actions = ruleDto2.Actions
                    Dim actionDto As ActionDto = New ActionDto()
                    Dim mailFilterAction = action
                    actionDto.ActionName = If(Not Equals(mailFilterAction.ActionName, Nothing), mailFilterAction.ActionName, "")
                    Dim mailFilterAction2 = action
                    actionDto.ActionValue = If(Not Equals(mailFilterAction2.ActionValue, Nothing), mailFilterAction2.ActionValue, "")
                    actions.Add(actionDto)
                Next
                Return ruleDto2
            End Function

            Public Function ToModel() As MailFilterRule
                Dim mailFilterRule As MailFilterRule = New MailFilterRule()
                Dim name = Me.Name
                mailFilterRule.Name = If(Not Equals(name, Nothing), name, "")
                mailFilterRule.Enabled = Enabled
                mailFilterRule.RunOnIncoming = RunOnIncoming
                Dim conditionLogic = Me.ConditionLogic
                mailFilterRule.ConditionLogic = If(Not Equals(conditionLogic, Nothing), conditionLogic, "AND")
                Dim mailFilterRule2 = mailFilterRule
                If Conditions IsNot Nothing Then
                    For Each condition In Conditions
                        Dim conditions = mailFilterRule2.Conditions
                        Dim mailFilterCondition As MailFilterCondition = New MailFilterCondition()
                        conditionLogic = condition.Field
                        mailFilterCondition.Field = If(Not Equals(conditionLogic, Nothing), conditionLogic, "")
                        name = condition.Op
                        mailFilterCondition.Operator = If(Not Equals(name, Nothing), name, "")
                        Dim value = condition.Value
                        mailFilterCondition.Value = If(Not Equals(value, Nothing), value, "")
                        conditions.Add(mailFilterCondition)
                    Next
                End If
                If Actions IsNot Nothing Then
                    For Each action In Actions
                        Dim actions = mailFilterRule2.Actions
                        Dim mailFilterAction As MailFilterAction = New MailFilterAction()
                        Dim value = action.ActionName
                        mailFilterAction.ActionName = If(Not Equals(value, Nothing), value, "")
                        conditionLogic = action.ActionValue
                        mailFilterAction.ActionValue = If(Not Equals(conditionLogic, Nothing), conditionLogic, "")
                        actions.Add(mailFilterAction)
                    Next
                End If
                Return mailFilterRule2
            End Function
        End Class

        Private Class ConditionDto
            Public Property Field As String

            Public Property Op As String

            Public Property Value As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Class ActionDto
            Public Property ActionName As String

            Public Property ActionValue As String

            <DebuggerNonUserCode>
            Public Sub New()
            End Sub
        End Class

        Private Const FiltersFileName As String = "filters.json"

        Private Const FilterRulesFileName As String = "msgFilterRules.dat"

        Public Shared LastError As String

        Public Shared LastImportSkipped As Boolean

        Public Shared Function FiltersJsonPath() As String
            Return Path.Combine(AccountStore.DataRoot(), "filters.json")
        End Function

        Public Shared Function LoadRules() As List(Of MailFilterRule)
            Dim path As String = FiltersJsonPath()
            Dim result As List(Of MailFilterRule)
            If Not File.Exists(path) Then
                result = New List(Of MailFilterRule)()
            Else
                Try
                    Dim input = File.ReadAllText(path, Encoding.UTF8)
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim list = javaScriptSerializer.Deserialize(Of List(Of RuleDto))(input)
                    If list Is Nothing Then
                        result = New List(Of MailFilterRule)()
                    Else
                        Dim list2 As List(Of MailFilterRule) = New List(Of MailFilterRule)()
                        For Each item In list
                            Dim mailFilterRule As MailFilterRule = item.ToModel()
                            If mailFilterRule IsNot Nothing Then
                                list2.Add(mailFilterRule)
                            End If
                        Next
                        result = list2
                    End If
                Catch projectError As Exception
                    ProjectData.SetProjectError(projectError)
                    result = New List(Of MailFilterRule)()
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function SaveRules(rules As List(Of MailFilterRule)) As Boolean
            LastError = Nothing
            Dim result As Boolean
            If rules Is Nothing Then
                LastError = "rules is required."
                result = False
            Else
                Try
                    Dim list As List(Of RuleDto) = New List(Of RuleDto)()
                    For Each rule In rules
                        If rule IsNot Nothing Then
                            list.Add(RuleDto.FromModel(rule))
                        End If
                    Next
                    Dim javaScriptSerializer As JavaScriptSerializer = New JavaScriptSerializer()
                    Dim contents = javaScriptSerializer.Serialize(list)
                    Call File.WriteAllText(FiltersJsonPath(), contents, Encoding.UTF8)
                    result = True
                Catch ex As Exception
                    ProjectData.SetProjectError(ex)
                    Dim ex2 = ex
                    LastError = ex2.Message
                    result = False
                    Call ProjectData.ClearProjectError()
                End Try
            End If
            Return result
        End Function

        Public Shared Function TryImportFromProfile(profileDir As String) As Integer
            LastError = Nothing
            LastImportSkipped = False
            If String.IsNullOrWhiteSpace(profileDir) Then
                LastError = "Profile folder is required."
                Return -1
            End If
            Dim path As String = IO.Path.Combine(profileDir.Trim(), "msgFilterRules.dat")
            If Not File.Exists(path) Then
                LastImportSkipped = True
                Return 0
            End If
            Dim list = ThunderbirdMsgFilterParser.ParseFile(path)
            If Not String.IsNullOrEmpty(ThunderbirdMsgFilterParser.LastError) Then
                LastError = ThunderbirdMsgFilterParser.LastError
                Return -1
            End If
            If Not SaveRules(list) Then
                Return -1
            End If
            Return list.Count
        End Function
    End Class

