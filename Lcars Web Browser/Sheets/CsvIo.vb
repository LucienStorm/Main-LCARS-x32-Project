Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.Text

''' <summary>
''' CSV load/save for SheetModel (UTF-8, quoted fields).
''' </summary>
Public Module CsvIo

    ''' <summary>
    ''' Loads CSV rows into a SheetModel (A1 = row0 col0).
    ''' </summary>
    Public Function Load(ByVal path As String) As SheetModel
        Dim model As New SheetModel()
        Dim lines As String() = File.ReadAllLines(path, New UTF8Encoding(False))
        Dim cells As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

        For row As Integer = 0 To lines.Length - 1
            Dim fields As List(Of String) = ParseCsvLine(lines(row))
            For col As Integer = 0 To fields.Count - 1
                Dim value As String = fields(col)
                If value <> "" Then
                    cells(SheetModel.AddressFrom(row, col)) = value
                End If
            Next
        Next

        model.ImportRawCells(cells)
        Return model
    End Function

    ''' <summary>
    ''' Saves SheetModel raw values to CSV.
    ''' </summary>
    Public Sub Save(ByVal model As SheetModel, ByVal path As String)
        Dim bounds As Tuple(Of Integer, Integer) = model.GetUsedBounds()
        Dim maxRow As Integer = Math.Max(bounds.Item1, 0)
        Dim maxCol As Integer = Math.Max(bounds.Item2, 0)
        Dim rawCells As Dictionary(Of String, String) = model.ExportRawCells()

        Dim sb As New StringBuilder()
        For row As Integer = 0 To maxRow
            Dim fields As New List(Of String)
            For col As Integer = 0 To maxCol
                Dim addr As String = SheetModel.AddressFrom(row, col)
                Dim value As String = ""
                If rawCells.ContainsKey(addr) Then value = rawCells(addr)
                fields.Add(EscapeCsvField(value))
            Next
            sb.AppendLine(String.Join(",", fields.ToArray()))
        Next

        Dim parentDir As String = System.IO.Path.GetDirectoryName(path)
        If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
            Directory.CreateDirectory(parentDir)
        End If
        File.WriteAllText(path, sb.ToString(), New UTF8Encoding(False))
    End Sub

    Private Function ParseCsvLine(ByVal line As String) As List(Of String)
        Dim fields As New List(Of String)()
        If line Is Nothing Then
            fields.Add("")
            Return fields
        End If

        Dim i As Integer = 0
        While i < line.Length
            If line(i) = """"c Then
                Dim field As New StringBuilder()
                i += 1
                While i < line.Length
                    If line(i) = """"c Then
                        If i + 1 < line.Length AndAlso line(i + 1) = """"c Then
                            field.Append("""")
                            i += 2
                        Else
                            i += 1
                            Exit While
                        End If
                    Else
                        field.Append(line(i))
                        i += 1
                    End If
                End While
                fields.Add(field.ToString())
                If i < line.Length AndAlso line(i) = ","c Then i += 1
            Else
                Dim start As Integer = i
                While i < line.Length AndAlso line(i) <> ","c
                    i += 1
                End While
                fields.Add(line.Substring(start, i - start))
                If i < line.Length AndAlso line(i) = ","c Then i += 1
            End If
        End While

        If line.Length = 0 OrElse line.EndsWith(",", StringComparison.Ordinal) Then
            fields.Add("")
        End If

        Return fields
    End Function

    Private Function EscapeCsvField(ByVal value As String) As String
        Dim text As String = If(value, "")
        If text.Contains(",") OrElse text.Contains("""") OrElse text.Contains(vbCr) OrElse text.Contains(vbLf) Then
            Return """" & text.Replace("""", """""") & """"
        End If
        Return text
    End Function

End Module
