#!/usr/bin/env bash
# =================================================================
#  install-linux.sh — Instalador de GestorInventario para Linux
#
#  Equivalente a install.ps1 pero adaptado a cualquier distro Linux.
#  Pasos:
#    0. Comprobar Git LFS (necesario para descargar el .bak real).
#    1. Crear carpeta certs/ y generar certificado autofirmado (PFX).
#    2. Generar el archivo .env (orden exacto, valores no editables).
#    3. Verificar si Docker está disponible.
#    4. Arrancar el servicio Docker si está parado.
#    5. Ejecutar "docker compose up -d --build".
#
#  NOTA SOBRE VARIABLES DE ENTORNO:
#  El docker-compose.yml lee las variables en MAYÚSCULAS (DB_HOST,
#  CLAVE_JWT, JWT_ISSUER, PUBLIC_KEY, etc.), que es como el código
#  C# las busca vía Environment.GetEnvironmentVariable(...). Por eso
#  el .env generado aquí está en MAYÚSCULAS también.
# =================================================================

# No usamos 'set -e' global: el script hace muchas llamadas a
# 'read' que devuelven código != 0 en EOF (entrada cerrada / TTY
# ausente) y a utilidades externas (openssl, docker, systemctl) que
# deben fallar de forma controlada sin abortar el flujo.
# Las salidas de error se redirigen a /dev/null solo donde no aporta.
set -u

# Detectar distribución (para mensajes específicos de instalación de Docker)
detect_distro() {
    if [ -f /etc/os-release ]; then
        . /etc/os-release
        DISTRO_ID="${ID:-unknown}"
        DISTRO_LIKE="${ID_LIKE:-}"
        DISTRO_PRETTY="${PRETTY_NAME:-$DISTRO_ID}"
    else
        DISTRO_ID="unknown"
        DISTRO_LIKE=""
        DISTRO_PRETTY="Linux desconocido"
    fi
}

detect_distro

# ─────────────────────────────────────────────────────────────────
# Detectar Docker Desktop y contexto activo
# ─────────────────────────────────────────────────────────────────
# Importante: en CachyOS (y en general con Docker Desktop instalado)
# coexisten dos motores:
#   - Docker Engine nativo  → socket /var/run/docker.sock
#   - Docker Desktop        → socket /home/$USER/.docker/desktop/docker.sock
#                              y contexto 'desktop-linux'
# Son MÁQUINAS DISTINTAS. Si haces 'sudo ./install-linux.sh' sin
# preservar el contexto, docker cae sobre /var/run/docker.sock y los
# contenedores NO aparecen en Docker Desktop.
#
# Por eso: si el contexto activo es 'desktop-linux', forzamos ese
# mismo contexto cuando invoquemos 'sudo' para docker (con -E).

DOCKER_HAS_DESKTOP=0
DOCKER_ACTIVE_CONTEXT=""
if command -v docker >/dev/null 2>&1; then
    DOCKER_ACTIVE_CONTEXT=$(docker context show 2>/dev/null || echo "")
    if [ "$DOCKER_ACTIVE_CONTEXT" = "desktop-linux" ]; then
        DOCKER_HAS_DESKTOP=1
    fi
fi

# Aviso si se está ejecutando con sudo (situación típica: el usuario
# leyó 'sudo ./install-linux.sh' en el README y lo lanzo asi).
if [ "$(id -u)" -eq 0 ]; then
    warn "  -> Este script se esta ejecutando como root (sudo)."
    if [ "$DOCKER_HAS_DESKTOP" -eq 1 ]; then
        dimc "     Tu contexto Docker activo es 'desktop-linux'."
        dimc "     Para que los contenedores aparezcan en Docker Desktop,"
        dimc "     el script preservara ese contexto al invocar sudo."
    else
        dimc "     Si tienes Docker Desktop, asegurate de que su motor esta"
        dimc "     corriendo y que tu contexto activo es 'desktop-linux':"
        dimc "       docker context use desktop-linux"
    fi
    printf "\n"
fi

# ─────────────────────────────────────────────────────────────────
# Utilidades
# ─────────────────────────────────────────────────────────────────
# Carpeta del script (compatible con bash; no usa $PSScriptRoot).
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Colores (desactivados si no es una TTY interactiva).
if [ -t 1 ]; then
    C_CYAN='\033[36m'
    C_GREEN='\033[32m'
    C_YELLOW='\033[33m'
    C_DARKCYAN='\033[36m'
    C_DARKGRAY='\033[90m'
    C_DARKYELLOW='\033[33m'
    C_RED='\033[31m'
    C_RESET='\033[0m'
else
    C_CYAN=''; C_GREEN=''; C_YELLOW=''; C_DARKCYAN=''
    C_DARKGRAY=''; C_DARKYELLOW=''; C_RED=''; C_RESET=''
fi

info()    { printf "${C_CYAN}%s${C_RESET}\n" "$*"; }
ok()      { printf "${C_GREEN}%s${C_RESET}\n" "$*"; }
warn()    { printf "${C_YELLOW}%s${C_RESET}\n" "$*"; }
dim()     { printf "${C_DARKGRAY}%s${C_RESET}\n" "$*"; }
dimc()    { printf "${C_DARKCYAN}%s${C_RESET}\n" "$*"; }
error()   { printf "${C_RED}%s${C_RESET}\n" "$*" >&2; }

