# version: 0.1.0
# purpose: Foreground-safe RIFT slash-command sender for live validation scaffolding only; not scanner core.

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

    [switch]$NoEnter,

    [int]$FocusDelayMilliseconds = 350,

    [int]$InterCharacterDelayMilliseconds = 8,

    [int]$PostSendDelayMilliseconds = 150
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $CommandText.StartsWith("/", [System.StringComparison]::Ordinal)) {
    throw "CommandText must be a slash command that starts with '/'. Refusing to send arbitrary chat text."
}

if ($InterCharacterDelayMilliseconds -lt 0) {
    throw "InterCharacterDelayMilliseconds must be non-negative."
}

if ($FocusDelayMilliseconds -lt 0) {
    throw "FocusDelayMilliseconds must be non-negative."
}

if ($PostSendDelayMilliseconds -lt 0) {
    throw "PostSendDelayMilliseconds must be non-negative."
}

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public static class RiftSlashInputNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
"@

$SW_RESTORE = 9
$INPUT_KEYBOARD = 1
$KEYEVENTF_KEYUP = 0x0002
$KEYEVENTF_UNICODE = 0x0004
$VK_ESCAPE = 0x1B
$VK_RETURN = 0x0D

function ConvertTo-WindowHandle {
    param([string]$HandleText)

    if ([string]::IsNullOrWhiteSpace($HandleText)) {
        return [IntPtr]::Zero
    }

    if ($HandleText.StartsWith("0x", [System.StringComparison]::OrdinalIgnoreCase)) {
        $raw = [UInt64]::Parse($HandleText.Substring(2), [System.Globalization.NumberStyles]::AllowHexSpecifier, [System.Globalization.CultureInfo]::InvariantCulture)
        return [IntPtr]([Int64]$raw)
    }

    return [IntPtr]([Int64]::Parse($HandleText, [System.Globalization.CultureInfo]::InvariantCulture))
}

function Format-WindowHandle {
    param([IntPtr]$Handle)

    if ($Handle -eq [IntPtr]::Zero) {
        return "0x0"
    }

    return ("0x{0:X}" -f $Handle.ToInt64())
}

function Get-WindowTitle {
    param([IntPtr]$Handle)

    $length = [RiftSlashInputNative]::GetWindowTextLength($Handle)
    if ($length -le 0) {
        return ""
    }

    $builder = New-Object System.Text.StringBuilder ($length + 1)
    [void][RiftSlashInputNative]::GetWindowText($Handle, $builder, $builder.Capacity)
    return $builder.ToString()
}

function Get-WindowOwnerProcessId {
    param([IntPtr]$Handle)

    $ownerProcessId = [uint32]0
    [void][RiftSlashInputNative]::GetWindowThreadProcessId($Handle, [ref]$ownerProcessId)
    return [int]$ownerProcessId
}

