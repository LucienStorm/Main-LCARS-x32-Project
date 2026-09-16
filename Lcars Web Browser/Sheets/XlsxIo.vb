Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Xml

''' <summary>
''' Minimal XLSX read/write without Office or DocumentFormat.OpenXml.
''' XLSX approach: hand-built Open XML (ZIP + XML) using System.IO.Compression only.
''' This avoids adding a NuGet package to the legacy x86 vbproj; CSV remains the
''' primary interchange format. XLSX support covers simple single-sheet values.
''' </summary>
Public Module XlsxIo

    Private Const SheetPath As String = "xl/worksheets/sheet1.xml"
    Private Const SharedStringsPath As String = "xl/sharedStrings.xml"

    ''' <summary>
    ''' Loads the first worksheet from an XLSX file into a SheetModel.
    ''' </summary>
    Public Function Load(ByVal path As String) As SheetModel
        Dim model As New SheetModel()
        Dim cells As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Dim sharedStrings As List(Of String) = Nothing

        Using archive As ZipArchive = ZipFile.OpenRead(path)
            Dim sharedEntry As ZipArchiveEntry = archive.GetEntry(SharedStringsPath)
            If sharedEntry IsNot Nothing Then
                sharedStrings = ReadSharedStrings(sharedEntry)
            End If

            Dim sheetEntry As ZipArchiveEntry = archive.GetEntry(SheetPath)
            If sheetEntry Is Nothing Then
                model.ImportRawCells(cells)
                Return model
            End If

            Using stream As Stream = sheetEntry.Open()
                Using reader As XmlReader = XmlReader.Create(stream)
                    Dim cellRef As String = ""
                    Dim cellType As String = ""
                    Dim inlineText As New StringBuilder()
                    Dim inInline As Boolean = False
                    Dim inValue As Boolean = False
                    Dim valueText As New StringBuilder()

                    While reader.Read()
                        If reader.NodeType = XmlNodeType.Element Then
                            If reader.Name = "c" Then
                                cellRef = reader.GetAttribute("r")
                                cellType = reader.GetAttribute("t")
                                inlineText.Clear()
                                valueText.Clear()
                                inInline = False
                                inValue = False
                            ElseIf reader.Name = "is" OrElse reader.Name = "t" Then
                                If reader.Name = "is" Then inInline = True
                            ElseIf reader.Name = "v" Then
                                inValue = True
                                valueText.Clear()
                            End If
                        ElseIf reader.NodeType = XmlNodeType.Text Then
                            If inInline Then inlineText.Append(reader.Value)
                            If inValue Then valueText.Append(reader.Value)
                        ElseIf reader.NodeType = XmlNodeType.EndElement Then
                            If reader.Name = "t" AndAlso inInline Then
                                ' keep collecting inline fragments
                            ElseIf reader.Name = "is" Then
                                inInline = False
                            ElseIf reader.Name = "v" Then
                                inValue = False
                            ElseIf reader.Name = "c" Then
                                Dim text As String = ResolveCellValue(cellType, valueText.ToString(), inlineText.ToString(), sharedStrings)
                                Dim addr As String = SheetModel.NormalizeAddress(cellRef)
                                If addr <> "" AndAlso text <> "" Then cells(addr) = text
                            End If
                        End If
                    End While
                End Using
            End Using
        End Using

        model.ImportRawCells(cells)
        Return model
    End Function

    ''' <summary>
    ''' Writes SheetModel display values to a minimal single-sheet XLSX.
    ''' </summary>
    Public Sub Save(ByVal model As SheetModel, ByVal path As String)
        Dim bounds As Tuple(Of Integer, Integer) = model.GetUsedBounds()
        Dim maxRow As Integer = Math.Max(bounds.Item1, 0)
        Dim maxCol As Integer = Math.Max(bounds.Item2, 0)

        Dim parentDir As String = System.IO.Path.GetDirectoryName(path)
        If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
            Directory.CreateDirectory(parentDir)
        End If

        If File.Exists(path) Then File.Delete(path)

        Using archive As ZipArchive = ZipFile.Open(path, ZipArchiveMode.Create)
            WriteEntry(archive, "[Content_Types].xml", BuildContentTypesXml())
            WriteEntry(archive, "_rels/.rels", BuildRootRelsXml())
            WriteEntry(archive, "xl/workbook.xml", BuildWorkbookXml())
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml())
            WriteEntry(archive, SheetPath, BuildSheetXml(model, maxRow, maxCol))
        End Using
    End Sub

    Private Function ReadSharedStrings(ByVal entry As ZipArchiveEntry) As List(Of String)
        Dim list As New List(Of String)()
        Using stream As Stream = entry.Open()
            Using reader As XmlReader = XmlReader.Create(stream)
                Dim current As New StringBuilder()
                Dim inText As Boolean = False
                While reader.Read()
                    If reader.NodeType = XmlNodeType.Element AndAlso reader.Name = "t" Then
                        inText = True
                        current.Clear()
                    ElseIf reader.NodeType = XmlNodeType.Text AndAlso inText Then
                        current.Append(reader.Value)
                    ElseIf reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name = "si" Then
                        list.Add(current.ToString())
                        inText = False
                    ElseIf reader.NodeType = XmlNodeType.EndElement AndAlso reader.Name = "t" Then
                        inText = False
                    End If
                End While
            End Using
        End Using
        Return list
    End Function

    Private Function ResolveCellValue(ByVal cellType As String, ByVal valueText As String, ByVal inlineText As String, ByVal sharedStrings As List(Of String)) As String
        If cellType = "s" Then
            Dim index As Integer
            If Integer.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, index) AndAlso sharedStrings IsNot Nothing AndAlso index >= 0 AndAlso index < sharedStrings.Count Then
                Return sharedStrings(index)
            End If
            Return ""
        End If
        If cellType = "inlineStr" OrElse inlineText.Length > 0 Then Return inlineText
        Return valueText
    End Function

    Private Function BuildSheetXml(ByVal model As SheetModel, ByVal maxRow As Integer, ByVal maxCol As Integer) As String
        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        sb.Append("<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">")
        sb.Append("<sheetData>")

        For row As Integer = 0 To maxRow
            sb.Append("<row r=""").Append(row + 1).Append(""">")
            For col As Integer = 0 To maxCol
                Dim addr As String = SheetModel.AddressFrom(row, col)
                Dim display As String = model.GetDisplay(addr)
                If display = "" Then Continue For

                Dim numeric As Double
                If Double.TryParse(display, NumberStyles.Float, CultureInfo.InvariantCulture, numeric) OrElse
                   Double.TryParse(display, NumberStyles.Float, CultureInfo.CurrentCulture, numeric) Then
                    sb.Append("<c r=""").Append(addr).Append("""><v>").Append(XmlEscape(numeric.ToString("G", CultureInfo.InvariantCulture))).Append("</v></c>")
                Else
                    sb.Append("<c r=""").Append(addr).Append(""" t=""inlineStr""><is><t>").Append(XmlEscape(display)).Append("</t></is></c>")
                End If
            Next
            sb.Append("</row>")
        Next

        sb.Append("</sheetData></worksheet>")
        Return sb.ToString()
    End Function

    Private Function BuildContentTypesXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8""?>" &
            "<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">" &
            "<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>" &
            "<Default Extension=""xml"" ContentType=""application/xml""/>" &
            "<Override PartName=""/xl/workbook.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>" &
            "<Override PartName=""/xl/worksheets/sheet1.xml"" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>" &
            "</Types>"
    End Function

    Private Function BuildRootRelsXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8""?>" &
            "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
            "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""xl/workbook.xml""/>" &
            "</Relationships>"
    End Function

    Private Function BuildWorkbookXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
            "<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">" &
            "<sheets><sheet name=""Sheet1"" sheetId=""1"" r:id=""rId1""/></sheets></workbook>"
    End Function

    Private Function BuildWorkbookRelsXml() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8""?>" &
            "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
            "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"" Target=""worksheets/sheet1.xml""/>" &
            "</Relationships>"
    End Function

    Private Sub WriteEntry(ByVal archive As ZipArchive, ByVal name As String, ByVal content As String)
        Dim entry As ZipArchiveEntry = archive.CreateEntry(name, CompressionLevel.Optimal)
        Using stream As Stream = entry.Open()
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(content)
            stream.Write(bytes, 0, bytes.Length)
        End Using
    End Sub

    Private Function XmlEscape(ByVal text As String) As String
        If String.IsNullOrEmpty(text) Then Return ""
        Dim cleaned As String = Regex.Replace(text, "[\x00-\x08\x0B\x0C\x0E-\x1F]", "")
        cleaned = cleaned.Replace("&", "&amp;")
        cleaned = cleaned.Replace("<", "&lt;")
        cleaned = cleaned.Replace(">", "&gt;")
        cleaned = cleaned.Replace("""", "&quot;")
        cleaned = cleaned.Replace("'", "&apos;")
        Return cleaned
    End Function

End Module