# open_browser: abre una URL en el navegador predeterminado.
# En Linux, el equivalente multiplataforma de Start-Process es xdg-open
# (presente en la mayoría de distros). En macOS sería 'open', pero
# este instalador es para Linux, así que priorizamos xdg-open.
open_browser() {
    local url="$1"
    if command -v xdg-open >/dev/null 2>&1; then
        xdg-open "$url" >/dev/null 2>&1 && dimc "  -> Abriendo $url en tu navegador" \
            || warn "  -> No se pudo abrir el navegador. Ve manualmente a: $url"
    elif command -v gio >/dev/null 2>&1; then
        gio open "$url" >/dev/null 2>&1 \
            || warn "  -> No se pudo abrir el navegador. Ve manualmente a: $url"
    elif command -v sensible-browser >/dev/null 2>&1; then
        sensible-browser "$url" >/dev/null 2>&1 \
            || warn "  -> No se pudo abrir el navegador. Ve manualmente a: $url"
    else
        warn "  -> No hay un navegador gráfico disponible. Ve manualmente a: $url"
    fi
}

# ─────────────────────────────────────────────────────────────────
# 0. Comprobar Git LFS y descargar el .bak real
# ─────────────────────────────────────────────────────────────────
# El proyecto incluye un backup de SQL Server (GestorInventario-*.bak)
# almacenado en Git LFS. Si el usuario hizo 'git clone' sin tener
# git-lfs instalado, el .bak en disco es solo un puntero de texto
# de ~134 bytes y SQL Server fallara con "volume is empty" al
# intentar RESTORE. Por eso comprobamos esto ANTES de tocar Docker.
info "\n[0/4] Comprobando Git LFS..."

# Localiza todos los .bak en la raiz del proyecto (mismo nivel que el script)
BAK_FILES=()
while IFS= read -r f; do
    BAK_FILES+=("$f")
done < <(find "$SCRIPT_DIR" -maxdepth 1 -type f -name '*.bak' 2>/dev/null)

# Si no hay .bak en la raiz, nada que comprobar (alguien lo ha borrado
# a proposito, o el proyecto migro a migraciones).
if [ "${#BAK_FILES[@]}" -eq 0 ]; then
    dimc "  -> No hay archivos .bak en la raiz del proyecto. Se omite la comprobacion LFS."
else
    # git-lfs marca los archivos descargados con un puntero que empieza
    # por 'version https://git-lfs.github.com/spec/v1'. Si el archivo
    # es texto corto, NO se ha descargado el blob real.
    NEEDS_LFS=0
    for f in "${BAK_FILES[@]}"; do
        if [ -s "$f" ] && head -c 60 "$f" 2>/dev/null | grep -q 'git-lfs.github.com/spec/v1'; then
            NEEDS_LFS=1
            break
        fi
    done

    if [ "$NEEDS_LFS" -eq 0 ]; then
        ok "  -> Los .bak ya estan descargados (no son punteros LFS). Nada que hacer."
    else
        warn "  -> Se han detectado .bak que siguen siendo punteros de Git LFS."
        dimc "     SQL Server no podra restaurarlos sin el contenido real."

        # 1) ¿Esta git-lfs disponible?
        if ! command -v git-lfs >/dev/null 2>&1; then
            warn "  -> 'git-lfs' NO esta instalado en este sistema."
            printf "\n"
            dimc "     Distribucion detectada: $DISTRO_PRETTY"
            dimc "     Instala git-lfs segun tu distro. Comandos habituales:"
            printf "\n"
            case "$DISTRO_ID" in
                ubuntu|debian|pop|linuxmint|elementary|zorin|kali|raspbian)
                    dimc "       Debian/Ubuntu y derivados:"
                    dimc "         sudo apt-get update && sudo apt-get install -y git-lfs"
                    ;;
                fedora|rhel|centos|rocky|almalinux|ol)
                    dimc "       Fedora/RHEL y derivados:"
                    dimc "         sudo dnf -y install git-lfs"
                    ;;
                arch|manjaro|endeavouros|garuda|cachyos)
                    dimc "       Arch/Manjaro/CachyOS y derivados:"
                    dimc "         sudo pacman -S --noconfirm git-lfs"
                    ;;
                opensuse*|sles)
                    dimc "       openSUSE/SLES:"
                    dimc "         sudo zypper install -y git-lfs"
                    ;;
                *)
                    dimc "       Distribucion no reconocida ($DISTRO_ID)."
                    dimc "       Compilar desde fuente: https://git-lfs.github.com/"
                    ;;
            esac
            printf "\n"
            dimc "     Tras instalar git-lfs, vuelve a ejecutar este script."
            exit 1
        fi

        ok "  -> 'git-lfs' esta instalado. Inicializando y descargando blobs..."

        # 2) 'git lfs install' solo afecta a hooks locales; idempotente.
        git lfs install >/dev/null 2>&1 || true

        # 3) Descargar el contenido real. Si falla (sin red, sin
        #    credenciales del remoto LFS, etc.) abortamos con claridad.
        if git lfs pull 2>&1 | tee /tmp/git-lfs-pull.log; then
            :
        else
            error "  -> 'git lfs pull' fallo. El .bak no se ha descargado."
            dimc "     Revisa /tmp/git-lfs-pull.log y la conectividad con GitHub."
            dimc "     Sin el .bak real, el RESTORE fallara en docker compose."
            exit 1
        fi

        # 4) Verificar que el pull realmente sustituyo el puntero por
        #    un archivo binario. Si sigue siendo texto, abortamos.
        STILL_POINTER=0
        for f in "${BAK_FILES[@]}"; do
            if head -c 60 "$f" 2>/dev/null | grep -q 'git-lfs.github.com/spec/v1'; then
                STILL_POINTER=1
                break
            fi
        done

        if [ "$STILL_POINTER" -eq 1 ]; then
            error "  -> Tras 'git lfs pull' el .bak sigue siendo un puntero LFS."
            dimc "     Posibles causas: el remoto no tiene el blob (push fallido),"
            dimc "     limite de banda de Git LFS agotado, o credenciales faltantes."
            exit 1
        fi

        ok "  -> .bak descargado correctamente desde Git LFS."
    fi
