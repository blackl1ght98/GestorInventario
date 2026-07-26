# =================================================================
#  install-local2.ps1 — Detector minimo de instancia + BD
#
#  PASOS DE AVANCE (smoke test incremental):
#    1. Autodetectar la instancia de SQL Server instalada.
#    2. Instalar el modulo SqlServer de PowerShell si hace falta.
#    3. Comprobar si la base 'GestorInventario' existe en esa
#       instancia ejecutando `SELECT DB_ID('GestorInventario')`.
#    4. Imprimir el resultado y devolver exit code:
#         0 -> la BD existe
#         2 -> la BD NO existe (se puede restaurar)
#         1 -> error de conexion / SQL
#
#  Este script es INTENCIONALMENTE tonto: no restaura, no toca
#  secretos, no regenera el DbContext. Su unica mision es decir
#  "la BD esta ahi o no lo esta". Una vez verde, encadenamos el
#  resto de pasos (install-local.ps1) en el mismo flujo.
# =================================================================

$ErrorActionPreference = 'Stop'

# ─────────────────────────────────────────────────────────────────
# Helpers locales (NO dependen de install-local.ps1)
# ─────────────────────────────────────────────────────────────────

# Devuelve una hashtable {NombreVisible = MSSQLxx.INSTANCE_ID} con las
# instancias de SQL Server instaladas. Equivalente a la funcion del
# mismo nombre en install-local.ps1 pero replicada aqui a proposito
# para mantener este script autosuficiente.
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

# Wrapper sobre Invoke-Sqlcmd que siempre usa TCP y autenticacion
# integrada. Evita que 'SQLEXPRESS' resuelva por Named Pipes desde
# PowerShell aunque SSMS (que si resuelve el alias local) pueda
# entrar. Lanza excepcion si la conexion falla.
function Invoke-SqlDb {
    param(
        [Parameter(Mandatory)] [string]$Instance,
        [Parameter(Mandatory)] [string]$Query,
        [int]$Timeout = 5
    )

    # Normalizar el nombre de la instancia:
    #   "SQLEXPRESS"                -> "tcp:localhost\SQLEXPRESS"
    #   "localhost\SQLEXPRESS"      -> "tcp:localhost\SQLEXPRESS"
    #   "DESKTOP-GN4VRAH\SQLEXPRESS"-> "tcp:DESKTOP-GN4VRAH\SQLEXPRESS"
    #   "tcp:loquesea"              -> respeta lo que ya viene
    $ds = $Instance.Trim()
    if ($ds -notmatch '^tcp:') {
        if ($ds -notmatch '[\\\/]') { $ds = "localhost\$ds" }
        $ds = "tcp:$ds"
    }

    $connStr = "Data Source=$ds;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=$Timeout"
    write-Host "  -> Conectando a '$ds'..." -ForegroundColor DarkGray
    return Invoke-Sqlcmd -ConnectionString $connStr -Query $Query -ErrorAction Stop
}

# Garantiza que el modulo SqlServer esta disponible. Si no, intenta
# instalarlo (descarga silenciosa, sin prompts interactivos).
function Initialize-SqlServerModule {
    if (Get-Module -ListAvailable -Name SqlServer) {
        Import-Module SqlServer -ErrorAction Stop | Out-Null
        return
    }

    # PSGallery puede no estar registrado o no ser de confianza; nos aseguramos.
    $repo = Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue
    if (-not $repo) {
        Register-PSRepository -Name PSGallery -SourceLocation 'https://www.powershellgallery.com/api/v2' -InstallationPolicy Trusted -ErrorAction Stop
    } elseif ($repo.InstallationPolicy -ne 'Trusted') {
        Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction Stop
    }

    # -Force evita la confirmación interactiva, -SkipPublisherCheck evita
    # otro prompt cuando el publisher no está aún en la lista de confianza.
    Install-Module -Name SqlServer -Scope CurrentUser -Force -AllowClobber -SkipPublisherCheck -ErrorAction Stop
    Import-Module SqlServer -ErrorAction Stop | Out-Null
}

