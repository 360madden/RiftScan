# version: 0.1.0
# purpose: AutoHotkey-backed RIFT slash-command sender for live validation scaffolding only; not scanner core.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$CommandText,

    [string]$TargetProcessName = "rift_x64",

    [int]$TargetProcessId,

    [string]$TargetWindowHandle,

    [string]$TargetTitleContains = "RIFT",

    [switch]$Focus,

    [switch]$DryRun,

    [switch]$SendEscapeBeforeCommand,

    [switch]$OpenChatBeforeCommand,

    [switch]$NoEnter,

    [ValidateSet("sendtext", "sendevent")]
    [string]$TextMode = "sendtext",

    [int]$FocusDelayMilliseconds = 350,

    [int]$InterCharacterDelayMilliseconds = 8,

    [int]$PostSendDelayMilliseconds = 150
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $CommandText.StartsWith("/", [System.StringComparison]::Ordinal)) {
    throw "CommandText must be a slash command that starts with '/'. Refusing to send arbitrary chat text."
}

function ConvertTo-WindowHandleValue {
    param([string]$HandleText)

    if ([string]::IsNullOrWhiteSpace($HandleText)) {
        return 0L
    }

    if ($HandleText.StartsWith("0x", [System.StringComparison]::OrdinalIgnoreCase)) {
        return [Int64][UInt64]::Parse($HandleText.Substring(2), [System.Globalization.NumberStyles]::AllowHexSpecifier, [System.Globalization.CultureInfo]::InvariantCulture)
    }

    return [Int64]::Parse($HandleText, [System.Globalization.CultureInfo]::InvariantCulture)
}

function Format-WindowHandle {
    param([Int64]$Handle)

    if ($Handle -eq 0) {
        return "0x0"
    }

    return ("0x{0:X}" -f $Handle)
}

function Find-AutoHotkeyExe {
    $candidates = @(
        "C:\Users\mrkoo\AppData\Local\Programs\AutoHotkey\v2\AutoHotkey64.exe",
        "C:\Users\mrkoo\AppData\Local\Programs\AutoHotkey\v2\AutoHotkey32.exe",
        "C:\Program Files\AutoHotkey\v2\AutoHotkey64.exe",
        "C:\Program Files\AutoHotkey\AutoHotkey64.exe",
        "C:\Program Files\AutoHotkey\AutoHotkey32.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    $command = Get-Command AutoHotkey64.exe, AutoHotkey32.exe -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) {
        return $command.Source
    }

    throw "AutoHotkey v2 executable was not found."
}

function Resolve-TargetProcess {
    param(
        [string]$ProcessName,
        [int]$ProcessId
    )

    $expectedName = [System.IO.Path]::GetFileNameWithoutExtension($ProcessName)
    if ($ProcessId -gt 0) {
        $process = Get-Process -Id $ProcessId -ErrorAction Stop
        if (-not [string]::Equals($process.ProcessName, $expectedName, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Requested PID $ProcessId is '$($process.ProcessName)', not '$expectedName'."
        }

        return $process
    }

    $candidates = @(Get-Process -Name $expectedName -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 })
    if ($candidates.Count -eq 0) {
        throw "No process named '$expectedName' with a main window was found."
    }

    if ($candidates.Count -gt 1) {
        $ids = ($candidates | Sort-Object Id | ForEach-Object { $_.Id }) -join ", "
        throw "Process name '$expectedName' matched multiple windowed processes ($ids). Use -TargetProcessId or -TargetWindowHandle."
    }

    return $candidates[0]
}

function Quote-ProcessArgument {
    param([AllowEmptyString()][string]$Value)

    return '"' + ($Value -replace '"', '\"') + '"'
}

$targetProcess = Resolve-TargetProcess -ProcessName $TargetProcessName -ProcessId $TargetProcessId
$targetHandle = ConvertTo-WindowHandleValue -HandleText $TargetWindowHandle
if ($targetHandle -eq 0) {
    $targetHandle = [Int64]$targetProcess.MainWindowHandle
}

if ($targetHandle -eq 0) {
    throw "Target process '$($targetProcess.ProcessName)' [$($targetProcess.Id)] does not expose a main window handle."
}

