' Lcars Web Browser/Documents/DocxDocumentAdapter.vb
Option Strict On
Option Explicit On

Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports System.Windows.Forms
Imports System.Xml

''' <summary>
''' Minimal .docx open/save for the unified editor (paragraph text).
''' </summary>
Public Module DocxDocumentAdapter

    Public Sub LoadInto(ByVal box As RichTextBox, ByVal filePath As String)
        Using zip As ZipArchive = ZipFile.OpenRead(filePath)
            Dim entry As ZipArchiveEntry = zip.GetEntry("word/document.xml")
            If entry Is Nothing Then Throw New InvalidDataException("DOCX missing word/document.xml")

            Dim xml As New XmlDocument()
            Using stream As Stream = entry.Open()
                xml.Load(stream)
            End Using

            Dim nsmgr As New XmlNamespaceManager(xml.NameTable)
            nsmgr.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main")

            Dim sb As New StringBuilder()
            Dim paragraphs As XmlNodeList = xml.SelectNodes("//w:p", nsmgr)
            If paragraphs IsNot Nothing Then
                For Each p As XmlNode In paragraphs
                    Dim line As New StringBuilder()
                    Dim texts As XmlNodeList = p.SelectNodes(".//w:t", nsmgr)
                    If texts IsNot Nothing Then
                        For Each t As XmlNode In texts
                            line.Append(If(t.InnerText, ""))
                        Next
                    End If
                    sb.AppendLine(line.ToString())
                Next
            End If
            box.Clear()
            box.Text = sb.ToString()
        End Using
    End Sub

    Public Sub SaveFrom(ByVal box As RichTextBox, ByVal filePath As String)
        Dim parentDir As String = System.IO.Path.GetDirectoryName(filePath)
        If Not String.IsNullOrEmpty(parentDir) AndAlso Not Directory.Exists(parentDir) Then
            Directory.CreateDirectory(parentDir)
        End If
        If File.Exists(filePath) Then File.Delete(filePath)

        Dim lines As String() = box.Lines
        Dim body As New StringBuilder()
        body.Append("<w:document xmlns:w=""http://schemas.openxmlformats.org/wordprocessingml/2006/main""><w:body>")
        For Each line As String In lines
            body.Append("<w:p><w:r><w:t xml:space=""preserve"">")
            body.Append(XmlEscape(line))
            body.Append("</w:t></w:r></w:p>")
        Next
        body.Append("<w:sectPr/></w:body></w:document>")

        Using zip As ZipArchive = ZipFile.Open(filePath, ZipArchiveMode.Create)
            WriteEntry(zip, "[Content_Types].xml",
                "<?xml version=""1.0"" encoding=""UTF-8""?>" &
                "<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">" &
                "<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>" &
                "<Default Extension=""xml"" ContentType=""application/xml""/>" &
                "<Override PartName=""/word/document.xml"" ContentType=""application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml""/>" &
                "</Types>")
            WriteEntry(zip, "_rels/.rels",
                "<?xml version=""1.0"" encoding=""UTF-8""?>" &
                "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
                "<Relationship Id=""rId1"" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"" Target=""word/document.xml""/>" &
                "</Relationships>")
            WriteEntry(zip, "word/document.xml", body.ToString())
        End Using
    End Sub

    Private Sub WriteEntry(ByVal zip As ZipArchive, ByVal name As String, ByVal content As String)
        Dim entry As ZipArchiveEntry = zip.CreateEntry(name, CompressionLevel.Optimal)
        Using writer As New StreamWriter(entry.Open(), New UTF8Encoding(False))
            writer.Write(content)
        End Using
    End Sub

    Private Function XmlEscape(ByVal text As String) As String
        If text Is Nothing Then Return ""
        Return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("""", "&quot;")
    End Function
End Module
