<#
.SYNOPSIS
    Command-line Unity runner for this project: compile check, EditMode tests and PlayMode tests in batch mode.

.DESCRIPTION
    The open Unity Editor holds the project lock, so batch mode cannot run on the working tree while it is open
    (HandleProjectAlreadyOpenInAnotherInstance). By default this script mirrors Assets/, Packages/ and ProjectSettings/
    into a private copy under %LOCALAPPDATA%\ArmySystemUnityCli\project and runs Unity there. The copy keeps its own
    Library/, so only the first run does a full import; later runs are incremental.

    Commands:
      compile   Import + compile all assemblies, then quit. Reports every "error CS" / "warning CS" line.
      editmode  Run EditMode tests (Unity Test Framework). Writes NUnit XML.
      playmode  Run PlayMode tests.
      smoke     Open the Troop test scene, play it for SmokeSeconds in real Play mode and fail on any error or
                exception (Editor/TroopCliSmoke.cs).
      all       compile, then editmode, then smoke. Stops at the first failure.
      sync      Only refresh the mirrored copy.
      watch     Poll .unity-cli\requests\*.json and run each request. Lets tools (or an agent in a VM that cannot start
                Windows processes) queue runs by dropping a small JSON file. Only the commands above are accepted.
      locate    Print the Unity.exe that would be used.

    Results go to <repo>\.unity-cli\results\ (gitignored): <id>.json (summary), <id>.log (Unity log), <id>.xml (tests).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\UnityCli\unity-cli.ps1 compile
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\UnityCli\unity-cli.ps1 editmode -TestFilter "MoraleRulesTests"
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools\UnityCli\unity-cli.ps1 watch
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('compile', 'editmode', 'playmode', 'smoke', 'all', 'sync', 'watch', 'locate')]
    [string]$Command = 'compile',

    # Optional Unity Test Framework -testFilter (test names, namespaces or regex; ';' separated).
    [string]$TestFilter = '',

    # Explicit Unity.exe. Otherwise $env:UNITY_EDITOR, then Unity Hub install locations for ProjectVersion.txt.
    [string]$UnityPath = '',

    # Where the mirrored project and its Library live.
    [string]$WorkRoot = (Join-Path $env:LOCALAPPDATA 'ArmySystemUnityCli'),

    # Run on the working tree itself instead of the mirror. Only works while the Editor is closed.
    [switch]$InPlace,

    # Watch mode poll interval.
    [int]$PollSeconds = 2,

    # Kill Unity if a single run takes longer than this.
    [int]$TimeoutMinutes = 60,

    # Request id (used by watch mode; generated when empty).
    [string]$Id = '',

    # Play time for the smoke command.
    [int]$SmokeSeconds = 120
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0

$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$CliDir = Join-Path $RepoRoot '.unity-cli'
$RequestDir = Join-Path $CliDir 'requests'
$ResultDir = Join-Path $CliDir 'results'
$MirrorRoot = Join-Path $WorkRoot 'project'
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Write-Utf8([string]$Path, [string]$Text) {
    [System.IO.File]::WriteAllText($Path, $Text, $Utf8NoBom)
}

function Ensure-Dir([string]$Path) {
    if (-not (Test-Path $Path)) { New-Item -ItemType Directory -Path $Path -Force | Out-Null }
}

function Get-ProjectVersion {
    $file = Join-Path $RepoRoot 'ProjectSettings\ProjectVersion.txt'
    $line = Get-Content $file | Where-Object { $_ -match '^m_EditorVersion:\s*(\S+)' } | Select-Object -First 1
    if (-not $line) { throw "Cannot read the editor version from $file" }
    return ($line -replace '^m_EditorVersion:\s*', '').Trim()
}