fi

# ─────────────────────────────────────────────────────────────────
# 1. Certificado autofirmado en certs/
# ─────────────────────────────────────────────────────────────────
info "\n[1/4] Generando certificado autofirmado..."

OUT_DIR="$SCRIPT_DIR/certs"
mkdir -p "$OUT_DIR"
PFX_PATH="$OUT_DIR/certificado.pfx"
CERT_PASSWORD='0000'

# Intentar generar el PFX y confiar en él.
# Necesitamos openssl para el certificado y, opcionalmente, la
# utilidad 'trust' o 'update-ca-certificates' para confiar a nivel
# de sistema. Si algo falla, no abortamos: continuamos sin cert.
CERT_GENERATED=0
if command -v openssl >/dev/null 2>&1; then
    CRT_PATH="$OUT_DIR/certificado.crt"
    KEY_PATH="$OUT_DIR/certificado.key"

    # Generar clave privada y certificado autofirmado en una sola
    # pasada (equivalente a New-SelfSignedCertificate -DnsName localhost).
    if openssl req -x509 -nodes -newkey rsa:2048 \
            -keyout "$KEY_PATH" \
            -out "$CRT_PATH" \
            -days 365 \
            -subj "/CN=localhost" \
            -addext "subjectAltName=DNS:localhost,IP:127.0.0.1" \
            2>/dev/null; then

        # Empaquetar clave + certificado en un PFX (PKCS#12),
        # equivalente a Export-PfxCertificate en PowerShell.
        if openssl pkcs12 -export \
                -out "$PFX_PATH" \
                -inkey "$KEY_PATH" \
                -in "$CRT_PATH" \
                -password "pass:$CERT_PASSWORD" 2>/dev/null; then
            CERT_GENERATED=1

            # Si el script se está ejecutando con 'sudo', los archivos
            # generados pertenecerán a root con permisos 0600. Después,
            # 'docker compose' se ejecuta SIN sudo y no podría leerlos.
            # Damos permisos de lectura a todos para evitar ese fallo.
            chmod 0644 "$CRT_PATH" 2>/dev/null || true
            chmod 0644 "$PFX_PATH" 2>/dev/null || true
            chmod 0644 "$KEY_PATH" 2>/dev/null || true
            # Si hay SUDO_USER, también cedemos la propiedad a ese
            # usuario para que pueda regenerar los certs sin sudo.
            if [ -n "${SUDO_USER:-}" ]; then
                chown "$SUDO_USER" "$OUT_DIR" "$CRT_PATH" "$KEY_PATH" "$PFX_PATH" 2>/dev/null || true
            fi

            ok "  -> Certificado exportado en: $PFX_PATH"

            # Intentar confiar en el certificado a nivel de sistema.
            # Diferentes distros usan comandos distintos; probamos varios.
            TRUSTED=0
            if command -v trust >/dev/null 2>&1; then
                # Debian / Ubuntu moderno (p11-kit) y Fedora reciente
                if trust anchor "$CRT_PATH" 2>/dev/null; then
                    ok "  -> Certificado agregado al almacén de confianza del sistema (trust)."
                    TRUSTED=1
                fi
            fi

            if [ "$TRUSTED" -eq 0 ] && command -v update-ca-certificates >/dev/null 2>&1; then
                # Debian / Ubuntu clásico
                if cp "$CRT_PATH" /usr/local/share/ca-certificates/gestorinventario.crt 2>/dev/null \
                    && update-ca-certificates >/dev/null 2>&1; then
                    ok "  -> Certificado agregado al almacén de confianza del sistema (ca-certificates)."
                    TRUSTED=1
                fi
            fi

            if [ "$TRUSTED" -eq 0 ] && command -v update-ca-trust >/dev/null 2>&1; then
                # Fedora / RHEL / CentOS / Arch con ca-trust
                if cp "$CRT_PATH" /etc/pki/ca-trust/source/anchors/gestorinventario.crt 2>/dev/null \
                    && update-ca-trust extract >/dev/null 2>&1; then
                    ok "  -> Certificado agregado al almacén de confianza del sistema (ca-trust)."
                    TRUSTED=1
                fi
            fi

            if [ "$TRUSTED" -eq 0 ]; then
                warn "  -> No se pudo agregar el certificado al almacén de confianza (¿ejecutar como root?)."
                dimc "     Puedes confiar en él manualmente copiando $CRT_PATH al almacén de tu distro."
            fi
        else
            warn "  -> No se pudo crear el archivo PFX."
        fi
    else
        warn "  -> No se pudo generar el certificado con openssl."
    fi
