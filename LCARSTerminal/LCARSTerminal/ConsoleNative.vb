' LCARSTerminal/ConsoleNative.vb
Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Win32 console + window interop for hosting a real conhost window inside LCARS chrome.
''' Unlike the ConPTY/redirected paths this gives the shell a genuine console, so line
''' editing, history and tab completion are handled by the shell itself.
''' </summary>
Friend Module ConsoleNative

    Public Const CREATE_NEW_CONSOLE As UInteger = &H10UI
    Public Const CREATE_UNICODE_ENVIRONMENT As UInteger = &H400UI
    Public Const STARTF_USESHOWWINDOW As Integer = &H1
    Public Const SW_HIDE As Short = 0
    Public Const SW_SHOWNA As Integer = 8

    Public Const GWL_STYLE As Integer = -16
    Public Const GWL_EXSTYLE As Integer = -20
    Public Const WS_CHILD As Integer = &H40000000
    Public Const WS_VISIBLE As Integer = &H10000000
    Public Const WS_CLIPSIBLINGS As Integer = &H4000000
    Public Const WS_VSCROLL As Integer = &H200000
    Public Const WS_HSCROLL As Integer = &H100000

    Public Const SWP_NOZORDER As Integer = &H4
    Public Const SWP_NOACTIVATE As Integer = &H10
    Public Const SWP_FRAMECHANGED As Integer = &H20

    Public Const GENERIC_READ As UInteger = &H80000000UI
    Public Const GENERIC_WRITE As UInteger = &H40000000UI
    Public Const FILE_SHARE_READ As UInteger = &H1UI
    Public Const FILE_SHARE_WRITE As UInteger = &H2UI
    Public Const OPEN_EXISTING As UInteger = 3UI
    Public ReadOnly INVALID_HANDLE_VALUE As New IntPtr(-1)

    Public Const KEY_EVENT As Short = &H1S

    <StructLayout(LayoutKind.Sequential)>
    Public Structure COORD
        Public X As Short
        Public Y As Short
        Public Sub New(ByVal xVal As Short, ByVal yVal As Short)
            X = xVal
            Y = yVal
        End Sub
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure SMALL_RECT
        Public Left As Short
        Public Top As Short
        Public Right As Short
        Public Bottom As Short
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Public Structure STARTUPINFO
        Public cb As Integer
        Public lpReserved As String
        Public lpDesktop As String
        Public lpTitle As String
        Public dwX As Integer
        Public dwY As Integer
        Public dwXSize As Integer
        Public dwYSize As Integer
        Public dwXCountChars As Integer
        Public dwYCountChars As Integer
        Public dwFillAttribute As Integer
        Public dwFlags As Integer
        Public wShowWindow As Short
        Public cbReserved2 As Short
        Public lpReserved2 As IntPtr
        Public hStdInput As IntPtr
        Public hStdOutput As IntPtr
        Public hStdError As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure PROCESS_INFORMATION
        Public hProcess As IntPtr
        Public hThread As IntPtr
        Public dwProcessId As Integer
        Public dwThreadId As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Public Structure KEY_EVENT_RECORD
        Public bKeyDown As Integer
        Public wRepeatCount As Short
        Public wVirtualKeyCode As Short
        Public wVirtualScanCode As Short
        Public UnicodeChar As Char
        Public dwControlKeyState As Integer
    End Structure

    ''' <summary>
    ''' INPUT_RECORD's event union starts at offset 4 (WORD EventType plus padding).
    ''' </summary>
    <StructLayout(LayoutKind.Explicit, CharSet:=CharSet.Unicode)>
    Public Structure INPUT_RECORD
        <FieldOffset(0)> Public EventType As Short
        <FieldOffset(4)> Public KeyEvent As KEY_EVENT_RECORD
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure CONSOLE_SCREEN_BUFFER_INFOEX
        Public cbSize As Integer
        Public dwSize As COORD
        Public dwCursorPosition As COORD
        Public wAttributes As Short
        Public srWindow As SMALL_RECT
        Public dwMaximumWindowSize As COORD
        Public wPopupAttributes As Short
        Public bFullscreenSupported As Integer
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=16)>
        Public ColorTable As UInteger()
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Public Structure CONSOLE_FONT_INFOEX
        Public cbSize As Integer
        Public nFont As Integer
        Public dwFontSize As COORD
        Public FontFamily As Integer
        Public FontWeight As Integer
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=32)>
        Public FaceName As String
    End Structure

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode, EntryPoint:="CreateProcessW")>
    Public Function CreateProcess(ByVal lpApplicationName As String, ByVal lpCommandLine As String, ByVal lpProcessAttributes As IntPtr, ByVal lpThreadAttributes As IntPtr, ByVal bInheritHandles As Boolean, ByVal dwCreationFlags As UInteger, ByVal lpEnvironment As IntPtr, ByVal lpCurrentDirectory As String, ByRef lpStartupInfo As STARTUPINFO, ByRef lpProcessInformation As PROCESS_INFORMATION) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function CloseHandle(ByVal hObject As IntPtr) As Boolean
    End Function

    Public Const JobObjectExtendedLimitInformation As Integer = 9
    Public Const JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE As Integer = &H2000

    ' SIZE_T / ULONG_PTR fields use IntPtr so the layout stays correct under x86.
    <StructLayout(LayoutKind.Sequential)>
    Public Structure JOBOBJECT_BASIC_LIMIT_INFORMATION
        Public PerProcessUserTimeLimit As Long
        Public PerJobUserTimeLimit As Long
        Public LimitFlags As Integer
        Public MinimumWorkingSetSize As IntPtr
        Public MaximumWorkingSetSize As IntPtr
        Public ActiveProcessLimit As Integer
        Public Affinity As IntPtr
        Public PriorityClass As Integer
        Public SchedulingClass As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure IO_COUNTERS
        Public ReadOperationCount As ULong
        Public WriteOperationCount As ULong
        Public OtherOperationCount As ULong
        Public ReadTransferCount As ULong
        Public WriteTransferCount As ULong
        Public OtherTransferCount As ULong
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        Public BasicLimitInformation As JOBOBJECT_BASIC_LIMIT_INFORMATION
        Public IoInfo As IO_COUNTERS
        Public ProcessMemoryLimit As IntPtr
        Public JobMemoryLimit As IntPtr
        Public PeakProcessMemoryUsed As IntPtr
        Public PeakJobMemoryUsed As IntPtr
    End Structure

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode, EntryPoint:="CreateJobObjectW")>
    Public Function CreateJobObject(ByVal lpJobAttributes As IntPtr, ByVal lpName As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function SetInformationJobObject(ByVal hJob As IntPtr, ByVal infoClass As Integer, ByRef lpJobObjectInfo As JOBOBJECT_EXTENDED_LIMIT_INFORMATION, ByVal cbJobObjectInfoLength As Integer) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function AssignProcessToJobObject(ByVal hJob As IntPtr, ByVal hProcess As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function AttachConsole(ByVal dwProcessId As UInteger) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function FreeConsole() As Boolean
    End Function

    <DllImport("kernel32.dll")>
    Public Function GetConsoleWindow() As IntPtr
    End Function

    ''' <summary>Passing a null handler with add=True makes this process ignore Ctrl+C while attached.</summary>
    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function SetConsoleCtrlHandler(ByVal handlerRoutine As IntPtr, ByVal fAdd As Boolean) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode, EntryPoint:="CreateFileW")>
    Public Function CreateFile(ByVal lpFileName As String, ByVal dwDesiredAccess As UInteger, ByVal dwShareMode As UInteger, ByVal lpSecurityAttributes As IntPtr, ByVal dwCreationDisposition As UInteger, ByVal dwFlagsAndAttributes As UInteger, ByVal hTemplateFile As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, EntryPoint:="WriteConsoleInputW")>
    Public Function WriteConsoleInput(ByVal hConsoleInput As IntPtr, <[In]()> ByVal lpBuffer As INPUT_RECORD(), ByVal nLength As UInteger, ByRef lpNumberOfEventsWritten As UInteger) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function GetConsoleScreenBufferInfoEx(ByVal hConsoleOutput As IntPtr, ByRef info As CONSOLE_SCREEN_BUFFER_INFOEX) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function SetConsoleScreenBufferInfoEx(ByVal hConsoleOutput As IntPtr, ByRef info As CONSOLE_SCREEN_BUFFER_INFOEX) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function SetConsoleScreenBufferSize(ByVal hConsoleOutput As IntPtr, ByVal dwSize As COORD) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function SetConsoleWindowInfo(ByVal hConsoleOutput As IntPtr, ByVal bAbsolute As Boolean, ByRef lpConsoleWindow As SMALL_RECT) As Boolean
    End Function

    ' Console font APIs have no A/W variants — ExactSpelling stops the marshaler appending W.
    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode, ExactSpelling:=True)>
    Public Function GetCurrentConsoleFontEx(ByVal hConsoleOutput As IntPtr, ByVal bMaximumWindow As Boolean, ByRef info As CONSOLE_FONT_INFOEX) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode, ExactSpelling:=True)>
    Public Function SetCurrentConsoleFontEx(ByVal hConsoleOutput As IntPtr, ByVal bMaximumWindow As Boolean, ByRef info As CONSOLE_FONT_INFOEX) As Boolean
    End Function

    <DllImport("kernel32.dll")>
    Public Function GetCurrentThreadId() As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function SetParent(ByVal hWndChild As IntPtr, ByVal hWndNewParent As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function GetWindowLong(ByVal hWnd As IntPtr, ByVal nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function SetWindowLong(ByVal hWnd As IntPtr, ByVal nIndex As Integer, ByVal dwNewLong As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function SetWindowPos(ByVal hWnd As IntPtr, ByVal hWndInsertAfter As IntPtr, ByVal x As Integer, ByVal y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal uFlags As Integer) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function MoveWindow(ByVal hWnd As IntPtr, ByVal x As Integer, ByVal y As Integer, ByVal nWidth As Integer, ByVal nHeight As Integer, ByVal bRepaint As Boolean) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Function ShowWindow(ByVal hWnd As IntPtr, ByVal nCmdShow As Integer) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Function IsWindow(ByVal hWnd As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Public Function GetParent(ByVal hWnd As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function GetWindowThreadProcessId(ByVal hWnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function AttachThreadInput(ByVal idAttach As Integer, ByVal idAttachTo As Integer, ByVal fAttach As Boolean) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Public Function SetFocus(ByVal hWnd As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Public Function VkKeyScan(ByVal ch As Char) As Short
    End Function

    <DllImport("user32.dll")>
    Public Function MapVirtualKey(ByVal uCode As Integer, ByVal uMapType As Integer) As Integer
    End Function

    Public Const TH32CS_SNAPPROCESS As UInteger = &H2UI

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Public Structure PROCESSENTRY32
        Public dwSize As Integer
        Public cntUsage As Integer
        Public th32ProcessID As Integer
        Public th32DefaultHeapID As IntPtr
        Public th32ModuleID As Integer
        Public cntThreads As Integer
        Public th32ParentProcessID As Integer
        Public pcPriClassBase As Integer
        Public dwFlags As Integer
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)>
        Public szExeFile As String
    End Structure

    <DllImport("kernel32.dll", SetLastError:=True)>
    Public Function CreateToolhelp32Snapshot(ByVal dwFlags As UInteger, ByVal th32ProcessID As UInteger) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Public Function Process32First(ByVal hSnapshot As IntPtr, ByRef lppe As PROCESSENTRY32) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Public Function Process32Next(ByVal hSnapshot As IntPtr, ByRef lppe As PROCESSENTRY32) As Boolean
    End Function

    ''' <summary>First direct child of parentPid, or 0 if none yet.</summary>
    Public Function FindFirstChildProcessId(ByVal parentPid As Integer) As Integer
        Dim snap As IntPtr = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0UI)
        If snap = INVALID_HANDLE_VALUE OrElse snap = IntPtr.Zero Then Return 0
        Try
            Dim pe As New PROCESSENTRY32()
            pe.dwSize = Marshal.SizeOf(GetType(PROCESSENTRY32))
            If Not Process32First(snap, pe) Then Return 0
            Do
                If pe.th32ParentProcessID = parentPid Then Return pe.th32ProcessID
            Loop While Process32Next(snap, pe)
            Return 0
        Finally
            CloseHandle(snap)
        End Try
    End Function
End Module