function Find-Unity {
    if ($UnityPath) {
        if (Test-Path $UnityPath) { return (Resolve-Path $UnityPath).Path }
        throw "UnityPath '$UnityPath' does not exist."
    }
    if ($env:UNITY_EDITOR -and (Test-Path $env:UNITY_EDITOR)) { return (Resolve-Path $env:UNITY_EDITOR).Path }

    $version = Get-ProjectVersion
    $candidates = New-Object System.Collections.Generic.List[string]

    # Unity Hub: custom install folder and the list of known editors.
    $hubData = Join-Path $env:APPDATA 'UnityHub'
    $secondary = Join-Path $hubData 'secondaryInstallPath.json'
    if (Test-Path $secondary) {
        try {
            $root = (Get-Content $secondary -Raw | ConvertFrom-Json)
            if ($root) { $candidates.Add((Join-Path $root "$version\Editor\Unity.exe")) }
        } catch { }
    }
    foreach ($name in @('editors-v2.json', 'editors.json')) {
        $editors = Join-Path $hubData $name
        if (Test-Path $editors) {
            $text = Get-Content $editors -Raw
            foreach ($m in [regex]::Matches($text, '"(?<p>[^"]*?' + [regex]::Escape($version) + '[^"]*?)"')) {
                $p = $m.Groups['p'].Value -replace '\\\\', '\'
                if ($p -like '*.exe') { $candidates.Add($p) } else { $candidates.Add((Join-Path $p 'Editor\Unity.exe')); $candidates.Add((Join-Path $p 'Unity.exe')) }
            }
        }
    }

    $candidates.Add("C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe")
    $candidates.Add("C:\Program Files\Unity $version\Editor\Unity.exe")
    foreach ($drive in (Get-PSDrive -PSProvider FileSystem | ForEach-Object { $_.Root })) {
        $candidates.Add((Join-Path $drive "$version\Editor\Unity.exe"))
        $candidates.Add((Join-Path $drive "Unity\Hub\Editor\$version\Editor\Unity.exe"))
        $candidates.Add((Join-Path $drive "Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"))
    }

    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) { return (Resolve-Path $c).Path }
    }
    throw "Unity $version not found. Pass -UnityPath or set UNITY_EDITOR to the Unity.exe path."
}

function Sync-Mirror {
    Ensure-Dir $MirrorRoot
    foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
        $src = Join-Path $RepoRoot $folder
        $dst = Join-Path $MirrorRoot $folder
        # /MIR mirrors deletions too. Robocopy exit codes below 8 mean success.
        & robocopy $src $dst /MIR /NFL /NDL /NJH /NJS /NP /R:2 /W:1 /XD '.git' | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "robocopy failed for $folder (exit $LASTEXITCODE)" }
    }
    $global:LASTEXITCODE = 0
}

function Get-ProjectPath {
    if ($InPlace) { return $RepoRoot }
    Sync-Mirror
    return $MirrorRoot
}

function Quote([string]$s) { return '"' + $s.Replace('"', '\"') + '"' }

function Invoke-Unity([string[]]$UnityArgs, [string]$LogPath) {
    $unity = Find-Unity
    $argLine = ($UnityArgs | ForEach-Object { if ($_ -match '\s|"') { Quote $_ } else { $_ } }) -join ' '
    $argLine = "$argLine -logFile $(Quote $LogPath)"
    if (Test-Path $LogPath) { Remove-Item $LogPath -Force }
    $proc = Start-Process -FilePath $unity -ArgumentList $argLine -PassThru -WindowStyle Hidden
    # Wait, but stop early on the timeout or when a cancel file appears (unity-cli.sh cancel).
    $cancel = Join-Path $CliDir 'cancel'
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    while (-not $proc.WaitForExit(2000)) {
        if (Test-Path $cancel) {
            Remove-Item $cancel -Force -ErrorAction SilentlyContinue
            & taskkill /PID $proc.Id /T /F | Out-Null
            return -998
        }
        if ((Get-Date) -gt $deadline) {
            & taskkill /PID $proc.Id /T /F | Out-Null
            return -999
        }
    }
    return $proc.ExitCode
}

function Read-CompilerMessages([string]$LogPath) {
    $errors = New-Object System.Collections.Generic.List[string]
    $warnings = New-Object System.Collections.Generic.List[string]
    $other = New-Object System.Collections.Generic.List[string]
    if (Test-Path $LogPath) {
        $seen = @{}
        foreach ($line in (Get-Content $LogPath)) {
            $t = $line.Trim()
            if ($seen.ContainsKey($t)) { continue }
            if ($t -match 'error CS\d+') { $errors.Add($t); $seen[$t] = 1 }
            elseif ($t -match 'warning CS\d+' -and $t -match '^Assets[\\/]') { $warnings.Add($t); $seen[$t] = 1 }
            elseif ($t -match 'Scripts have compiler errors|HandleProjectAlreadyOpenInAnotherInstance|No valid Unity Editor license|Aborting batchmode due to failure|Error building Player|Exception:|^\[TroopSmoke\]') { $other.Add($t); $seen[$t] = 1 }
        }
    }
    return @{ errors = @($errors); warnings = @($warnings); problems = @($other) }
}