# Devuelve $true si la base 'GestorInventario' existe en la instancia dada.
# OJO: Invoke-Sqlcmd con una sola fila devuelve el DataRow directamente,
# NO un array de filas. Por eso se accede con $row.Id, no $row[0].Id
# (eso ultimo daba la primera columna 'Id' por indice, luego .Id era la
# propiedad del valor escalar y daba $null -> siempre 'no existe').
function Test-DatabaseExists {
    param([string]$Instance)
    $q = "SELECT DB_ID('GestorInventario') AS Id"
    $row = Invoke-SqlDb -Instance $Instance -Query $q -ErrorAction Stop
    return ($null -ne $row -and $null -ne $row.Id)
}

# ─────────────────────────────────────────────────────────────────
# Utilidades de salida y exit codes
# ─────────────────────────────────────────────────────────────────

# 0 -> la BD EXISTE en la instancia detectada.
# 2 -> la instancia responde pero la BD NO EXISTE.
# 1 -> error de conexion / SQL / modulo no instalable.
$script:ExitOk        = 0
$script:ExitNotFound  = 2
$script:ExitError     = 1

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "[probe] $Message" -ForegroundColor Cyan
    Write-Host "----------------------------------------" -ForegroundColor DarkGray
}

function Finish-WithResult {
    param([bool]$Exists, [string]$Instance)
    if ($Exists) {
        Write-Host "  Instancia:  $Instance" -ForegroundColor DarkGray
        Write-Host "  Base datos: 'GestorInventario' YA EXISTE" -ForegroundColor Green
        Write-Host "----------------------------------------" -ForegroundColor DarkGray
        Write-Host "Resultado:   BD existe" -ForegroundColor Green
        Write-Host "Exit code:   $script:ExitOk" -ForegroundColor Green
        exit $script:ExitOk
    } else {
        Write-Host "  Instancia:  $Instance" -ForegroundColor DarkGray
        Write-Host "  Base datos: 'GestorInventario' NO EXISTE" -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor DarkGray
        Write-Host "Resultado:   BD no existe (se podria restaurar)" -ForegroundColor Yellow
        Write-Host "Exit code:   $script:ExitNotFound" -ForegroundColor Yellow
        exit $script:ExitNotFound
    }
}

# ─────────────────────────────────────────────────────────────────
# Flujo principal
# ─────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "=== Detector de instancia + BD (GestorInventario) ===" -ForegroundColor Cyan
Write-Host "  (smoke test: NO restaura, NO toca secretos, NO regenera DbContext)" -ForegroundColor DarkGray
Write-Host ""

# 1) Autodetectar la instancia del registro; si hay varias, dejar elegir.
Write-Step "1) Autodetectando instancia SQL Server"

$instances = Get-SqlInstances
if (-not $instances -or $instances.Count -eq 0) {
    Write-Host "  -> No se encontraron instancias de SQL Server instaladas." -ForegroundColor Red
    Write-Host "     Esperado al menos: HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Resultado:   error (sin instancia)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

# Forzar [string[]] antes de Sort-Object: si el array tiene UN solo
# elemento, el pipeline "Sort-Object" desempaqueta ese string caracter
# a caracter y $ordered[0] acaba siendo la primera letra del nombre
# (ej. 'S' en vez de 'SQLEXPRESS'). Usamos -InputObject para evitar el
# desempaquetado por pipeline y mantener el array intacto.
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
            Write-Host "Resultado:   error (seleccion invalida)" -ForegroundColor Red
            Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
            exit $script:ExitError
        }
    }
    $instance = $sel
}
Write-Host "  -> Seleccionada: $instance" -ForegroundColor Green

# 2) Asegurar el modulo SqlServer.
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

# 3) Detectar la BD.
Write-Step "3) Comprobando base de datos 'GestorInventario'"

try {
    $exists = Test-DatabaseExists -Instance $instance
} catch {
    Write-Host "  -> No se pudo consultar la instancia '$instance'." -ForegroundColor Red
    Write-Host "     Detalle: $($_.Exception.Message)" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "Resultado:   error (conexion fallida)" -ForegroundColor Red
    Write-Host "Exit code:   $script:ExitError" -ForegroundColor Red
    exit $script:ExitError
}

Finish-WithResult -Exists $exists -Instance $instance
