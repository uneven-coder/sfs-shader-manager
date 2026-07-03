<#
.SYNOPSIS
  Reads the live shaders.Config.txt (the game's saved shader overrides) and hardcodes those
  values into the Atmo/Clouds shader source's [ShaderArg] defaults and CreateDefaultWorldProfile/
  CreateDefaultScaledProfile() literals, so they become the new baked-in defaults.

.NOTES
  Only touches fields actually present in the config file's per-shader override map. Fields not
  present (never edited from the UI default) are left untouched in the source.
#>
param(
    [string]$ConfigPath = "C:\Program Files (x86)\Steam\steamapps\common\Spaceflight Simulator\Spaceflight Simulator Game\Mods\sfs-shaders\shaders.Config.txt",
    [string]$AtmoShaderCs = (Join-Path $PSScriptRoot "..\Shaders\assets\Atmo\AtmoShader.cs")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ConfigPath)) {
    Write-Error "Config file not found: $ConfigPath"
    exit 1
}
if (-not (Test-Path $AtmoShaderCs)) {
    Write-Error "AtmoShader.cs not found: $AtmoShaderCs"
    exit 1
}

$config = Get-Content $ConfigPath -Raw | ConvertFrom-Json
$source = Get-Content $AtmoShaderCs -Raw
$changed = 0

function Format-Literal {
    param($Value, [string]$Existing)

    if ($Existing -eq 'true' -or $Existing -eq 'false') {
        return (([bool]$Value).ToString().ToLowerInvariant())
    }

    if ($Existing -match '^[-+]?[0-9]+$') {
        # Existing literal has no decimal point / 'f' suffix -> treat as an int field.
        return [string][int64]$Value
    }

    $numText = ([double]$Value).ToString([System.Globalization.CultureInfo]::InvariantCulture)
    if ($Existing.EndsWith('f')) { return "${numText}f" }
    return $numText
}

# --- Top-level scalar fields (e.g. AtmoShader's RayleighStrength, MieStrength, ...) ---
# These live as `[ShaderArg(..., defaultValue: X)] public T FieldName;` on a single line.
$atmoShaderOverrides = $config.Shaders.AtmoShader
if ($atmoShaderOverrides) {
    $lines = $source -split "`r?`n"
    foreach ($prop in $atmoShaderOverrides.PSObject.Properties) {
        $fieldPath = $prop.Name
        if ($fieldPath -match '\.') { continue } # nested paths belong to CloudsShader below

        $fieldRegex = "public\s+\S+\s+$([regex]::Escape($fieldPath))\s*;"
        $matchedLine = $false
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -notmatch $fieldRegex) { continue }
            if ($lines[$i] -notmatch 'defaultValue:\s*([^,)\n]+)') {
                Write-Warning "Field '$fieldPath' has no [ShaderArg] defaultValue on its line; skipped."
                continue
            }

            $existing = $Matches[1].Trim()
            $newLiteral = Format-Literal -Value $prop.Value -Existing $existing
            $lines[$i] = $lines[$i] -replace 'defaultValue:\s*[^,)\n]+', "defaultValue: $newLiteral"
            $matchedLine = $true
            $changed++
            Write-Host "AtmoShader.$fieldPath -> $newLiteral"
        }
        if (-not $matchedLine) {
            Write-Warning "Field '$fieldPath' not found in AtmoShader.cs; skipped."
        }
    }
    $source = $lines -join "`n"
}

# --- CloudsShader per-tab fields (World.CloudAlpha, Scaled.CloudType, ...) ---
# These live inside CreateDefaultWorldProfile()/CreateDefaultScaledProfile() struct literals.
$cloudsShaderOverrides = $config.Shaders.CloudsShader
if ($cloudsShaderOverrides) {
    foreach ($tab in @('World', 'Scaled')) {
        $methodName = if ($tab -eq 'World') { 'CreateDefaultWorldProfile' } else { 'CreateDefaultScaledProfile' }
        $blockMatch = [regex]::Match($source, "private static CameraProfile $methodName\(\).*?\};", [System.Text.RegularExpressions.RegexOptions]::Singleline)
        if (-not $blockMatch.Success) {
            Write-Warning "Could not locate $methodName() in AtmoShader.cs; skipped its fields."
            continue
        }

        $block = $blockMatch.Value
        $blockLines = $block -split "`r?`n"
        foreach ($prop in $cloudsShaderOverrides.PSObject.Properties) {
            if (-not $prop.Name.StartsWith("$tab.")) { continue }
            $field = $prop.Name.Substring($tab.Length + 1)

            $fieldRegex = "^\s*$([regex]::Escape($field))\s*=\s*(.+?)\s*,?\s*$"
            $matchedLine = $false
            for ($i = 0; $i -lt $blockLines.Count; $i++) {
                if ($blockLines[$i] -notmatch $fieldRegex) { continue }
                $existing = $Matches[1].Trim()
                $newLiteral = Format-Literal -Value $prop.Value -Existing $existing
                $blockLines[$i] = $blockLines[$i] -replace [regex]::Escape($existing), $newLiteral
                $matchedLine = $true
                $changed++
                Write-Host "CloudsShader.$($prop.Name) -> $newLiteral"
                break
            }
            if (-not $matchedLine) {
                Write-Warning "Field '$($prop.Name)' not found in $methodName(); skipped."
            }
        }

        $newBlock = $blockLines -join "`n"
        $source = $source.Substring(0, $blockMatch.Index) + $newBlock + $source.Substring($blockMatch.Index + $blockMatch.Length)
    }
}

if ($changed -eq 0) {
    Write-Host "No overrides found in config to apply; AtmoShader.cs left unchanged."
    exit 0
}

Set-Content -Path $AtmoShaderCs -Value $source -NoNewline
Write-Host "`nApplied $changed field(s) from '$ConfigPath' into '$AtmoShaderCs'."