$autoHotkeyExe = Find-AutoHotkeyExe
$tempScript = Join-Path $env:TEMP ("riftscan-send-rift-slash-" + [Guid]::NewGuid().ToString("N") + ".ahk")
$ahkSource = @'
#Requires AutoHotkey v2.0
#SingleInstance Force

JsonEscape(value) {
    value := StrReplace(value, "\", "\\")
    value := StrReplace(value, '"', '\"')
    value := StrReplace(value, "`r", "\r")
    value := StrReplace(value, "`n", "\n")
    value := StrReplace(value, "`t", "\t")
    return value
}

JsonBool(value) {
    return value ? "true" : "false"
}

GetWindowInfo(hwnd) {
    if (!hwnd) {
        return Map("hwnd", "0x0", "pid", 0, "title", "")
    }

    pid := 0
    title := ""
    try pid := WinGetPID("ahk_id " hwnd)
    try title := WinGetTitle("ahk_id " hwnd)
    return Map("hwnd", Format("0x{:X}", hwnd), "pid", pid, "title", title)
}

WriteResult(success, exitCode, failure, commandText, dryRun, enterSent, openChatBeforeCommand, textMode, targetHwnd, targetPid, targetTitle, beforeInfo, afterFocusInfo, afterSendInfo) {
    json := "{"
    json .= '"success":' JsonBool(success) ","
    json .= '"exit_code":' exitCode ","
    json .= '"failure":"' JsonEscape(failure) '",'
    json .= '"backend":"autohotkey-sendtext",'
    json .= '"dry_run":' JsonBool(dryRun) ","
    json .= '"command_text":"' JsonEscape(commandText) '",'
    json .= '"enter_sent":' JsonBool(enterSent) ","
    json .= '"open_chat_before_command":' JsonBool(openChatBeforeCommand) ","
    json .= '"text_mode":"' JsonEscape(textMode) '",'
    json .= '"target_process_id":' targetPid ","
    json .= '"target_window_handle":"' Format("0x{:X}", targetHwnd) '",'
    json .= '"target_window_title":"' JsonEscape(targetTitle) '",'
    json .= '"foreground_before_process_id":' beforeInfo["pid"] ","
    json .= '"foreground_before_window_handle":"' beforeInfo["hwnd"] '",'
    json .= '"foreground_before_window_title":"' JsonEscape(beforeInfo["title"]) '",'
    json .= '"foreground_after_focus_process_id":' afterFocusInfo["pid"] ","
    json .= '"foreground_after_focus_window_handle":"' afterFocusInfo["hwnd"] '",'
    json .= '"foreground_after_focus_window_title":"' JsonEscape(afterFocusInfo["title"]) '",'
    json .= '"foreground_after_send_process_id":' afterSendInfo["pid"] ","
    json .= '"foreground_after_send_window_handle":"' afterSendInfo["hwnd"] '",'
    json .= '"foreground_after_send_window_title":"' JsonEscape(afterSendInfo["title"]) '"'
    json .= "}"
    FileAppend(json, "*")
    ExitApp(exitCode)
}

if (A_Args.Length < 12) {
    FileAppend('{"success":false,"exit_code":9,"failure":"missing_arguments"}', "*")
    ExitApp(9)
}

commandText := A_Args[1]
targetPid := Integer(A_Args[2])
targetHwnd := Integer(A_Args[3])
titleContains := A_Args[4]
focusRequested := A_Args[5] = "true"
dryRun := A_Args[6] = "true"
sendEscape := A_Args[7] = "true"
openChatBeforeCommand := A_Args[8] = "true"
noEnter := A_Args[9] = "true"
textMode := A_Args[10]
focusDelay := Integer(A_Args[11])
postSendDelay := Integer(A_Args[12])
enterSent := !noEnter

beforeInfo := GetWindowInfo(WinExist("A"))
emptyInfo := GetWindowInfo(0)

if (!WinExist("ahk_id " targetHwnd)) {
    WriteResult(false, 2, "target_window_not_found", commandText, dryRun, enterSent, openChatBeforeCommand, textMode, targetHwnd, targetPid, "", beforeInfo, emptyInfo, emptyInfo)
}

actualPid := WinGetPID("ahk_id " targetHwnd)
targetTitle := WinGetTitle("ahk_id " targetHwnd)
if (actualPid != targetPid) {
    WriteResult(false, 3, "target_pid_mismatch", commandText, dryRun, enterSent, openChatBeforeCommand, textMode, targetHwnd, targetPid, targetTitle, beforeInfo, emptyInfo, emptyInfo)
}

if (titleContains != "" && !InStr(targetTitle, titleContains)) {
    WriteResult(false, 4, "target_title_mismatch", commandText, dryRun, enterSent, openChatBeforeCommand, textMode, targetHwnd, targetPid, targetTitle, beforeInfo, emptyInfo, emptyInfo)
}

if (focusRequested) {
    try WinRestore("ahk_id " targetHwnd)
    try WinActivate("ahk_id " targetHwnd)
    if (focusDelay > 0) {
        Sleep(focusDelay)
    }
}

afterFocusInfo := GetWindowInfo(WinExist("A"))
if (!dryRun && afterFocusInfo["pid"] != targetPid) {
    WriteResult(false, 5, "target_not_foreground_after_focus", commandText, dryRun, enterSent, openChatBeforeCommand, textMode, targetHwnd, targetPid, targetTitle, beforeInfo, afterFocusInfo, afterFocusInfo)
}

if (!dryRun) {
    if (sendEscape) {
        Send("{Escape}")
        Sleep(25)
    }

    if (openChatBeforeCommand) {
        Send("{Enter}")
        Sleep(50)
    }

    if (textMode = "sendevent") {
        SendEvent(commandText)
    } else {
        SendText(commandText)
    }
    Sleep(25)
    if (!noEnter) {
        Send("{Enter}")
    }

    if (postSendDelay > 0) {
        Sleep(postSendDelay)
    }
}

afterSendInfo := GetWindowInfo(WinExist("A"))
WriteResult(true, 0, "", commandText, dryRun, enterSent, openChatBeforeCommand, textMode, targetHwnd, targetPid, targetTitle, beforeInfo, afterFocusInfo, afterSendInfo)
'@

Set-Content -LiteralPath $tempScript -Value $ahkSource -Encoding UTF8
try {
    $argumentList = @(
        Quote-ProcessArgument -Value $tempScript
        Quote-ProcessArgument -Value $CommandText
        Quote-ProcessArgument -Value ([string]$targetProcess.Id)
        Quote-ProcessArgument -Value ([string]$targetHandle)
        Quote-ProcessArgument -Value $TargetTitleContains
        Quote-ProcessArgument -Value ($Focus.IsPresent.ToString().ToLowerInvariant())
        Quote-ProcessArgument -Value ($DryRun.IsPresent.ToString().ToLowerInvariant())
        Quote-ProcessArgument -Value ($SendEscapeBeforeCommand.IsPresent.ToString().ToLowerInvariant())
        Quote-ProcessArgument -Value ($OpenChatBeforeCommand.IsPresent.ToString().ToLowerInvariant())
        Quote-ProcessArgument -Value ($NoEnter.IsPresent.ToString().ToLowerInvariant())
        Quote-ProcessArgument -Value $TextMode
        Quote-ProcessArgument -Value ([string]$FocusDelayMilliseconds)
        Quote-ProcessArgument -Value ([string]$PostSendDelayMilliseconds)
    ) -join " "

    $processStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $processStartInfo.FileName = $autoHotkeyExe
    $processStartInfo.Arguments = $argumentList
    $processStartInfo.UseShellExecute = $false
    $processStartInfo.RedirectStandardOutput = $true
    $processStartInfo.RedirectStandardError = $true
    $process = [System.Diagnostics.Process]::Start($processStartInfo)
    $stdout = $process.StandardOutput.ReadToEnd()
    $stderr = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
        Write-Output $stdout
    }

    if (-not [string]::IsNullOrWhiteSpace($stderr)) {
        Write-Error $stderr
    }

    exit $process.ExitCode
}
finally {
    Remove-Item -LiteralPath $tempScript -Force -ErrorAction SilentlyContinue
}

# END_OF_SCRIPT
