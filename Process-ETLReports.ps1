# Processes all ETL files in a folder with a single processor flag
# 2024 Chad Schultz

param (
    [Parameter(Mandatory=$true)][System.IO.DirectoryInfo] $ETLReportsPath,
    [Parameter(Mandatory=$true)][System.IO.DirectoryInfo] $ETLFilespath,
    [Parameter(Mandatory=$true)][ValidateSet("processes","tasks","gpos","winlogon","pnp","services","hardfaults","diskio","fileio","providerinfo","minifilter","minifiltersummary","cpusample","cpusamplenoidle","bootphases","processzombies")][string] $Processor,
    [Parameter(Mandatory=$true)][System.IO.DirectoryInfo] $OutputFolder
)

Write-Verbose "ETLReports path: $ETLReportsPath"

if (Test-Path -Path $ETLFilespath -PathType Container) {
    Write-Verbose "Path $ETLFilespath good."
    
    $etlfiles = (Get-ChildItem -Path $ETLFilespath | Where-Object {$_.Extension.ToLower() -eq ".etl"})
    $count_etlfiles = $etlfiles.Count

    Write-Verbose "Path $ETLFilespath has $count_etlfiles .etl files."

    Pause

    foreach($file in $etlfiles) {
        $count += 1
        $filename = $file.FullName
        Write-Verbose "Processing file #$count - $filename to $OutputFolder"
        $procargs = "--infile:`"$filename`" --processor:$Processor --outfolder:`"$OutputFolder`""
        Write-Verbose $procargs
        Write-Verbose ""
        Start-Process -FilePath $ETLReportsPath -ArgumentList $procargs -Wait
    }

}

# Remove very small datasets
$etlfiles = (Get-ChildItem -Path $OutputFolder | Where-Object { ($_.Extension.ToLower() -eq ".csv" -and $_.Length -lt 16) }) | Remove-Item