else
    warn "  -> openssl no está instalado; no se puede generar el certificado."
fi

if [ "$CERT_GENERATED" -eq 0 ]; then
    warn "     Continuando sin certificado (lo necesitarás si sirves HTTPS localmente)."
fi

# ─────────────────────────────────────────────────────────────────
# 2. Generar .env
# ─────────────────────────────────────────────────────────────────
info "\n[2/4] Generando archivo .env..."

ENV_PATH="$SCRIPT_DIR/.env"
JWT_REFERENCE='IntroduceClaveLargaergoherofiygkeuidgrf7ieurygf97836trf98egfiuytrf'
JWT_LENGTH=${#JWT_REFERENCE}   # 79

# Orden EXACTO en que aparecerán las variables en .env.
# Coincide con las variables que docker-compose.yml consume
# y con las que el código C# lee de Environment.GetEnvironmentVariable.
#
# IS_MFA_ENABLED se eliminó: el código C# ya no la consulta.
# LOGIN_MODE y AUTH_MODE no se piden al usuario: van con default fijo.
#   - LOGIN_MODE admite: StandardLogin | MfaLogin
#   - AUTH_MODE  admite: Symmetric | AsymmetricFixed | AsymmetricDynamic
ORDER=(
    'DB_HOST'
    'DB_NAME'
    'DB_SA_PASSWORD'
    'DB_SA_USERNAME'
    'DB_SQLUSER'
    'DB_SQLUSER_PASSWORD'
    'CLAVE_JWT'
    'REDIS_CONNECTION_STRING'
    'JWT_ISSUER'
    'JWT_AUDIENCE'
    'PUBLIC_KEY'
    'PRIVATE_KEY'
    'PAYPAL_BASEURL'
    'PAYPAL_CLIENTID'
    'PAYPAL_CLIENTSECRET'
    'PAYPAL_RETURN_URL'
    'PAYPAL_CANCEL_URL'
    'EMAIL_HOST'
    'EMAIL_PORT'
    'EMAIL_USERNAME'
    'EMAIL_PASSWORD'
    'CertificatePassword'
    'LOGIN_MODE'
    'AUTH_MODE'
    'LICENSE_AUTOMAPPER'
    'TELEGRAM_USER'
    'APP_DOCKER_URL'
)

# Defaults en MAYÚSCULAS. Los vacíos ("") se pedirán al usuario.
# Los que tienen valor fijo se imprimen como "(fijo, no editable)".
# LOGIN_MODE y AUTH_MODE se preguntan con opciones válidas; si se
# pulsa ENTER, toman el default.
declare -A DEFAULTS=(
    [DB_HOST]='SQL-Server-Local'
    [DB_NAME]='GestorInventario'
    [DB_SA_PASSWORD]='SQL#1234'
    [DB_SA_USERNAME]='sa'
    [DB_SQLUSER]='sqluser'
    [DB_SQLUSER_PASSWORD]='12345678SQL#1234'
    [CLAVE_JWT]=''
    [REDIS_CONNECTION_STRING]='redis:6379'
    [JWT_ISSUER]='GestorInvetarioEmisor'
    [JWT_AUDIENCE]='GestorInventarioCliente'
    [PUBLIC_KEY]=''
    [PRIVATE_KEY]=''
    [PAYPAL_BASEURL]='https://api-m.sandbox.paypal.com/'
    [PAYPAL_CLIENTID]=''
    [PAYPAL_CLIENTSECRET]=''
    [PAYPAL_RETURN_URL]='https://localhost:8081/Payment/Success'
    [PAYPAL_CANCEL_URL]='https://localhost:8081/Payment/Cancel'
    [EMAIL_HOST]='smtp.gmail.com'
    [EMAIL_PORT]='587'
    [EMAIL_USERNAME]=''
    [EMAIL_PASSWORD]=''
    [CertificatePassword]='0000'
    [LOGIN_MODE]='MfaLogin'
    [AUTH_MODE]='AsymmetricDynamic'
    [LICENSE_AUTOMAPPER]=''
    [TELEGRAM_USER]=''
    [APP_DOCKER_URL]='https://localhost:8081'
)

# Genera una cadena aleatoria criptográficamente segura.
# Equivalente a New-RandomString en PowerShell.
# Usa /dev/urandom si existe (Linux/BSD), o un fallback portable.
new_random_string() {
    local length="$1"
    local chars='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789'
    local out='' i c
    for ((i = 0; i < length; i++)); do
        # 1 byte de urandom → módulo sobre el alfabeto
        if [ -r /dev/urandom ]; then
            c=$(od -An -N1 -tu1 /dev/urandom | tr -d ' ')
        else
            c=$((RANDOM % 256))
        fi
        out+="${chars:$((c % ${#chars})):1}"
    done
    printf '%s' "$out"
}

# Genera un par RSA en formato XML legacy de .NET, el mismo que
# rsa.FromXmlString() espera en C#. El equivalente a ToXmlString()
# no existe nativamente en bash, así que montamos el XML a mano
# siguiendo el esquema exacto que produce .NET Framework:
#   <RSAKeyValue>
#     <Modulus>BASE64</Modulus>
#     <Exponent>BASE64</Exponent>
#   </RSAKeyValue>
# Para la privada se añaden <P>, <Q>, <DP>, <DQ>, <InverseQ>, <D>
# con los componentes CRT (Chinese Remainder Theorem).
#
# Por qué no se usa PEM: el código C# usa RSA.FromXmlString() sobre
# claves XML, no PEM. Mantener el mismo formato evita una migración
# en el lado .NET.
new_rsa_keypair() {
    local bits="${1:-2048}"
    local tmpdir
    tmpdir=$(mktemp -d)
    local priv="$tmpdir/priv.pem"

    # 1. Generar clave privada en PEM.
    # Usamos 'genpkey' (recomendado en OpenSSL 3.x). Fallback a
    # 'genrsa <bits>' sin guión para OpenSSL 1.x antiguos.
    if ! openssl genpkey -algorithm RSA -pkeyopt "rsa_keygen_bits:$bits" -out "$priv" 2>/dev/null; then
        openssl genrsa "$bits" -out "$priv" 2>/dev/null
    fi

    # 2. Volcado textual con todos los componentes. La salida de
    #    'openssl rsa -text' tiene bloques con etiquetas:
    #        modulus:, publicExponent:, privateExponent:,
    #        prime1:, prime2:, exponent1:, exponent2:, coefficient:
    #    seguidos de líneas con bytes en hex delimitados por ':'
    local text
    text=$(openssl rsa -in "$priv" -text -noout 2>/dev/null)

    # Helper: extrae el bloque hex que sigue a una etiqueta como
    # 'modulus:' hasta la siguiente etiqueta. Devuelve hex limpio
    # (sin ':' ni espacios ni saltos).
    extract_field() {
        local label="$1"
        printf '%s\n' "$text" | awk -v L="^${label}:" '
            $0 ~ L { f=1; next }
            f && /^    [0-9a-f]/ {
                # Quitar los 4 espacios iniciales y los ':'
                sub(/^    /, "")
                gsub(/:/, "")
                print
                next
            }
            f && /^[^ ]/ { exit }
        ' | tr -d '\n'
    }

    local modulus_hex exponent_hex
    modulus_hex=$(extract_field "modulus")

    # publicExponent está en una sola línea: 'publicExponent: 65537 (0x10001)'
    # Tomamos la segunda columna en decimal y la pasamos a hex.
    local exp_dec exp_hex_no_pad
    exp_dec=$(printf '%s\n' "$text" | awk '/^publicExponent:/{print $2; exit}')
    exp_hex_no_pad=$(printf '%x' "$exp_dec" 2>/dev/null)
    # .NET quiere el exponente en formato "big-endian" con tamaño
    # estándar; los exponentes RSA son 3 o 5+ bytes. Aseguramos
    # padding a longitud múltiplo de 2.
    if [ "${#exp_hex_no_pad}" = "5" ]; then
        exponent_hex="0${exp_hex_no_pad}"
    else
        exponent_hex="$exp_hex_no_pad"
    fi

    local d_hex p_hex q_hex dp_hex dq_hex invq_hex
    d_hex=$(extract_field "privateExponent")
    p_hex=$(extract_field "prime1")
    q_hex=$(extract_field "prime2")
    dp_hex=$(extract_field "exponent1")
    dq_hex=$(extract_field "exponent2")
    invq_hex=$(extract_field "coefficient")

    # Helper: hex → base64 (sin saltos de línea) para incrustar en XML.
    hex_to_b64() {
        printf '%s' "$1" | xxd -r -p 2>/dev/null | base64 -w 0 2>/dev/null
    }

    # Si modulus está vacío, falló el parser; usamos la clave PEM
    # completa como fallback (no se podrá importar en .NET como XML,
    # pero al menos dejamos un valor no vacío para diagnóstico).
    if [ -z "$modulus_hex" ]; then
        warn "   Aviso: no se pudieron extraer los componentes RSA. .NET no podrá usar estas claves."
        PUBLIC_KEY_XML=""
        PRIVATE_KEY_XML=""
        rm -rf "$tmpdir"
        return 0
    fi

    local mod_b64 exp_b64
    mod_b64=$(hex_to_b64 "$modulus_hex")
    exp_b64=$(hex_to_b64 "$exponent_hex")
    local p_b64 q_b64 d_b64 dp_b64 dq_b64 invq_b64
    p_b64=$(hex_to_b64 "$p_hex")
    q_b64=$(hex_to_b64 "$q_hex")
    d_b64=$(hex_to_b64 "$d_hex")
    dp_b64=$(hex_to_b64 "$dp_hex")
    dq_b64=$(hex_to_b64 "$dq_hex")
    invq_b64=$(hex_to_b64 "$invq_hex")

    # Formato XML legacy de .NET RSA.ToXmlString().
    local public_xml private_xml
    public_xml="<RSAKeyValue><Modulus>${mod_b64}</Modulus><Exponent>${exp_b64}</Exponent></RSAKeyValue>"
    private_xml="<RSAKeyValue><Modulus>${mod_b64}</Modulus><Exponent>${exp_b64}</Exponent><P>${p_b64}</P><Q>${q_b64}</Q><DP>${dp_b64}</DP><DQ>${dq_b64}</DQ><InverseQ>${invq_b64}</InverseQ><D>${d_b64}</D></RSAKeyValue>"

    rm -rf "$tmpdir"

    PUBLIC_KEY_XML="$public_xml"
    PRIVATE_KEY_XML="$private_xml"
}

# Pide un valor SOLO si no hay default. Si hay default, lo imprime como fijo y lo usa tal cual.
# Equivalente a la función Ask del script PowerShell.
ask() {
    local key="$1"
    local prompt="$2"
    local default="${3:-}"

    if [ -n "$default" ]; then
        printf "${C_DARKGRAY}%-55s = %s  (fijo, no editable)${C_RESET}\n" "$prompt" "$default"
        VALUES[$key]="$default"
        return 0
    fi

    local response
    # 'read' puede fallar si no hay TTY o si se redirige /dev/null
    # (devuelve 1 en EOF). Capturamos el fallo y tratamos la respuesta
    # como vacía para que el script no aborte.
    if ! read -r -p "$prompt (sin valor por defecto): " response; then
        response=""
    fi
    if [ -z "$response" ]; then response=""; fi
    VALUES[$key]="$response"
}

# Pide una opción validada de un conjunto. ENTER = default.
# Equivalente a Read-Choice del script PowerShell.
read_choice() {
    local key="$1"
    local prompt="$2"
    shift 2
    local options=("$@")
    local default="${options[-1]}"   # el último argumento no es opción, es default
    unset 'options[-1]'
    local display
    display=$(IFS=' | '; echo "${options[*]}")

    while true; do
        local raw
        # 'read' puede fallar en EOF (sin TTY). Lo tratamos como ENTER.
        if ! read -r -p "$prompt  [$display]  (por defecto: $default): " raw; then
            raw=""
        fi
        local value
        if [ -z "$raw" ]; then
            value="$default"
        else
            value=$(printf '%s' "$raw" | sed 's/^[[:space:]]*//;s/[[:space:]]*$//')
        fi
        for opt in "${options[@]}"; do
            if [ "$value" = "$opt" ]; then
                VALUES[$key]="$value"
                ok "   -> $key = $value"
                return 0
            fi
        done
        warn "   -> '$value' no es válido. Opciones: $display"
    done
}

dim "   Los valores con texto por defecto NO se pueden editar; los vacíos se piden."

declare -A VALUES
for k in "${ORDER[@]}"; do VALUES[$k]="${DEFAULTS[$k]:-}"; done

# [2.1] SQL Server
info "\n   [2.1] Base de datos SQL Server"
ask 'DB_HOST'              'Host de SQL Server'             "${VALUES[DB_HOST]}"
ask 'DB_NAME'              'Nombre de la base de datos'     "${VALUES[DB_NAME]}"
ask 'DB_SA_USERNAME'       'Usuario SA'                     "${VALUES[DB_SA_USERNAME]}"
ask 'DB_SA_PASSWORD'       'Contrasena SA'                  "${VALUES[DB_SA_PASSWORD]}"
ask 'DB_SQLUSER'           'Usuario SQL'                    "${VALUES[DB_SQLUSER]}"
ask 'DB_SQLUSER_PASSWORD'  'Contrasena del usuario SQL'     "${VALUES[DB_SQLUSER_PASSWORD]}"

# [2.2] Seguridad
info "\n   [2.2] Seguridad y autenticacion"
jwt_prompt="Clave JWT (secreto). Si la dejas vacia se generara una automatica de $JWT_LENGTH caracteres"
ask 'CLAVE_JWT' "$jwt_prompt" ""
if [ -z "${VALUES[CLAVE_JWT]}" ]; then
    VALUES[CLAVE_JWT]=$(new_random_string "$JWT_LENGTH")
    ok "   -> Clave JWT generada automaticamente."
fi

# [2.3] PayPal
info "\n   [2.3] PayPal"
dimc "   A continuacion se abrira el portal de PayPal Developers en tu navegador."
dimc "   Inicia sesion, crea una app en sandbox y copia aqui ClientId y ClientSecret."
# Ignoramos el fallo de 'read' en EOF: significa que el script
# se está ejecutando en un entorno no interactivo. Aun así abrimos
# el navegador, que es lo que el usuario espera.
read -r -p "   Pulsa ENTER para abrir la pagina de PayPal" || true
open_browser 'https://developer.paypal.com/home/'
ask 'PAYPAL_CLIENTID'     'PayPal ClientId'     ''
ask 'PAYPAL_CLIENTSECRET' 'PayPal ClientSecret' ''

# [2.4] Email
info "\n   [2.4] Correo electronico (contrasena de aplicacion)"
ask 'EMAIL_USERNAME'     'Email (remitente)'   ''
if [ -n "${VALUES[EMAIL_USERNAME]}" ]; then
    dimc "   A continuacion se abrira la pagina de contrasenas de aplicacion de Google."
    dimc "   Genera una contrasena para 'Correo' (o 'Mail') y pegala aqui."
    read -r -p "   Pulsa ENTER para abrir la pagina de Google" || true
    open_browser 'https://myaccount.google.com/apppasswords'
fi
ask 'EMAIL_PASSWORD'     'Contrasena de aplicacion de Google' ''

# [2.5] Licencia AutoMapper
info "\n   [2.5] Licencia de AutoMapper (LuckyPennySoftware)"
dimc "   A continuacion se abrira la pagina de LuckyPennySoftware."
dimc "   Elige el plan GRATUITO, genera tu clave de licencia y pegala aqui abajo."
read -r -p "   Pulsa ENTER para abrir la pagina de LuckyPennySoftware" || true
open_browser 'https://luckypennysoftware.com/'
ask 'LICENSE_AUTOMAPPER' 'Licencia AutoMapper (pega aqui la clave)' ''

# [2.6] Otros
info "\n   [2.6] Otros"
ask 'CertificatePassword'  'Contrasena del certificado'        "${VALUES[CertificatePassword]}"
ask 'TELEGRAM_USER'        'Usuario CallMeBot (Telegram)'      ''

# [2.7] Modo de aplicación (login y autenticación JWT)
# Estos dos los lee el código C# desde .env, así que se preguntan
# aquí para que el usuario elija y se vuelquen en el archivo.
#   - LOGIN_MODE: StandardLogin | MfaLogin
#   - AUTH_MODE : Symmetric | AsymmetricFixed | AsymmetricDynamic
info "\n   [2.7] Modo de aplicacion"
read_choice 'LOGIN_MODE' 'Modo de login'                'StandardLogin' 'MfaLogin'                   "${VALUES[LOGIN_MODE]}"
read_choice 'AUTH_MODE'  'Modo de autenticacion JWT'    'Symmetric' 'AsymmetricFixed' 'AsymmetricDynamic' "${VALUES[AUTH_MODE]}"

# Generar claves RSA en XML (.NET legacy: <Modulus><Exponent>...</Exponent></Modulus>
# para la pública y +P, Q, DP, DQ, InverseQ, D para la privada)
info "\n   Generando par de claves RSA (2048 bits)..."
new_rsa_keypair 2048
VALUES[PUBLIC_KEY]="$PUBLIC_KEY_XML"
VALUES[PRIVATE_KEY]="$PRIVATE_KEY_XML"
ok "   Claves RSA generadas correctamente."

# Volcar .env. Si un valor contiene saltos de línea, lo entrecomillamos.
{
    for k in "${ORDER[@]}"; do
        v="${VALUES[$k]}"
        if printf '%s' "$v" | grep -q $'\n'; then
            # Valor multilínea: entrecomillar.
            # Las comillas dobles internas se escapan con barra invertida.
            esc=$(printf '%s' "$v" | sed 's/"/\\"/g')
            printf '%s="%s"\n' "$k" "$esc"
        else
            printf '%s=%s\n' "$k" "$v"
        fi
    done
} > "$ENV_PATH"
ok "\n   -> .env generado en: $ENV_PATH"

# ─────────────────────────────────────────────────────────────────
# 3. Detectar Docker
# ─────────────────────────────────────────────────────────────────
info "\n[3/4] Comprobando Docker..."

docker_installed() { command -v docker >/dev/null 2>&1; }

# docker_with_ctx: ejecuta un comando docker usando el contexto activo
# del usuario. Si el script corre con sudo, preserva HOME y DOCKER_*
# para que docker caiga sobre el MISMO motor que ve el usuario en
# Docker Desktop (no sobre /var/run/docker.sock).
docker_with_ctx() {
    # Cuando NO estamos como root, no hay sudo: ejecucion directa.
    if [ "$(id -u)" -ne 0 ]; then
        docker "$@"
        return $?
    fi

    # Estamos como root. Si hay SUDO_USER y su contexto es desktop-linux,
    # re-leemos su ~/.docker/config.json para mantener coherencia.
    local ctx="$DOCKER_ACTIVE_CONTEXT"
    if [ -n "${SUDO_USER:-}" ] && [ "$ctx" = "desktop-linux" ]; then
        ctx=$(sudo -u "$SUDO_USER" -H bash -c 'docker context show 2>/dev/null' 2>/dev/null || echo "desktop-linux")
    fi

    if [ -n "$ctx" ] && [ "$ctx" != "default" ]; then
        # Bajamos al usuario original con -E (preserva PATH, HOME,
        # DOCKER_CONFIG) y fijamos DOCKER_CONTEXT explicitamente.
        sudo -E -u "${SUDO_USER:-root}" env DOCKER_CONTEXT="$ctx" docker "$@"
    else
        docker "$@"
    fi
}

docker_running() {
    if ! docker_installed; then return 1; fi
    if docker_with_ctx info >/dev/null 2>&1; then return 0; else return 1; fi
}

if ! docker_installed; then
    warn "  -> Docker NO esta instalado en este equipo."
    printf "\n"
    dimc "     Distribucion detectada: $DISTRO_PRETTY"
    dimc "     Instala Docker segun tu distro. Comandos habituales:"
    printf "\n"
    case "$DISTRO_ID" in
        ubuntu|debian|pop|linuxmint|elementary|zorin|kali|raspbian)
            dimc "       Debian/Ubuntu y derivados:"
            dimc "         sudo apt-get update"
            dimc "         sudo apt-get install -y ca-certificates curl gnupg"
            dimc "         sudo install -m 0755 -d /etc/apt/keyrings"
            dimc "         curl -fsSL https://download.docker.com/linux/$DISTRO_ID/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg"
            dimc "         echo \"deb [arch=\$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/$DISTRO_ID \$(. /etc/os-release && echo \"\$VERSION_CODENAME\") stable\" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null"
            dimc "         sudo apt-get update"
            dimc "         sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin"
            dimc "         sudo usermod -aG docker \$USER   # para no usar sudo cada vez"
            ;;
        fedora|rhel|centos|rocky|almalinux|ol)
            dimc "       Fedora/RHEL y derivados:"
            dimc "         sudo dnf -y install dnf-plugins-core"
            dimc "         sudo dnf config-manager --add-repo https://download.docker.com/linux/fedora/docker-ce.repo"
            dimc "         sudo dnf -y install docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin"
            dimc "         sudo systemctl enable --now docker"
            dimc "         sudo usermod -aG docker \$USER"
            ;;
        arch|manjaro|endeavouros|garuda)
            dimc "       Arch/Manjaro y derivados:"
            dimc "         sudo pacman -Syu --noconfirm docker docker-compose"
            dimc "         sudo systemctl enable --now docker.service"
            dimc "         sudo usermod -aG docker \$USER"
            ;;
        opensuse*|sles)
            dimc "       openSUSE/SLES:"
            dimc "         sudo zypper addrepo https://download.docker.com/linux/opensuse/docker-ce.repo"
            dimc "         sudo zypper refresh"
            dimc "         sudo zypper install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin"
            dimc "         sudo systemctl enable --now docker"
            dimc "         sudo usermod -aG docker \$USER"
            ;;
        *)
            dimc "       Distribucion no reconocida ($DISTRO_ID)."
            dimc "       Consulta la guia oficial: https://docs.docker.com/engine/install/"
            dimc "       Cualquier distro moderna puede usar el script oficial:"
            dimc "         curl -fsSL https://get.docker.com -o get-docker.sh && sudo sh get-docker.sh"
            ;;
    esac
    printf "\n"
    dimc "     Tras instalarlo, vuelve a ejecutar este script."
    dimc "     El archivo .env ya esta generado y listo para usar."
    exit 0
