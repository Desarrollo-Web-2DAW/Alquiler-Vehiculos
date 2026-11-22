$ErrorActionPreference = "Stop"

# Ejecutar desde cualquier carpeta: solo combina informes de ESTA ejecucion.
$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "Alquiler-Vehiculos/Alquiler-Vehiculos.slnx"
$settings = Join-Path $root "Alquiler-Vehiculos/coverage.runsettings"
$tests = Join-Path $root "Alquiler-Vehiculos/Alquiler-Vehiculos.Tests"
$run = (Get-Date -Format "yyyyMMdd-HHmmss") + "-" + [Guid]::NewGuid().ToString("N")
$results = Join-Path $tests "TestResults/$run"
$report = Join-Path $tests "coveragereport"

Get-Command dotnet, docker, reportgenerator -ErrorAction Stop | Out-Null
docker info --format '{{.ServerVersion}}'
if ($LASTEXITCODE -ne 0) { throw "Docker no esta disponible. Arranca Docker Desktop." }

$previous = $env:ALQUILER_TEST_RESULTS_DIRECTORY
try {
    $env:ALQUILER_TEST_RESULTS_DIRECTORY = $results
    dotnet test $solution --settings $settings --collect:"XPlat Code Coverage" --results-directory $results
    if ($LASTEXITCODE -ne 0) { throw "Han fallado las pruebas. Resultados: $results" }

    reportgenerator "-reports:$results/**/coverage.cobertura.xml" "-targetdir:$report" '-reporttypes:Html;XmlSummary;TextSummary'
    if ($LASTEXITCODE -ne 0) { throw "No se ha podido generar el informe de cobertura." }
    Write-Host "Informe: $report/index.html"
    Write-Host "Resultados de esta ejecucion: $results"
} finally {
    $env:ALQUILER_TEST_RESULTS_DIRECTORY = $previous
}
