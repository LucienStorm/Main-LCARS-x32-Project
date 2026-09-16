' Lcars Web Browser/Documents/DocumentTemplates.vb
Option Strict On
Option Explicit On

Imports System.Text

''' <summary>
''' Built-in starting documents for the unified editor.
''' RTF templates use alignment, fonts, spacing so they look intentional on open.
''' </summary>
Public Module DocumentTemplates

    Public Function BlankRtf() As String
        Return "{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\f0\fs22\par}"
    End Function

    Public Function BlankMarkdown() As String
        Return "# Untitled" & vbCrLf & vbCrLf
    End Function

    ''' <summary>Classic left-aligned business letter (Times, justified body).</summary>
    Public Function ProfessionalLetterRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Times New Roman;}{\f1 Segoe UI;}}")
        sb.Append("{\colortbl;\red0\green0\blue0;\red60\green60\blue60;}")
        sb.Append("\f0\fs24")
        sb.Append("\pard\ql\sa60 Your Name\par")
        sb.Append("\pard\ql\sa60 Street Address\par")
        sb.Append("\pard\ql\sa60 City, State ZIP\par")
        sb.Append("\pard\ql\sa200 Phone  |  Email\par")
        sb.Append("\pard\ql\sa200 ").Append(DateTime.Now.ToString("MMMM d, yyyy")).Append("\par")
        sb.Append("\pard\ql\sa60 Recipient Name\par")
        sb.Append("\pard\ql\sa60 Title / Organization\par")
        sb.Append("\pard\ql\sa200 Street Address\par")
        sb.Append("\pard\ql\sa200 Dear Recipient,\par")
        sb.Append("\pard\qj\fi720\sa200 Write the body of your letter here. Use complete paragraphs; this template starts justified with a first-line indent so the letter reads formally.\par")
        sb.Append("\pard\qj\fi720\sa200 Add a second paragraph for the closing thought or call to action.\par")
        sb.Append("\pard\ql\sa60 Sincerely,\par")
        sb.Append("\pard\ql\sa60\par\par\par")
        sb.Append("\pard\ql Your Name\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    ''' <summary>Centered letterhead + left body (formal / institutional).</summary>
    Public Function FormalLetterRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Georgia;}{\f1 Times New Roman;}}")
        sb.Append("\f0\fs28")
        sb.Append("\pard\qc\b YOUR ORGANIZATION\b0\par")
        sb.Append("\f0\fs18")
        sb.Append("\pard\qc\sa40 Department or Division\par")
        sb.Append("\pard\qc\sa40 123 Main Street  ·  City, State ZIP\par")
        sb.Append("\pard\qc\sa200 phone  ·  email  ·  web\par")
        sb.Append("\pard\qc\sa200 ________________________________________\par")
        sb.Append("\f1\fs24")
        sb.Append("\pard\ql\sa200 ").Append(DateTime.Now.ToString("MMMM d, yyyy")).Append("\par")
        sb.Append("\pard\ql\sa60 Recipient Name\par")
        sb.Append("\pard\ql\sa60 Organization\par")
        sb.Append("\pard\ql\sa200 Address\par")
        sb.Append("\pard\ql\sa200 Dear Recipient,\par")
        sb.Append("\pard\qj\sa200 Body of the formal letter. Keep tone courteous and direct.\par")
        sb.Append("\pard\ql\sa60 Respectfully,\par")
        sb.Append("\pard\ql\sa60\par\par\par")
        sb.Append("\pard\ql\b Name\b0\par")
        sb.Append("\pard\ql Title\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    ''' <summary>Warm personal letter, slightly larger Georgia, left-aligned.</summary>
    Public Function PersonalLetterRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Georgia;}}")
        sb.Append("\f0\fs26")
        sb.Append("\pard\qr\sa200 ").Append(DateTime.Now.ToString("MMMM d, yyyy")).Append("\par")
        sb.Append("\pard\ql\sa200 Dear Friend,\par")
        sb.Append("\pard\ql\sa200 I hope this finds you well.\par")
        sb.Append("\pard\ql\sa200 Write your news and thoughts here.\par")
        sb.Append("\pard\ql\sa60 Warmly,\par")
        sb.Append("\pard\ql\sa60\par\par")
        sb.Append("\pard\ql Your Name\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function MemoRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Segoe UI;}{\f1 Consolas;}}")
        sb.Append("\f0\fs22")
        sb.Append("\pard\qc\b\fs32 MEMORANDUM\b0\fs22\par")
        sb.Append("\pard\qc\sa200 ________________________________________\par")
        sb.Append("\pard\ql\sa80\b TO:\b0\tab\par")
        sb.Append("\pard\ql\sa80\b FROM:\b0\tab\par")
        sb.Append("\pard\ql\sa80\b DATE:\b0\tab ").Append(DateTime.Now.ToString("MMMM d, yyyy")).Append("\par")
        sb.Append("\pard\ql\sa200\b SUBJECT:\b0\tab\par")
        sb.Append("\pard\ql\sa200 ________________________________________\par")
        sb.Append("\pard\qj\sa200 Memo body. State purpose in the first sentence, then supporting detail.\par")
        sb.Append("\pard\ql\sa60\b Action requested:\b0\par")
        sb.Append("\pard\ql · \par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function MeetingAgendaRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Segoe UI;}}")
        sb.Append("\f0\fs22")
        sb.Append("\pard\qc\b\fs28 MEETING AGENDA\b0\fs22\par")
        sb.Append("\pard\qc\sa40 ").Append(DateTime.Now.ToString("dddd, MMMM d, yyyy")).Append("\par")
        sb.Append("\pard\qc\sa200 Time  ·  Location / Call link\par")
        sb.Append("\pard\ql\sa80\b Facilitator:\b0\par")
        sb.Append("\pard\ql\sa200\b Attendees:\b0\par")
        sb.Append("\pard\ql\sa80\b 1. Opening / roll call\b0\tab (5 min)\par")
        sb.Append("\pard\ql\sa80\b 2. Review prior actions\b0\tab (10 min)\par")
        sb.Append("\pard\ql\sa80\b 3. Main topic\b0\tab (20 min)\par")
        sb.Append("\pard\ql\sa80\b 4. Decisions\b0\tab (10 min)\par")
        sb.Append("\pard\ql\sa200\b 5. Close / next meeting\b0\tab (5 min)\par")
        sb.Append("\pard\ql\b Notes:\b0\par")
        sb.Append("\pard\ql\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function MeetingNotesMarkdown() As String
        Dim sb As New StringBuilder()
        sb.AppendLine("# Meeting Notes")
        sb.AppendLine()
        sb.AppendLine("**Date:** " & DateTime.Now.ToString("yyyy-MM-dd"))
        sb.AppendLine("**Attendees:**")
        sb.AppendLine()
        sb.AppendLine("## Agenda")
        sb.AppendLine()
        sb.AppendLine("1. ")
        sb.AppendLine()
        sb.AppendLine("## Discussion")
        sb.AppendLine()
        sb.AppendLine("- ")
        sb.AppendLine()
        sb.AppendLine("## Decisions")
        sb.AppendLine()
        sb.AppendLine("- ")
        sb.AppendLine()
        sb.AppendLine("## Action Items")
        sb.AppendLine()
        sb.AppendLine("- [ ] Owner — task — due")
        sb.AppendLine()
        Return sb.ToString()
    End Function

    Public Function ReportRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Calibri;}{\f1 Georgia;}}")
        sb.Append("\f1\fs36")
        sb.Append("\pard\qc\sa120\b Report Title\b0\par")
        sb.Append("\f0\fs22")
        sb.Append("\pard\qc\sa40 Subtitle or project name\par")
        sb.Append("\pard\qc\sa40 Prepared by: Your Name\par")
        sb.Append("\pard\qc\sa400 ").Append(DateTime.Now.ToString("MMMM d, yyyy")).Append("\par")
        sb.Append("\pard\ql\sa120\b\fs26 1. Summary\b0\fs22\par")
        sb.Append("\pard\qj\sa200 One-paragraph executive summary.\par")
        sb.Append("\pard\ql\sa120\b\fs26 2. Background\b0\fs22\par")
        sb.Append("\pard\qj\sa200 Context and scope.\par")
        sb.Append("\pard\ql\sa120\b\fs26 3. Findings\b0\fs22\par")
        sb.Append("\pard\qj\sa200 Key findings and evidence.\par")
        sb.Append("\pard\ql\sa120\b\fs26 4. Recommendations\b0\fs22\par")
        sb.Append("\pard\ql\sa80 · Recommendation one\par")
        sb.Append("\pard\ql\sa200 · Recommendation two\par")
        sb.Append("\pard\ql\sa120\b\fs26 5. Next steps\b0\fs22\par")
        sb.Append("\pard\qj Next actions and owners.\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function ProposalRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Segoe UI;}}")
        sb.Append("\f0\fs22")
        sb.Append("\pard\ql\sa40\b\fs28 PROJECT PROPOSAL\b0\fs22\par")
        sb.Append("\pard\ql\sa200 ").Append(DateTime.Now.ToString("MMMM d, yyyy")).Append("\par")
        sb.Append("\pard\ql\sa80\b Project:\b0\par")
        sb.Append("\pard\ql\sa80\b Sponsor:\b0\par")
        sb.Append("\pard\ql\sa200\b Author:\b0\par")
        sb.Append("\pard\ql\sa120\b\fs24 Objective\b0\fs22\par")
        sb.Append("\pard\qj\sa200 What success looks like.\par")
        sb.Append("\pard\ql\sa120\b\fs24 Scope\b0\fs22\par")
        sb.Append("\pard\ql\sa80 In scope:\par")
        sb.Append("\pard\ql\sa80 · \par")
        sb.Append("\pard\ql\sa200 Out of scope:\par")
        sb.Append("\pard\ql\sa120\b\fs24 Timeline\b0\fs22\par")
        sb.Append("\pard\ql\sa80 Phase 1 — \par")
        sb.Append("\pard\ql\sa200 Phase 2 — \par")
        sb.Append("\pard\ql\sa120\b\fs24 Budget / resources\b0\fs22\par")
        sb.Append("\pard\qj\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function ResumeRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Calibri;}{\f1 Segoe UI;}}")
        sb.Append("\f0\fs22")
        sb.Append("\pard\qc\b\fs32 YOUR NAME\b0\fs22\par")
        sb.Append("\pard\qc\sa200 City, State  ·  phone  ·  email  ·  portfolio\par")
        sb.Append("\pard\ql\sa80\b\fs24 SUMMARY\b0\fs22\par")
        sb.Append("\pard\qc\sa40 ________________________________________\par")
        sb.Append("\pard\qj\sa200 Two sentences on strengths and target role.\par")
        sb.Append("\pard\ql\sa80\b\fs24 EXPERIENCE\b0\fs22\par")
        sb.Append("\pard\qc\sa40 ________________________________________\par")
        sb.Append("\pard\ql\sa40\b Job Title\b0  —  Company\par")
        sb.Append("\pard\ql\sa80\i Dates\i0\par")
        sb.Append("\pard\ql\sa40 · Achievement with impact\par")
        sb.Append("\pard\ql\sa200 · Achievement with impact\par")
        sb.Append("\pard\ql\sa80\b\fs24 EDUCATION\b0\fs22\par")
        sb.Append("\pard\qc\sa40 ________________________________________\par")
        sb.Append("\pard\ql Degree, School — Year\par")
        sb.Append("\pard\ql\sa200\b\fs24 SKILLS\b0\fs22\par")
        sb.Append("\pard\qc\sa40 ________________________________________\par")
        sb.Append("\pard\ql Skill group: item, item, item\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function InvoiceRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Segoe UI;}{\f1 Consolas;}}")
        sb.Append("\f0\fs22")
        sb.Append("\pard\ql\b\fs28 INVOICE\b0\fs22\par")
        sb.Append("\pard\qr\sa40 Invoice #: \par")
        sb.Append("\pard\qr\sa200 Date: ").Append(DateTime.Now.ToString("yyyy-MM-dd")).Append("\par")
        sb.Append("\pard\ql\sa40\b From\b0\par")
        sb.Append("\pard\ql\sa200 Your Name / Business\par")
        sb.Append("\pard\ql\sa40\b Bill to\b0\par")
        sb.Append("\pard\ql\sa200 Client Name\par")
        sb.Append("\pard\ql\sa80\b Description\b0\tab\tab\b Qty\b0\tab\b Amount\b0\par")
        sb.Append("\pard\ql\sa40 ________________________________________\par")
        sb.Append("\pard\ql\sa40 Service or item\tab\tab 1\tab 0.00\par")
        sb.Append("\pard\ql\sa200 ________________________________________\par")
        sb.Append("\pard\qr\sa40 Subtotal:\tab 0.00\par")
        sb.Append("\pard\qr\sa40 Tax:\tab 0.00\par")
        sb.Append("\pard\qr\sa200\b Total due:\b0\tab 0.00\par")
        sb.Append("\pard\ql Payment terms: Due upon receipt\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function CoverPageRtf() As String
        Dim sb As New StringBuilder()
        sb.Append("{\rtf1\ansi\deff0")
        sb.Append("{\fonttbl{\f0 Georgia;}{\f1 Segoe UI;}}")
        sb.Append("\f0\fs22")
        sb.Append("\pard\qc\par\par\par\par")
        sb.Append("\pard\qc\b\fs40 Document Title\b0\par")
        sb.Append("\pard\qc\sa200\fs24 Subtitle\fs22\par")
        sb.Append("\pard\qc\sa400 ________________________________________\par")
        sb.Append("\f1\fs22")
        sb.Append("\pard\qc\sa80 Author Name\par")
        sb.Append("\pard\qc\sa80 Organization\par")
        sb.Append("\pard\qc ").Append(DateTime.Now.ToString("MMMM yyyy")).Append("\par")
        sb.Append("}")
        Return sb.ToString()
    End Function

    Public Function ToDoListMarkdown() As String
        Dim sb As New StringBuilder()
        sb.AppendLine("# To-Do")
        sb.AppendLine()
        sb.AppendLine("**" & DateTime.Now.ToString("dddd, MMMM d") & "**")
        sb.AppendLine()
        sb.AppendLine("## Today")
        sb.AppendLine()
        sb.AppendLine("- [ ] ")
        sb.AppendLine("- [ ] ")
        sb.AppendLine()
        sb.AppendLine("## Later")
        sb.AppendLine()
        sb.AppendLine("- [ ] ")
        sb.AppendLine()
        sb.AppendLine("## Done")
        sb.AppendLine()
        sb.AppendLine("- [x] ")
        sb.AppendLine()
        Return sb.ToString()
    End Function
End Module
