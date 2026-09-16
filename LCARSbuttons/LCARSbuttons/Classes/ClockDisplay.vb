Option Strict On

''' <summary>
''' Clock / stardate display mode for LCARS x32 (Earth, classic TNG, or Modern).
''' </summary>
Public Enum ClockMode
    Earth = 0
    TNG = 1
    Modern = 2
End Enum

''' <summary>
''' Sub-format used when ClockMode is Modern.
''' </summary>
Public Enum ModernStardateStyle
    Fractional = 0
    TwentyFourHour = 1
End Enum

''' <summary>
''' Resolves clock mode settings and formats date/time for display and speech.
''' </summary>
Public NotInheritable Class ClockDisplay

    Private Const AppName As String = "LCARS x32"
    Private Const Section As String = "Application"
    Private Const ClockModeKey As String = "ClockMode"
    Private Const ModernStyleKey As String = "ModernStardateStyle"

    Private Sub New()
    End Sub

    ''' <summary>
    ''' Ensures ClockMode exists, migrating from legacy Stardate boolean if needed.
    ''' </summary>
    Public Shared Sub EnsureMigrated()
        Dim existing As String = GetSetting(AppName, Section, ClockModeKey, "")
        If existing <> "" Then Return

        Dim legacyStardate As Boolean = CBool(GetSetting(AppName, Section, "Stardate", "TRUE"))
        If legacyStardate Then
            SetClockMode(ClockMode.TNG)
        Else
            SetClockMode(ClockMode.Earth)
        End If
        SetModernStyle(ModernStardateStyle.Fractional)
    End Sub

    ''' <summary>
    ''' Returns the configured clock mode, migrating legacy settings on first use.
    ''' </summary>
    Public Shared Function GetClockMode() As ClockMode
        EnsureMigrated()
        Dim raw As String = GetSetting(AppName, Section, ClockModeKey, "Earth")
        Select Case raw.Trim().ToUpperInvariant()
            Case "TNG"
                Return ClockMode.TNG
            Case "MODERN"
                Return ClockMode.Modern
            Case "EARTH"
                Return ClockMode.Earth
            Case Else
                Return ClockMode.Earth
        End Select
    End Function

    ''' <summary>
    ''' Persists the clock mode.
    ''' </summary>
    Public Shared Sub SetClockMode(ByVal mode As ClockMode)
        Dim value As String
        Select Case mode
            Case ClockMode.TNG
                value = "TNG"
            Case ClockMode.Modern
                value = "Modern"
            Case Else
                value = "Earth"
        End Select
        SaveSetting(AppName, Section, ClockModeKey, value)
    End Sub

    ''' <summary>
    ''' Returns the Modern sub-format; invalid values become Fractional.
    ''' </summary>
    Public Shared Function GetModernStyle() As ModernStardateStyle
        EnsureMigrated()
        Dim raw As String = GetSetting(AppName, Section, ModernStyleKey, "Fractional")
        Select Case raw.Trim().ToUpperInvariant()
            Case "TWENTYFOURHOUR", "24H", "24HOUR"
                Return ModernStardateStyle.TwentyFourHour
            Case Else
                Return ModernStardateStyle.Fractional
        End Select
    End Function

    ''' <summary>
    ''' Persists the Modern sub-format.
    ''' </summary>
    Public Shared Sub SetModernStyle(ByVal style As ModernStardateStyle)
        Dim value As String
        If style = ModernStardateStyle.TwentyFourHour Then
            value = "TwentyFourHour"
        Else
            value = "Fractional"
        End If
        SaveSetting(AppName, Section, ModernStyleKey, value)
    End Sub

    ''' <summary>
    ''' Formats a date/time for the main clock and similar displays.
    ''' </summary>
    Public Shared Function FormatClock(ByVal value As Date) As String
        Select Case GetClockMode()
            Case ClockMode.TNG
                Return Stardate.getStardate(value).ToString("F2")
            Case ClockMode.Modern
                Return FormatModern(value, GetModernStyle())
            Case Else
                Return FormatEarth(value)
        End Select
    End Function

    ''' <summary>
    ''' Formats a date/time for Explorer properties (same modes as the clock).
    ''' </summary>
    Public Shared Function FormatDateTime(ByVal value As Date) As String
        Return FormatClock(value)
    End Function

    ''' <summary>
    ''' Text suitable for speech output for the current clock mode.
    ''' </summary>
    Public Shared Function FormatForSpeech(ByVal value As Date) As String
        Select Case GetClockMode()
            Case ClockMode.TNG
                Return Stardate.getStardate(value).ToString("F1")
            Case ClockMode.Modern
                Return FormatModern(value, GetModernStyle())
            Case Else
                Return value.ToLongDateString() & " " & value.ToLongTimeString()
        End Select
    End Function

    ''' <summary>
    ''' Builds a Modern stardate string.
    ''' </summary>
    Public Shared Function FormatModern(ByVal value As Date, ByVal style As ModernStardateStyle) As String
        Dim datePart As String = value.ToString("yyyyMMdd")
        If style = ModernStardateStyle.TwentyFourHour Then
            Return datePart & "." & value.ToString("HHmm")
        End If

        Dim fractionOfDay As Double = value.TimeOfDay.TotalSeconds / 86400.0R
        Dim fracText As String = fractionOfDay.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture)
        ' "0.8333" -> ".8333"
        If fracText.StartsWith("0") Then
            fracText = fracText.Substring(1)
        Else
            fracText = "." & fracText
        End If
        Return datePart & fracText
    End Function

    Private Shared Function FormatEarth(ByVal value As Date) As String
        Dim timeFormat As String
        Dim dateFormat As String
        Try
            Dim myReg As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser
            myReg = myReg.OpenSubKey("Control Panel\International")
            timeFormat = CStr(myReg.GetValue("sTimeFormat", "h:mm:sstt"))
            dateFormat = CStr(myReg.GetValue("sShortDate", "M/d/yyyy"))
        Catch ex As Exception
            timeFormat = "h:mm:sstt"
            dateFormat = "M/d/yyyy"
        End Try
        Return Format(value, timeFormat) & " " & Format(value.Date, dateFormat)
    End Function

End Class
