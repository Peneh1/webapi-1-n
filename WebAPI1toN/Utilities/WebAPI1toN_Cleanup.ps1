# .\Cleanup.ps1

# Set these variables, as they define the data to keep and from where
Set-Variable -Name "HoursToKeep" -Value "-4"
Set-Variable -Name "fileRemovalPath" -Value "C:\WebAPI1toN-Deploy\Data"


# Relatively simple job, of triming any files out of folder, that are older then $HoursToKeep
$CurrentDateTime = Get-Date
$MinimumDateTimeToKeep = $CurrentDateTime.AddHours($HoursToKeep)

Write-Host "CurrentDateTime : [ $CurrentDateTime ]"
Write-Host "MinimumDateTimeToKeep : [ $MinimumDateTimeToKeep ]"

Write-Host "FileRemovalPath: [$fileRemovalPath] "

Get-ChildItem -Path $fileRemovalPath | Where-Object {$_.LastWriteTime -lt (Get-Date).AddHours($HoursToKeep)} | Remove-Item -Recurse -ErrorAction SilentlyContinue -Force



