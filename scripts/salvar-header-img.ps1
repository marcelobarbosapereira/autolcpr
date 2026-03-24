param(
    [string]$ConfigPath = "nfe_config.json",
    [string]$OutputPath = "src/AutoLCPR.Application/Relatorios/header.img"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-AbsolutePath {
    param([string]$PathValue)

    if ([System.IO.Path]::IsPathRooted($PathValue)) {
        return $PathValue
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $PathValue))
}

try {
    $configAbsolutePath = Resolve-AbsolutePath -PathValue $ConfigPath

    if (-not (Test-Path -LiteralPath $configAbsolutePath -PathType Leaf)) {
        throw "Arquivo de configuracao nao encontrado: $configAbsolutePath"
    }

    $configRaw = Get-Content -LiteralPath $configAbsolutePath -Raw
    $config = $configRaw | ConvertFrom-Json

    $imagemCabecalho = $config.imagemCabecalho

    if ([string]::IsNullOrWhiteSpace($imagemCabecalho)) {
        throw "A configuracao 'imagemCabecalho' esta vazia no arquivo: $configAbsolutePath"
    }

    $imagemOrigem = Resolve-AbsolutePath -PathValue $imagemCabecalho

    if (-not (Test-Path -LiteralPath $imagemOrigem -PathType Leaf)) {
        throw "Arquivo de imagem nao encontrado: $imagemOrigem"
    }

    $outputAbsolutePath = Resolve-AbsolutePath -PathValue $OutputPath
    $outputDirectory = Split-Path -Path $outputAbsolutePath -Parent

    if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }

    Copy-Item -LiteralPath $imagemOrigem -Destination $outputAbsolutePath -Force

    Write-Host "Imagem copiada com sucesso para: $outputAbsolutePath"
    exit 0
}
catch {
    Write-Error $_
    exit 1
}
