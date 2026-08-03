# =================================================================
#  install-local.ps1 — Detector + Restaurador de BD
#
#  PASOS:
#    1. Autodetectar la instancia de SQL Server instalada.
#    2. Instalar el modulo SqlServer de PowerShell si hace falta.
#    3. Comprobar si la base 'GestorInventario' existe Y esta ONLINE.
#    4. Si NO existe / no esta ONLINE, buscar un .bak en la carpeta
#       del script y restaurar el ULTIMO backup set del .bak.
#    5. Verificar que la base quedo operativa.
#
#  Exit codes:
#    0 -> la BD existe y esta ONLINE (ya estaba o se restauro OK)
#    2 -> la BD NO existe y no hay .bak disponible
#    1 -> error de conexion / SQL / restore fallido
# =================================================================

$ErrorActionPreference = 'Stop'

# ─────────────────────────────────────────────────────────────────
# Helpers locales
# ─────────────────────────────────────────────────────────────────

function Get-SqlInstances {
    $result = @{}
    $reg = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL' -ErrorAction SilentlyContinue
    if ($reg) {
        foreach ($p in $reg.PSObject.Properties) {
            if ($p.Name -notmatch '^PS' -and $null -ne $p.Value) {
                $result[$p.Name] = $p.Value
            }
        }
    }
    return $result
}

function Get-SqlDefaultDataPath {
    param([string]$InstanceName)

    $instances = Get-SqlInstances
    $instanceId = $instances[$InstanceName]
    if (-not $instanceId) { return $null }

    $regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\MSSQLServer"
    $dataPath = (Get-ItemProperty -Path $regPath -Name 'DefaultData' -ErrorAction SilentlyContinue).DefaultData
    $logPath  = (Get-ItemProperty -Path $regPath -Name 'DefaultLog'  -ErrorAction SilentlyContinue).DefaultLog

    if (-not $dataPath) {
        $sqlRoot = (Get-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$instanceId\Setup" -Name 'SQLDataRoot' -ErrorAction SilentlyContinue).SQLDataRoot
        if ($sqlRoot) {
            $dataPath = Join-Path $sqlRoot 'DATA'
            $logPath  = Join-Path $sqlRoot 'DATA'
        }
    }

    if (-not $dataPath) {
        $programFiles = ${env:ProgramFiles}
        $candidate = Join-Path $programFiles "Microsoft SQL Server\MSSQL16.$InstanceName\MSSQL\DATA"
        if (Test-Path $candidate) {
            $dataPath = $candidate
            $logPath  = $candidate
        }
    }

    return @{
        DataPath = $dataPath
        LogPath  = $logPath
    }
}

function Invoke-SqlDb {
    param(
        [Parameter(Mandatory)] [string]$Instance,
        [Parameter(Mandatory)] [string]$Query,
        [int]$Timeout = 5
    )

    $ds = $Instance.Trim()
    if ($ds -notmatch '^tcp:') {
        if ($ds -notmatch '[\\\/]') { $ds = "localhost\$ds" }
        $ds = "tcp:$ds"
    }

    $connStr = "Data Source=$ds;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=$Timeout"
    Write-Host "  -> Conectando a '$ds'..." -ForegroundColor DarkGray
    return Invoke-Sqlcmd -ConnectionString $connStr -Query $Query -ErrorAction Stop
}

function Initialize-SqlServerModule {
    if (Get-Module -ListAvailable -Name SqlServer) {
        Import-Module SqlServer -ErrorAction Stop | Out-Null
        return
    }

    $repo = Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue
    if (-not $repo) {
        Register-PSRepository -Name PSGallery -SourceLocation 'https://www.powershellgallery.com/api/v2' -InstallationPolicy Trusted -ErrorAction Stop
    } elseif ($repo.InstallationPolicy -ne 'Trusted') {
        Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction Stop
    }

    Install-Module -Name SqlServer -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
    Import-Module SqlServer -ErrorAction Stop | Out-Null
}

function Test-DatabaseExists {
    param([string]$Instance)
    $q = @"
SELECT TOP 1 name
FROM sys.databases
WHERE name = 'GestorInventario'
  AND state_desc = 'ONLINE'
"@
    $row = Invoke-SqlDb -Instance $Instance -Query $q -ErrorAction Stop
    return ($null -ne $row -and $null -ne $row.name)
}

function Remove-DatabaseIfRestoring {
    param([string]$Instance)
    $q = "SELECT state_desc FROM sys.databases WHERE name = 'GestorInventario'"
    $row = Invoke-SqlDb -Instance $Instance -Query $q -ErrorAction Stop
    if ($null -ne $row -and $row.state_desc -eq 'RESTORING') {
        Write-Host "  -> Base en estado RESTORING. Eliminando restos..." -ForegroundColor Yellow
        Invoke-SqlDb -Instance $Instance -Query "DROP DATABASE [GestorInventario]" -Timeout 30 | Out-Null
        return $true
    }
    return $false
}

function Find-BackupFile {
    param([Parameter(Mandatory)][string]$Directory)

    if (-not (Test-Path $Directory)) { return $null }

    $files = @(Get-ChildItem -Path $Directory -Filter '*.bak' -File | Sort-Object Name)
    if ($files.Count -eq 0) { return $null }

    if ($files.Count -eq 1) { return $files[0].FullName }

    Write-Host "  -> Backups detectados:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $files.Count; $i++) {
        Write-Host ("    [{0}] {1}" -f ($i + 1), $files[$i].Name) -ForegroundColor DarkCyan
    }
    $sel = Read-Host "  Elige el numero de backup (ENTER para usar '$($files[0].Name)')"
    if ([string]::IsNullOrWhiteSpace($sel)) { return $files[0].FullName }

    if ($sel -match '^\d+$') {
        $idx = [int]$sel
        if ($idx -ge 1 -and $idx -le $files.Count) { return $files[$idx - 1].FullName }
    }
    throw "Seleccion de backup invalida."
}

