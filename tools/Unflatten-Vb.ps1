# Restores VB source that had newlines replaced with spaces.
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$src = [IO.File]::ReadAllText($Path)
$src = $src -replace '[\r\n]+', ' '
$src = $src -replace '[ \t]+', ' '
$src = $src.Trim()

$tokens = New-Object System.Collections.Generic.List[string]
$cur = New-Object System.Text.StringBuilder
$inStr = $false
for ($i = 0; $i -lt $src.Length; $i++) {
    $ch = $src[$i]
    if ($inStr) {
        [void]$cur.Append($ch)
        if ($ch -eq [char]34) {
            if (($i + 1) -lt $src.Length -and $src[$i + 1] -eq [char]34) {
                [void]$cur.Append([char]34)
                $i++
                continue
            }
            $inStr = $false
        }
        continue
    }
    if ($ch -eq [char]34) {
        [void]$cur.Append($ch)
        $inStr = $true
        continue
    }
    if ($ch -eq ' ') {
        if ($cur.Length -gt 0) {
            $tokens.Add($cur.ToString())
            [void]$cur.Clear()
        }
        continue
    }
    [void]$cur.Append($ch)
}
if ($cur.Length -gt 0) { $tokens.Add($cur.ToString()) }

$mods = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($m in @(
        'Partial', 'Public', 'Private', 'Friend', 'Protected', 'Shared', 'Shadows',
        'Overridable', 'Overrides', 'MustOverride', 'NotOverridable', 'MustInherit',
        'NotInheritable', 'ReadOnly', 'WriteOnly', 'WithEvents', 'Default', 'Overloads',
        'Narrowing', 'Widening', 'Custom', 'Async', 'Iterator')) {
    [void]$mods.Add($m)
}

$decls = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($m in @(
        'Sub', 'Function', 'Property', 'Class', 'Module', 'Structure', 'Interface',
        'Enum', 'Const', 'Dim', 'Event', 'Operator', 'Delegate', 'Get', 'Set', 'Namespace')) {
    [void]$decls.Add($m)
}

$stmtStart = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($m in @(
        'Option', 'Imports', 'Namespace', 'Return', 'Throw', 'Dim', 'Const', 'If', 'For', 'While',
        'Do', 'Select', 'Try', 'Using', 'With', 'SyncLock', 'AddHandler', 'RemoveHandler',
        'RaiseEvent', 'Inherits', 'Implements', 'Exit', 'Continue', 'GoTo', 'Stop',
        '#Region', '#End', 'Else', 'ElseIf', 'Catch', 'Finally', 'Case', 'Next', 'Loop',
        'Public', 'Private', 'Friend', 'Protected', 'Partial', 'Shared', 'End')) {
    [void]$stmtStart.Add($m)
}

$exprContinue = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($m in @(
        'Then', 'Else', 'To', 'Step', 'In', 'As', 'Is', 'IsNot', 'AndAlso', 'OrElse', 'And', 'Or', 'Xor',
        'Mod', 'Like', 'New', 'Not', 'AddressOf', 'GetType', 'TypeOf', 'From', 'Aggregate',
        '=', '+=', '-=', '&=', '+', '-', '&', ',', '(', '.', 'With')) {
    [void]$exprContinue.Add($m)
}

function Test-IsStmtStartToken {
    param([string]$Tok, [string]$Prev)
    if ([string]::IsNullOrEmpty($Tok)) { return $false }
    if ($Tok.StartsWith("'''") -or ($Tok.StartsWith("'") -and $Tok.Length -gt 1)) { return $true }
    if ($Tok -match '^\w+\.\w+' -and -not $exprContinue.Contains($Prev) -and $Prev -notmatch '[=(&+\-,]$' -and $Prev -ne '(') {
        return $true
    }
    if (-not $stmtStart.Contains($Tok)) { return $false }
    if ($mods.Contains($Prev) -and ($mods.Contains($Tok) -or $decls.Contains($Tok))) { return $false }
    return $true
}

