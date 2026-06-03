# Extract-Definitions.ps1
# Recursively exports each Job and Folder in a directory tree using Export-JAMSXml

param (
    [Parameter(Mandatory=$true)]
    [string]$ServerName,

    [Parameter(Mandatory=$true)]
    [string]$ExportPath
)

try {

    Write-Host "Connecting to JAMS Server: $ServerName"
    Write-Host "Exporting definitions to: $ExportPath"

    Import-Module JAMS

    # Ensure Export Path exists
    if (-not (Test-Path $ExportPath)) {
        New-Item -Path $ExportPath -ItemType Directory | Out-Null
    }

    New-PSDrive -Name JD -PSProvider JAMS -Root $ServerName -ErrorAction Stop | Out-Null

    # Recursively process each item
    Get-ChildItem -Path JD:\ -Recurse | Where-Object { $_ -is [MVPSI.JAMS.Job] -or $_ -is [MVPSI.JAMS.Folder] } | ForEach-Object {
        $item = Get-Item $_.PSPath
        if ($item -ne $null -and $item.QualifiedName -ne $null) {
            # Compute relative path from RootPath
            $relativePath = $item.QualifiedName.TrimStart('\')
            $exportDir = Join-Path $ExportPath (Split-Path $relativePath -Parent)
            # Ensure export directory exists
            if (-not (Test-Path $exportDir)) {
                New-Item -Path $exportDir -ItemType Directory -Force | Out-Null
            }
            $exportFile = Join-Path $exportDir ($item.Name + ".xml")
            Export-JAMSXML -InputObject $item -Path $exportFile
        }
    }
}
catch {
    Write-Error "An error occurred: $_"
    Write-Error "At: $($_.InvocationInfo.PositionMessage)"

    Exit 1
}
finally {
    # Clean up PSDrive
    Remove-PSDrive -Name JD -ErrorAction SilentlyContinue
}