# Lee los archivos internos del .bak (para WITH MOVE)
function Get-BackupFileList {
    param(
        [Parameter(Mandatory)] [string]$Instance,
        [Parameter(Mandatory)] [string]$BackupPath
    )
    $escaped = $BackupPath.Replace("'", "''")
    $q = "RESTORE FILELISTONLY FROM DISK = N'$escaped'"
    $rows = Invoke-SqlDb -Instance $Instance -Query $q -Timeout 30

    if ($null -eq $rows) { return @() }
    if ($rows -is [System.Data.DataRow]) { return @($rows) }
    if ($rows -is [array]) { return $rows }
    return @($rows)
}

# Lee los backup sets dentro del .bak y devuelve el de Position mas alta (el mas reciente)
function Get-LatestBackupPosition {
    param(
        [Parameter(Mandatory)] [string]$Instance,
        [Parameter(Mandatory)] [string]$BackupPath
    )
    $escaped = $BackupPath.Replace("'", "''")
    $q = "RESTORE HEADERONLY FROM DISK = N'$escaped'"
    $rows = Invoke-SqlDb -Instance $Instance -Query $q -Timeout 30

    if ($null -eq $rows) { return $null }
    if ($rows -is [System.Data.DataRow]) { $rows = @($rows) }
    elseif ($rows -isnot [array]) { $rows = @($rows) }

    if ($rows.Count -eq 0) { return $null }

    $latest = $rows | Sort-Object Position | Select-Object -Last 1
    return [int]$latest.Position
}

