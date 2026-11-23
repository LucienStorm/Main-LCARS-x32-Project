Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Net.Security
Imports System.Net.Sockets
Imports System.Security
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.VisualBasic.CompilerServices
    Public NotInheritable Class XmppChatService
        Public Shared LastError As String

        Private Sub New()
        End Sub

        Public Shared Function TrySendMessage(host As String, port As Integer, jid As String, password As String, toJid As String, body As String) As Boolean
            LastError = Nothing
            Dim text As String = If(Not Equals(host, Nothing), host, "").Trim()
            Dim text2 As String = If(Not Equals(jid, Nothing), jid, "").Trim()
            Dim text3 As String = If(Not Equals(toJid, Nothing), toJid, "").Trim()
            Dim raw = If(Not Equals(body, Nothing), body, "")
            Dim result As Boolean
            If text.Length = 0 OrElse text2.Length = 0 OrElse text3.Length = 0 Then
                LastError = "Host, JID, and recipient are required."
                result = False
            Else
                If port <= 0 Then
                    port = 5222
                End If
                If Not text2.Contains("@") Then
                    LastError = "JID must include @domain."
                    result = False
                Else
                    Dim raw2 = text2.Substring(text2.IndexOf("@"c) + 1)
                    Try
                        Using tcpClient As TcpClient = New TcpClient()
                            tcpClient.Connect(text, port)
                            Dim innerStream As NetworkStream = tcpClient.GetStream()
                            Dim sslStream As SslStream = New SslStream(innerStream, leaveInnerStreamOpen:=False, Function(a, b, c, d) True)
                            sslStream.AuthenticateAsClient(text)
                            Dim streamWriter As StreamWriter = New StreamWriter(sslStream, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
                            streamWriter.NewLine = vbLf
                            streamWriter.AutoFlush = True
                            Dim streamWriter2 = streamWriter
                            Dim reader As StreamReader = New StreamReader(sslStream, Encoding.UTF8)
                            streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=""" & XmlEscape(raw2) & """ xmlns=""jabber:client"" xmlns:stream=""http://etherx.jabber.org/streams"" version=""1.0"">")
                            WaitForFeatures(reader)
                            Dim text4 = Convert.ToBase64String(Encoding.UTF8.GetBytes(vbNullChar & text2 & vbNullChar & If(Not Equals(password, Nothing), password, "")))
                            streamWriter2.WriteLine("<auth xmlns=""urn:ietf:params:xml:ns:xmpp-sasl"" mechanism=""PLAIN"">" & text4 & "</auth>")
                            ReadUntil(reader, "success")
                            streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=""" & XmlEscape(raw2) & """ xmlns=""jabber:client"" xmlns:stream=""http://etherx.jabber.org/streams"" version=""1.0"">")
                            WaitForFeatures(reader)
                            Dim text5 As String = "lcars" & Guid.NewGuid().ToString("N").Substring(0, 8)
                            streamWriter2.WriteLine("<iq type=""set"" id=""bind1""><bind xmlns=""urn:ietf:params:xml:ns:xmpp-bind""><resource>" & text5 & "</resource></bind></iq>")
                            ReadUntil(reader, "bind")
                            Dim text6 As String = Guid.NewGuid().ToString("N")
                            streamWriter2.WriteLine("<message to=""" & XmlEscape(text3) & """ type=""chat"" id=""" & text6 & """><body>" & XmlEscape(raw) & "</body></message>")
                            streamWriter2.WriteLine("</stream:stream>")
                        End Using
                        result = True
                    Catch ex As Exception
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = ex2.Message
                        result = False
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Public Shared Function TryPullIncomingMessages(host As String, port As Integer, jid As String, password As String, Optional listenSeconds As Integer = 6) As String
            LastError = Nothing
            Dim text As String = If(Not Equals(host, Nothing), host, "").Trim()
            Dim text2 As String = If(Not Equals(jid, Nothing), jid, "").Trim()
            Dim result As String
            If text.Length = 0 OrElse text2.Length = 0 Then
                LastError = "Host and JID are required."
                result = ""
            Else
                If port <= 0 Then
                    port = 5222
                End If
                If Not text2.Contains("@") Then
                    LastError = "JID must include @domain."
                    result = ""
                Else
                    Dim raw = text2.Substring(text2.IndexOf("@"c) + 1)
                    If listenSeconds < 2 Then
                        listenSeconds = 2
                    End If
                    If listenSeconds > 20 Then
                        listenSeconds = 20
                    End If
                    Dim list As List(Of String) = New List(Of String)()
                    Dim regex As Regex = New Regex("<body>([\s\S]*?)</body>", RegexOptions.IgnoreCase)
                    Try
                        Using tcpClient As TcpClient = New TcpClient()
                            tcpClient.Connect(text, port)
                            Dim networkStream As NetworkStream = tcpClient.GetStream()
                            Dim sslStream As SslStream = New SslStream(networkStream, leaveInnerStreamOpen:=False, Function(a, b, c, d) True)
                            sslStream.AuthenticateAsClient(text)
                            Dim streamWriter As StreamWriter = New StreamWriter(sslStream, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
                            streamWriter.NewLine = vbLf
                            streamWriter.AutoFlush = True
                            Dim streamWriter2 = streamWriter
                            Dim streamReader As StreamReader = New StreamReader(sslStream, Encoding.UTF8)
                            streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=""" & XmlEscape(raw) & """ xmlns=""jabber:client"" xmlns:stream=""http://etherx.jabber.org/streams"" version=""1.0"">")
                            WaitForFeatures(streamReader)
                            Dim text3 = Convert.ToBase64String(Encoding.UTF8.GetBytes(vbNullChar & text2 & vbNullChar & If(Not Equals(password, Nothing), password, "")))
                            streamWriter2.WriteLine("<auth xmlns=""urn:ietf:params:xml:ns:xmpp-sasl"" mechanism=""PLAIN"">" & text3 & "</auth>")
                            ReadUntil(streamReader, "success")
                            streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=""" & XmlEscape(raw) & """ xmlns=""jabber:client"" xmlns:stream=""http://etherx.jabber.org/streams"" version=""1.0"">")
                            WaitForFeatures(streamReader)
                            Dim text4 As String = "lcars" & Guid.NewGuid().ToString("N").Substring(0, 8)
                            streamWriter2.WriteLine("<iq type=""set"" id=""bind1""><bind xmlns=""urn:ietf:params:xml:ns:xmpp-bind""><resource>" & text4 & "</resource></bind></iq>")
                            ReadUntil(streamReader, "bind")
                            Dim t = Date.UtcNow.AddSeconds(listenSeconds)
                            Dim stringBuilder As StringBuilder = New StringBuilder()
                            While Date.Compare(Date.UtcNow, t) < 0 AndAlso sslStream.CanRead
                                If networkStream.DataAvailable Then
                                    Dim text5 As String = streamReader.ReadLine()
                                    If Equals(text5, Nothing) Then
                                        Exit While
                                    End If
                                    stringBuilder.AppendLine(text5)
                                    For Each item As Match In regex.Matches(text5)
                                        Dim text6 = DecodeXmlText(item.Groups(1).Value)
                                        If text6.Length > 0 Then
                                            list.Add(text6)
                                        End If
                                    Next
                                Else
                                    Thread.Sleep(200)
                                End If
                            End While
                            streamWriter2.WriteLine("</stream:stream>")
                        End Using
                        result = (If(list.Count <> 0, String.Join(vbCrLf, list.ToArray()), ("(NO MESSAGES IN " & listenSeconds.ToString() & "S)")))
                    Catch ex As Exception
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = ex2.Message
                        result = ""
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Private Shared Sub WaitForFeatures(reader As StreamReader)
            Dim t = Date.UtcNow.AddSeconds(8.0)
            While Date.Compare(Date.UtcNow, t) < 0
                Dim text As String = reader.ReadLine()
                If Equals(text, Nothing) OrElse text.IndexOf("<stream:features", StringComparison.OrdinalIgnoreCase) >= 0 OrElse text.IndexOf("<features", StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Exit While
                End If
            End While
        End Sub

        Private Shared Sub ReadUntil(reader As StreamReader, token As String)
            Dim t = Date.UtcNow.AddSeconds(8.0)
            While Date.Compare(Date.UtcNow, t) < 0
                Dim text As String = reader.ReadLine()
                If Equals(text, Nothing) Then
                    Exit While
                End If
                If text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    Return
                End If
            End While
            Throw New IOException("XMPP stream did not reach " & token & ".")
        End Sub

        Private Shared Function XmlEscape(raw As String) As String
            Return SecurityElement.Escape(If(Not Equals(raw, Nothing), raw, ""))
        End Function

        Public Shared Function TryFetchRosterJids(host As String, port As Integer, jid As String, password As String) As List(Of String)
            LastError = Nothing
            Dim list As List(Of String) = New List(Of String)()
            Dim text As String = If(Not Equals(host, Nothing), host, "").Trim()
            Dim text2 As String = If(Not Equals(jid, Nothing), jid, "").Trim()
            Dim result As List(Of String)
            If text.Length = 0 OrElse text2.Length = 0 Then
                LastError = "Host and JID are required."
                result = list
            Else
                If port <= 0 Then
                    port = 5222
                End If
                If Not text2.Contains("@") Then
                    LastError = "JID must include @domain."
                    result = list
                Else
                    Dim raw = text2.Substring(text2.IndexOf("@"c) + 1)
                    Dim regex As Regex = New Regex("jid=['""]([^'""]+)['""]", RegexOptions.IgnoreCase)
                    Try
                        Using tcpClient As TcpClient = New TcpClient()
                            tcpClient.Connect(text, port)
                            Dim networkStream As NetworkStream = tcpClient.GetStream()
                            Dim sslStream As SslStream = New SslStream(networkStream, leaveInnerStreamOpen:=False, Function(a, b, c, d) True)
                            sslStream.AuthenticateAsClient(text)
                            Dim streamWriter As StreamWriter = New StreamWriter(sslStream, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
                            streamWriter.NewLine = vbLf
                            streamWriter.AutoFlush = True
                            Dim streamWriter2 = streamWriter
                            Dim streamReader As StreamReader = New StreamReader(sslStream, Encoding.UTF8)
                            streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=""" & XmlEscape(raw) & """ xmlns=""jabber:client"" xmlns:stream=""http://etherx.jabber.org/streams"" version=""1.0"">")
                            WaitForFeatures(streamReader)
                            Dim text3 = Convert.ToBase64String(Encoding.UTF8.GetBytes(vbNullChar & text2 & vbNullChar & If(Not Equals(password, Nothing), password, "")))
                            streamWriter2.WriteLine("<auth xmlns=""urn:ietf:params:xml:ns:xmpp-sasl"" mechanism=""PLAIN"">" & text3 & "</auth>")
                            ReadUntil(streamReader, "success")
                            streamWriter2.WriteLine("<?xml version='1.0'?><stream:stream to=""" & XmlEscape(raw) & """ xmlns=""jabber:client"" xmlns:stream=""http://etherx.jabber.org/streams"" version=""1.0"">")
                            WaitForFeatures(streamReader)
                            Dim text4 As String = "lcars" & Guid.NewGuid().ToString("N").Substring(0, 8)
                            streamWriter2.WriteLine("<iq type=""set"" id=""bind1""><bind xmlns=""urn:ietf:params:xml:ns:xmpp-bind""><resource>" & text4 & "</resource></bind></iq>")
                            ReadUntil(streamReader, "bind")
                            streamWriter2.WriteLine("<iq type=""get"" id=""roster1""><query xmlns=""jabber:iq:roster""/></iq>")
                            Dim t = Date.UtcNow.AddSeconds(6.0)
                            Dim stringBuilder As StringBuilder = New StringBuilder()
                            While Date.Compare(Date.UtcNow, t) < 0
                                If Not networkStream.DataAvailable Then
                                    Thread.Sleep(150)
                                    Continue While
                                End If
                                Dim text5 As String = streamReader.ReadLine()
                                If Equals(text5, Nothing) Then
                                    Exit While
                                End If
                                stringBuilder.AppendLine(text5)
                                For Each item As Match In regex.Matches(text5)
                                    Dim text6 As String = item.Groups(1).Value.Trim()
                                    If text6.Length > 0 AndAlso Not list.Contains(text6) Then
                                        list.Add(text6)
                                    End If
                                Next
                                If text5.IndexOf("</iq>", StringComparison.OrdinalIgnoreCase) < 0 OrElse list.Count <= 0 Then
                                    Continue While
                                End If
                                Exit While
                            End While
                            streamWriter2.WriteLine("</stream:stream>")
                        End Using
                        result = list
                    Catch ex As Exception
                        ProjectData.SetProjectError(ex)
                        Dim ex2 = ex
                        LastError = ex2.Message
                        result = list
                        Call ProjectData.ClearProjectError()
                    End Try
                End If
            End If
            Return result
        End Function

        Private Shared Function DecodeXmlText(raw As String) As String
            Dim text As String = If(Not Equals(raw, Nothing), raw, "").Trim()
            If text.Length = 0 Then
                Return ""
            End If
            text = text.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&").Replace("&quot;", """").Replace("&apos;", "'")
            Return text.Trim()
        End Function
    End Class