function Find-ThenBlockEnd {
    param([int]$ThenIndex)
    # Returns index of End (of End If), or -1; also statement count inside block
    $depth = 1
    $stmts = 0
    $i = $ThenIndex + 1
    $prev = 'Then'
    $sawStmt = $false
    while ($i -lt $tokens.Count) {
        $t = $tokens[$i]
        if ($t -eq 'If' -and $prev -ne 'End') {
            # nested If — find its Then and continue; depth++
            $depth++
        }
        if ($t -eq 'End' -and ($i + 1) -lt $tokens.Count -and $tokens[$i + 1] -eq 'If') {
            $depth--
            if ($depth -eq 0) {
                return @{ EndIndex = $i; StmtCount = $stmts }
            }
            $i += 2
            $prev = 'If'
            continue
        }
        if ($t -eq 'ElseIf' -or ($t -eq 'Else' -and $depth -eq 1)) {
            if ($depth -eq 1) {
                return @{ EndIndex = $i; StmtCount = $stmts; AtElse = $true }
            }
        }
        if (Test-IsStmtStartToken $t $prev) {
            if ($t -ne 'End' -and $t -ne 'Else' -and $t -ne 'ElseIf') {
                $stmts++
            }
        }
        $prev = $t
        $i++
    }
    return @{ EndIndex = -1; StmtCount = $stmts }
}

$lines = New-Object System.Collections.Generic.List[string]
$lineToks = New-Object System.Collections.Generic.List[string]
$prev = ''
$forceBreakAfterThen = $false
$singleLineThen = $false

for ($ti = 0; $ti -lt $tokens.Count; $ti++) {
    $tok = $tokens[$ti]
    $break = $false

    if ($lineToks.Count -gt 0) {
        if ($tok.StartsWith("'''") -or ($tok.StartsWith("'") -and $tok.Length -gt 1)) {
            $break = $true
        }
        elseif ($prev -eq 'Then') {
            $thenIdx = $ti - 1
            $info = Find-ThenBlockEnd $thenIdx
            if ($info.EndIndex -ge 0 -and [int]$info.StmtCount -gt 1) {
                $break = $true
                $singleLineThen = $false
            }
            else {
                $break = $false
                $singleLineThen = $true
            }
        }
        elseif ($singleLineThen -and (Test-IsStmtStartToken $tok $prev)) {
            # Finished single-line Then statement
            $break = $true
            $singleLineThen = $false
            # Skip orphan End If
            if ($tok -eq 'End' -and ($ti + 1) -lt $tokens.Count -and $tokens[$ti + 1] -eq 'If') {
                $lines.Add([string]::Join(' ', $lineToks.ToArray()))
                $lineToks.Clear()
                $ti++ # skip If
                $prev = 'If'
                continue
            }
        }
        elseif (Test-IsStmtStartToken $tok $prev) {
            if ($mods.Contains($prev) -and ($mods.Contains($tok) -or $decls.Contains($tok))) {
                $break = $false
            }
            else {
                $break = $true
            }
        }
    }

    if ($break) {
        $lines.Add([string]::Join(' ', $lineToks.ToArray()))
        $lineToks.Clear()
    }
    $lineToks.Add($tok)
    $prev = $tok
}
if ($lineToks.Count -gt 0) {
    $lines.Add([string]::Join(' ', $lineToks.ToArray()))
}

# Merge End/For/Select/Exit/Continue with following keyword when split.
$merged = New-Object System.Collections.Generic.List[string]
for ($i = 0; $i -lt $lines.Count; $i++) {
    $a = $lines[$i]
    if ($i + 1 -lt $lines.Count) {
        $b = $lines[$i + 1]
        if ($a -eq 'End' -and $b -match '^(Sub|Function|Property|Class|Module|Structure|Interface|Enum|Namespace|If|Select|Try|Using|With|While|SyncLock|Get|Set)\b') {
            $merged.Add('End ' + $b)
            $i++
            continue
        }
        if ($a -eq '#End' -and $b -match '^Region\b') {
            $merged.Add('#End ' + $b)
            $i++
            continue
        }
        if ($a -eq 'For' -and $b -match '^Each\b') {
            $merged.Add('For ' + $b)
            $i++
            continue
        }
        if ($a -eq 'Select' -and $b -match '^Case\b') {
            $merged.Add('Select ' + $b)
            $i++
            continue
        }
        if ($a -eq 'Exit' -and $b -match '^(Sub|Function|Property|For|Do|While|Select|Try)\b') {
            $merged.Add('Exit ' + $b)
            $i++
            continue
        }
        if ($a -eq 'Continue' -and $b -match '^(For|Do|While)\b') {
            $merged.Add('Continue ' + $b)
            $i++
            continue
        }
        if ($b -eq 'End If' -and $a -match '(?i)^If\b.+\bThen\b.+\S' -and $a -notmatch '(?i)\bThen\s*$') {
            $merged.Add($a)
            $i++
            continue
        }
    }
    $merged.Add($a)
}

$text = [string]::Join([Environment]::NewLine, $merged.ToArray())
[IO.File]::WriteAllText($Path, $text + [Environment]::NewLine)