function Restore-DatabaseFromBackup {
    param(
        [Parameter(Mandatory)] [string]$Instance,
        [Parameter(Mandatory)] [string]$BackupPath,
        [Parameter(Mandatory)] [string]$DataPath,
        [Parameter(Mandatory)] [string]$LogPath,
        [string]$DatabaseName = 'GestorInventario'
    )

    # --- 1) Detectar el ultimo backup set dentro del .bak ---
    Write-Host "  -> Analizando backup sets dentro del archivo..." -ForegroundColor DarkGray
    $position = Get-LatestBackupPosition -Instance $Instance -BackupPath $BackupPath
    if ($null -eq $position) {
        throw "No se pudo leer la cabecera del backup (RESTORE HEADERONLY)."
    }
    Write-Host "  -> Se restaurara el backup set numero $position (el mas reciente)." -ForegroundColor DarkGray

    # --- 2) Leer lista de archivos (mdf/ldf/ndf) ---
    Write-Host "  -> Analizando estructura de archivos..." -ForegroundColor DarkGray
    $fileList = Get-BackupFileList -Instance $Instance -BackupPath $BackupPath
    if ($fileList.Count -eq 0) {
        throw "No se pudo leer la lista de archivos del backup (RESTORE FILELISTONLY)."
    }

    $moves = @()
    foreach ($file in $fileList) {
        $logicalName  = $file.LogicalName
        $physicalName = Split-Path -Leaf $file.PhysicalName

        if ($physicalName -notmatch '\.\w+$') {
            $ext = if ($file.Type -eq 'L') { '.ldf' } else { '.mdf' }
            $physicalName += $ext
        }

        $destDir = if ($file.Type -eq 'L') { $LogPath } else { $DataPath }
        $destPath = Join-Path $destDir $physicalName
        $moves += "MOVE N'$logicalName' TO N'$destPath'"
    }

    $escapedBackup = $BackupPath.Replace("'", "''")
    $moveClause = $moves -join ",`n        "

    # --- 3) RESTORE con FILE = N (el backup set correcto) ---
    $q = @"
RESTORE DATABASE [$DatabaseName]
FROM DISK = N'$escapedBackup'
WITH
    FILE = $position,
    $moveClause,
    RECOVERY,
    REPLACE,
    STATS = 10
"@

    Write-Host "  -> Restaurando (puede tardar varios minutos)..." -ForegroundColor DarkGray
    Invoke-SqlDb -Instance $Instance -Query $q -Timeout 600 | Out-Null
}

# ─────────────────────────────────────────────────────────────────
# Utilidades de salida y exit codes
# ─────────────────────────────────────────────────────────────────

$script:ExitOk        = 0
$script:ExitNotFound  = 2
$script:ExitError     = 1

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "[probe] $Message" -ForegroundColor Cyan
    Write-Host "----------------------------------------" -ForegroundColor DarkGray
}

function Finish-Exists {
    param([string]$Instance)
    Write-Host "  Instancia:  $Instance" -ForegroundColor DarkGray
    Write-Host "  Base datos: 'GestorInventario' YA EXISTE y esta ONLINE" -ForegroundColor Green
    Write-Host "----------------------------------------" -ForegroundColor DarkGray
    Write-Host "Resultado:   BD lista" -ForegroundColor Green
    Write-Host "Exit code:   $script:ExitOk" -ForegroundColor Green
    exit $script:ExitOk
}

# ─────────────────────────────────────────────────────────────────
# Flujo principal
# ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== Detector + Restaurador de BD (GestorInventario) ===" -ForegroundColor Cyan
Write-Host "  (restaura automaticamente desde .bak si la BD no existe)" -ForegroundColor DarkGray
Write-Host ""

# 1) Autodetectar instancia
Write-Step "1) Autodetectando instancia SQL Server"

