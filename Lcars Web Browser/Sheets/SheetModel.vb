Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text.RegularExpressions

''' <summary>
''' In-memory spreadsheet cell store with formula evaluation for arithmetic and SUM.
''' </summary>
Public Class SheetModel

    Private ReadOnly _cells As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _displayCache As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
    Private _evaluating As HashSet(Of String)

    Public Event ModelChanged As EventHandler

    ''' <summary>
    ''' Raw cell input (literal or formula). Empty when unset.
    ''' </summary>
    Public Function GetRaw(ByVal address As String) As String
        Dim key As String = NormalizeAddress(address)
        If key = "" Then Return ""
        If _cells.ContainsKey(key) Then Return _cells(key)
        Return ""
    End Function

    ''' <summary>
    ''' Display value after formula evaluation.
    ''' </summary>
    Public Function GetDisplay(ByVal address As String) As String
        Dim key As String = NormalizeAddress(address)
        If key = "" Then Return ""
        If _displayCache.ContainsKey(key) Then Return _displayCache(key)
        Return ""
    End Function

    ''' <summary>
    ''' Sets cell input and recalculates all formulas.
    ''' </summary>
    Public Sub SetRaw(ByVal address As String, ByVal rawValue As String)
        Dim key As String = NormalizeAddress(address)
        If key = "" Then Return

        Dim text As String = If(rawValue, "").Trim()
        If text = "" Then
            _cells.Remove(key)
        Else
            _cells(key) = text
        End If

        RecalculateAll()
        RaiseEvent ModelChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Clears all cells.
    ''' </summary>
    Public Sub Clear()
        _cells.Clear()
        _displayCache.Clear()
        RaiseEvent ModelChanged(Me, EventArgs.Empty)
    End Sub

    ''' <summary>
    ''' Returns bounds (max row/col, 0-based) covering all stored cells.
    ''' </summary>
    Public Function GetUsedBounds() As Tuple(Of Integer, Integer)
        Dim maxRow As Integer = 0
        Dim maxCol As Integer = 0
        For Each key As String In _cells.Keys
            Dim pos As Tuple(Of Integer, Integer) = ParseAddress(key)
            If pos Is Nothing Then Continue For
            If pos.Item1 > maxRow Then maxRow = pos.Item1
            If pos.Item2 > maxCol Then maxCol = pos.Item2
        Next
        Return Tuple.Create(maxRow, maxCol)
    End Function

    ''' <summary>
    ''' Copies all raw cell values into a new dictionary keyed by address.
    ''' </summary>
    Public Function ExportRawCells() As Dictionary(Of String, String)
        Return New Dictionary(Of String, String)(_cells, StringComparer.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Replaces all cells from a dictionary of raw values.
    ''' </summary>
    Public Sub ImportRawCells(ByVal cells As Dictionary(Of String, String))
        _cells.Clear()
        If cells IsNot Nothing Then
            For Each pair As KeyValuePair(Of String, String) In cells
                Dim key As String = NormalizeAddress(pair.Key)
                If key = "" Then Continue For
                Dim text As String = If(pair.Value, "").Trim()
                If text <> "" Then _cells(key) = text
            Next
        End If
        RecalculateAll()
        RaiseEvent ModelChanged(Me, EventArgs.Empty)
    End Sub

    Private Sub RecalculateAll()
        _displayCache.Clear()
        _evaluating = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each key As String In _cells.Keys
            _displayCache(key) = EvaluateCell(key)
        Next
        _evaluating = Nothing
    End Sub

    Private Function EvaluateCell(ByVal address As String) As String
        Dim key As String = NormalizeAddress(address)
        If key = "" OrElse Not _cells.ContainsKey(key) Then Return ""

        If _evaluating IsNot Nothing AndAlso _evaluating.Contains(key) Then Return "#ERR"
        If _evaluating IsNot Nothing Then _evaluating.Add(key)

        Try
            Dim raw As String = _cells(key)
            If Not raw.StartsWith("=", StringComparison.Ordinal) Then Return raw

            Dim expr As String = raw.Substring(1).Trim()
            If expr = "" Then Return ""

            Dim sumMatch As Match = Regex.Match(expr, "^SUM\(([A-Z]+)(\d+):([A-Z]+)(\d+)\)$", RegexOptions.IgnoreCase)
            If sumMatch.Success Then
                Dim startAddr As String = sumMatch.Groups(1).Value & sumMatch.Groups(2).Value
                Dim endAddr As String = sumMatch.Groups(3).Value & sumMatch.Groups(4).Value
                Dim sum As Double = SumRange(startAddr, endAddr)
                If Double.IsNaN(sum) Then Return "#ERR"
                Return FormatNumber(sum)
            End If

            Dim addMatch As Match = Regex.Match(expr, "^([A-Z]+\d+)\+([A-Z]+\d+)$", RegexOptions.IgnoreCase)
            If addMatch.Success Then
                Dim left As Double = GetNumericValue(addMatch.Groups(1).Value)
                Dim right As Double = GetNumericValue(addMatch.Groups(2).Value)
                If Double.IsNaN(left) OrElse Double.IsNaN(right) Then Return "#ERR"
                Return FormatNumber(left + right)
            End If

            Return "#ERR"
        Finally
            If _evaluating IsNot Nothing Then _evaluating.Remove(key)
        End Try
    End Function

    Private Function SumRange(ByVal startAddr As String, ByVal endAddr As String) As Double
        Dim startPos As Tuple(Of Integer, Integer) = ParseAddress(startAddr)
        Dim endPos As Tuple(Of Integer, Integer) = ParseAddress(endAddr)
        If startPos Is Nothing OrElse endPos Is Nothing Then Return Double.NaN

        Dim minRow As Integer = Math.Min(startPos.Item1, endPos.Item1)
        Dim maxRow As Integer = Math.Max(startPos.Item1, endPos.Item1)
        Dim minCol As Integer = Math.Min(startPos.Item2, endPos.Item2)
        Dim maxCol As Integer = Math.Max(startPos.Item2, endPos.Item2)

        Dim total As Double = 0
        For row As Integer = minRow To maxRow
            For col As Integer = minCol To maxCol
                Dim value As Double = GetNumericValue(AddressFrom(row, col))
                If Double.IsNaN(value) Then Return Double.NaN
                total += value
            Next
        Next
        Return total
    End Function

    Private Function GetNumericValue(ByVal address As String) As Double
        Dim key As String = NormalizeAddress(address)
        If key = "" Then Return Double.NaN

        If _evaluating IsNot Nothing AndAlso _evaluating.Contains(key) Then Return Double.NaN

        Dim text As String = ""
        If _displayCache.ContainsKey(key) Then
            text = _displayCache(key)
        ElseIf _cells.ContainsKey(key) Then
            Dim raw As String = _cells(key)
            If raw.StartsWith("=", StringComparison.Ordinal) Then
                text = EvaluateCell(key)
            Else
                text = raw
            End If
        End If

        If text = "#ERR" Then Return Double.NaN
        If text = "" Then Return 0
        Dim parsed As Double
        If Double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, parsed) Then Return parsed
        If Double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, parsed) Then Return parsed
        Return Double.NaN
    End Function

    Private Shared Function FormatNumber(ByVal value As Double) As String
        If value = Math.Truncate(value) AndAlso value >= -9000000000000000 AndAlso value <= 9000000000000000 Then
            Return Convert.ToInt64(value).ToString(CultureInfo.InvariantCulture)
        End If
        Return value.ToString("G", CultureInfo.InvariantCulture)
    End Function

    Public Shared Function AddressFrom(ByVal row As Integer, ByVal col As Integer) As String
        Return ColToLetters(col) & (row + 1).ToString(CultureInfo.InvariantCulture)
    End Function

    Public Shared Function ParseAddress(ByVal address As String) As Tuple(Of Integer, Integer)
        Dim text As String = NormalizeAddress(address)
        If text = "" Then Return Nothing
        Dim match As Match = Regex.Match(text, "^([A-Z]+)(\d+)$", RegexOptions.IgnoreCase)
        If Not match.Success Then Return Nothing

        Dim col As Integer = LettersToCol(match.Groups(1).Value)
        Dim row As Integer
        If Not Integer.TryParse(match.Groups(2).Value, NumberStyles.Integer, CultureInfo.InvariantCulture, row) Then Return Nothing
        row -= 1
        If row < 0 OrElse col < 0 Then Return Nothing
        Return Tuple.Create(row, col)
    End Function

    Public Shared Function NormalizeAddress(ByVal address As String) As String
        Dim text As String = If(address, "").Trim().ToUpperInvariant()
        If text = "" Then Return ""
        Dim match As Match = Regex.Match(text, "^([A-Z]+)(\d+)$")
        If Not match.Success Then Return ""
        Return match.Groups(1).Value & match.Groups(2).Value
    End Function

    Public Shared Function ColToLetters(ByVal col As Integer) As String
        Dim index As Integer = col
        Dim letters As String = ""
        Do
            letters = Chr(65 + (index Mod 26)) & letters
            index = index \ 26 - 1
        Loop While index >= 0
        Return letters
    End Function

    Private Shared Function LettersToCol(ByVal letters As String) As Integer
        Dim col As Integer = 0
        For Each ch As Char In letters.ToUpperInvariant()
            col = col * 26 + (Asc(ch) - 64)
        Next
        Return col - 1
    End Function

End Class
