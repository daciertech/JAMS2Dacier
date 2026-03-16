# Extract-Definitions.ps1
# Recursively exports each item in a directory tree using Export-JAMSXML

param (
    [Parameter(Mandatory=$true)]
    [string]$RootPath,

    [Parameter(Mandatory=$false)]
    [string]$ExportPath = "$RootPath\Exports"
)

# Ensure Export Path exists
if (-not (Test-Path $ExportPath)) {
    New-Item -Path $ExportPath -ItemType Directory | Out-Null
}

# Recursively process each item
Get-ChildItem -Path $RootPath -Recurse | ForEach-Object {
    $item = Get-Item $_.FullName
    # Compute relative path from RootPath
    $relativePath = $item.FullName.Substring($RootPath.Length).TrimStart('\')
    $exportDir = Join-Path $ExportPath (Split-Path $relativePath -Parent)
    # Ensure export directory exists
    if (-not (Test-Path $exportDir)) {
        New-Item -Path $exportDir -ItemType Directory -Force | Out-Null
    }
    $exportFile = Join-Path $exportDir ($item.Name + ".xml")
    Export-JAMSXML -InputObject $item -Path $exportFile
}