$instances = Get-SqlInstances
if (-not $instances -or $instances.Count -eq 0) {
    Write-Host "  -> No se encontraron instancias de SQL Server instaladas." -ForegroundColor Red
    Write-Host ""
    Write-Host "Resultado:   error (sin instancia)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

$ordered = Sort-Object -InputObject ([string[]]$instances.Keys)
if ($ordered.Count -eq 1) {
    $instance = $ordered[0]
    Write-Host "  -> Unica instancia detectada: $instance" -ForegroundColor Green
} else {
    Write-Host "  -> Instancias detectadas:" -ForegroundColor Yellow
    for ($i = 0; $i -lt $ordered.Count; $i++) {
        Write-Host ("    [{0}] {1}" -f ($i + 1), $ordered[$i]) -ForegroundColor DarkCyan
    }
    $first = $ordered[0]
    $sel = Read-Host "  Elige el numero de instancia (ENTER para usar '$first')"
    if ([string]::IsNullOrWhiteSpace($sel)) { $sel = $first }
    elseif ($sel -match '^\d+$') {
        $idx = [int]$sel
        if ($idx -ge 1 -and $idx -le $ordered.Count) { $sel = $ordered[$idx - 1] }
        else {
            Write-Host "  -> Opcion fuera de rango." -ForegroundColor Red
            Write-Host ""
            Write-Host "Resultado:   error (seleccion invalida)" -ForegroundColor Red
            Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
            exit $script:ExitError
        }
    }
    $instance = $sel
}
Write-Host "  -> Seleccionada: $instance" -ForegroundColor Green

# 1.5) Detectar carpeta de datos por defecto
Write-Step "1.5) Detectando carpeta de datos por defecto"

$paths = Get-SqlDefaultDataPath -InstanceName $instance
if (-not $paths -or -not $paths.DataPath) {
    Write-Host "  -> No se pudo detectar la carpeta de datos de SQL Server." -ForegroundColor Red
    Write-Host ""
    Write-Host "Resultado:   error (ruta de datos desconocida)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

Write-Host "  -> Data path: $($paths.DataPath)" -ForegroundColor Green
Write-Host "  -> Log path:  $($paths.LogPath)"  -ForegroundColor Green

# 2) Asegurar modulo SqlServer
Write-Step "2) Asegurando modulo SqlServer"

try {
    Initialize-SqlServerModule
    Write-Host "  -> Modulo SqlServer disponible." -ForegroundColor Green
} catch {
    Write-Host "  -> No se pudo cargar el modulo SqlServer." -ForegroundColor Red
    Write-Host "     Detalle: $($_.Exception.Message)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Resultado:   error (modulo no instalable)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

# 3) Detectar la BD
Write-Step "3) Comprobando base de datos 'GestorInventario'"

try {
    Remove-DatabaseIfRestoring -Instance $instance | Out-Null
    $exists = Test-DatabaseExists -Instance $instance
} catch {
    Write-Host "  -> No se pudo consultar la instancia '$instance'." -ForegroundColor Red
    Write-Host "     Detalle: $($_.Exception.Message)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Resultado:   error (conexion fallida)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

if ($exists) {
    Finish-Exists -Instance $instance
}

# 4) Buscar .bak
Write-Step "4) Buscando backup (.bak) en la carpeta del script"

$scriptDir = $PSScriptRoot
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

Write-Host "  -> Carpeta de busqueda: $scriptDir" -ForegroundColor DarkGray

try {
    $backupPath = Find-BackupFile -Directory $scriptDir
} catch {
    Write-Host "  -> $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Resultado:   error (seleccion invalida)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

if (-not $backupPath) {
    Write-Host "  -> No se encontro ningun archivo .bak." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Resultado:   BD no existe y no hay backup automatico" -ForegroundColor Yellow
    Write-Host "Exit code:   $script:ExitNotFound" -ForegroundColor Yellow
    exit $script:ExitNotFound
}

Write-Host "  -> Backup seleccionado: $(Split-Path -Leaf $backupPath)" -ForegroundColor Green

# 5) Restaurar
Write-Step "5) Restaurando base de datos 'GestorInventario'"

try {
    Restore-DatabaseFromBackup `
        -Instance $instance `
        -BackupPath $backupPath `
        -DataPath $paths.DataPath `
        -LogPath $paths.LogPath `
        -DatabaseName 'GestorInventario'
    Write-Host "  -> Comando RESTORE ejecutado sin errores." -ForegroundColor Green
} catch {
    Write-Host "  -> Error durante la restauracion." -ForegroundColor Red
    Write-Host "     Detalle: $($_.Exception.Message)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Resultado:   error (restore fallido)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

# 6) Verificacion final
Write-Step "6) Verificando que la base quedo operativa"

try {
    $exists = Test-DatabaseExists -Instance $instance
    if (-not $exists) {
        throw "La restauracion finalizo pero la base de datos no es accesible."
    }
    Write-Host "  -> Base de datos verificada y operativa." -ForegroundColor Green
    Write-Host ""
    Write-Host "Resultado:   BD restaurada exitosamente" -ForegroundColor Green
    Write-Host "Exit code:   $script:ExitOk" -ForegroundColor Green
    exit $script:ExitOk
} catch {
    Write-Host "  -> Error en la verificacion post-restore." -ForegroundColor Red
    Write-Host "     Detalle: $($_.Exception.Message)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Resultado:   error (verificacion fallida)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}