function Read-TestResults([string]$XmlPath) {
    if (-not (Test-Path $XmlPath)) { return $null }
    [xml]$doc = Get-Content $XmlPath -Raw
    $run = $doc.DocumentElement
    $failed = @()
    foreach ($case in @($doc.SelectNodes('//test-case'))) {
        if ([string]$case.GetAttribute('result') -ne 'Failed') { continue }
        $msg = ''
        $node = $case.SelectSingleNode('failure/message')
        if ($node -ne $null) { $msg = [string]$node.InnerText }
        $failed += [pscustomobject]@{ name = [string]$case.GetAttribute('fullname'); message = $msg.Trim() }
    }
    return [pscustomobject]@{
        total = [int]$run.GetAttribute('total'); passed = [int]$run.GetAttribute('passed')
        failed = [int]$run.GetAttribute('failed'); skipped = [int]$run.GetAttribute('skipped')
        inconclusive = [int]$run.GetAttribute('inconclusive')
        failures = $failed
    }
}

function New-RunId {
    if ($Id) { return $Id }
    return (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + ([guid]::NewGuid().ToString('N').Substring(0, 6))
}

function Invoke-Step([string]$Step, [string]$RunId, [string]$Filter) {
    Ensure-Dir $ResultDir
    $started = Get-Date
    $log = Join-Path $ResultDir "$RunId-$Step.log"
    $xml = Join-Path $ResultDir "$RunId-$Step.xml"
    $project = Get-ProjectPath

    $unityArgs = @('-batchmode', '-nographics', '-projectPath', $project)
    if ($Step -eq 'compile') {
        $unityArgs += '-quit'
    } elseif ($Step -eq 'smoke') {
        $unityArgs += @('-executeMethod', 'TroopCliSmoke.Run', '-smokeSeconds', "$SmokeSeconds")
    } else {
        $platform = 'EditMode'
        if ($Step -eq 'playmode') { $platform = 'PlayMode' }
        $unityArgs += @('-runTests', '-testPlatform', $platform, '-testResults', $xml)
        if ($Filter) { $unityArgs += @('-testFilter', $Filter) }
    }

    $exit = Invoke-Unity $unityArgs $log
    $messages = Read-CompilerMessages $log
    $tests = $null
    $isTestRun = ($Step -eq 'editmode') -or ($Step -eq 'playmode')
    if ($isTestRun) { $tests = Read-TestResults $xml }

    $ok = ($exit -eq 0) -and ($messages.errors.Count -eq 0)
    if ($isTestRun) { $ok = $ok -and ($null -ne $tests) -and ($tests.failed -eq 0) }

    return [ordered]@{
        step = $Step
        success = $ok
        exitCode = $exit
        timedOut = ($exit -eq -999)
        cancelled = ($exit -eq -998)
        seconds = [int]((Get-Date) - $started).TotalSeconds
        project = $project
        log = $log
        testResults = $(if ($isTestRun) { $xml } else { $null })
        errors = $messages.errors
        warnings = $messages.warnings
        problems = $messages.problems
        tests = $tests
    }
}

function Invoke-CliCommand([string]$Cmd, [string]$RunId, [string]$Filter) {
    $steps = @($Cmd)
    if ($Cmd -eq 'all') { $steps = @('compile', 'editmode', 'smoke') }
    $results = @()
    $ok = $true
    foreach ($s in $steps) {
        $r = Invoke-Step $s $RunId $Filter
        $results += $r
        if (-not $r.success) { $ok = $false; break }
    }
    $summary = [ordered]@{
        id = $RunId
        command = $Cmd
        filter = $Filter
        success = $ok
        finished = (Get-Date).ToString('o')
        unity = (Find-Unity)
        steps = $results
    }
    Ensure-Dir $ResultDir
    Write-Utf8 (Join-Path $ResultDir "$RunId.json") ($summary | ConvertTo-Json -Depth 8)
    return $summary
}

function Show-Summary($Summary) {
    $state = 'FAILED'
    if ($Summary.success) { $state = 'OK' }
    Write-Host ("[{0}] {1} {2}" -f $state, $Summary.command, $Summary.id)
    foreach ($s in $Summary.steps) {
        Write-Host ("  {0}: exit {1}, {2}s, {3} errors, {4} warnings" -f $s.step, $s.exitCode, $s.seconds, $s.errors.Count, $s.warnings.Count)
        foreach ($e in $s.errors) { Write-Host "    $e" -ForegroundColor Red }
        foreach ($p in $s.problems) { Write-Host "    $p" -ForegroundColor Yellow }
        if ($s.tests) {
            Write-Host ("    tests: {0} total, {1} passed, {2} failed" -f $s.tests.total, $s.tests.passed, $s.tests.failed)
            foreach ($f in $s.tests.failures) { Write-Host "    FAIL $($f.name): $($f.message)" -ForegroundColor Red }
        }
        Write-Host "    log: $($s.log)"
    }
}

function Start-Watch {
    Ensure-Dir $RequestDir
    Ensure-Dir $ResultDir
    $heartbeat = Join-Path $CliDir 'runner.json'
    Write-Host "Unity CLI runner watching $RequestDir (Ctrl+C to stop)."
    Write-Host "Unity: $(Find-Unity)"
    Write-Host "Mirror: $MirrorRoot"
    $allowed = @('compile', 'editmode', 'playmode', 'smoke', 'all', 'sync')
    while ($true) {
        Write-Utf8 $heartbeat (([ordered]@{ pid = $PID; alive = (Get-Date).ToString('o'); state = 'idle'; mirror = $MirrorRoot }) | ConvertTo-Json)
        $requests = Get-ChildItem -Path $RequestDir -Filter '*.json' -File -ErrorAction SilentlyContinue | Sort-Object LastWriteTime
        foreach ($file in $requests) {
            $runId = [IO.Path]::GetFileNameWithoutExtension($file.Name)
            try {
                $req = Get-Content $file.FullName -Raw | ConvertFrom-Json
                Remove-Item $file.FullName -Force
                $cmd = [string]$req.command
                $filter = ''
                if ($req.PSObject.Properties.Name -contains 'filter' -and $req.filter) { $filter = [string]$req.filter }
                if ($allowed -notcontains $cmd) { throw "Rejected command '$cmd'." }
                if ($filter -and ($filter -notmatch '^[A-Za-z0-9_.;|*()\[\]\-]+$')) { throw "Rejected filter '$filter'." }
                if ($runId -notmatch '^[A-Za-z0-9_\-]+$') { throw "Rejected request id '$runId'." }
                Write-Utf8 $heartbeat (([ordered]@{ pid = $PID; alive = (Get-Date).ToString('o'); state = "running $cmd"; request = $runId; mirror = $MirrorRoot }) | ConvertTo-Json)
                Write-Host "$(Get-Date -Format HH:mm:ss) $runId -> $cmd $filter"
                # Each request runs in a fresh PowerShell, so edits to this script apply without restarting the runner.
                $childArgs = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Quote $PSCommandPath), $cmd, '-Id', $runId)
                if ($filter) { $childArgs += @('-TestFilter', $filter) }
                if ($UnityPath) { $childArgs += @('-UnityPath', (Quote $UnityPath)) }
                $child = Start-Process -FilePath 'powershell.exe' -ArgumentList ($childArgs -join ' ') -NoNewWindow -PassThru -Wait
                if (-not (Test-Path (Join-Path $ResultDir "$runId.json"))) {
                    throw "Child run exited with code $($child.ExitCode) without writing a result."
                }
            } catch {
                Write-Host "$runId failed: $_" -ForegroundColor Red
                Write-Utf8 (Join-Path $ResultDir "$runId.json") (([ordered]@{ id = $runId; success = $false; error = "$_ $($_.ScriptStackTrace)"; finished = (Get-Date).ToString('o'); steps = @() }) | ConvertTo-Json)
            }
        }
        Start-Sleep -Seconds $PollSeconds
    }
}

switch ($Command) {
    'locate' { Write-Host (Find-Unity); break }
    'sync' {
        Sync-Mirror
        if ($Id) {
            Ensure-Dir $ResultDir
            Write-Utf8 (Join-Path $ResultDir "$Id.json") (([ordered]@{ id = $Id; command = 'sync'; success = $true; finished = (Get-Date).ToString('o'); steps = @() }) | ConvertTo-Json)
        }
        Write-Host "Mirrored into $MirrorRoot"
        break
    }
    'watch' { Start-Watch; break }
    default {
        $runId = New-RunId
        try {
            $summary = Invoke-CliCommand $Command $runId $TestFilter
        } catch {
            Ensure-Dir $ResultDir
            Write-Utf8 (Join-Path $ResultDir "$runId.json") (([ordered]@{ id = $runId; command = $Command; success = $false; error = "$_ $($_.ScriptStackTrace)"; finished = (Get-Date).ToString('o'); steps = @() }) | ConvertTo-Json)
            Write-Host "Failed: $_" -ForegroundColor Red
            exit 2
        }
        Show-Summary $summary
        if (-not $summary.success) { exit 1 }
    }
}