fi

ok "  -> Docker esta instalado."

# ─────────────────────────────────────────────────────────────────
# 4. Arrancar Docker si está parado y desplegar
# ─────────────────────────────────────────────────────────────────
info "\n[4/4] Desplegando con docker compose..."

if ! docker_running; then
    warn "  -> Docker no esta en ejecucion. Intentando arrancar el servicio..."

    # En Linux, Docker normalmente corre como servicio systemd.
    # Si estamos en un sistema con systemctl y permisos, intentamos
    # arrancarlo. Si no, avisamos al usuario.
    STARTED=0
    if command -v systemctl >/dev/null 2>&1; then
        if sudo systemctl start docker 2>/dev/null; then
            dimc "  -> Servicio 'docker' arrancado con systemctl."
            STARTED=1
        fi
    fi

    # Alternativa: servicio OpenRC (Gentoo, Alpine, algunos derivados).
    if [ "$STARTED" -eq 0 ] && command -v rc-service >/dev/null 2>&1; then
        if sudo rc-service docker start 2>/dev/null; then
            dimc "  -> Servicio 'docker' arrancado con rc-service."
            STARTED=1
        fi
    fi

    # Alternativa: comando 'service' clásico (sysvinit).
    if [ "$STARTED" -eq 0 ] && command -v service >/dev/null 2>&1; then
        if sudo service docker start 2>/dev/null; then
            dimc "  -> Servicio 'docker' arrancado con service."
            STARTED=1
        fi
    fi

    if [ "$STARTED" -eq 0 ]; then
        warn "  -> No se pudo arrancar Docker automáticamente. Hazlo manualmente:"
        dimc "       sudo systemctl start docker    # systemd"
        dimc "       sudo rc-service docker start  # OpenRC"
        dimc "       sudo service docker start     # sysvinit"
    fi

    # Esperar hasta 90 segundos a que Docker responda
    DEADLINE=$((SECONDS + 90))
    READY=0
    while [ "$SECONDS" -lt "$DEADLINE" ]; do
        if docker_running; then
            READY=1
            break
        fi
        dim "  ... esperando a que Docker responda"
        sleep 5
    done

    if [ "$READY" -eq 0 ]; then
        warn "  -> Docker no arranco a tiempo. Vuelve a ejecutar este script cuando este listo."
        exit 0
    fi
