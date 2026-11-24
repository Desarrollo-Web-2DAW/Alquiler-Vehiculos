param(
    [string[]]$Carpetas = @("4 - VEHICULOS", "5 - MARCAS", "6 - CONCESIONARIOS", "7 - HISTORICO CATALOGO"),
    [switch]$MatrizPermisos
)

$ErrorActionPreference = "Stop"
$raiz = Split-Path $PSScriptRoot -Parent
$proyecto = "alquiler-e2e"
$nombreInforme = "$proyecto-" + (Get-Date -Format "yyyyMMdd-HHmmss-fff")
$compose = @("compose", "--project-directory", $raiz, "-f", (Join-Path $raiz "compose.e2e.yaml"), "-p", $proyecto)
# Los nombres son fijos: impedir ejecuciones simultáneas o eliminar un entorno previo.
$contenedoresPrevios = @(docker ps -aq --filter "label=com.docker.compose.project=$proyecto")
if ($LASTEXITCODE -ne 0) { throw "Docker no está disponible." }
if ($contenedoresPrevios.Count -gt 0) {
    throw "Ya existe un entorno alquiler-e2e. Revísalo en Docker Desktop antes de iniciar otro; no se ha modificado."
}
$passwordAnterior = $env:E2E_PASSWORD
$jwtAnterior = $env:E2E_JWT
$env:E2E_PASSWORD = [Guid]::NewGuid().ToString("N") + "aA1!"
$env:E2E_JWT = [Guid]::NewGuid().ToString("N") + [Guid]::NewGuid().ToString("N")

try {
    docker info --format '{{.ServerVersion}}'
    if ($LASTEXITCODE -ne 0) { throw "Docker no está disponible." }

    New-Item -ItemType Directory -Force (Join-Path $raiz "TestResults/bruno") | Out-Null
    docker @compose build bruno
    if ($LASTEXITCODE -ne 0) { throw "No se pudo preparar Bruno CLI." }

    docker @compose run --rm --no-deps --name "$proyecto-bruno" bruno --version
    if ($LASTEXITCODE -ne 0) { throw "No se pudo consultar la versión de Bruno." }
    docker @compose run --rm --no-deps --name "$proyecto-bruno" bruno run --help
    if ($LASTEXITCODE -ne 0) { throw "No se pudo consultar la ayuda de Bruno." }

    docker @compose up --build --wait --wait-timeout 180 api
    if ($LASTEXITCODE -ne 0) {
        docker @compose logs --tail 80 api
        throw "La API de pruebas no llegó a estar disponible."
    }

    $comunes = @("--env", "local", "--sandbox", "developer", "--bail",
        "--env-var", "baseUrl=http://api:8080",
        "--reporter-skip-all-headers", "--reporter-skip-body")
    foreach ($perfil in @("admin", "cliente", "trabajador", "responsable", "mecanico")) {
        $comunes += @("--env-var", "${perfil}Password=$env:E2E_PASSWORD")
    }

    $recorridos = @(@{ Nombre = "catalogo"; Argumentos = @("run") + $Carpetas })
    if ($MatrizPermisos) {
        foreach ($perfil in @("anonimo", "cliente", "trabajador", "responsable", "mecanico")) {
            $recorridos += @{ Nombre = "permisos-$perfil"; Argumentos = @("run", "8 - PERMISOS CATALOGO", "--env-var", "perfilMatriz=$perfil") }
        }
    }

    foreach ($recorrido in $recorridos) {
        $archivoInforme = "$nombreInforme-$($recorrido.Nombre).xml"
        $argumentos = $recorrido.Argumentos + $comunes + @("--reporter-junit", "/reports/$archivoInforme")
        docker @compose run --rm --no-deps --name "$proyecto-bruno" bruno @argumentos
        if ($LASTEXITCODE -ne 0) { throw "Bruno ha detectado fallos. Revisa TestResults/bruno/$archivoInforme." }
        $informe = Join-Path $raiz "TestResults/bruno/$archivoInforme"
        [xml]$junit = Get-Content -LiteralPath $informe -Raw
        $casos = @($junit.SelectNodes("//testcase"))
        $fallos = @($junit.SelectNodes("//failure | //error"))
        if ($casos.Count -eq 0 -or $fallos.Count -gt 0) {
            throw "El informe debe contener tests ejecutados y ningún fallo. Casos: $($casos.Count); fallos: $($fallos.Count)."
        }
        Write-Host "Tests registrados en JUnit: $($casos.Count); fallos: $($fallos.Count)."
        Write-Host "Bruno completado. Informe: TestResults/bruno/$archivoInforme"
    }
} finally {
    # Solo el proyecto de esta ejecución. Sus bases y subidas son efímeras.
    docker @compose down --remove-orphans
    $env:E2E_PASSWORD = $passwordAnterior
    $env:E2E_JWT = $jwtAnterior
}