function Resolve-TargetWindow {
    param(
        [string]$ProcessName,
        [int]$ProcessId,
        [string]$WindowHandle,
        [string]$TitleContains
    )

    $handle = ConvertTo-WindowHandle -HandleText $WindowHandle
    $process = $null

    if ($handle -ne [IntPtr]::Zero) {
        if (-not [RiftSlashInputNative]::IsWindow($handle)) {
            throw "Target window handle '$WindowHandle' is not a valid window."
        }

        $ownerProcessId = Get-WindowOwnerProcessId -Handle $handle
        if ($ProcessId -gt 0 -and $ownerProcessId -ne $ProcessId) {
            throw "Target window handle '$WindowHandle' belongs to PID $ownerProcessId, not requested PID $ProcessId."
        }

        $process = Get-Process -Id $ownerProcessId -ErrorAction Stop
    }
    elseif ($ProcessId -gt 0) {
        $process = Get-Process -Id $ProcessId -ErrorAction Stop
        $handle = [IntPtr]$process.MainWindowHandle
    }
    else {
        $expectedName = [System.IO.Path]::GetFileNameWithoutExtension($ProcessName)
        $candidates = @(Get-Process -Name $expectedName -ErrorAction Stop | Where-Object { $_.MainWindowHandle -ne 0 })
        if ($candidates.Count -eq 0) {
            throw "No process named '$expectedName' with a main window was found."
        }

        if ($candidates.Count -gt 1) {
            $ids = ($candidates | Sort-Object Id | ForEach-Object { $_.Id }) -join ", "
            throw "Process name '$expectedName' matched multiple windowed processes ($ids). Use -TargetProcessId or -TargetWindowHandle."
        }

        $process = $candidates[0]
        $handle = [IntPtr]$process.MainWindowHandle
    }

    $expectedProcessName = [System.IO.Path]::GetFileNameWithoutExtension($ProcessName)
    if (-not [string]::Equals($process.ProcessName, $expectedProcessName, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Resolved PID $($process.Id) is process '$($process.ProcessName)', not '$expectedProcessName'."
    }

    if ($handle -eq [IntPtr]::Zero) {
        throw "Resolved process '$($process.ProcessName)' [$($process.Id)] does not expose a main window handle."
    }

    if (-not [RiftSlashInputNative]::IsWindow($handle)) {
        throw "Resolved target window $(Format-WindowHandle -Handle $handle) is not valid."
    }

    $ownerProcessId = Get-WindowOwnerProcessId -Handle $handle
    if ($ownerProcessId -ne $process.Id) {
        throw "Resolved target window $(Format-WindowHandle -Handle $handle) belongs to PID $ownerProcessId, not PID $($process.Id)."
    }

    $title = Get-WindowTitle -Handle $handle
    if (-not [string]::IsNullOrWhiteSpace($TitleContains) -and
        $title.IndexOf($TitleContains, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Resolved target window title '$title' does not contain '$TitleContains'."
    }

    return [pscustomobject]@{
        Process = $process
        WindowHandle = $handle
        WindowTitle = $title
    }
}

function Get-ForegroundInfo {
    $foregroundHandle = [RiftSlashInputNative]::GetForegroundWindow()
    $foregroundProcessId = 0
    if ($foregroundHandle -ne [IntPtr]::Zero) {
        $foregroundProcessId = Get-WindowOwnerProcessId -Handle $foregroundHandle
    }

    $foregroundProcessName = ""
    if ($foregroundProcessId -gt 0) {
        $foregroundProcess = Get-Process -Id $foregroundProcessId -ErrorAction SilentlyContinue
        if ($foregroundProcess) {
            $foregroundProcessName = $foregroundProcess.ProcessName
        }
    }

    return [pscustomobject]@{
        WindowHandle = $foregroundHandle
        ProcessId = $foregroundProcessId
        ProcessName = $foregroundProcessName
        WindowTitle = if ($foregroundHandle -ne [IntPtr]::Zero) { Get-WindowTitle -Handle $foregroundHandle } else { "" }
    }
}

function Focus-TargetWindow {
    param(
        [System.Diagnostics.Process]$Process,
        [IntPtr]$WindowHandle,
        [int]$DelayMilliseconds
    )

    $foregroundHandle = [RiftSlashInputNative]::GetForegroundWindow()
    $foregroundProcessId = [uint32]0
    $foregroundThreadId = if ($foregroundHandle -ne [IntPtr]::Zero) {
        [RiftSlashInputNative]::GetWindowThreadProcessId($foregroundHandle, [ref]$foregroundProcessId)
    }
    else {
        [uint32]0
    }

    $targetProcessId = [uint32]0
    $targetThreadId = [RiftSlashInputNative]::GetWindowThreadProcessId($WindowHandle, [ref]$targetProcessId)
    $currentThreadId = [RiftSlashInputNative]::GetCurrentThreadId()

    try {
        if ($foregroundThreadId -ne 0 -and $foregroundThreadId -ne $currentThreadId) {
            [void][RiftSlashInputNative]::AttachThreadInput($currentThreadId, $foregroundThreadId, $true)
        }

        if ($targetThreadId -ne 0 -and $targetThreadId -ne $currentThreadId) {
            [void][RiftSlashInputNative]::AttachThreadInput($currentThreadId, $targetThreadId, $true)
        }

        [void][RiftSlashInputNative]::ShowWindow($WindowHandle, $SW_RESTORE)
        [void][RiftSlashInputNative]::BringWindowToTop($WindowHandle)
        [void][RiftSlashInputNative]::SetForegroundWindow($WindowHandle)
        if ($DelayMilliseconds -gt 0) {
            Start-Sleep -Milliseconds $DelayMilliseconds
        }
    }
    finally {
        if ($targetThreadId -ne 0 -and $targetThreadId -ne $currentThreadId) {
            [void][RiftSlashInputNative]::AttachThreadInput($currentThreadId, $targetThreadId, $false)
        }

        if ($foregroundThreadId -ne 0 -and $foregroundThreadId -ne $currentThreadId) {
            [void][RiftSlashInputNative]::AttachThreadInput($currentThreadId, $foregroundThreadId, $false)
        }
    }
}

function Send-VirtualKey {
    param(
        [int]$VirtualKey,
        [switch]$KeyUp
    )

    $input = New-Object RiftSlashInputNative+INPUT
    $input.type = $INPUT_KEYBOARD
    $input.U.ki.wVk = [uint16]$VirtualKey
    $input.U.ki.wScan = 0
    $input.U.ki.dwFlags = if ($KeyUp) { $KEYEVENTF_KEYUP } else { 0 }
    $input.U.ki.time = 0
    $input.U.ki.dwExtraInfo = [IntPtr]::Zero

    $inputSize = [Runtime.InteropServices.Marshal]::SizeOf([type][RiftSlashInputNative+INPUT])
    $sent = [RiftSlashInputNative]::SendInput([uint32]1, @($input), $inputSize)
    if ($sent -ne 1) {
        throw "SendInput sent $sent of 1 virtual-key inputs for key $VirtualKey."
    }
}

function Send-UnicodeCharacter {
    param(
        [char]$Character,
        [switch]$KeyUp
    )

    $input = New-Object RiftSlashInputNative+INPUT
    $input.type = $INPUT_KEYBOARD
    $input.U.ki.wVk = 0
    $input.U.ki.wScan = [uint16][char]$Character
    $input.U.ki.dwFlags = if ($KeyUp) { $KEYEVENTF_UNICODE -bor $KEYEVENTF_KEYUP } else { $KEYEVENTF_UNICODE }
    $input.U.ki.time = 0
    $input.U.ki.dwExtraInfo = [IntPtr]::Zero

    $inputSize = [Runtime.InteropServices.Marshal]::SizeOf([type][RiftSlashInputNative+INPUT])
    $sent = [RiftSlashInputNative]::SendInput([uint32]1, @($input), $inputSize)
    if ($sent -ne 1) {
        throw "SendInput sent $sent of 1 unicode inputs for character '$Character'."
    }
}

function Send-UnicodeText {
    param(
        [string]$Text,
        [int]$DelayMilliseconds
    )

    foreach ($character in $Text.ToCharArray()) {
        Send-UnicodeCharacter -Character $character
        Send-UnicodeCharacter -Character $character -KeyUp
        if ($DelayMilliseconds -gt 0) {
            Start-Sleep -Milliseconds $DelayMilliseconds
        }
    }
}

$target = Resolve-TargetWindow -ProcessName $TargetProcessName -ProcessId $TargetProcessId -WindowHandle $TargetWindowHandle -TitleContains $TargetTitleContains
$foregroundBefore = Get-ForegroundInfo

if ($Focus) {
    Focus-TargetWindow -Process $target.Process -WindowHandle $target.WindowHandle -DelayMilliseconds $FocusDelayMilliseconds
}

$foregroundAfterFocus = Get-ForegroundInfo
$isTargetForeground = $foregroundAfterFocus.ProcessId -eq $target.Process.Id

if (-not $DryRun -and -not $isTargetForeground) {
    throw "RIFT is not the foreground window after focus gate. Refusing slash-command input."
}

if (-not $DryRun) {
    if ($SendEscapeBeforeCommand) {
        Send-VirtualKey -VirtualKey $VK_ESCAPE
        Send-VirtualKey -VirtualKey $VK_ESCAPE -KeyUp
        if ($InterCharacterDelayMilliseconds -gt 0) {
            Start-Sleep -Milliseconds $InterCharacterDelayMilliseconds
        }
    }

    Send-UnicodeText -Text $CommandText -DelayMilliseconds $InterCharacterDelayMilliseconds
    if (-not $NoEnter) {
        Send-VirtualKey -VirtualKey $VK_RETURN
        Send-VirtualKey -VirtualKey $VK_RETURN -KeyUp
    }

    if ($PostSendDelayMilliseconds -gt 0) {
        Start-Sleep -Milliseconds $PostSendDelayMilliseconds
    }
}

$foregroundAfterSend = Get-ForegroundInfo
[pscustomobject]@{
    success = $true
    dry_run = $DryRun.IsPresent
    command_text = $CommandText
    enter_sent = -not $NoEnter.IsPresent
    target_process_id = $target.Process.Id
    target_process_name = $target.Process.ProcessName
    target_window_handle = Format-WindowHandle -Handle $target.WindowHandle
    target_window_title = $target.WindowTitle
    foreground_before_process_id = $foregroundBefore.ProcessId
    foreground_before_process_name = $foregroundBefore.ProcessName
    foreground_before_window_handle = Format-WindowHandle -Handle $foregroundBefore.WindowHandle
    foreground_before_window_title = $foregroundBefore.WindowTitle
    foreground_after_focus_process_id = $foregroundAfterFocus.ProcessId
    foreground_after_focus_process_name = $foregroundAfterFocus.ProcessName
    foreground_after_focus_window_handle = Format-WindowHandle -Handle $foregroundAfterFocus.WindowHandle
    foreground_after_focus_window_title = $foregroundAfterFocus.WindowTitle
    foreground_after_send_process_id = $foregroundAfterSend.ProcessId
    foreground_after_send_process_name = $foregroundAfterSend.ProcessName
    foreground_after_send_window_handle = Format-WindowHandle -Handle $foregroundAfterSend.WindowHandle
    foreground_after_send_window_title = $foregroundAfterSend.WindowTitle
    sent_utc = (Get-Date).ToUniversalTime().ToString("o", [System.Globalization.CultureInfo]::InvariantCulture)
} | ConvertTo-Json -Depth 4

# END_OF_SCRIPT