fi

ok "  -> Docker en ejecucion. Construyendo y levantando contenedores..."

# Ejecutar docker compose desde la carpeta del script
# Equivalente a Push-Location/Pop-Location en PowerShell.
cd "$SCRIPT_DIR"
# Usamos docker_with_ctx para que, si se ejecuta con sudo y el contexto
# activo es 'desktop-linux', los contenedores se creen en Docker Desktop
# (mismo motor que ve el usuario en la GUI), no en /var/run/docker.sock.
if docker_with_ctx compose up -d --build; then
    ok "\n  -> Despliegue completado."
    printf "\n"
    docker_with_ctx compose ps
else
    rc=$?
    warn "\n  -> 'docker compose' finalizo con errores (codigo $rc). Revisa los logs."
fi

if [ "$(id -u)" -eq 0 ] && [ "$DOCKER_HAS_DESKTOP" -eq 1 ]; then
    dimc "\n  -> Los contenedores se han creado con el contexto 'desktop-linux'."
    dimc "     Deberian aparecer ya en tu Docker Desktop."
    if [ -n "${SUDO_USER:-}" ]; then
        dimc "     Si no los ves, abre Docker Desktop con tu usuario normal:"
        dimc "       sudo -u $SUDO_USER -H docker desktop start"
    fi
fi

info "\n=== Instalador finalizado ==="
